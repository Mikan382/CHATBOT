-- AlterTable
ALTER TABLE "order_process_log" ADD COLUMN     "link_id" BIGINT,
ADD COLUMN     "new_link_status" "OrderStatus",
ADD COLUMN     "new_status" "OrderMainStatus",
ADD COLUMN     "old_link_status" "OrderStatus",
ADD COLUMN     "old_status" "OrderMainStatus";

-- AddForeignKey
ALTER TABLE "order_process_log" ADD CONSTRAINT "fk_opl_order_links" FOREIGN KEY ("link_id") REFERENCES "order_links"("link_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
