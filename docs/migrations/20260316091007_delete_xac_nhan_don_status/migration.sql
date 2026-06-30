/*
  Warnings:

  - The values [CHO_XAC_NHAN] on the enum `OrderMainStatus` will be removed. If these variants are still used in the database, this will fail.

*/
-- AlterEnum
BEGIN;
CREATE TYPE "OrderMainStatus_new" AS ENUM ('DA_XAC_NHAN', 'CHO_THANH_TOAN', 'CHO_MUA', 'DAU_GIA_THANH_CONG', 'CHO_THANH_TOAN_DAU_GIA', 'CHO_NHAP_KHO_NN', 'CHO_DONG_GOI', 'DANG_XU_LY', 'DA_DU_HANG', 'CHO_THANH_TOAN_SHIP', 'CHO_VAN_CHUYEN_KHO', 'CHO_GIAO', 'DA_GIAO', 'DA_HUY');
ALTER TABLE "orders" ALTER COLUMN "status" TYPE "OrderMainStatus_new" USING ("status"::text::"OrderMainStatus_new");
ALTER TABLE "partial_shipment" ALTER COLUMN "status" TYPE "OrderMainStatus_new" USING ("status"::text::"OrderMainStatus_new");
ALTER TYPE "OrderMainStatus" RENAME TO "OrderMainStatus_old";
ALTER TYPE "OrderMainStatus_new" RENAME TO "OrderMainStatus";
DROP TYPE "public"."OrderMainStatus_old";
COMMIT;
