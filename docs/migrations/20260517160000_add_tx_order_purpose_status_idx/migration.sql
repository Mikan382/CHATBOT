-- Composite index for computeOrderBalance queries:
--   transaction.findMany WHERE order_id = ? AND purpose IN (...) AND status IN (...)
-- Current indexes only cover (order_id, reason, status) — `reason` column khác `purpose`,
-- nên Postgres không dùng được cho query có filter purpose.
--
-- Không dùng CREATE INDEX CONCURRENTLY vì Prisma migrate wrap trong transaction.
-- Chấp nhận lock table write trong thời gian build index.

CREATE INDEX IF NOT EXISTS idx_tx_order_purpose_status
  ON "transaction" (order_id, purpose, status);
