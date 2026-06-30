-- CreateTable: khung giờ làm việc của kho (warehouse_location). Nhân viên kho DOMESTIC
-- tự khai; dùng để gate tạo phiếu draft-domestic carrier=OTHER ngoài giờ. Additive — new table only.
CREATE TABLE "warehouse_location_operating_hours" (
    "id" BIGSERIAL NOT NULL,
    "location_id" BIGINT NOT NULL,
    "day_of_week" INTEGER NOT NULL,
    "open_time" VARCHAR(5) NOT NULL,
    "close_time" VARCHAR(5) NOT NULL,
    "is_active" BOOLEAN NOT NULL DEFAULT true,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT now(),
    "updated_at" TIMESTAMPTZ(6) NOT NULL,

    CONSTRAINT "warehouse_location_operating_hours_pkey" PRIMARY KEY ("id")
);

-- CreateIndex: lookup "kho đang mở?" theo (location_id, day_of_week, is_active).
CREATE INDEX "idx_wloh_location_day_active" ON "warehouse_location_operating_hours"("location_id", "day_of_week", "is_active");

-- AddForeignKey
ALTER TABLE "warehouse_location_operating_hours"
    ADD CONSTRAINT "warehouse_location_operating_hours_location_id_fkey"
    FOREIGN KEY ("location_id") REFERENCES "warehouse_location"("location_id")
    ON DELETE CASCADE ON UPDATE CASCADE;
