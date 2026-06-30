-- Add company attribution as an analytical dimension for CRM/dashboard reads.
-- This is not an authorization boundary.
ALTER TABLE "orders" ADD COLUMN "company_id" UUID;

CREATE INDEX "idx_orders_company_id" ON "orders"("company_id");
