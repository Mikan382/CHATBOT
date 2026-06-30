-- AlterTable
ALTER TABLE "route_exchange_rate"
ADD COLUMN "created_at" TIMESTAMPTZ(6) DEFAULT CURRENT_TIMESTAMP,
ADD COLUMN "created_by" BIGINT;

-- AddForeignKey
ALTER TABLE "route_exchange_rate"
ADD CONSTRAINT "route_exchange_rate_created_by_fkey"
FOREIGN KEY ("created_by") REFERENCES "staff"("account_id")
ON DELETE NO ACTION ON UPDATE NO ACTION;
