WITH eligible_warehouses AS (
  SELECT
    w.warehouse_id,
    w.order_id,
    COALESCE(w.billable_weight, 0)::double precision AS billable_weight
  FROM warehouse w
  WHERE w.status IN (
    'CHO_GIAO'::"WarehouseStatus",
    'DA_GIAO'::"WarehouseStatus"
  )
),
weight_agg AS (
  SELECT
    o.customer_id,
    COALESCE(SUM(ew.billable_weight), 0)::double precision AS total_weight
  FROM eligible_warehouses ew
  JOIN orders o ON o.order_id = ew.order_id
  GROUP BY o.customer_id
),
goods_agg AS (
  SELECT
    o.customer_id,
    COALESCE(SUM(COALESCE(ol.final_price_vnd, 0)), 0)::numeric(38, 2) AS goods_amount
  FROM eligible_warehouses ew
  JOIN orders o ON o.order_id = ew.order_id
  LEFT JOIN order_links ol ON ol.warehouse_id = ew.warehouse_id
  GROUP BY o.customer_id
),
ship_agg AS (
  SELECT
    x.customer_id,
    COALESCE(SUM(x.price_ship), 0)::numeric(38, 2) AS ship_amount
  FROM (
    SELECT DISTINCT
      o.customer_id,
      o.order_id,
      COALESCE(o.price_ship, 0)::numeric(38, 2) AS price_ship
    FROM eligible_warehouses ew
    JOIN orders o ON o.order_id = ew.order_id
  ) x
  GROUP BY x.customer_id
),
amount_agg AS (
  SELECT
    c.account_id AS customer_id,
    (
      COALESCE(sa.ship_amount, 0)::numeric(38, 2) +
      COALESCE(ga.goods_amount, 0)::numeric(38, 2)
    )::numeric(38, 2) AS total_amount
  FROM customer c
  LEFT JOIN ship_agg sa ON sa.customer_id = c.account_id
  LEFT JOIN goods_agg ga ON ga.customer_id = c.account_id
),
order_agg AS (
  SELECT
    o.customer_id,
    COUNT(DISTINCT o.order_id)::int AS total_orders
  FROM orders o
  WHERE o.status IS DISTINCT FROM 'DA_HUY'::"OrderMainStatus"
  GROUP BY o.customer_id
),
final_agg AS (
  SELECT
    c.account_id,
    COALESCE(wa.total_weight, 0)::double precision AS total_weight,
    COALESCE(aa.total_amount, 0)::numeric(38, 2) AS total_amount,
    COALESCE(oa.total_orders, 0)::int AS total_orders
  FROM customer c
  LEFT JOIN weight_agg wa ON wa.customer_id = c.account_id
  LEFT JOIN amount_agg aa ON aa.customer_id = c.account_id
  LEFT JOIN order_agg oa ON oa.customer_id = c.account_id
)
UPDATE customer c
SET
  total_weight = f.total_weight,
  total_amount = f.total_amount,
  total_orders = f.total_orders
FROM final_agg f
WHERE c.account_id = f.account_id
  AND (
    c.total_weight IS DISTINCT FROM f.total_weight
    OR c.total_amount IS DISTINCT FROM f.total_amount
    OR c.total_orders IS DISTINCT FROM f.total_orders
  );
