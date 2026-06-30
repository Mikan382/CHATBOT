-- Quyết định của sale: ai book hãng (CUSTOMER = khách tự book | STAFF_WAREHOUSE_DOMESTIC = kho nội book).
-- Tách khỏi created_by_role (audit ai tạo). NULL = chưa chỉ định (scan fallback created_by_role).
ALTER TABLE "ship_order" ADD COLUMN "booked_by_role" "AccountRole";
