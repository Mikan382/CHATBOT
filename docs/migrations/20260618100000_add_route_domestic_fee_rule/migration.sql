-- CreateEnum
CREATE TYPE "DomesticFeeChargeMode" AS ENUM ('FLAT', 'PER_KG');

-- CreateTable
CREATE TABLE "route_domestic_fee_rule" (
    "id" BIGSERIAL NOT NULL,
    "route_id" BIGINT NOT NULL,
    "min_weight" DECIMAL(38,2) NOT NULL,
    "max_weight" DECIMAL(38,2),
    "charge_mode" "DomesticFeeChargeMode" NOT NULL,
    "price" DECIMAL(38,2) NOT NULL,
    "currency" TEXT DEFAULT 'VND',
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMPTZ(6),

    CONSTRAINT "route_domestic_fee_rule_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE INDEX "idx_route_domestic_fee_rule_tier" ON "route_domestic_fee_rule"("route_id", "min_weight", "max_weight");

-- AddForeignKey
ALTER TABLE "route_domestic_fee_rule" ADD CONSTRAINT "route_domestic_fee_rule_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE CASCADE ON UPDATE CASCADE;
