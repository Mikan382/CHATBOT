-- Expand phase (rename vnpost_tracking_code -> carrier_tracking_code, đồng bộ với model `domestic`).
-- Additive: thêm cột mới nullable + backfill từ cột cũ. Cột cũ vnpost_tracking_code GIỮ NGUYÊN
-- (dual-write/read trong giai đoạn chuyển tiếp). Việc drop cột cũ là phase contract sau khi FE migrate.

-- AddColumn
ALTER TABLE "draft_domestic" ADD COLUMN "carrier_tracking_code" VARCHAR(255);

-- Backfill: chép giá trị hiện có từ cột cũ sang cột mới.
UPDATE "draft_domestic"
SET "carrier_tracking_code" = "vnpost_tracking_code"
WHERE "carrier_tracking_code" IS NULL
  AND "vnpost_tracking_code" IS NOT NULL;
