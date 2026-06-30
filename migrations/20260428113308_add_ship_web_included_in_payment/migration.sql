-- Add ship_web_included_in_payment to orders table
-- Tracks whether ship_web was factored into payment_after_auction during confirmAuction
-- Prevents formula mismatch: pre-payment uses (1+fee%) × rate, post-payment uses rate only

ALTER TABLE "orders"
ADD COLUMN "ship_web_included_in_payment" BOOLEAN NOT NULL DEFAULT FALSE;

-- Backfill: set true for existing auction orders past payment where ship_web > 0
-- These had ship_web baked into payment_after_auction with purchase fee
UPDATE "orders"
SET "ship_web_included_in_payment" = TRUE
WHERE "order_type" = 'DAU_GIA'
  AND "status" IN ('CHO_NHAP_KHO_NN', 'CHO_THANH_TOAN_DAU_GIA', 'DA_GIAO')
  AND EXISTS (
    SELECT 1 FROM "order_links" ol
    WHERE ol."order_id" = "orders"."order_id"
      AND ol."ship_web" IS NOT NULL
      AND ol."ship_web" > 0
  );
