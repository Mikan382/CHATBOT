-- AlterEnum
-- This migration adds more than one value to an enum.
-- With PostgreSQL versions 11 and earlier, this is not possible
-- in a single migration. This can be worked around by creating
-- multiple migrations, each migration adding only one value to
-- the enum.


ALTER TYPE "TransactionPurpose" ADD VALUE 'PARTIAL_DELIVERY_REFUND';
ALTER TYPE "TransactionPurpose" ADD VALUE 'PARTIAL_DELIVERY_REFUND_WALLET';

-- AlterTable
ALTER TABLE "transaction" ADD COLUMN     "order_link_id" BIGINT;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_order_link_id_fkey" FOREIGN KEY ("order_link_id") REFERENCES "order_links"("link_id") ON DELETE SET NULL ON UPDATE CASCADE;
