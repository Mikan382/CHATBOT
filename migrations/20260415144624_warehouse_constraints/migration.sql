/*
  Warnings:

  - The primary key for the `route_warehouse_locations` table will be changed. If it partially fails, the table could be left without primary key constraint.
  - Added the required column `type` to the `route_warehouse_locations` table without a default value. This is not possible if the table is not empty.

*/
-- CreateEnum
CREATE TYPE "WarehouseType" AS ENUM ('FOREIGN', 'DOMESTIC');

-- AlterTable
ALTER TABLE "staff" ADD COLUMN     "assigned_route_id" BIGINT;

-- AlterTable
ALTER TABLE "warehouse" ADD COLUMN     "route_id" BIGINT;

-- AlterTable
ALTER TABLE "warehouse_location" ADD COLUMN "type_new" "WarehouseType";

-- Backfill old SMALLINT values: 0 => FOREIGN, 1 => DOMESTIC
UPDATE "warehouse_location"
SET "type_new" = CASE
  WHEN "type" = 0 THEN 'FOREIGN'::"WarehouseType"
  WHEN "type" = 1 THEN 'DOMESTIC'::"WarehouseType"
  ELSE NULL
END;

ALTER TABLE "warehouse_location" DROP COLUMN "type";
ALTER TABLE "warehouse_location" RENAME COLUMN "type_new" TO "type";

-- AlterTable
ALTER TABLE "route_warehouse_locations" ADD COLUMN "type" "WarehouseType";

-- Backfill type from warehouse location
UPDATE "route_warehouse_locations" rwl
SET "type" = wl."type"
FROM "warehouse_location" wl
WHERE wl."location_id" = rwl."location_id";

-- Fallback for old rows that still have NULL after backfill
UPDATE "route_warehouse_locations"
SET "type" = 'DOMESTIC'::"WarehouseType"
WHERE "type" IS NULL;

ALTER TABLE "route_warehouse_locations"
ALTER COLUMN "type" SET NOT NULL;

ALTER TABLE "route_warehouse_locations"
DROP CONSTRAINT "route_warehouse_locations_pkey";

ALTER TABLE "route_warehouse_locations"
ADD CONSTRAINT "route_warehouse_locations_pkey" PRIMARY KEY ("route_id", "location_id", "type");

-- AddForeignKey
ALTER TABLE "staff" ADD CONSTRAINT "staff_assigned_route_id_fkey" FOREIGN KEY ("assigned_route_id") REFERENCES "route"("route_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "warehouse_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE SET NULL ON UPDATE CASCADE;