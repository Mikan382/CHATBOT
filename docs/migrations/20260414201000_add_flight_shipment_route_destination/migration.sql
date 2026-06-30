ALTER TABLE "flight_shipment"
  ADD COLUMN IF NOT EXISTS "route_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "destination_id" BIGINT;

ALTER TABLE "flight_shipment"
  DROP CONSTRAINT IF EXISTS "fk_flight_shipment_route",
  DROP CONSTRAINT IF EXISTS "fk_flight_shipment_destination";

ALTER TABLE "flight_shipment"
  ADD CONSTRAINT "fk_flight_shipment_route"
    FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT "fk_flight_shipment_destination"
    FOREIGN KEY ("destination_id") REFERENCES "destination"("destination_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
