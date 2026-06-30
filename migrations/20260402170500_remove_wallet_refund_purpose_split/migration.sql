-- Unify refund purpose: remove wallet-vs-bank split in TransactionPurpose.

-- 1) Normalize legacy rows first.
UPDATE "transaction"
SET "payment_method" = 'WALLET'
WHERE "purpose" IN ('ORDER_REFUND_WALLET', 'PARTIAL_DELIVERY_REFUND_WALLET')
  AND "payment_method" IS NULL;

UPDATE "transaction"
SET "purpose" = 'ORDER_REFUND'
WHERE "purpose" = 'ORDER_REFUND_WALLET';

UPDATE "transaction"
SET "purpose" = 'PARTIAL_DELIVERY_REFUND'
WHERE "purpose" = 'PARTIAL_DELIVERY_REFUND_WALLET';

-- 2) Recreate enum without *_WALLET variants.
ALTER TYPE "TransactionPurpose" RENAME TO "TransactionPurpose_old";

CREATE TYPE "TransactionPurpose" AS ENUM (
  'ORDER_PAYMENT',
  'ORDER_REFUND',
  'WALLET_DEPOSIT',
  'WALLET_WITHDRAW',
  'ADJUSTMENT',
  'SHIPPING_FEE_PAYMENT',
  'PARTIAL_DELIVERY_REFUND'
);

ALTER TABLE "transaction"
  ALTER COLUMN "purpose" TYPE "TransactionPurpose"
  USING ("purpose"::text::"TransactionPurpose");

DROP TYPE "TransactionPurpose_old";
