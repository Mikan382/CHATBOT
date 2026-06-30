-- #1097: ship_order mang địa chỉ giao riêng (snapshot free-text, không FK), tên
-- sub-carrier tự nhập, và giờ khách hẹn book. Tất cả additive nullable → an toàn.
ALTER TABLE "ship_order" ADD COLUMN "sub_carrier_note" VARCHAR(255);
ALTER TABLE "ship_order" ADD COLUMN "address" VARCHAR(255);
ALTER TABLE "ship_order" ADD COLUMN "phone_number" VARCHAR(255);
ALTER TABLE "ship_order" ADD COLUMN "booking_time" TIMESTAMPTZ(6);
