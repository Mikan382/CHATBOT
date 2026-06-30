-- AlterEnum
-- This migration adds more than one value to an enum.
-- With PostgreSQL versions 11 and earlier, this is not possible
-- in a single migration. This can be worked around by creating
-- multiple migrations, each migration adding only one value to
-- the enum.


ALTER TYPE "TransactionPurpose" ADD VALUE 'PURCHASE_FEE_PAYMENT';
ALTER TYPE "TransactionPurpose" ADD VALUE 'INSURANCE_FEE_PAYMENT';
ALTER TYPE "TransactionPurpose" ADD VALUE 'EXTRA_SERVICE_FEE';
ALTER TYPE "TransactionPurpose" ADD VALUE 'WALLET_REVERSAL';
