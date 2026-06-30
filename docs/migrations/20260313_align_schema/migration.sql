-- CreateEnum
CREATE TYPE "AccountRole" AS ENUM ('CUSTOMER', 'STAFF_SALE', 'LEAD_SALE', 'STAFF_PURCHASER', 'STAFF_WAREHOUSE_FOREIGN', 'STAFF_WAREHOUSE_DOMESTIC', 'MANAGER', 'ADMIN', 'HR', 'ACCOUNTANT', 'MARKETING', 'LEAD_MARKETING');

-- CreateEnum
CREATE TYPE "AccountStatus" AS ENUM ('HOAT_DONG', 'BI_KHOA');

-- CreateEnum
CREATE TYPE "AssignType" AS ENUM ('THU_CONG', 'DANG_KI_TK', 'DAT_CHI_TIEU');

-- CreateEnum
CREATE TYPE "DomesticStatus" AS ENUM ('NHAN_HANG', 'CHUYEN_KHO', 'SAN_SANG_GIAO', 'DA_GIAO');

-- CreateEnum
CREATE TYPE "DraftDomesticCarrier" AS ENUM ('VNPOST', 'OTHER');

-- CreateEnum
CREATE TYPE "DraftDomesticStatus" AS ENUM ('WAIT_IMPORT', 'DRAFT', 'LOCKED', 'EXPORTED');

-- CreateEnum
CREATE TYPE "ExpenseRequestStatus" AS ENUM ('CHO_DUYET', 'DA_DUYET', 'TU_CHOI', 'DA_HUY');

-- CreateEnum
CREATE TYPE "FlightStatus" AS ENUM ('DANG_CHO', 'HOAN_THANH');

-- CreateEnum
CREATE TYPE "MarketingPosition" AS ENUM ('HOME', 'BLOG', 'PROMOTION');

-- CreateEnum
CREATE TYPE "MediaFolder" AS ENUM ('orders', 'purchases', 'vouchers', 'customers', 'staffs', 'websites', 'warehouses', 'domestics', 'payments', 'misc');

-- CreateEnum
CREATE TYPE "OrderLogAction" AS ENUM ('XAC_NHAN_DON', 'TAO_THANH_TOAN_HANG', 'DA_THANH_TOAN', 'DA_MUA_HANG', 'DAU_GIA_THANH_CONG', 'DA_NHAP_KHO_NN', 'TAO_THANH_TOAN_SHIP', 'DA_THANH_TOAN_SHIP', 'DA_DONG_GOI', 'DA_BAY', 'DA_NHAP_KHO_HN', 'DA_NHAP_KHO_SG', 'DA_GIAO', 'DA_CHINH_SUA', 'DA_XOA', 'HOAN_TIEN', 'DA_HUY');

-- CreateEnum
CREATE TYPE "OrderMainStatus" AS ENUM ('CHO_XAC_NHAN', 'DA_XAC_NHAN', 'CHO_THANH_TOAN', 'CHO_MUA', 'DAU_GIA_THANH_CONG', 'CHO_THANH_TOAN_DAU_GIA', 'CHO_NHAP_KHO_NN', 'CHO_DONG_GOI', 'DANG_XU_LY', 'DA_DU_HANG', 'CHO_THANH_TOAN_SHIP', 'CHO_VAN_CHUYEN_KHO', 'CHO_GIAO', 'DA_GIAO', 'DA_HUY');

-- CreateEnum
CREATE TYPE "OrderStatus" AS ENUM ('CHO_MUA', 'DA_MUA', 'DAU_GIA_THANH_CONG', 'MUA_SAU', 'DA_NHAP_KHO_NN', 'DA_DONG_GOI', 'DANG_CHUYEN_VN', 'CHO_NHAP_KHO_VN', 'DA_NHAP_KHO_VN', 'CHO_TRUNG_CHUYEN', 'CHO_GIAO', 'DANG_GIAO', 'DA_GIAO', 'DA_HUY');

-- CreateEnum
CREATE TYPE "OrderType" AS ENUM ('MUA_HO', 'KY_GUI', 'DAU_GIA', 'CHUYEN_TIEN');

-- CreateEnum
CREATE TYPE "PackingStatus" AS ENUM ('CHO_BAY', 'DA_BAY', 'DA_NHAP_KHO_VN', 'DA_CHUYEN_KHO');

-- CreateEnum
CREATE TYPE "PaymentMethod" AS ENUM ('TIEN_MAT', 'CHUYEN_KHOAN');

