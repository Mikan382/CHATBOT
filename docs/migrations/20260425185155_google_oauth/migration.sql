/*
  Warnings:

  - A unique constraint covering the columns `[google_id]` on the table `account` will be added. If there are existing duplicate values, this will fail.

*/
-- DropIndex
DROP INDEX IF EXISTS "order_cancel_request_link_pending_idx";

-- DropIndex
DROP INDEX IF EXISTS "idx_order_links_status_shipment_order";

-- DropIndex
DROP INDEX IF EXISTS "idx_order_process_log_order_action_ts";

-- DropIndex
DROP INDEX IF EXISTS "idx_order_process_log_order_new_status_ts";

-- DropIndex
DROP INDEX IF EXISTS "idx_payment_purpose_status_paid_action";

-- AlterTable
ALTER TABLE "account" ADD COLUMN     "avatar_url" TEXT,
ADD COLUMN     "google_id" VARCHAR(255);

-- CreateIndex
CREATE UNIQUE INDEX "account_google_id_key" ON "account"("google_id");
