-- #1098: snapshot/payload tuỳ action (vd snapshotBefore + changedFields + changeReason khi sửa ship order)
ALTER TABLE "order_process_log" ADD COLUMN "metadata" JSONB;
