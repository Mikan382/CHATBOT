-- AlterTable
ALTER TABLE "route_weight_rule" ADD COLUMN     "price_ship" DECIMAL(38,2),
ALTER COLUMN "billable_weight" DROP NOT NULL;
