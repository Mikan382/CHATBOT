-- AlterTable: human-review gate for full-auto CRM orders (#211). Additive, nullable.
-- NULL = not a CRM-gated order (every existing manual / portal order is unaffected).
-- REVIEW_PENDING = created by the full-auto bridge and held (never auto-active);
-- staff approve → APPROVED (order activates) or REJECTED (order cancelled).
-- English values on purpose: the engagement-sync adapter is the CRM↔BE-2 translation
-- seam, so BE-2's operational status enum (OrderMainStatus, Vietnamese) is left untouched.
ALTER TABLE "orders" ADD COLUMN "crm_review_status" VARCHAR(16);

CREATE INDEX "idx_orders_crm_review_status" ON "orders"("crm_review_status");
