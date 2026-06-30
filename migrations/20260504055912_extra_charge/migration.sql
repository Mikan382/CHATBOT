-- AlterTable
ALTER TABLE "product_type" ADD COLUMN     "extra_charge" DECIMAL(38,2);

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_customer_id_fkey" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_staff_id_fkey" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_order_id_fkey" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_order_link_id_fkey" FOREIGN KEY ("order_link_id") REFERENCES "order_links"("link_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_partial_shipment_id_fkey" FOREIGN KEY ("partial_shipment_id") REFERENCES "partial_shipment"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_parent_transaction_id_fkey" FOREIGN KEY ("parent_transaction_id") REFERENCES "transaction"("transaction_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_payment_id_fkey" FOREIGN KEY ("payment_id") REFERENCES "payment"("payment_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_evidence_image_id_fkey" FOREIGN KEY ("evidence_image_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction" ADD CONSTRAINT "transaction_mediaId_fkey" FOREIGN KEY ("mediaId") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;
