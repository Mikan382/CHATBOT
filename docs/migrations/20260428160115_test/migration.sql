-- DropIndex
DROP INDEX "idx_domestic_carrier_is_deleted";

-- DropIndex
DROP INDEX "idx_dct_is_deleted";

-- DropIndex
DROP INDEX "uq_dct_active_carrier_type";

-- AlterTable
ALTER TABLE "domestic_carrier_template" ALTER COLUMN "updated_at" DROP DEFAULT;
