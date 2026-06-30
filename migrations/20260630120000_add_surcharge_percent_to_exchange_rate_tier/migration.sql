-- AddColumn: surcharge_percent to route_exchange_rate_tier
-- Phần trăm phụ thu theo giá trị đơn hàng (FE dùng để auto-fill purchaseFee khi tạo đơn)
ALTER TABLE "route_exchange_rate_tier"
  ADD COLUMN "surcharge_percent" DECIMAL(5,2) NOT NULL DEFAULT 0;
