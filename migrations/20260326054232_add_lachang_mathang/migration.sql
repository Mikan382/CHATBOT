-- AlterEnum
ALTER TYPE "WarehouseStatus" ADD VALUE 'LAC_HANG';
ALTER TYPE "WarehouseStatus" ADD VALUE 'MAT_HANG';

-- AlterTable
ALTER TABLE "warehouse"
  ADD COLUMN "lost_reported_at" TIMESTAMPTZ(6),
  ADD COLUMN "lost_confirmed_at" TIMESTAMPTZ(6);
