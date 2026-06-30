-- ============================================================
-- Migration: transaction_ledger_refund_foundation
-- Purpose:
-- 1) Add ledger semantics to transaction table (accrual/settlement/reversal)
-- 2) Add reason classification for refund sources
-- 3) Link settlement entries to payment and parent transaction
-- ============================================================

-- STEP 1: Enums (outside transaction-safe style)
CREATE TYPE "TransactionEntryKind" AS ENUM (
  'ACCRUAL',
  'SETTLEMENT',
  'REVERSAL'
);

CREATE TYPE "TransactionReason" AS ENUM (
  'LINK_CANCELLED',
  'PARTIAL_DELIVERY_SHORTAGE',
  'LOST_OR_DAMAGED',
  'INSURANCE_REFUND',
  'MANUAL_ADJUSTMENT'
);

-- STEP 2: Schema changes
ALTER TABLE "transaction"
  ADD COLUMN "entry_kind" "TransactionEntryKind" NOT NULL DEFAULT 'SETTLEMENT',
  ADD COLUMN "reason" "TransactionReason",
  ADD COLUMN "source_key" VARCHAR(120),
  ADD COLUMN "remaining_amount" DECIMAL(20,2),
  ADD COLUMN "parent_transaction_id" BIGINT,
  ADD COLUMN "payment_id" BIGINT;

ALTER TABLE "transaction"
  ADD CONSTRAINT "transaction_parent_transaction_id_fkey"
  FOREIGN KEY ("parent_transaction_id") REFERENCES "transaction"("transaction_id")
  ON DELETE SET NULL ON UPDATE CASCADE;

ALTER TABLE "transaction"
  ADD CONSTRAINT "transaction_payment_id_fkey"
  FOREIGN KEY ("payment_id") REFERENCES "payment"("payment_id")
  ON DELETE SET NULL ON UPDATE CASCADE;

-- STEP 3: Indexes / uniqueness
CREATE UNIQUE INDEX "uk_transaction_source_key"
  ON "transaction"("source_key")
  WHERE "source_key" IS NOT NULL;

CREATE INDEX "idx_tx_parent" ON "transaction"("parent_transaction_id");
CREATE INDEX "idx_tx_payment" ON "transaction"("payment_id");
CREATE INDEX "idx_tx_order_reason_status"
  ON "transaction"("order_id", "reason", "status");

-- STEP 4: Integrity checks
ALTER TABLE "transaction"
  ADD CONSTRAINT "chk_tx_remaining_amount"
  CHECK (
    (
      "entry_kind" = 'ACCRUAL'
      AND "remaining_amount" IS NOT NULL
      AND "remaining_amount" >= 0
      AND "remaining_amount" <= "amount"
    )
    OR
    (
      "entry_kind" IN ('SETTLEMENT', 'REVERSAL')
      AND ("remaining_amount" IS NULL OR "remaining_amount" = 0)
    )
  );

ALTER TABLE "transaction"
  ADD CONSTRAINT "chk_tx_parent_for_settlement"
  CHECK (
    "parent_transaction_id" IS NULL
    OR "entry_kind" IN ('SETTLEMENT', 'REVERSAL')
  );
