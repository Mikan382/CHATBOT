ALTER TABLE "flight_shipment"
  DROP COLUMN IF EXISTS "internal_code";

DROP INDEX IF EXISTS "uk_flight_shipment_internal_code";
