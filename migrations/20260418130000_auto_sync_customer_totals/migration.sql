-- Auto-sync denormalized customer totals on every relevant write path.
-- Rules:
-- - total_weight: SUM(warehouse.net_weight) for warehouse.status IN (CHO_GIAO, DA_GIAO)
-- - total_amount: SUM(order_links.final_price_vnd from eligible warehouses) + SUM(order.price_ship per eligible order)
-- - total_orders: COUNT(DISTINCT orders) where orders.status != DA_HUY

DROP TRIGGER IF EXISTS trg_orders_customer_totals_sync ON orders;
DROP TRIGGER IF EXISTS trg_warehouse_customer_totals_sync ON warehouse;
DROP TRIGGER IF EXISTS trg_order_links_customer_totals_sync ON order_links;

DROP FUNCTION IF EXISTS public.trg_order_links_customer_totals_sync_fn();
DROP FUNCTION IF EXISTS public.trg_warehouse_customer_totals_sync_fn();
DROP FUNCTION IF EXISTS public.trg_orders_customer_totals_sync_fn();
DROP FUNCTION IF EXISTS public.fn_recompute_customer_totals_by_warehouse(BIGINT);
DROP FUNCTION IF EXISTS public.fn_recompute_customer_totals_by_order(BIGINT);
DROP FUNCTION IF EXISTS public.fn_recompute_customer_totals(BIGINT);

