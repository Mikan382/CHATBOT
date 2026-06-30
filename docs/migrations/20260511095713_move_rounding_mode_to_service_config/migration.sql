/*
  Warnings:

  - You are about to drop the column `rounding_mode` on the `route_weight_rule` table. All the data in the column will be lost.

*/
-- AlterTable
ALTER TABLE "route_weight_rule" DROP COLUMN "rounding_mode";

-- CreateTable
CREATE TABLE "route_service_config" (
    "id" BIGSERIAL NOT NULL,
    "route_id" BIGINT NOT NULL,
    "service_type" "AirServiceType" NOT NULL,
    "rounding_mode" "WeightRoundingMode",
    "min_weight_floor" DECIMAL(38,2),

    CONSTRAINT "route_service_config_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "route_service_config_route_id_service_type_key" ON "route_service_config"("route_id", "service_type");

-- AddForeignKey
ALTER TABLE "route_service_config" ADD CONSTRAINT "route_service_config_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE CASCADE ON UPDATE CASCADE;
