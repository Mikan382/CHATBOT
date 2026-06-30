ALTER TABLE "draft_domestic"
  ADD COLUMN "allingo_fee_delivery"  DECIMAL(12, 2),
  ADD COLUMN "allingo_fee_insurance" DECIMAL(12, 2),
  ADD COLUMN "allingo_fee_cod"       DECIMAL(12, 2),
  ADD COLUMN "allingo_fee_total"     DECIMAL(12, 2),
  ADD COLUMN "allingo_fee_currency"  VARCHAR(10),
  ADD COLUMN "allingo_synced_at"     TIMESTAMPTZ;