CREATE OR REPLACE FUNCTION public.fn_recompute_customer_totals(p_customer_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
  IF p_customer_id IS NULL THEN
    RETURN;
  END IF;

  WITH eligible_warehouses AS (
    SELECT
      w.warehouse_id,
      w.order_id,
      COALESCE(w.net_weight, 0)::double precision AS package_weight
    FROM warehouse w
    JOIN orders o ON o.order_id = w.order_id
    WHERE o.customer_id = p_customer_id
      AND o.status IS DISTINCT FROM 'DA_HUY'::"OrderMainStatus"
      AND w.status IN (
        'CHO_GIAO'::"WarehouseStatus",
        'DA_GIAO'::"WarehouseStatus"
      )
  ),
  weight_agg AS (
    SELECT COALESCE(SUM(ew.package_weight), 0)::double precision AS total_weight
    FROM eligible_warehouses ew
  ),
  goods_agg AS (
    SELECT COALESCE(SUM(COALESCE(ol.final_price_vnd, 0)), 0)::numeric(38, 2) AS goods_amount
    FROM eligible_warehouses ew
    LEFT JOIN order_links ol ON ol.warehouse_id = ew.warehouse_id
  ),
  ship_agg AS (
    SELECT COALESCE(SUM(x.price_ship), 0)::numeric(38, 2) AS ship_amount
    FROM (
      SELECT DISTINCT
        o.order_id,
        COALESCE(o.price_ship, 0)::numeric(38, 2) AS price_ship
      FROM eligible_warehouses ew
      JOIN orders o ON o.order_id = ew.order_id
    ) x
  ),
  amount_agg AS (
    SELECT
      (COALESCE(sa.ship_amount, 0)::numeric(38, 2) +
       COALESCE(ga.goods_amount, 0)::numeric(38, 2))::numeric(38, 2) AS total_amount
    FROM ship_agg sa
    CROSS JOIN goods_agg ga
  ),
  order_agg AS (
    SELECT COUNT(DISTINCT o.order_id)::int AS total_orders
    FROM orders o
    WHERE o.customer_id = p_customer_id
      AND o.status IS DISTINCT FROM 'DA_HUY'::"OrderMainStatus"
  )
  UPDATE customer c
  SET
    total_weight = COALESCE((SELECT total_weight FROM weight_agg), 0),
    total_amount = COALESCE((SELECT total_amount FROM amount_agg), 0),
    total_orders = COALESCE((SELECT total_orders FROM order_agg), 0)
  WHERE c.account_id = p_customer_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_recompute_customer_totals_by_order(p_order_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
  v_customer_id BIGINT;
BEGIN
  IF p_order_id IS NULL THEN
    RETURN;
  END IF;

  SELECT o.customer_id
  INTO v_customer_id
  FROM orders o
  WHERE o.order_id = p_order_id;

  PERFORM public.fn_recompute_customer_totals(v_customer_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_recompute_customer_totals_by_warehouse(p_warehouse_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
  v_order_id BIGINT;
BEGIN
  IF p_warehouse_id IS NULL THEN
    RETURN;
  END IF;

  SELECT w.order_id
  INTO v_order_id
  FROM warehouse w
  WHERE w.warehouse_id = p_warehouse_id;

  PERFORM public.fn_recompute_customer_totals_by_order(v_order_id);
END;
$$;

CREATE OR REPLACE FUNCTION public.trg_orders_customer_totals_sync_fn()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    PERFORM public.fn_recompute_customer_totals(NEW.customer_id);
  ELSIF TG_OP = 'UPDATE' THEN
    IF NEW.customer_id IS DISTINCT FROM OLD.customer_id THEN
      PERFORM public.fn_recompute_customer_totals(OLD.customer_id);
    END IF;
    PERFORM public.fn_recompute_customer_totals(NEW.customer_id);
  ELSIF TG_OP = 'DELETE' THEN
    PERFORM public.fn_recompute_customer_totals(OLD.customer_id);
  END IF;

  RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.trg_warehouse_customer_totals_sync_fn()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    PERFORM public.fn_recompute_customer_totals_by_order(NEW.order_id);
  ELSIF TG_OP = 'UPDATE' THEN
    IF NEW.order_id IS DISTINCT FROM OLD.order_id THEN
      PERFORM public.fn_recompute_customer_totals_by_order(OLD.order_id);
    END IF;
    PERFORM public.fn_recompute_customer_totals_by_order(NEW.order_id);
  ELSIF TG_OP = 'DELETE' THEN
    PERFORM public.fn_recompute_customer_totals_by_order(OLD.order_id);
  END IF;

  RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.trg_order_links_customer_totals_sync_fn()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    PERFORM public.fn_recompute_customer_totals_by_warehouse(NEW.warehouse_id);
  ELSIF TG_OP = 'UPDATE' THEN
    IF NEW.warehouse_id IS DISTINCT FROM OLD.warehouse_id THEN
      PERFORM public.fn_recompute_customer_totals_by_warehouse(OLD.warehouse_id);
    END IF;
    PERFORM public.fn_recompute_customer_totals_by_warehouse(NEW.warehouse_id);
  ELSIF TG_OP = 'DELETE' THEN
    PERFORM public.fn_recompute_customer_totals_by_warehouse(OLD.warehouse_id);
  END IF;

  RETURN NULL;
END;
$$;

CREATE TRIGGER trg_orders_customer_totals_sync
AFTER INSERT OR UPDATE OF customer_id, status, price_ship OR DELETE
ON orders
FOR EACH ROW
EXECUTE FUNCTION public.trg_orders_customer_totals_sync_fn();

CREATE TRIGGER trg_warehouse_customer_totals_sync
AFTER INSERT OR UPDATE OF order_id, status, net_weight OR DELETE
ON warehouse
FOR EACH ROW
EXECUTE FUNCTION public.trg_warehouse_customer_totals_sync_fn();

CREATE TRIGGER trg_order_links_customer_totals_sync
AFTER INSERT OR UPDATE OF warehouse_id, final_price_vnd OR DELETE
ON order_links
FOR EACH ROW
EXECUTE FUNCTION public.trg_order_links_customer_totals_sync_fn();

-- One-time resync after enabling triggers.
DO $$
DECLARE
  r RECORD;
BEGIN
  FOR r IN SELECT c.account_id FROM customer c LOOP
    PERFORM public.fn_recompute_customer_totals(r.account_id);
  END LOOP;
END;
$$;
