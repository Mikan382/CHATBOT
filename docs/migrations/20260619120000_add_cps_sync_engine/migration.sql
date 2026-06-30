-- Smart customer→CPS sync engine (fingerprint delta + on-demand job).
-- Additive only: a nullable column on the legacy customer table + a new job table.

-- Per-customer fingerprint of the last synced profile (name|email|phone|gender|status|street|province|ward).
-- Lets the delta sweep skip rows whose synced fields are unchanged (monolith has no updated_at).
ALTER TABLE "customer" ADD COLUMN "cps_sync_hash" VARCHAR(64);

-- One row per on-demand sync run; the CRM "Đồng bộ khách hàng" button polls it for progress.
CREATE TABLE "cps_sync_job" (
    "id"          UUID         NOT NULL DEFAULT gen_random_uuid(),
    "mode"        VARCHAR(16)  NOT NULL DEFAULT 'delta',
    "status"      VARCHAR(16)  NOT NULL DEFAULT 'queued',
    "dry_run"     BOOLEAN      NOT NULL DEFAULT false,
    "batch_size"  INTEGER      NOT NULL DEFAULT 500,
    "cursor"      BIGINT,
    "scanned"     INTEGER      NOT NULL DEFAULT 0,
    "changed"     INTEGER      NOT NULL DEFAULT 0,
    "skipped"     INTEGER      NOT NULL DEFAULT 0,
    "failed"      INTEGER      NOT NULL DEFAULT 0,
    "error"       TEXT,
    "created_by"  VARCHAR(64),
    "started_at"  TIMESTAMPTZ(6),
    "finished_at" TIMESTAMPTZ(6),
    "created_at"  TIMESTAMPTZ(6) NOT NULL DEFAULT now(),
    CONSTRAINT "cps_sync_job_pkey" PRIMARY KEY ("id")
);

CREATE INDEX "idx_cps_sync_job_status" ON "cps_sync_job" ("status", "created_at");
