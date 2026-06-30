-- AlterTable
ALTER TABLE "domestic" ADD COLUMN     "allingo_order_id" VARCHAR(100),
ADD COLUMN     "allingo_quoted_price" DECIMAL(12,2),
ADD COLUMN     "allingo_service_id" VARCHAR(100),
ADD COLUMN     "allingo_service_name" VARCHAR(100),
ADD COLUMN     "allingo_status" VARCHAR(50);

-- CreateIndex
CREATE INDEX "idx_domestic_allingo_order_id" ON "domestic"("allingo_order_id");
