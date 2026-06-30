-- Migrate invoice_image and transfer_image from varchar paths to media table FK IDs

ALTER TABLE "expense_request" ADD COLUMN "invoice_image_id" BIGINT;
ALTER TABLE "expense_request" ADD COLUMN "transfer_image_id" BIGINT;

ALTER TABLE "expense_request"
  ADD CONSTRAINT "fk_expense_request_invoice_image_id"
  FOREIGN KEY ("invoice_image_id") REFERENCES "media"("id") ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE "expense_request"
  ADD CONSTRAINT "fk_expense_request_transfer_image_id"
  FOREIGN KEY ("transfer_image_id") REFERENCES "media"("id") ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE "expense_request" DROP COLUMN IF EXISTS "invoice_image";
ALTER TABLE "expense_request" DROP COLUMN IF EXISTS "transfer_image";
