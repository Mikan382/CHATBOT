-- Add new media foreign key columns for flight_shipment files
ALTER TABLE "flight_shipment"
  ADD COLUMN IF NOT EXISTS "awb_file_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "invoice_file_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "export_license_id" BIGINT;

-- Normalize and collect legacy path data
DROP TABLE IF EXISTS "_flight_shipment_awb_media_map";
CREATE TABLE "_flight_shipment_awb_media_map" AS
SELECT
  fs."flight_shipment_id",
  LEFT(
    REGEXP_REPLACE(
      REGEXP_REPLACE(BTRIM(fs."awb_file_path"), '^https?://[^/]+', ''),
      '^/+',
      ''
    ),
    255
  ) AS "file_path"
FROM "flight_shipment" fs
WHERE fs."awb_file_path" IS NOT NULL AND BTRIM(fs."awb_file_path") <> '';

DROP TABLE IF EXISTS "_flight_shipment_invoice_media_map";
CREATE TABLE "_flight_shipment_invoice_media_map" AS
SELECT
  fs."flight_shipment_id",
  LEFT(
    REGEXP_REPLACE(
      REGEXP_REPLACE(BTRIM(fs."invoice_file_path"), '^https?://[^/]+', ''),
      '^/+',
      ''
    ),
    255
  ) AS "file_path"
FROM "flight_shipment" fs
WHERE fs."invoice_file_path" IS NOT NULL AND BTRIM(fs."invoice_file_path") <> '';

DROP TABLE IF EXISTS "_flight_shipment_export_license_media_map";
CREATE TABLE "_flight_shipment_export_license_media_map" AS
SELECT
  fs."flight_shipment_id",
  LEFT(
    REGEXP_REPLACE(
      REGEXP_REPLACE(BTRIM(fs."export_license_path"), '^https?://[^/]+', ''),
      '^/+',
      ''
    ),
    255
  ) AS "file_path"
FROM "flight_shipment" fs
WHERE fs."export_license_path" IS NOT NULL AND BTRIM(fs."export_license_path") <> '';

-- Insert missing media rows for AWB files
INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  nextval(pg_get_serial_sequence('"media"', 'id')),
  LEFT(
    COALESCE(
      NULLIF(REGEXP_REPLACE(src."file_path", '^.*/', ''), ''),
      'flight_awb_' || src."sample_flight_shipment_id"
    ),
    255
  ),
  src."file_path",
  0,
  'application/octet-stream',
  'misc'::"MediaFolder"
FROM (
  SELECT
    m."file_path",
    MIN(m."flight_shipment_id") AS "sample_flight_shipment_id"
  FROM "_flight_shipment_awb_media_map" m
  WHERE m."file_path" IS NOT NULL AND BTRIM(m."file_path") <> ''
  GROUP BY m."file_path"
) src
LEFT JOIN "media" existing ON existing."file_path" = src."file_path"
WHERE existing."id" IS NULL;

-- Insert missing media rows for invoice files
INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  nextval(pg_get_serial_sequence('"media"', 'id')),
  LEFT(
    COALESCE(
      NULLIF(REGEXP_REPLACE(src."file_path", '^.*/', ''), ''),
      'flight_invoice_' || src."sample_flight_shipment_id"
    ),
    255
  ),
  src."file_path",
  0,
  'application/octet-stream',
  'misc'::"MediaFolder"
FROM (
  SELECT
    m."file_path",
    MIN(m."flight_shipment_id") AS "sample_flight_shipment_id"
  FROM "_flight_shipment_invoice_media_map" m
  WHERE m."file_path" IS NOT NULL AND BTRIM(m."file_path") <> ''
  GROUP BY m."file_path"
) src
LEFT JOIN "media" existing ON existing."file_path" = src."file_path"
WHERE existing."id" IS NULL;

-- Insert missing media rows for export license files
INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  nextval(pg_get_serial_sequence('"media"', 'id')),
  LEFT(
    COALESCE(
      NULLIF(REGEXP_REPLACE(src."file_path", '^.*/', ''), ''),
      'flight_export_' || src."sample_flight_shipment_id"
    ),
    255
  ),
  src."file_path",
  0,
  'application/octet-stream',
  'misc'::"MediaFolder"
FROM (
  SELECT
    m."file_path",
    MIN(m."flight_shipment_id") AS "sample_flight_shipment_id"
  FROM "_flight_shipment_export_license_media_map" m
  WHERE m."file_path" IS NOT NULL AND BTRIM(m."file_path") <> ''
  GROUP BY m."file_path"
) src
LEFT JOIN "media" existing ON existing."file_path" = src."file_path"
WHERE existing."id" IS NULL;

-- Backfill new foreign keys
UPDATE "flight_shipment" fs
SET "awb_file_id" = media."id"
FROM "_flight_shipment_awb_media_map" map
JOIN "media" media ON media."file_path" = map."file_path"
WHERE fs."flight_shipment_id" = map."flight_shipment_id";

UPDATE "flight_shipment" fs
SET "invoice_file_id" = media."id"
FROM "_flight_shipment_invoice_media_map" map
JOIN "media" media ON media."file_path" = map."file_path"
WHERE fs."flight_shipment_id" = map."flight_shipment_id";

UPDATE "flight_shipment" fs
SET "export_license_id" = media."id"
FROM "_flight_shipment_export_license_media_map" map
JOIN "media" media ON media."file_path" = map."file_path"
WHERE fs."flight_shipment_id" = map."flight_shipment_id";

-- Add foreign key constraints
ALTER TABLE "flight_shipment"
  DROP CONSTRAINT IF EXISTS "flight_shipment_awb_file_id_fkey",
  DROP CONSTRAINT IF EXISTS "flight_shipment_invoice_file_id_fkey",
  DROP CONSTRAINT IF EXISTS "flight_shipment_export_license_id_fkey";

ALTER TABLE "flight_shipment"
  ADD CONSTRAINT "flight_shipment_awb_file_id_fkey"
    FOREIGN KEY ("awb_file_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT "flight_shipment_invoice_file_id_fkey"
    FOREIGN KEY ("invoice_file_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT "flight_shipment_export_license_id_fkey"
    FOREIGN KEY ("export_license_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- Drop legacy path columns (including deprecated fields)
ALTER TABLE "flight_shipment"
  DROP COLUMN IF EXISTS "awb_file_path",
  DROP COLUMN IF EXISTS "invoice_file_path",
  DROP COLUMN IF EXISTS "export_license_path",
  DROP COLUMN IF EXISTS "packing_list_path",
  DROP COLUMN IF EXISTS "single_invoice_path";

DROP TABLE IF EXISTS "_flight_shipment_awb_media_map";
DROP TABLE IF EXISTS "_flight_shipment_invoice_media_map";
DROP TABLE IF EXISTS "_flight_shipment_export_license_media_map";
