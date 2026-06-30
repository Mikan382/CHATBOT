-- AlterEnum
-- This migration adds more than one value to an enum.
-- With PostgreSQL versions 11 and earlier, this is not possible
-- in a single migration. This can be worked around by creating
-- multiple migrations, each migration adding only one value to
-- the enum.


ALTER TYPE "MediaFolder" ADD VALUE IF NOT EXISTS 'warehouses';
ALTER TYPE "MediaFolder" ADD VALUE IF NOT EXISTS 'domestics';
ALTER TYPE "MediaFolder" ADD VALUE IF NOT EXISTS 'payments';

-- AlterTable
ALTER TABLE "partial_shipment" ALTER COLUMN "shipment_date" SET DATA TYPE TIMESTAMPTZ(6);