-- CreateEnum
CREATE TYPE "PaymentPurpose" AS ENUM ('THANH_TOAN_DON_HANG', 'THANH_TOAN_VAN_CHUYEN');

-- CreateEnum
CREATE TYPE "PaymentStatus" AS ENUM ('CHO_THANH_TOAN', 'DA_THANH_TOAN', 'CHO_THANH_TOAN_SHIP', 'DA_THANH_TOAN_SHIP', 'DA_HOAN_TIEN');

-- CreateEnum
CREATE TYPE "RepackStatus" AS ENUM ('DANG_THUC_HIEN', 'HOAN_THANH', 'DA_HUY');

-- CreateEnum
CREATE TYPE "VatStatus" AS ENUM ('CHUA_VAT', 'CO_VAT');

-- CreateEnum
CREATE TYPE "VoucherType" AS ENUM ('PHAN_TRAM', 'CO_DINH');

-- CreateEnum
CREATE TYPE "WarehouseStatus" AS ENUM ('DA_NHAP_KHO_NN', 'DA_NHAP_KHO_VN', 'CHO_GIAO', 'DA_GIAO', 'DANG_DOI_TRA', 'CHO_XU_LY');

-- DropIndex
DROP INDEX "idx_draft_domestic_carrier";

-- DropIndex
DROP INDEX "idx_draft_domestic_status";

-- DropIndex
DROP INDEX "idx_ol_purchase_status";

-- DropIndex
DROP INDEX "idx_orders_route_status";

-- DropIndex
DROP INDEX "idx_orders_staff_status";

-- DropIndex
DROP INDEX "idx_orders_status";

-- AlterTable
ALTER TABLE "account"
  ALTER COLUMN "created_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "role" TYPE "AccountRole" USING ("role"::text::"AccountRole"),
  ALTER COLUMN "status" TYPE "AccountStatus" USING ("status"::text::"AccountStatus"),
  ALTER COLUMN "status" SET DEFAULT 'HOAT_DONG';

-- AlterTable
ALTER TABLE "auto_payment"
  ALTER COLUMN "created_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "payment_purpose" TYPE "PaymentPurpose" USING ("payment_purpose"::text::"PaymentPurpose");

-- AlterTable
ALTER TABLE "customer_voucher" ALTER COLUMN "assigned_date" SET DATA TYPE TIMESTAMPTZ(6),
ALTER COLUMN "used_date" SET DATA TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "domestic"
  ALTER COLUMN "timestamp" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "status" TYPE "DomesticStatus" USING ("status"::text::"DomesticStatus"),
  ALTER COLUMN "carrier" TYPE "DraftDomesticCarrier" USING ("carrier"::text::"DraftDomesticCarrier");

-- AlterTable
ALTER TABLE "draft_domestic"
  ALTER COLUMN "carrier" TYPE "DraftDomesticCarrier" USING ("carrier"::text::"DraftDomesticCarrier"),
  ALTER COLUMN "status" TYPE "DraftDomesticStatus" USING ("status"::text::"DraftDomesticStatus");

-- AlterTable
ALTER TABLE "draft_domestic_shipping_list" ADD CONSTRAINT "draft_domestic_shipping_list_pkey" PRIMARY KEY ("id");

-- AlterTable
ALTER TABLE "expense_request"
  ALTER COLUMN "created_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "payment_method" TYPE "PaymentMethod" USING ("payment_method"::text::"PaymentMethod"),
  ALTER COLUMN "status" TYPE "ExpenseRequestStatus" USING ("status"::text::"ExpenseRequestStatus"),
  ALTER COLUMN "vat_status" TYPE "VatStatus" USING ("vat_status"::text::"VatStatus");

-- AlterTable   
ALTER TABLE "feedback" ALTER COLUMN "created_at" SET DATA TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "flight_shipment"
  ALTER COLUMN "air_freight_paid_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "arrival_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "created_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "customs_paid_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "status" TYPE "FlightStatus" USING ("status"::text::"FlightStatus"),
  ALTER COLUMN "updated_at" TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "marketing_media"
  ADD COLUMN IF NOT EXISTS "updated_date" TIMESTAMPTZ(6),
  ALTER COLUMN "created_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "end_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "position" TYPE "MarketingPosition" USING ("position"::text::"MarketingPosition"),
  ALTER COLUMN "start_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "status" TYPE "AccountStatus" USING ("status"::text::"AccountStatus"),
  ALTER COLUMN "status" SET DEFAULT 'HOAT_DONG';

