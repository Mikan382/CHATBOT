-- CreateEnum
CREATE TYPE "CancelRequestStatus" AS ENUM ('CHO_XU_LY', 'DA_DUYET', 'TU_CHOI');

-- AlterEnum
-- This migration adds more than one value to an enum.
-- With PostgreSQL versions 11 and earlier, this is not possible
-- in a single migration. This can be worked around by creating
-- multiple migrations, each migration adding only one value to
-- the enum.


ALTER TYPE "OrderLogAction" ADD VALUE 'YEU_CAU_HUY';
ALTER TYPE "OrderLogAction" ADD VALUE 'DUYET_YEU_CAU_HUY';
ALTER TYPE "OrderLogAction" ADD VALUE 'TU_CHOI_YEU_CAU_HUY';

-- AlterEnum
ALTER TYPE "OrderMainStatus" ADD VALUE 'YEU_CAU_HUY';

-- CreateTable
CREATE TABLE "order_cancel_request" (
    "request_id" BIGSERIAL NOT NULL,
    "order_id" BIGINT NOT NULL,
    "requester_id" BIGINT NOT NULL,
    "processor_id" BIGINT,
    "customer_reason" VARCHAR(500) NOT NULL,
    "bank_name" VARCHAR(255),
    "bank_account" VARCHAR(50),
    "bank_holder" VARCHAR(255),
    "note" VARCHAR(500),
    "reject_reason" VARCHAR(500),
    "status" "CancelRequestStatus" NOT NULL DEFAULT 'CHO_XU_LY',
    "previous_status" "OrderMainStatus",
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "processed_at" TIMESTAMPTZ(6),

    CONSTRAINT "order_cancel_request_pkey" PRIMARY KEY ("request_id")
);

-- AddForeignKey
ALTER TABLE "order_cancel_request" ADD CONSTRAINT "order_cancel_request_order_id_fkey" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_cancel_request" ADD CONSTRAINT "order_cancel_request_requester_id_fkey" FOREIGN KEY ("requester_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_cancel_request" ADD CONSTRAINT "order_cancel_request_processor_id_fkey" FOREIGN KEY ("processor_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
