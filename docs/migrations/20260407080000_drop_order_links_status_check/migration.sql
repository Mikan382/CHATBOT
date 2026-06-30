-- Drop legacy CHECK constraint on order_links.status
-- Column uses OrderStatus enum type; constraint blocks new enum values like DANG_DAU_GIA.
ALTER TABLE "order_links" DROP CONSTRAINT IF EXISTS "order_links_status_check";
