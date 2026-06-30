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
ALTER TABLE "orders" ALTER COLUMN "staff_id" DROP NOT NULL;
