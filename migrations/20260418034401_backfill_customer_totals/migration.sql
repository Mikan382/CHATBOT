-- prisma/migrations/<timestamp>_backfill_customer_totals/migration.sql
WITH order_agg AS (
  SELECT
    o.customer_id,
    COUNT(DISTINCT o.order_id)::int AS total_orders,
    COALESCE(SUM(o.final_price_order), 0)::numeric(38,2) AS total_amount
  FROM orders o
  GROUP BY o.customer_id
),
weight_agg AS (
  SELECT
    o.customer_id,
    COALESCE(SUM(w.billable_weight), 0)::double precision AS total_weight
  FROM orders o
  LEFT JOIN warehouse w ON w.order_id = o.order_id
  GROUP BY o.customer_id
),
final_agg AS (
  SELECT
    c.account_id,
    COALESCE(oa.total_orders, 0) AS total_orders,
    COALESCE(oa.total_amount, 0)::numeric(38,2) AS total_amount,
    COALESCE(wa.total_weight, 0)::double precision AS total_weight
  FROM customer c
  LEFT JOIN order_agg oa ON oa.customer_id = c.account_id
  LEFT JOIN weight_agg wa ON wa.customer_id = c.account_id
)
UPDATE customer c
SET
  total_orders = f.total_orders,
  total_amount = f.total_amount,
  total_weight = f.total_weight
FROM final_agg f
WHERE c.account_id = f.account_id
  AND (
    c.total_orders IS DISTINCT FROM f.total_orders
    OR c.total_amount IS DISTINCT FROM f.total_amount
    OR c.total_weight IS DISTINCT FROM f.total_weight
  );
