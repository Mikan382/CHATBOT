ALTER TABLE "domestic_carrier"
  ADD COLUMN IF NOT EXISTS "is_deleted" BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS "deleted_at" TIMESTAMPTZ(6);

ALTER TABLE "domestic_carrier_template"
  ADD COLUMN IF NOT EXISTS "is_deleted" BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS "deleted_at" TIMESTAMPTZ(6);

UPDATE "domestic_carrier"
SET "is_active" = false
WHERE "is_deleted" = true
  AND "is_active" = true;

UPDATE "domestic_carrier_template"
SET "is_active" = false
WHERE "is_deleted" = true
  AND "is_active" = true;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'chk_domestic_carrier_deleted_requires_inactive'
  ) THEN
    ALTER TABLE "domestic_carrier"
      ADD CONSTRAINT "chk_domestic_carrier_deleted_requires_inactive"
      CHECK (NOT ("is_deleted" = true AND "is_active" = true));
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'chk_dct_deleted_requires_inactive'
  ) THEN
    ALTER TABLE "domestic_carrier_template"
      ADD CONSTRAINT "chk_dct_deleted_requires_inactive"
      CHECK (NOT ("is_deleted" = true AND "is_active" = true));
  END IF;
END $$;

DROP INDEX IF EXISTS "uq_dct_active_carrier_type";

CREATE UNIQUE INDEX IF NOT EXISTS "uq_dct_active_carrier_type"
  ON "domestic_carrier_template" ("carrier_id", "template_type")
  WHERE "is_active" = true AND "is_deleted" = false;

CREATE INDEX IF NOT EXISTS "idx_domestic_carrier_is_deleted"
  ON "domestic_carrier" ("is_deleted");

CREATE INDEX IF NOT EXISTS "idx_dct_is_deleted"
  ON "domestic_carrier_template" ("is_deleted");
