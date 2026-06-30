-- CreateTable: transactional outbox for relaying events to external services
-- (e.g. customer-profile-service over Pub/Sub tiximax.iam.v1). Additive — new table only.
CREATE TABLE "event_outbox" (
    "id" BIGSERIAL NOT NULL,
    "aggregate_type" VARCHAR(64) NOT NULL,
    "aggregate_id" VARCHAR(64) NOT NULL,
    "event_type" VARCHAR(128) NOT NULL,
    "payload" JSONB NOT NULL,
    "status" VARCHAR(16) NOT NULL DEFAULT 'pending',
    "retry_count" INTEGER NOT NULL DEFAULT 0,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT now(),
    "processed_at" TIMESTAMPTZ(6),

    CONSTRAINT "event_outbox_pkey" PRIMARY KEY ("id")
);

-- CreateIndex: relay polls pending rows oldest-first.
CREATE INDEX "idx_event_outbox_pending" ON "event_outbox"("status", "created_at");
