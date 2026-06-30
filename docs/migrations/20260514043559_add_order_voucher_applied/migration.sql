-- CreateTable
CREATE TABLE "order_voucher_applied" (
    "id" BIGSERIAL NOT NULL,
    "order_id" BIGINT NOT NULL,
    "payment_id" BIGINT NOT NULL,
    "voucher_id" BIGINT NOT NULL,
    "discount_amount" DECIMAL(38,2) NOT NULL,
    "applied_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "applied_by" BIGINT NOT NULL,

    CONSTRAINT "order_voucher_applied_pkey" PRIMARY KEY ("id")
);

-- AddForeignKey
ALTER TABLE "order_voucher_applied" ADD CONSTRAINT "order_voucher_applied_order_id_fkey" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_voucher_applied" ADD CONSTRAINT "order_voucher_applied_payment_id_fkey" FOREIGN KEY ("payment_id") REFERENCES "payment"("payment_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_voucher_applied" ADD CONSTRAINT "order_voucher_applied_voucher_id_fkey" FOREIGN KEY ("voucher_id") REFERENCES "voucher"("voucher_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_voucher_applied" ADD CONSTRAINT "order_voucher_applied_applied_by_fkey" FOREIGN KEY ("applied_by") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
