-- AlterTable: thêm cancel_reason vào orders
-- Dùng cho đơn portal bị STAFF_CS từ chối (CHO_XAC_NHAN → DA_HUY)
ALTER TABLE "orders" ADD COLUMN "cancel_reason" VARCHAR(500);
