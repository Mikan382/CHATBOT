-- AlterTable: link a BE-2 order back to the CRM opportunity / order_draft that
-- spawned it (order bridge #196). Additive, nullable. The UNIQUE index on
-- crm_order_draft_id enforces one BE-2 order per CRM draft — idempotency under
-- at-least-once delivery (Postgres treats NULLs as distinct, so manual orders
-- with NULL crm_order_draft_id are unaffected).
ALTER TABLE "orders" ADD COLUMN "crm_opportunity_id" VARCHAR(64);
ALTER TABLE "orders" ADD COLUMN "crm_order_draft_id" VARCHAR(64);

CREATE UNIQUE INDEX "orders_crm_order_draft_id_key" ON "orders"("crm_order_draft_id");
CREATE INDEX "idx_orders_crm_opportunity_id" ON "orders"("crm_opportunity_id");

-- CreateTable: inbox dedup log for consumed CRM events (order_draft.dispatched).
-- event_id is the stable CloudEvent id from the CRM outbox → unique = idempotent.
CREATE TABLE "crm_inbox_event_log" (
    "id" BIGSERIAL NOT NULL,
    "event_id" VARCHAR(64) NOT NULL,
    "event_type" VARCHAR(128) NOT NULL,
    "order_draft_id" VARCHAR(64),
    "order_id" BIGINT,
    "outcome" VARCHAR(24) NOT NULL,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT now(),

    CONSTRAINT "crm_inbox_event_log_pkey" PRIMARY KEY ("id")
);

CREATE UNIQUE INDEX "crm_inbox_event_log_event_id_key" ON "crm_inbox_event_log"("event_id");
