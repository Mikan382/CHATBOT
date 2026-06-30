-- Customer self-pay: payment không gắn staff nào
ALTER TABLE "payment" ALTER COLUMN "staff_id" DROP NOT NULL;