-- AlterTable
ALTER TABLE "order_links"
  ADD COLUMN IF NOT EXISTS "different_fee" DECIMAL(38,2),
  ADD COLUMN IF NOT EXISTS "purchase_image_id" BIGINT,
  ALTER COLUMN "status" TYPE "OrderStatus" USING ("status"::text::"OrderStatus");


-- AlterTable
ALTER TABLE "order_process_log"
  ALTER COLUMN "action" TYPE "OrderLogAction" USING ("action"::text::"OrderLogAction"),
  ALTER COLUMN "role_at_time" TYPE "AccountRole" USING ("role_at_time"::text::"AccountRole"),
  ALTER COLUMN "timestamp" TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "orders"
  ALTER COLUMN "created_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "order_type" TYPE "OrderType" USING ("order_type"::text::"OrderType"),
  ALTER COLUMN "pinned_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "status" TYPE "OrderMainStatus" USING ("status"::text::"OrderMainStatus");

-- AlterTable
ALTER TABLE "otp" ALTER COLUMN "expiration" SET DATA TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "packing"
  ALTER COLUMN "packed_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "status" TYPE "PackingStatus" USING ("status"::text::"PackingStatus"),
  ALTER COLUMN "fly_time" TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "partial_shipment"
  ALTER COLUMN "status" TYPE "OrderMainStatus"
  USING ("status"::"OrderMainStatus");

-- AlterTable
ALTER TABLE "payment"
  ALTER COLUMN "action_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "status" TYPE "PaymentStatus" USING ("status"::text::"PaymentStatus"),
  ALTER COLUMN "purpose" TYPE "PaymentPurpose" USING ("purpose"::text::"PaymentPurpose"),
  ALTER COLUMN "purpose" SET DEFAULT 'THANH_TOAN_DON_HANG',
  ALTER COLUMN "paid_time" TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "purchases"
  ADD COLUMN IF NOT EXISTS "invoice_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "purchase_image_id" BIGINT,
  ALTER COLUMN "purchase_time" SET DATA TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "repack" ALTER COLUMN "completed_at" SET DATA TYPE TIMESTAMPTZ(6),
ALTER COLUMN "created_at" SET DATA TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "shipment_tracking" ALTER COLUMN "timestamp" SET DATA TYPE TIMESTAMPTZ(6);

-- AlterTable
ALTER TABLE "voucher"
  ALTER COLUMN "assign_type" TYPE "AssignType" USING ("assign_type"::text::"AssignType"),
  ALTER COLUMN "end_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "start_date" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "type" TYPE "VoucherType" USING ("type"::text::"VoucherType");

-- AlterTable
ALTER TABLE "warehouse"
  ADD COLUMN IF NOT EXISTS "image_check_id" BIGINT,
  ADD COLUMN IF NOT EXISTS "image_id" BIGINT,
  ALTER COLUMN "created_at" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "status" TYPE "WarehouseStatus" USING ("status"::text::"WarehouseStatus"),
  ALTER COLUMN "arrival_time" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "delivery_time" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "dispatch_time" TYPE TIMESTAMPTZ(6),
  ALTER COLUMN "updated_at" TYPE TIMESTAMPTZ(6);

-- CreateTable
CREATE TABLE "media" (
    "id" BIGSERIAL NOT NULL,
    "file_name" VARCHAR(255) NOT NULL,
    "file_path" VARCHAR(255) NOT NULL,
    "file_size" INTEGER NOT NULL,
    "mime_type" VARCHAR(100) NOT NULL,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "uploader_id" BIGINT,
    "folder" "MediaFolder" NOT NULL DEFAULT 'misc',

    CONSTRAINT "media_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "order_check_images" (
    "order_id" BIGINT NOT NULL,
    "media_id" BIGINT NOT NULL,

    CONSTRAINT "order_check_images_pkey" PRIMARY KEY ("order_id","media_id")
);

-- Migrate legacy image columns into media
DROP TABLE IF EXISTS "_purchase_invoice_media_map";
CREATE TABLE "_purchase_invoice_media_map" AS
SELECT
  p."purchase_id",
  nextval(pg_get_serial_sequence('"media"', 'id')) AS "media_id",
  p."invoice" AS "file_path"
FROM "purchases" p
WHERE p."invoice" IS NOT NULL AND BTRIM(p."invoice") <> '';

INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  m."media_id",
  LEFT(COALESCE(NULLIF(REVERSE(SPLIT_PART(REVERSE(REPLACE(m."file_path", '\', '/')), '/', 1)), ''), 'invoice_' || m."purchase_id"), 255),
  LEFT(m."file_path", 255),
  0,
  'application/octet-stream',
  'purchases'
FROM "_purchase_invoice_media_map" m;

UPDATE "purchases" p
SET "invoice_id" = m."media_id"
FROM "_purchase_invoice_media_map" m
WHERE p."purchase_id" = m."purchase_id";

DROP TABLE IF EXISTS "_purchase_image_media_map";
CREATE TABLE "_purchase_image_media_map" AS
SELECT
  p."purchase_id",
  nextval(pg_get_serial_sequence('"media"', 'id')) AS "media_id",
  p."purchase_image" AS "file_path"
FROM "purchases" p
WHERE p."purchase_image" IS NOT NULL AND BTRIM(p."purchase_image") <> '';

INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  m."media_id",
  LEFT(COALESCE(NULLIF(REVERSE(SPLIT_PART(REVERSE(REPLACE(m."file_path", '\', '/')), '/', 1)), ''), 'purchase_' || m."purchase_id"), 255),
  LEFT(m."file_path", 255),
  0,
  'application/octet-stream',
  'purchases'
FROM "_purchase_image_media_map" m;

UPDATE "purchases" p
SET "purchase_image_id" = m."media_id"
FROM "_purchase_image_media_map" m
WHERE p."purchase_id" = m."purchase_id";

DROP TABLE IF EXISTS "_order_link_purchase_image_media_map";
CREATE TABLE "_order_link_purchase_image_media_map" AS
SELECT
  ol."link_id",
  nextval(pg_get_serial_sequence('"media"', 'id')) AS "media_id",
  ol."purchase_image" AS "file_path"
FROM "order_links" ol
WHERE ol."purchase_image" IS NOT NULL AND BTRIM(ol."purchase_image") <> '';

INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  m."media_id",
  LEFT(COALESCE(NULLIF(REVERSE(SPLIT_PART(REVERSE(REPLACE(m."file_path", '\', '/')), '/', 1)), ''), 'order_link_' || m."link_id"), 255),
  LEFT(m."file_path", 255),
  0,
  'application/octet-stream',
  'orders'
FROM "_order_link_purchase_image_media_map" m;

UPDATE "order_links" ol
SET "purchase_image_id" = m."media_id"
FROM "_order_link_purchase_image_media_map" m
WHERE ol."link_id" = m."link_id";

DROP TABLE IF EXISTS "_warehouse_image_media_map";
CREATE TABLE "_warehouse_image_media_map" AS
SELECT
  w."warehouse_id",
  nextval(pg_get_serial_sequence('"media"', 'id')) AS "media_id",
  w."image" AS "file_path"
FROM "warehouse" w
WHERE w."image" IS NOT NULL AND BTRIM(w."image") <> '';

INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  m."media_id",
  LEFT(COALESCE(NULLIF(REVERSE(SPLIT_PART(REVERSE(REPLACE(m."file_path", '\', '/')), '/', 1)), ''), 'warehouse_' || m."warehouse_id"), 255),
  LEFT(m."file_path", 255),
  0,
  'application/octet-stream',
  'warehouses'
FROM "_warehouse_image_media_map" m;

UPDATE "warehouse" w
SET "image_id" = m."media_id"
FROM "_warehouse_image_media_map" m
WHERE w."warehouse_id" = m."warehouse_id";

DROP TABLE IF EXISTS "_warehouse_image_check_media_map";
CREATE TABLE "_warehouse_image_check_media_map" AS
SELECT
  w."warehouse_id",
  nextval(pg_get_serial_sequence('"media"', 'id')) AS "media_id",
  w."image_check" AS "file_path"
FROM "warehouse" w
WHERE w."image_check" IS NOT NULL AND BTRIM(w."image_check") <> '';

INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  m."media_id",
  LEFT(COALESCE(NULLIF(REVERSE(SPLIT_PART(REVERSE(REPLACE(m."file_path", '\', '/')), '/', 1)), ''), 'warehouse_check_' || m."warehouse_id"), 255),
  LEFT(m."file_path", 255),
  0,
  'application/octet-stream',
  'warehouses'
FROM "_warehouse_image_check_media_map" m;

UPDATE "warehouse" w
SET "image_check_id" = m."media_id"
FROM "_warehouse_image_check_media_map" m
WHERE w."warehouse_id" = m."warehouse_id";

DROP TABLE IF EXISTS "_order_check_image_media_map";
CREATE TABLE "_order_check_image_media_map" AS
SELECT
  o."order_id",
  nextval(pg_get_serial_sequence('"media"', 'id')) AS "media_id",
  img."file_path"
FROM "orders" o
CROSS JOIN LATERAL UNNEST(o."image_check") AS img("file_path")
WHERE img."file_path" IS NOT NULL AND BTRIM(img."file_path") <> '';

INSERT INTO "media" ("id", "file_name", "file_path", "file_size", "mime_type", "folder")
SELECT
  m."media_id",
  LEFT(COALESCE(NULLIF(REVERSE(SPLIT_PART(REVERSE(REPLACE(m."file_path", '\', '/')), '/', 1)), ''), 'order_' || m."order_id"), 255),
  LEFT(m."file_path", 255),
  0,
  'application/octet-stream',
  'orders'
FROM "_order_check_image_media_map" m;

INSERT INTO "order_check_images" ("order_id", "media_id")
SELECT m."order_id", m."media_id"
FROM "_order_check_image_media_map" m;

DROP TABLE IF EXISTS "_purchase_invoice_media_map";
DROP TABLE IF EXISTS "_purchase_image_media_map";
DROP TABLE IF EXISTS "_order_link_purchase_image_media_map";
DROP TABLE IF EXISTS "_warehouse_image_media_map";
DROP TABLE IF EXISTS "_warehouse_image_check_media_map";
DROP TABLE IF EXISTS "_order_check_image_media_map";

-- Drop legacy image columns after media migration
ALTER TABLE "purchases"
  DROP COLUMN IF EXISTS "invoice",
  DROP COLUMN IF EXISTS "purchase_image";

ALTER TABLE "order_links"
  DROP COLUMN IF EXISTS "purchase_image";

ALTER TABLE "warehouse"
  DROP COLUMN IF EXISTS "image",
  DROP COLUMN IF EXISTS "image_check";

ALTER TABLE "orders"
  DROP COLUMN IF EXISTS "image_check";

-- CreateTable
CREATE TABLE "refresh_token" (
    "id" BIGSERIAL NOT NULL,
    "token" VARCHAR(255) NOT NULL,
    "account_id" BIGINT NOT NULL,
    "user_agent" VARCHAR(255),
    "ip_address" VARCHAR(45),
    "is_revoked" BOOLEAN NOT NULL DEFAULT false,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "expires_at" TIMESTAMPTZ(6) NOT NULL,

    CONSTRAINT "refresh_token_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "route_weight_rule" (
    "id" BIGSERIAL NOT NULL,
    "route_id" BIGINT NOT NULL,
    "min_weight" DECIMAL(38,2) NOT NULL,
    "max_weight" DECIMAL(38,2),
    "billable_weight" DECIMAL(38,2) NOT NULL,

    CONSTRAINT "route_weight_rule_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "system_sequence" (
    "sequence_type" VARCHAR(50) NOT NULL,
    "current_value" INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT "system_sequence_pkey" PRIMARY KEY ("sequence_type")
);

-- CreateIndex
CREATE UNIQUE INDEX "uk_refresh_token" ON "refresh_token"("token");

-- AddForeignKey
ALTER TABLE "customer" ADD CONSTRAINT "customer_staff_id_fkey" FOREIGN KEY ("staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- AddForeignKey
ALTER TABLE "order_links" ADD CONSTRAINT "order_links_purchase_image_id_fkey" FOREIGN KEY ("purchase_image_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "purchases" ADD CONSTRAINT "purchases_invoice_id_fkey" FOREIGN KEY ("invoice_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "purchases" ADD CONSTRAINT "purchases_purchase_image_id_fkey" FOREIGN KEY ("purchase_image_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "warehouse_image_check_id_fkey" FOREIGN KEY ("image_check_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "warehouse" ADD CONSTRAINT "warehouse_image_id_fkey" FOREIGN KEY ("image_id") REFERENCES "media"("id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "media" ADD CONSTRAINT "media_uploader_id_fkey" FOREIGN KEY ("uploader_id") REFERENCES "account"("account_id") ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "order_check_images" ADD CONSTRAINT "order_check_images_media_id_fkey" FOREIGN KEY ("media_id") REFERENCES "media"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "order_check_images" ADD CONSTRAINT "order_check_images_order_id_fkey" FOREIGN KEY ("order_id") REFERENCES "orders"("order_id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "refresh_token" ADD CONSTRAINT "refresh_token_account_id_fkey" FOREIGN KEY ("account_id") REFERENCES "account"("account_id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "route_weight_rule" ADD CONSTRAINT "route_weight_rule_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route"("route_id") ON DELETE CASCADE ON UPDATE CASCADE;

