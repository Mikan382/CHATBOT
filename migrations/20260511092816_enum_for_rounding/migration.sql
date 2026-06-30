-- CreateEnum
CREATE TYPE "WeightRoundingMode" AS ENUM ('THRESHOLD_MODE', 'STEP_MODE', 'FLOOR_MODE');

-- AlterTable
ALTER TABLE "route_weight_rule" ADD COLUMN     "rounding_mode" "WeightRoundingMode";

-- RenameForeignKey
ALTER TABLE "expense_request" RENAME CONSTRAINT "fk_expense_request_invoice_image_id" TO "expense_request_invoice_image_id_fkey";

-- RenameForeignKey
ALTER TABLE "expense_request" RENAME CONSTRAINT "fk_expense_request_transfer_image_id" TO "expense_request_transfer_image_id_fkey";
