-- AlterTable
ALTER TABLE "ship_order" ADD COLUMN     "allingo_booked_at" TIMESTAMPTZ(6),
ADD COLUMN     "allingo_cancellation_reason" VARCHAR(500),
ADD COLUMN     "allingo_delivered_at" TIMESTAMPTZ(6),
ADD COLUMN     "allingo_delivery_id" VARCHAR(100),
ADD COLUMN     "allingo_driver_license_plate" VARCHAR(50),
ADD COLUMN     "allingo_driver_name" VARCHAR(255),
ADD COLUMN     "allingo_driver_phone" VARCHAR(50),
ADD COLUMN     "allingo_driver_photo_url" VARCHAR(500),
ADD COLUMN     "allingo_failure_reason" VARCHAR(500),
ADD COLUMN     "allingo_fee_cod" DECIMAL(12,2),
ADD COLUMN     "allingo_fee_currency" VARCHAR(10),
ADD COLUMN     "allingo_fee_delivery" DECIMAL(12,2),
ADD COLUMN     "allingo_fee_insurance" DECIMAL(12,2),
ADD COLUMN     "allingo_fee_total" DECIMAL(12,2),
ADD COLUMN     "allingo_order_id" VARCHAR(100),
ADD COLUMN     "allingo_partner_name" VARCHAR(100),
ADD COLUMN     "allingo_partner_track_id" VARCHAR(100),
ADD COLUMN     "allingo_previous_status" VARCHAR(50),
ADD COLUMN     "allingo_quoted_price" DECIMAL(12,2),
ADD COLUMN     "allingo_service_id" VARCHAR(100),
ADD COLUMN     "allingo_service_name" VARCHAR(100),
ADD COLUMN     "allingo_status" VARCHAR(50),
ADD COLUMN     "allingo_synced_at" TIMESTAMPTZ(6),
ADD COLUMN     "allingo_track_id" VARCHAR(100);

-- CreateIndex
CREATE INDEX "idx_ship_order_allingo_order_id" ON "ship_order"("allingo_order_id");
