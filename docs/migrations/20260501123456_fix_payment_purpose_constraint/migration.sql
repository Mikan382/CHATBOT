-- Update payment purpose check constraint to include THANH_TOAN_SAU_DAU_GIA and ensure alignment with PaymentPurpose enum.
ALTER TABLE "payment" DROP CONSTRAINT IF EXISTS "chk_payment_purpose";

ALTER TABLE "payment"
  ADD CONSTRAINT "chk_payment_purpose"
  CHECK (
    "purpose" IS NULL
    OR ("purpose")::text = ANY (
      ARRAY[
        'THANH_TOAN_DON_HANG'::text,
        'THANH_TOAN_VAN_CHUYEN'::text,
        'PHI_BAO_HIEM'::text,
        'BOI_THUONG_MAT_HANG'::text,
        'HOAN_TIEN_GIAO_THIEU'::text,
        'THANH_TOAN_SAU_DAU_GIA'::text
      ]
    )
  );
