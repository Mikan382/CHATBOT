-- Insert ALLINGO as a new domestic_carrier (additive, không đụng OTHER)
INSERT INTO "domestic_carrier" ("carrier_name", "carrier_code", "is_active", "is_deleted", "created_at", "updated_at")
VALUES ('Allingo', 'ALLINGO', true, false, NOW(), NOW())
ON CONFLICT ("carrier_code") DO NOTHING;

-- AlterTable draft_domestic: thêm allingo fields (all nullable, additive)
ALTER TABLE "draft_domestic"
  ADD COLUMN IF NOT EXISTS "allingo_order_id"    VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_service_id"  VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_service_name" VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_quoted_price" DECIMAL(12,2),
  ADD COLUMN IF NOT EXISTS "allingo_status"       VARCHAR(50);

-- Index để lookup nhanh theo allingo_order_id
CREATE INDEX IF NOT EXISTS "idx_draft_domestic_allingo_order_id"
  ON "draft_domestic"("allingo_order_id");
