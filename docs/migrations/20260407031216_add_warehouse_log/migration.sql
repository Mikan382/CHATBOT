-- CreateTable
CREATE TABLE "warehouse_log" (
    "log_id" BIGSERIAL NOT NULL,
    "warehouse_id" BIGINT,
    "staff_id" BIGINT,
    "link_id" BIGINT,
    "action" VARCHAR(255),
    "old_value" JSONB,
    "new_value" JSONB,
    "note" TEXT,
    "timestamp" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "warehouse_log_pkey" PRIMARY KEY ("log_id")
);

-- AddForeignKey
ALTER TABLE "warehouse_log" ADD CONSTRAINT "fk_wl_warehouse" FOREIGN KEY ("warehouse_id") REFERENCES "warehouse"("warehouse_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse_log" ADD CONSTRAINT "fk_wl_staff" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "warehouse_log" ADD CONSTRAINT "fk_wl_order_links" FOREIGN KEY ("link_id") REFERENCES "order_links"("link_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
