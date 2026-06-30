-- CreateEnum
CREATE TYPE "ShipOrderStatus" AS ENUM ('PENDING', 'EXPORTED', 'CANCELLED');

-- CreateTable
CREATE TABLE "ship_order" (
    "ship_order_id" BIGSERIAL NOT NULL,
    "ship_code" VARCHAR(255) NOT NULL,
    "draft_domestic_id" BIGINT NOT NULL,
    "shipping_list" VARCHAR(255)[],
    "created_by" BIGINT NOT NULL,
    "created_by_role" "AccountRole" NOT NULL,
    "status" "ShipOrderStatus" NOT NULL DEFAULT 'PENDING',
    "domestic_id" BIGINT,
    "carrier_id" BIGINT,
    "carrier_tracking_code" VARCHAR(255),
    "note" VARCHAR(500),
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "ship_order_pkey" PRIMARY KEY ("ship_order_id")
);

-- CreateIndex
CREATE UNIQUE INDEX "ship_order_ship_code_key" ON "ship_order"("ship_code");

-- CreateIndex
CREATE UNIQUE INDEX "ship_order_domestic_id_key" ON "ship_order"("domestic_id");

-- CreateIndex
CREATE INDEX "idx_ship_order_draft_domestic_id" ON "ship_order"("draft_domestic_id");

-- CreateIndex
CREATE INDEX "idx_ship_order_status" ON "ship_order"("status");

-- CreateIndex
CREATE INDEX "idx_ship_order_created_by" ON "ship_order"("created_by");

-- AddForeignKey
ALTER TABLE "ship_order" ADD CONSTRAINT "ship_order_draft_domestic_id_fkey" FOREIGN KEY ("draft_domestic_id") REFERENCES "draft_domestic"("draft_domestic_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "ship_order" ADD CONSTRAINT "ship_order_domestic_id_fkey" FOREIGN KEY ("domestic_id") REFERENCES "domestic"("domestic_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "ship_order" ADD CONSTRAINT "ship_order_carrier_id_fkey" FOREIGN KEY ("carrier_id") REFERENCES "domestic_carrier"("carrier_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
