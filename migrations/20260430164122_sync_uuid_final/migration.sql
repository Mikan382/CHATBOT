-- AlterTable
ALTER TABLE "transaction" ALTER COLUMN "transaction_id" DROP DEFAULT;

-- CreateIndex
CREATE INDEX "idx_tx_parent" ON "transaction"("parent_transaction_id");

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_parent_transaction_id_fkey" FOREIGN KEY ("parent_transaction_id") REFERENCES "transaction"("transaction_id") ON DELETE SET NULL ON UPDATE CASCADE;
