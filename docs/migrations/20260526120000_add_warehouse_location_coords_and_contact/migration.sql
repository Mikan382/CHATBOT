ALTER TABLE "warehouse_location"
  ADD COLUMN "contact_name"  VARCHAR(255),
  ADD COLUMN "contact_phone" VARCHAR(50),
  ADD COLUMN "lat"           DOUBLE PRECISION,
  ADD COLUMN "lng"           DOUBLE PRECISION;
