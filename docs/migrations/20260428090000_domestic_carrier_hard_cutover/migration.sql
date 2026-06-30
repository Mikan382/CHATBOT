-- Hard cutover carrier storage from DraftDomesticCarrier enum columns to
-- configurable domestic_carrier records. NULL enum values remain NULL.

INSERT INTO "domestic_carrier" (
  "carrier_name",
  "carrier_code",
  "is_active",
  "created_at",
  "updated_at"
)
VALUES
  ('VNPOST', 'VNPOST', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  ('J&T', 'JT', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
  ('OTHER', 'OTHER', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT ("carrier_code") DO UPDATE SET
  "carrier_name" = EXCLUDED."carrier_name",
  "is_active" = true,
  "updated_at" = CURRENT_TIMESTAMP;

ALTER TABLE "draft_domestic"
  ADD COLUMN IF NOT EXISTS "carrier_id" BIGINT;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'draft_domestic_carrier_id_fkey'
  ) THEN
    ALTER TABLE "draft_domestic"
      ADD CONSTRAINT "draft_domestic_carrier_id_fkey"
      FOREIGN KEY ("carrier_id") REFERENCES "domestic_carrier"("carrier_id")
      ON DELETE NO ACTION ON UPDATE NO ACTION;
  END IF;
END $$;

UPDATE "domestic" d
SET "carrier_id" = dc."carrier_id"
FROM "domestic_carrier" dc
WHERE d."carrier" IS NOT NULL
  AND d."carrier_id" IS NULL
  AND dc."carrier_code" =
    CASE d."carrier"::text
      WHEN 'J&T' THEN 'JT'
      ELSE d."carrier"::text
    END;

UPDATE "draft_domestic" d
SET "carrier_id" = dc."carrier_id"
FROM "domestic_carrier" dc
WHERE d."carrier" IS NOT NULL
  AND d."carrier_id" IS NULL
  AND dc."carrier_code" =
    CASE d."carrier"::text
      WHEN 'J&T' THEN 'JT'
      ELSE d."carrier"::text
    END;

CREATE INDEX IF NOT EXISTS "idx_domestic_carrier_id"
  ON "domestic"("carrier_id");

CREATE INDEX IF NOT EXISTS "idx_draft_domestic_carrier_id"
  ON "draft_domestic"("carrier_id");

ALTER TABLE "draft_domestic" DROP COLUMN IF EXISTS "carrier";
ALTER TABLE "domestic" DROP COLUMN IF EXISTS "carrier";

DROP TYPE IF EXISTS "DraftDomesticCarrier";
