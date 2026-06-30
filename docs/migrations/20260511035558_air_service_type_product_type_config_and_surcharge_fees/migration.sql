-- CreateEnum
CREATE TYPE "AirServiceType" AS ENUM ('CLEAN', 'MIXED');

-- DropIndex
DROP INDEX "expense_request_source_key_idx";

-- AlterTable
ALTER TABLE "orders" ADD COLUMN     "inspection_fee" DECIMAL(38,2) DEFAULT 0,
ADD COLUMN     "repack_fee" DECIMAL(38,2) DEFAULT 0,
ADD COLUMN     "repack_required" BOOLEAN NOT NULL DEFAULT false,
ADD COLUMN     "service_type" "AirServiceType";

-- AlterTable
ALTER TABLE "route_weight_rule" ADD COLUMN     "service_type" "AirServiceType";

-- CreateTable
CREATE TABLE "route_product_type_config" (
    "id" BIGSERIAL NOT NULL,
    "route_id" BIGINT NOT NULL,
    "product_type_id" BIGINT NOT NULL,
    "service_type" "AirServiceType",
    "is_enabled" BOOLEAN NOT NULL DEFAULT true,
    "extra_charge" DECIMAL(38,2),
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMPTZ(6),

    CONSTRAINT "route_product_type_config_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "route_surcharge_config" (
    "id" BIGSERIAL NOT NULL,
    "route_id" BIGINT NOT NULL,
    "service_type" "AirServiceType",
    "inspection_fee_default" DECIMAL(38,2),
    "repack_fee_default" DECIMAL(38,2),
    "is_active" BOOLEAN NOT NULL DEFAULT true,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMPTZ(6),

    CONSTRAINT "route_surcharge_config_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE INDEX "idx_route_product_type_config_route_service" ON "route_product_type_config"("route_id", "service_type");

-- CreateIndex
CREATE UNIQUE INDEX "uq_route_product_type_config" ON "route_product_type_config"("route_id", "product_type_id", "service_type");

-- CreateIndex
CREATE INDEX "idx_route_surcharge_config_route_active" ON "route_surcharge_config"("route_id", "is_active");

-- CreateIndex
CREATE UNIQUE INDEX "uq_route_surcharge_config" ON "route_surcharge_config"("route_id", "service_type");

-- CreateIndex
CREATE INDEX "idx_route_weight_rule_service_tier" ON "route_weight_rule"("route_id", "service_type", "min_weight", "max_weight");

-- AddForeignKey
ALTER TABLE "route_product_type_config" ADD CONSTRAINT "route_product_type_config_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "route_product_type_config" ADD CONSTRAINT "route_product_type_config_product_type_id_fkey" FOREIGN KEY ("product_type_id") REFERENCES "product_type"("product_type_id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "route_surcharge_config" ADD CONSTRAINT "route_surcharge_config_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE CASCADE ON UPDATE CASCADE;
