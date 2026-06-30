-- Backfill legacy order_links rows after adding delivered_quantity.
-- Keep idempotent: only fill rows that are still NULL.
UPDATE "order_links"
SET "delivered_quantity" = "quantity"::BIGINT
WHERE "delivered_quantity" IS NULL;
