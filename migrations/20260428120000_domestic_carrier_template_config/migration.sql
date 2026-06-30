DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_type
    WHERE typname = 'DomesticCarrierTemplateType'
  ) THEN
    CREATE TYPE "DomesticCarrierTemplateType" AS ENUM ('EXPORT_EXCEL', 'IMPORT_TRACKING');
  END IF;
END $$;

CREATE TABLE IF NOT EXISTS "domestic_carrier_template" (
  "template_id" BIGSERIAL NOT NULL,
  "carrier_id" BIGINT NOT NULL,
  "template_type" "DomesticCarrierTemplateType" NOT NULL,
  "template_name" VARCHAR(255) NOT NULL,
  "sheet_name" VARCHAR(255),
  "start_row" INTEGER NOT NULL DEFAULT 2,
  "header_row_count" INTEGER NOT NULL DEFAULT 1,
  "template_config" JSONB NOT NULL,
  "is_active" BOOLEAN NOT NULL DEFAULT true,
  "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "updated_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT "domestic_carrier_template_pkey" PRIMARY KEY ("template_id"),
  CONSTRAINT "domestic_carrier_template_carrier_id_fkey"
    FOREIGN KEY ("carrier_id")
    REFERENCES "domestic_carrier"("carrier_id")
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

CREATE INDEX IF NOT EXISTS "idx_dct_carrier_id"
  ON "domestic_carrier_template"("carrier_id");

CREATE INDEX IF NOT EXISTS "idx_dct_template_type"
  ON "domestic_carrier_template"("template_type");

CREATE INDEX IF NOT EXISTS "idx_dct_carrier_type"
  ON "domestic_carrier_template"("carrier_id", "template_type");

CREATE UNIQUE INDEX IF NOT EXISTS "uq_dct_active_carrier_type"
  ON "domestic_carrier_template"("carrier_id", "template_type")
  WHERE "is_active" = true;

INSERT INTO "domestic_carrier_template" (
  "carrier_id",
  "template_type",
  "template_name",
  "sheet_name",
  "start_row",
  "header_row_count",
  "template_config",
  "is_active",
  "created_at",
  "updated_at"
)
SELECT
  dc."carrier_id",
  'EXPORT_EXCEL'::"DomesticCarrierTemplateType",
  'VNPOST Export Default',
  'VNPOST Invoicing',
  2,
  1,
  $$
  {
    "fileNamePrefix": "File_In_Ma_VNPOST_Ngay",
    "columns": [
      { "header": "STT", "key": "stt", "width": 8, "sourceField": "stt" },
      { "header": "Mã ship", "key": "shipCode", "width": 16, "sourceField": "shipCode" },
      { "header": "Tên Khách Hàng", "key": "customerName", "width": 28, "sourceField": "customerName" },
      { "header": "SĐT", "key": "phoneNumber", "width": 16, "sourceField": "phoneNumber" },
      { "header": "Địa Chỉ", "key": "address", "width": 45, "sourceField": "address" },
      { "header": "Tên Sale", "key": "staffName", "width": 24, "sourceField": "staffName" },
      { "header": "Danh sách mã", "key": "shippingCodes", "width": 60, "sourceField": "shippingCodes" },
      { "header": "Trọng Lượng", "key": "weight", "width": 14, "sourceField": "weight" },
      { "header": "Mã vận đơn", "key": "trackingCode", "width": 24, "sourceField": "trackingCode" },
      { "header": "Số mã", "key": "shipmentCount", "width": 10, "sourceField": "shipmentCount" }
    ]
  }
  $$::jsonb,
  true,
  CURRENT_TIMESTAMP,
  CURRENT_TIMESTAMP
FROM "domestic_carrier" dc
WHERE dc."carrier_code" = 'VNPOST'
  AND NOT EXISTS (
    SELECT 1
    FROM "domestic_carrier_template" t
    WHERE t."carrier_id" = dc."carrier_id"
      AND t."template_type" = 'EXPORT_EXCEL'::"DomesticCarrierTemplateType"
      AND t."is_active" = true
  );

INSERT INTO "domestic_carrier_template" (
  "carrier_id",
  "template_type",
  "template_name",
  "sheet_name",
  "start_row",
  "header_row_count",
  "template_config",
  "is_active",
  "created_at",
  "updated_at"
)
SELECT
  dc."carrier_id",
  'EXPORT_EXCEL'::"DomesticCarrierTemplateType",
  'JT Export Default',
  'Danh sách',
  2,
  1,
  $$
  {
    "fileNamePrefix": "File_Xuat_JT_Ngay",
    "columns": [
      { "header": "STT", "key": "stt", "width": 8, "sourceField": "stt" },
      { "header": "Mã Đơn Khách Hàng", "key": "shipCode", "width": 22, "sourceField": "shipCode" },
      { "header": "Tên Người Nhận (*)", "key": "customerName", "width": 28, "sourceField": "customerName" },
      { "header": "Số ĐT Người Nhận (*)", "key": "phoneNumber", "width": 20, "sourceField": "phoneNumber" },
      { "header": "Địa Chỉ Người Nhận (*)", "key": "address", "width": 45, "sourceField": "address" },
      { "header": "Tên Hàng Hóa (*)", "key": "productName", "width": 24, "sourceField": "productName" },
      { "header": "Trọng lượng (gram)  (*)", "key": "weight", "width": 20, "sourceField": "weightGram" },
      { "header": "Tiền hàng (*)", "key": "codAmount", "width": 16, "sourceField": "codAmount" },
      { "header": "Giá trị hàng (VND) (*)", "key": "goodsValue", "width": 20, "sourceField": "goodsValue" },
      { "header": "Ghi Chú Giao Hàng", "key": "deliveryNote", "width": 40, "sourceField": "deliveryNote" },
      { "header": "           Cấu hình giao hàng\n1 = Cho xem hàng nhưng không cho thử\n2 = Cho thử hàng\n3 = Không cho xem hàng\nMặc định là 1", "key": "deliveryConfig", "width": 36, "sourceField": "deliveryConfig" },
      { "header": "           Cấu hình thu hộ\n0 -  Thu hộ = Tiền hàng\n1 - Thu hộ = Tiền hàng + Phí\nMặc định là 0", "key": "codConfig", "width": 32, "sourceField": "codConfig" },
      { "header": "Dài \n(Cm)", "key": "lengthCm", "width": 12, "sourceField": "lengthCm" },
      { "header": "Rộng\n(Cm)", "key": "widthCm", "width": 12, "sourceField": "widthCm" },
      { "header": "Cao\n(Cm)", "key": "heightCm", "width": 12, "sourceField": "heightCm" }
    ]
  }
  $$::jsonb,
  true,
  CURRENT_TIMESTAMP,
  CURRENT_TIMESTAMP
FROM "domestic_carrier" dc
WHERE dc."carrier_code" = 'JT'
  AND NOT EXISTS (
    SELECT 1
    FROM "domestic_carrier_template" t
    WHERE t."carrier_id" = dc."carrier_id"
      AND t."template_type" = 'EXPORT_EXCEL'::"DomesticCarrierTemplateType"
      AND t."is_active" = true
  );

INSERT INTO "domestic_carrier_template" (
  "carrier_id",
  "template_type",
  "template_name",
  "sheet_name",
  "start_row",
  "header_row_count",
  "template_config",
  "is_active",
  "created_at",
  "updated_at"
)
SELECT
  dc."carrier_id",
  'IMPORT_TRACKING'::"DomesticCarrierTemplateType",
  'VNPOST Import Default',
  NULL,
  2,
  2,
  $$
  {
    "layouts": [
      {
        "mode": "direct",
        "startRow": 2,
        "shipCodeAliases": ["shipcode"],
        "trackingAliases": ["sohieubuugui", "vnposttrackingcode"],
        "extractRule": "none",
        "trackingCodeLabel": "vnPostTrackingCode"
      },
      {
        "mode": "direct",
        "startRow": 2,
        "shipCodeAliases": ["hovaten"],
        "trackingAliases": ["sohieubuugui", "vnposttrackingcode"],
        "extractRule": "recipient_ship_code",
        "trackingCodeLabel": "Số hiệu bưu gửi"
      },
      {
        "mode": "grouped",
        "startRow": 3,
        "shipCodeAliases": ["thongtinnguoinhanhovaten"],
        "trackingAliases": ["sohieubuugui", "vnposttrackingcode"],
        "extractRule": "recipient_ship_code",
        "trackingCodeLabel": "Số hiệu bưu gửi"
      }
    ]
  }
  $$::jsonb,
  true,
  CURRENT_TIMESTAMP,
  CURRENT_TIMESTAMP
FROM "domestic_carrier" dc
WHERE dc."carrier_code" = 'VNPOST'
  AND NOT EXISTS (
    SELECT 1
    FROM "domestic_carrier_template" t
    WHERE t."carrier_id" = dc."carrier_id"
      AND t."template_type" = 'IMPORT_TRACKING'::"DomesticCarrierTemplateType"
      AND t."is_active" = true
  );

INSERT INTO "domestic_carrier_template" (
  "carrier_id",
  "template_type",
  "template_name",
  "sheet_name",
  "start_row",
  "header_row_count",
  "template_config",
  "is_active",
  "created_at",
  "updated_at"
)
SELECT
  dc."carrier_id",
  'IMPORT_TRACKING'::"DomesticCarrierTemplateType",
  'JT Import Default',
  NULL,
  2,
  2,
  $$
  {
    "layouts": [
      {
        "mode": "direct",
        "startRow": 2,
        "shipCodeAliases": ["madonkhachhang", "shipcode"],
        "trackingAliases": ["madoitac"],
        "extractRule": "none",
        "trackingCodeLabel": "Mã Đối Tác"
      },
      {
        "mode": "grouped",
        "startRow": 3,
        "shipCodeAliases": ["madonkhachhang", "shipcode"],
        "trackingAliases": ["madoitac"],
        "extractRule": "none",
        "trackingCodeLabel": "Mã Đối Tác"
      }
    ]
  }
  $$::jsonb,
  true,
  CURRENT_TIMESTAMP,
  CURRENT_TIMESTAMP
FROM "domestic_carrier" dc
WHERE dc."carrier_code" = 'JT'
  AND NOT EXISTS (
    SELECT 1
    FROM "domestic_carrier_template" t
    WHERE t."carrier_id" = dc."carrier_id"
      AND t."template_type" = 'IMPORT_TRACKING'::"DomesticCarrierTemplateType"
      AND t."is_active" = true
  );
