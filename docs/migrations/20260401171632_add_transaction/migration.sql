-- CreateEnum
CREATE TYPE "TransactionType" AS ENUM ('INCOME', 'OUTCOME');

-- CreateEnum
CREATE TYPE "TransactionPurpose" AS ENUM ('ORDER_PAYMENT', 'ORDER_REFUND', 'WALLET_DEPOSIT', 'WALLET_WITHDRAW', 'ORDER_REFUND_WALLET', 'ADJUSTMENT', 'SHIPPING_FEE_PAYMENT');

-- CreateEnum
CREATE TYPE "TransactionStatus" AS ENUM ('PENDING', 'SUCCESS', 'FAILED', 'CANCELLED');

-- AlterEnum
-- This migration adds more than one value to an enum.
-- With PostgreSQL versions 11 and earlier, this is not possible
-- in a single migration. This can be worked around by creating
-- multiple migrations, each migration adding only one value to
-- the enum.


ALTER TYPE "PaymentMethod" ADD VALUE 'COD';
ALTER TYPE "PaymentMethod" ADD VALUE 'BANK_TRANSFER';
ALTER TYPE "PaymentMethod" ADD VALUE 'QR_CODE';
ALTER TYPE "PaymentMethod" ADD VALUE 'WALLET';
ALTER TYPE "PaymentMethod" ADD VALUE 'CASH';
ALTER TYPE "PaymentMethod" ADD VALUE 'GATEWAY';

-- CreateTable
CREATE TABLE "transaction" (
    "transaction_id" BIGSERIAL NOT NULL,
    "transaction_code" TEXT NOT NULL,
    "amount" DECIMAL(20,2) NOT NULL,
    "before_balance" DECIMAL(20,2) NOT NULL,
    "after_balance" DECIMAL(20,2) NOT NULL,
    "type" "TransactionType" NOT NULL,
    "purpose" "TransactionPurpose" NOT NULL,
    "status" "TransactionStatus" NOT NULL DEFAULT 'SUCCESS',
    "payment_method" "PaymentMethod",
    "reference_code" TEXT,
    "metadata" JSONB,
    "note" TEXT,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,
    "customer_id" BIGINT NOT NULL,
    "staff_id" BIGINT,
    "order_id" BIGINT,
    "partial_shipment_id" BIGINT,
    "evidence_image_id" BIGINT,
    "mediaId" BIGINT,

    CONSTRAINT "transaction_pkey" PRIMARY KEY ("transaction_id")
);

-- CreateIndex
CREATE UNIQUE INDEX "transaction_transaction_code_key" ON "transaction"("transaction_code");

-- CreateIndex
CREATE INDEX "transaction_customer_id_idx" ON "transaction"("customer_id");

-- CreateIndex
CREATE INDEX "transaction_transaction_code_idx" ON "transaction"("transaction_code");

-- CreateIndex
CREATE INDEX "transaction_purpose_idx" ON "transaction"("purpose");

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_customer_id_fkey" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_staff_id_fkey" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_order_id_fkey" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_partial_shipment_id_fkey" FOREIGN KEY ("partial_shipment_id") REFERENCES "partial_shipment"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_evidence_image_id_fkey" FOREIGN KEY ("evidence_image_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_mediaId_fkey" FOREIGN KEY ("mediaId") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;
