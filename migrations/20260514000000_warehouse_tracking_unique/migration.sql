-- Before applying this migration, verify no duplicate tracking_code exists:
-- SELECT tracking_code, COUNT(*) FROM warehouse GROUP BY tracking_code HAVING COUNT(*) > 1;

-- CreateIndex (IF NOT EXISTS to handle cases where index was already created manually)
CREATE UNIQUE INDEX IF NOT EXISTS "uk_warehouse_tracking_code" ON "warehouse"("tracking_code");
