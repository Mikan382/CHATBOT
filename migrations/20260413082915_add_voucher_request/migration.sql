-- CreateEnum
CREATE TYPE "VoucherRequestStatus" AS ENUM ('CHO_DUYET', 'DA_DUYET', 'TU_CHOI', 'DA_HUY');

-- CreateTable
CREATE TABLE "voucher_request" (
    "voucher_request_id" BIGSERIAL NOT NULL,
    "requester_id" BIGINT NOT NULL,
    "reviewer_id" BIGINT,
    "customer_id" BIGINT NOT NULL,
    "customer_voucher_id" BIGINT,
    "voucher_type" "VoucherType" NOT NULL,
    "value" DECIMAL(38,2) NOT NULL,
    "min_order_value" DECIMAL(38,2),
    "end_date" TIMESTAMPTZ(6),
    "request_reason" VARCHAR(500) NOT NULL,
    "reject_reason" VARCHAR(500),
    "status" "VoucherRequestStatus" NOT NULL DEFAULT 'CHO_DUYET',
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "reviewed_at" TIMESTAMPTZ(6),

    CONSTRAINT "voucher_request_pkey" PRIMARY KEY ("voucher_request_id")
);

-- CreateIndex
CREATE UNIQUE INDEX "voucher_request_customer_voucher_id_key" ON "voucher_request"("customer_voucher_id");

-- AddForeignKey
ALTER TABLE "voucher_request" ADD CONSTRAINT "voucher_request_requester_id_fkey" FOREIGN KEY ("requester_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "voucher_request" ADD CONSTRAINT "voucher_request_reviewer_id_fkey" FOREIGN KEY ("reviewer_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "voucher_request" ADD CONSTRAINT "voucher_request_customer_id_fkey" FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "voucher_request" ADD CONSTRAINT "voucher_request_customer_voucher_id_fkey" FOREIGN KEY ("customer_voucher_id") REFERENCES "customer_voucher"("customer_voucher_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
