-- Rename CHO_PURCHASER → CHO_DUYET_PURCHASER trong ExpenseRequestStatus
-- Postgres không hỗ trợ ALTER TYPE RENAME VALUE trực tiếp (cần recreate type)

-- Bước 1: Đổi tên type cũ
ALTER TYPE "ExpenseRequestStatus" RENAME TO "ExpenseRequestStatus_old";

-- Bước 2: Tạo type mới với tên đúng
CREATE TYPE "ExpenseRequestStatus" AS ENUM (
  'CHO_DUYET',
  'CHO_DUYET_PURCHASER',
  'TU_CHOI_PURCHASER',
  'DA_DUYET',
  'TU_CHOI',
  'DA_HUY'
);

-- Bước 3: Migrate dữ liệu trên cột status
ALTER TABLE "expense_request"
  ALTER COLUMN "status" TYPE "ExpenseRequestStatus"
  USING CASE status::text
    WHEN 'CHO_PURCHASER' THEN 'CHO_DUYET_PURCHASER'::"ExpenseRequestStatus"
    ELSE status::text::"ExpenseRequestStatus"
  END;

-- Bước 4: Xóa type cũ
DROP TYPE "ExpenseRequestStatus_old";

-- Add unique constraint on source_key (prevent duplicate expense requests)
-- NULL values are safe: Postgres treats each NULL as distinct for unique constraints
ALTER TABLE "expense_request"
  ADD CONSTRAINT "uk_expense_request_source_key" UNIQUE ("source_key");
