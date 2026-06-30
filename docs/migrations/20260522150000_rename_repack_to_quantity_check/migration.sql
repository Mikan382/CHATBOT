ALTER TABLE "orders"
RENAME COLUMN "repack_required" TO "quantity_check_required";

ALTER TABLE "orders"
RENAME COLUMN "repack_fee" TO "quantity_check_fee";

ALTER TABLE "route_surcharge_config"
RENAME COLUMN "repack_fee_default" TO "quantity_check_fee_default";

UPDATE "route_surcharge_config"
SET "quantity_check_fee_default" = 10000;
