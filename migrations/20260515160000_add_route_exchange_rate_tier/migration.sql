-- CreateTable
CREATE TABLE "route_exchange_rate_tier" (
    "id" BIGSERIAL NOT NULL,
    "route_id" BIGINT NOT NULL,
    "min_order_value" DECIMAL(38,2) NOT NULL,
    "max_order_value" DECIMAL(38,2),
    "exchange_rate" DECIMAL(38,2) NOT NULL,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMPTZ(6),

    CONSTRAINT "route_exchange_rate_tier_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE INDEX "idx_route_exchange_rate_tier_route_min" ON "route_exchange_rate_tier"("route_id", "min_order_value");

-- AddForeignKey
ALTER TABLE "route_exchange_rate_tier" ADD CONSTRAINT "route_exchange_rate_tier_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE CASCADE ON UPDATE CASCADE;
