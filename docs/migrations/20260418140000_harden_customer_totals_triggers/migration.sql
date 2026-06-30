-- Harden customer totals triggers:
-- 1. Advisory lock in fn_recompute_customer_totals to prevent deadlocks on concurrent writes
-- 2. WHEN clauses on all 3 triggers to skip no-op updates

-- ─── 1. Recompute function with advisory lock ────────────────────────────────

CREATE OR REPLACE FUNCTION public.fn_recompute_customer_totals(p_customer_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
  IF p_customer_id IS NULL THEN
    RETURN;
  END IF;

  -- Serialize concurrent recomputes for the same customer.
  -- pg_advisory_xact_lock is released automatically at transaction end.
  PERFORM pg_advisory_xact_lock(p_customer_id);

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

-- ─── 2. Narrow triggers with WHEN clauses ────────────────────────────────────

-- Drop old single triggers (created by 20260418130000_auto_sync_customer_totals)
DROP TRIGGER IF EXISTS trg_orders_customer_totals_sync ON orders;
DROP TRIGGER IF EXISTS trg_warehouse_customer_totals_sync ON warehouse;
DROP TRIGGER IF EXISTS trg_order_links_customer_totals_sync ON order_links;
-- Drop split triggers in case this migration is re-run
DROP TRIGGER IF EXISTS trg_orders_customer_totals_sync_ins_del ON orders;
DROP TRIGGER IF EXISTS trg_orders_customer_totals_sync_upd ON orders;
DROP TRIGGER IF EXISTS trg_warehouse_customer_totals_sync_ins_del ON warehouse;
DROP TRIGGER IF EXISTS trg_warehouse_customer_totals_sync_upd ON warehouse;
DROP TRIGGER IF EXISTS trg_order_links_customer_totals_sync_ins_del ON order_links;
DROP TRIGGER IF EXISTS trg_order_links_customer_totals_sync_upd ON order_links;

-- Split into INSERT/DELETE (always fire) + UPDATE (WHEN clause guards no-ops)
-- PostgreSQL does not allow OLD refs in INSERT WHEN or NEW refs in DELETE WHEN.

-- orders
CREATE TRIGGER trg_orders_customer_totals_sync_ins_del
AFTER INSERT OR DELETE
ON orders
FOR EACH ROW
EXECUTE FUNCTION public.trg_orders_customer_totals_sync_fn();

CREATE TRIGGER trg_orders_customer_totals_sync_upd
AFTER UPDATE OF customer_id, status, price_ship
ON orders
FOR EACH ROW
WHEN (
  NEW.customer_id IS DISTINCT FROM OLD.customer_id OR
  NEW.status      IS DISTINCT FROM OLD.status      OR
  NEW.price_ship  IS DISTINCT FROM OLD.price_ship
)
EXECUTE FUNCTION public.trg_orders_customer_totals_sync_fn();

-- warehouse
CREATE TRIGGER trg_warehouse_customer_totals_sync_ins_del
AFTER INSERT OR DELETE
ON warehouse
FOR EACH ROW
EXECUTE FUNCTION public.trg_warehouse_customer_totals_sync_fn();

CREATE TRIGGER trg_warehouse_customer_totals_sync_upd
AFTER UPDATE OF order_id, status, net_weight
ON warehouse
FOR EACH ROW
WHEN (
  NEW.order_id   IS DISTINCT FROM OLD.order_id   OR
  NEW.status     IS DISTINCT FROM OLD.status     OR
  NEW.net_weight IS DISTINCT FROM OLD.net_weight
)
EXECUTE FUNCTION public.trg_warehouse_customer_totals_sync_fn();

-- order_links
CREATE TRIGGER trg_order_links_customer_totals_sync_ins_del
AFTER INSERT OR DELETE
ON order_links
FOR EACH ROW
EXECUTE FUNCTION public.trg_order_links_customer_totals_sync_fn();

CREATE TRIGGER trg_order_links_customer_totals_sync_upd
AFTER UPDATE OF warehouse_id, final_price_vnd
ON order_links
FOR EACH ROW
WHEN (
  NEW.warehouse_id    IS DISTINCT FROM OLD.warehouse_id    OR
  NEW.final_price_vnd IS DISTINCT FROM OLD.final_price_vnd
)
EXECUTE FUNCTION public.trg_order_links_customer_totals_sync_fn();
