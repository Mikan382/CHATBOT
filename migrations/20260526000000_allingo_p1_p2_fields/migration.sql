-- P1: lưu track_id, delivery info, driver info
-- P2: lưu previous_status, delivered_at, failure/cancellation reason

-- domestic table
ALTER TABLE "domestic"
  ADD COLUMN IF NOT EXISTS "allingo_track_id"              VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_delivery_id"           VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_partner_name"          VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_partner_track_id"      VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_previous_status"       VARCHAR(50),
  ADD COLUMN IF NOT EXISTS "allingo_delivered_at"          TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS "allingo_failure_reason"        VARCHAR(500),
  ADD COLUMN IF NOT EXISTS "allingo_cancellation_reason"   VARCHAR(500),
  ADD COLUMN IF NOT EXISTS "allingo_driver_name"           VARCHAR(255),
  ADD COLUMN IF NOT EXISTS "allingo_driver_phone"          VARCHAR(50),
  ADD COLUMN IF NOT EXISTS "allingo_driver_photo_url"      VARCHAR(500),
  ADD COLUMN IF NOT EXISTS "allingo_driver_license_plate"  VARCHAR(50),
  ADD COLUMN IF NOT EXISTS "allingo_booked_at"             TIMESTAMPTZ;

-- draft_domestic table
ALTER TABLE "draft_domestic"
  ADD COLUMN IF NOT EXISTS "allingo_track_id"              VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_delivery_id"           VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_partner_name"          VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_partner_track_id"      VARCHAR(100),
  ADD COLUMN IF NOT EXISTS "allingo_previous_status"       VARCHAR(50),
  ADD COLUMN IF NOT EXISTS "allingo_delivered_at"          TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS "allingo_failure_reason"        VARCHAR(500),
  ADD COLUMN IF NOT EXISTS "allingo_cancellation_reason"   VARCHAR(500),
  ADD COLUMN IF NOT EXISTS "allingo_driver_name"           VARCHAR(255),
  ADD COLUMN IF NOT EXISTS "allingo_driver_phone"          VARCHAR(50),
  ADD COLUMN IF NOT EXISTS "allingo_driver_photo_url"      VARCHAR(500),
  ADD COLUMN IF NOT EXISTS "allingo_driver_license_plate"  VARCHAR(50),
  ADD COLUMN IF NOT EXISTS "allingo_booked_at"             TIMESTAMPTZ;
