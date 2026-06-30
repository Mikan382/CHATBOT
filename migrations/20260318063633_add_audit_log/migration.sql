-- CreateTable
CREATE TABLE "system_audit_log" (
    "id" BIGSERIAL NOT NULL,
    "table_name" VARCHAR(100) NOT NULL,
    "record_id" VARCHAR(255) NOT NULL,
    "action" VARCHAR(20) NOT NULL,
    "old_values" JSONB,
    "new_values" JSONB,
    "actor_id" BIGINT,
    "actor_role" VARCHAR(50),
    "ip_address" VARCHAR(45),
    "user_agent" TEXT,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "system_audit_log_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE INDEX "idx_audit_table_record" ON "system_audit_log"("table_name", "record_id");

-- CreateIndex
CREATE INDEX "idx_audit_time" ON "system_audit_log"("created_at");
