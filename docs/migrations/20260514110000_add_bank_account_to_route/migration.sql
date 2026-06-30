-- AlterTable
ALTER TABLE "route" ADD COLUMN "bank_account_id" BIGINT;

-- AddForeignKey
ALTER TABLE "route" ADD CONSTRAINT "fk_route_bank_account" FOREIGN KEY ("bank_account_id") REFERENCES "bank_account"("id") ON DELETE SET NULL ON UPDATE NO ACTION;
