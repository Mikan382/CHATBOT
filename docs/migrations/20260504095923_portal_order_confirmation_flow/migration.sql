-- AlterEnum
ALTER TYPE "AccountRole" ADD VALUE 'STAFF_CS';

-- AlterEnum
-- This migration adds more than one value to an enum.
-- With PostgreSQL versions 11 and earlier, this is not possible
-- in a single migration. This can be worked around by creating
-- multiple migrations, each migration adding only one value to
-- the enum.


ALTER TYPE "OrderLogAction" ADD VALUE 'DUYET_DON_CUSTOMER';
ALTER TYPE "OrderLogAction" ADD VALUE 'TU_CHOI_DON_CUSTOMER';

-- AlterEnum
ALTER TYPE "OrderMainStatus" ADD VALUE 'CHO_XAC_NHAN';

-- AlterEnum
ALTER TYPE "OrderStatus" ADD VALUE 'CHO_XAC_NHAN';

-- AlterTable
ALTER TABLE "orders" ALTER COLUMN "destination_id" DROP NOT NULL;
