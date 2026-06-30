-- Drop legacy CHECK constraint on expense_request.payment_method.
-- This was created by Spring Boot/Hibernate and only allows the original values (TIEN_MAT, CHUYEN_KHOAN).
-- The column now uses the PaymentMethod enum type which enforces valid values directly.
ALTER TABLE "expense_request" DROP CONSTRAINT IF EXISTS "expense_request_payment_method_check";
