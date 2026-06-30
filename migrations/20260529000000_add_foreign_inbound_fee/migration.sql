-- Phí vận chuyển nội địa nước ngoài (nhà khách -> kho đầu NN), nguyên tệ
ALTER TABLE "warehouse"
  ADD COLUMN "foreign_inbound_fee" DECIMAL(38,2),
  ADD COLUMN "foreign_inbound_fee_currency" VARCHAR(10);

-- Snapshot phí ngoại tệ tại thời điểm tạo partial-shipment (tỉ giá #3)
ALTER TABLE "partial_shipment"
  ADD COLUMN "foreign_fee_amount" DECIMAL(38,2),
  ADD COLUMN "foreign_fee_exchange_rate" DECIMAL(38,2),
  ADD COLUMN "foreign_fee_vnd" DECIMAL(38,2);
