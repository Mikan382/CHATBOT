ALTER TABLE "domestic"
  ADD COLUMN IF NOT EXISTS "allingo_booked_at" TIMESTAMPTZ;

ALTER TABLE "draft_domestic"
  ADD COLUMN IF NOT EXISTS "allingo_booked_at" TIMESTAMPTZ;
