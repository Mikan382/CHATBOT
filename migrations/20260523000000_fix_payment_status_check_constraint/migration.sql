-- Drop constraint cũ từ Spring Boot era (thiếu CHO_THANH_TOAN_BH, DA_THANH_TOAN_BH, DA_HUY)
ALTER TABLE "payment" DROP CONSTRAINT IF EXISTS "payment_status_check";

-- Recreate với đầy đủ 8 giá trị theo PaymentStatus enum hiện tại
ALTER TABLE "payment" ADD CONSTRAINT "payment_status_check"
  CHECK (("status")::text = ANY (ARRAY[
    'CHO_THANH_TOAN'::character varying,
    'DA_THANH_TOAN'::character varying,
    'CHO_THANH_TOAN_SHIP'::character varying,
    'DA_THANH_TOAN_SHIP'::character varying,
    'DA_HOAN_TIEN'::character varying,
    'CHO_THANH_TOAN_BH'::character varying,
    'DA_THANH_TOAN_BH'::character varying,
    'DA_HUY'::character varying
  ]::text[]));