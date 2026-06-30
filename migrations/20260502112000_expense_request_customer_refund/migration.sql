-- Re-apply customer refund workflow on top of current shared history.
DROP TABLE IF EXISTS "order_refund_request";
DROP TYPE IF EXISTS "RefundStatus";

DO $$ BEGIN
  CREATE TYPE "ExpenseRequestType" AS ENUM ('INTERNAL_EXPENSE', 'CUSTOMER_REFUND');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
  CREATE TYPE "ExpenseRequestSource" AS ENUM (
    'MANUAL',
    'ORDER_LEFTOVER',
    'CANCEL_ORDER',
    'CANCEL_LINK',
    'PARTIAL_DELIVERY',
    'SHIPPING_CREDIT_REMAINING'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

ALTER TABLE "expense_request"
  ADD COLUMN IF NOT EXISTS "request_type" "ExpenseRequestType" NOT NULL DEFAULT 'INTERNAL_EXPENSE',
  ADD COLUMN IF NOT EXISTS "source" "ExpenseRequestSource",
  ADD COLUMN IF NOT EXISTS "customer_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "order_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "cancel_request_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "refund_method" "RefundMethod",
  ADD COLUMN IF NOT EXISTS "refund_amount" DECIMAL(38, 2),
  ADD COLUMN IF NOT EXISTS "payment_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "transaction_id" UUID,
  ADD COLUMN IF NOT EXISTS "evidence_image_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "source_key" VARCHAR(120),
  ADD COLUMN IF NOT EXISTS "manager_note" VARCHAR(500),
  ADD COLUMN IF NOT EXISTS "processed_at" TIMESTAMPTZ(6);

CREATE INDEX IF NOT EXISTS "expense_request_source_key_idx"
  ON "expense_request"("source_key");

CREATE INDEX IF NOT EXISTS "expense_request_request_type_status_idx"
  ON "expense_request"("request_type", "status");

CREATE INDEX IF NOT EXISTS "expense_request_customer_id_idx"
  ON "expense_request"("customer_id");

CREATE INDEX IF NOT EXISTS "expense_request_order_id_idx"
  ON "expense_request"("order_id");

CREATE INDEX IF NOT EXISTS "expense_request_cancel_request_id_idx"
  ON "expense_request"("cancel_request_id");

ALTER TABLE "expense_request" DROP CONSTRAINT IF EXISTS "expense_request_customer_id_fkey";
ALTER TABLE "expense_request" DROP CONSTRAINT IF EXISTS "expense_request_order_id_fkey";
ALTER TABLE "expense_request" DROP CONSTRAINT IF EXISTS "expense_request_cancel_request_id_fkey";
ALTER TABLE "expense_request" DROP CONSTRAINT IF EXISTS "expense_request_payment_id_fkey";
ALTER TABLE "expense_request" DROP CONSTRAINT IF EXISTS "expense_request_transaction_id_fkey";
ALTER TABLE "expense_request" DROP CONSTRAINT IF EXISTS "expense_request_evidence_image_id_fkey";

ALTER TABLE "expense_request"
  ADD CONSTRAINT "expense_request_customer_id_fkey"
    FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id")
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT "expense_request_order_id_fkey"
    FOREIGN KEY ("order_id") REFERENCES "orders"("order_id")
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT "expense_request_cancel_request_id_fkey"
    FOREIGN KEY ("cancel_request_id") REFERENCES "order_cancel_request"("request_id")
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT "expense_request_payment_id_fkey"
    FOREIGN KEY ("payment_id") REFERENCES "payment"("payment_id")
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT "expense_request_transaction_id_fkey"
    FOREIGN KEY ("transaction_id") REFERENCES "transaction"("transaction_id")
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT "expense_request_evidence_image_id_fkey"
    FOREIGN KEY ("evidence_image_id") REFERENCES "media"("id")
    ON DELETE NO ACTION ON UPDATE NO ACTION;
