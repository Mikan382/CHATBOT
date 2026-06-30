-- Drop bank fields from order_cancel_request (moved to order_refund_request)
ALTER TABLE "order_cancel_request" DROP COLUMN IF EXISTS "bank_name";
ALTER TABLE "order_cancel_request" DROP COLUMN IF EXISTS "bank_account";
ALTER TABLE "order_cancel_request" DROP COLUMN IF EXISTS "bank_holder";

-- CreateEnum
CREATE TYPE "RefundMethod" AS ENUM ('WALLET', 'BANK');

-- CreateEnum
CREATE TYPE "RefundStatus" AS ENUM ('CHO_HOAN_TIEN', 'DA_HOAN_TIEN');

-- CreateTable
CREATE TABLE "order_refund_request" (
    "refund_id"          BIGSERIAL NOT NULL,
    "cancel_request_id"  BIGINT NOT NULL,
    "refund_method"      "RefundMethod" NOT NULL,
    "refund_amount"      DECIMAL(38,2),
    "bank_name"          VARCHAR(255),
    "bank_account"       VARCHAR(50),
    "bank_holder"        VARCHAR(255),
    "manager_note"       VARCHAR(500),
    "evidence_image_id"  BIGINT,
    "manager_id"         BIGINT,
    "status"             "RefundStatus" NOT NULL DEFAULT 'CHO_HOAN_TIEN',
    "created_at"         TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "refunded_at"        TIMESTAMPTZ(6),

    CONSTRAINT "order_refund_request_pkey" PRIMARY KEY ("refund_id")
);

-- CreateIndex
CREATE UNIQUE INDEX "order_refund_request_cancel_request_id_key" ON "order_refund_request"("cancel_request_id");

-- AddForeignKey
ALTER TABLE "order_refund_request" ADD CONSTRAINT "order_refund_request_cancel_request_id_fkey"
    FOREIGN KEY ("cancel_request_id") REFERENCES "order_cancel_request"("request_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_refund_request" ADD CONSTRAINT "order_refund_request_evidence_image_id_fkey"
    FOREIGN KEY ("evidence_image_id") REFERENCES "media"("id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_refund_request" ADD CONSTRAINT "order_refund_request_manager_id_fkey"
    FOREIGN KEY ("manager_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
