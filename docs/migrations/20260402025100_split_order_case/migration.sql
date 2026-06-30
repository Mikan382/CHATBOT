-- CreateEnum
CREATE TYPE "PartialDeliveryReason" AS ENUM ('THIEU_HANG_NGOAI_NUOC', 'THIEU_HANG_KHO_NN', 'THIEU_HANG_KHO_VN', 'THIEU_HANG_GIAO');

-- AlterEnum
ALTER TYPE "OrderLogAction" ADD VALUE 'GIAO_THIEU_HANG';

-- AlterEnum
ALTER TYPE "PaymentPurpose" ADD VALUE 'HOAN_TIEN_GIAO_THIEU';

-- AlterTable
ALTER TABLE "order_links" ADD COLUMN     "delivered_quantity" BIGINT,
ADD COLUMN     "parent_link_id" BIGINT,
ADD COLUMN     "split_reason" "PartialDeliveryReason";

-- AddForeignKey
ALTER TABLE "order_links" ADD CONSTRAINT "order_links_parent_link_id_fkey" FOREIGN KEY ("parent_link_id") REFERENCES "order_links"("link_id") ON DELETE SET NULL ON UPDATE CASCADE;
