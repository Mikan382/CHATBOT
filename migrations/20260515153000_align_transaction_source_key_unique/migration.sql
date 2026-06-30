-- Align transaction.source_key unique index with Prisma schema.
-- PostgreSQL unique indexes allow multiple NULL values, so this preserves
-- the effective uniqueness rule from the previous partial unique index.
DROP INDEX IF EXISTS "uk_transaction_source_key";

CREATE UNIQUE INDEX IF NOT EXISTS "uk_transaction_source_key" ON "transaction"("source_key");
