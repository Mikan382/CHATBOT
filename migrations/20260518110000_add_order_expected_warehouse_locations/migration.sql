ALTER TABLE "orders"
  ADD COLUMN "foreign_warehouse_location_id" BIGINT,
  ADD COLUMN "domestic_warehouse_location_id" BIGINT;

ALTER TABLE "orders"
  ADD CONSTRAINT "fk_orders_foreign_warehouse_location"
  FOREIGN KEY ("foreign_warehouse_location_id")
  REFERENCES "warehouse_location"("location_id")
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

ALTER TABLE "orders"
  ADD CONSTRAINT "fk_orders_domestic_warehouse_location"
  FOREIGN KEY ("domestic_warehouse_location_id")
  REFERENCES "warehouse_location"("location_id")
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

CREATE INDEX "idx_orders_foreign_warehouse_location_id"
  ON "orders"("foreign_warehouse_location_id");

CREATE INDEX "idx_orders_domestic_warehouse_location_id"
  ON "orders"("domestic_warehouse_location_id");
