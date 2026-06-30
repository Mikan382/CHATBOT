-- CreateEnum
CREATE TYPE "OrderJobStatus" AS ENUM ('PENDING', 'ACCEPTED');

-- CreateTable
CREATE TABLE "order_job" (
    "id" BIGSERIAL NOT NULL,
    "order_id" BIGINT NOT NULL,
    "current_staff_id" BIGINT NOT NULL,
    "assigned_at" TIMESTAMPTZ(6) NOT NULL,
    "expires_at" TIMESTAMPTZ(6) NOT NULL,
    "tried_staff_ids" BIGINT[] DEFAULT ARRAY[]::BIGINT[],
    "status" "OrderJobStatus" NOT NULL DEFAULT 'PENDING',
    "accepted_at" TIMESTAMPTZ(6),
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMPTZ(6) NOT NULL,

    CONSTRAINT "order_job_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "order_job_order_id_key" ON "order_job"("order_id");

-- CreateIndex
CREATE INDEX "order_job_status_expires_at_idx" ON "order_job"("status", "expires_at");

-- CreateIndex
CREATE INDEX "order_job_current_staff_id_status_idx" ON "order_job"("current_staff_id", "status");

-- AddForeignKey
ALTER TABLE "order_job" ADD CONSTRAINT "order_job_order_id_fkey" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE CASCADE ON UPDATE NO ACTION;
