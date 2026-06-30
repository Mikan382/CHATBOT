-- #1133-followup: lưu người NHẬN thùng packing tại kho VN (bước POST /domestics/received).
-- Trước đây dấu vết người nhận chỉ nằm gián tiếp ở domestic(status=NHAN_HANG).staff_id qua
-- packing.domestic_id — write-only, không expose ra API. Lưu thẳng lên packing để truy vết
-- per-thùng bền vững, độc lập bảng domestic đang quá tải (xem #1165).

-- AlterTable
ALTER TABLE "packing" ADD COLUMN "received_by_staff_id" BIGINT;
ALTER TABLE "packing" ADD COLUMN "received_at" TIMESTAMPTZ(6);

-- AddForeignKey (nullable — thùng chưa nhận = NULL)
ALTER TABLE "packing" ADD CONSTRAINT "fk_packing_received_by_staff" FOREIGN KEY ("received_by_staff_id") REFERENCES "staff"("account_id") ON DELETE NO ACTION ON UPDATE NO ACTION;

-- CreateIndex (báo cáo "staff × thùng × ngày")
CREATE INDEX "idx_packing_received_by_staff_id" ON "packing"("received_by_staff_id");

-- Backfill từ domestic NHAN_HANG hiện có. packing.domestic_id chỉ được set bởi
-- createDomesticForWarehousing (luôn status NHAN_HANG), nên join này an toàn; guard status
-- để chắc chắn không lấy nhầm domestic loại khác.
UPDATE "packing" p
SET "received_by_staff_id" = d."staff_id",
    "received_at" = d."timestamp"
FROM "domestic" d
WHERE p."domestic_id" = d."domestic_id"
  AND d."status" = 'NHAN_HANG'
  AND p."received_by_staff_id" IS NULL;
