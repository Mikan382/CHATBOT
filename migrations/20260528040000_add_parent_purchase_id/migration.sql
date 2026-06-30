-- AlterTable: add parent_purchase_id to purchases for tracking split origin
ALTER TABLE "purchases" ADD COLUMN "parent_purchase_id" BIGINT;

-- AddForeignKey
ALTER TABLE "purchases" ADD CONSTRAINT "purchases_parent_purchase_id_fkey"
  FOREIGN KEY ("parent_purchase_id") REFERENCES "purchases"("purchase_id")
  ON DELETE SET NULL ON UPDATE CASCADE;

-- CreateIndex
CREATE INDEX "idx_purchases_parent_purchase_id" ON "purchases"("parent_purchase_id");
