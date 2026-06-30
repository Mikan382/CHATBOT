-- Allow refund requests to be recreated after rejection/cancel while keeping
-- one active pending request per source_key.

ALTER TABLE "expense_request"
ADD COLUMN IF NOT EXISTS "approved_amount" DECIMAL(38, 2);

ALTER TABLE "expense_request"
DROP CONSTRAINT IF EXISTS "uk_expense_request_source_key";

DROP INDEX IF EXISTS "uk_expense_request_source_key";

CREATE INDEX IF NOT EXISTS "idx_expense_request_source_key"
ON "expense_request"("source_key");

CREATE UNIQUE INDEX IF NOT EXISTS "uk_expense_request_source_key_pending"
ON "expense_request"("source_key")
WHERE "status" IN ('CHO_DUYET', 'CHO_DUYET_PURCHASER');
