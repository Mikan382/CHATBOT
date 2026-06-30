-- CreateTable
CREATE TABLE "customer_sales" (
    "customer_id" BIGINT NOT NULL,
    "staff_id" BIGINT NOT NULL,
    "assigned_at" TIMESTAMPTZ(6) NOT NULL DEFAULT NOW(),

    CONSTRAINT "customer_sales_pkey" PRIMARY KEY ("customer_id","staff_id")
);

-- CreateIndex
CREATE INDEX "idx_customer_sales_staff_id" ON "customer_sales"("staff_id");

-- Migrate existing data: mỗi customer đang có staff_id → tạo 1 dòng customer_sales
INSERT INTO "customer_sales" ("customer_id", "staff_id")
SELECT "account_id", "staff_id"
FROM "customer"
WHERE "staff_id" IS NOT NULL;

-- AddForeignKey
ALTER TABLE "customer_sales" ADD CONSTRAINT "customer_sales_customer_id_fkey"
    FOREIGN KEY ("customer_id") REFERENCES "customer"("account_id") ON DELETE CASCADE ON UPDATE NO ACTION;

ALTER TABLE "customer_sales" ADD CONSTRAINT "customer_sales_staff_id_fkey"
    FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE CASCADE ON UPDATE NO ACTION;

-- DropColumn (sau khi đã migrate xong data)
ALTER TABLE "customer" DROP COLUMN "staff_id";
