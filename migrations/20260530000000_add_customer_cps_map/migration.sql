-- AlterTable: add 1-1 map columns linking BE-2 customer to customer-profile-service (CPS) Customer.id (UUID).
-- Additive-only: both columns NULLABLE, no default constraint. Metadata-only change on Postgres
-- (no table rewrite, no long lock). Existing customers keep NULL. Never promote to NOT NULL —
-- legacy customers may have no CPS counterpart. See MIGRATION_PLAN--customer-engagement-sync.md.
ALTER TABLE "customer" ADD COLUMN "cps_customer_id" VARCHAR(64);
ALTER TABLE "customer" ADD COLUMN "cps_synced_at" TIMESTAMPTZ(6);

-- CreateIndex: reverse-sync consumer looks up customer by cps_customer_id when CPS emits
-- customer.profile.created.
CREATE INDEX "idx_customer_cps_customer_id" ON "customer"("cps_customer_id");
