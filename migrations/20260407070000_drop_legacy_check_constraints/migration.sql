-- Drop legacy CHECK constraints that block new enum values.
-- These were created when columns were varchar; now columns use proper enum types,
-- so the check constraints are redundant and must be removed.
ALTER TABLE "order_process_log" DROP CONSTRAINT IF EXISTS "order_process_log_action_check";
ALTER TABLE "orders" DROP CONSTRAINT IF EXISTS "orders_status_check";
