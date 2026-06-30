-- đảm bảo enum có role mới
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'STAFF_FINANCE';
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'IT';

-- bỏ check cũ đang chặn role mới
ALTER TABLE "account" DROP CONSTRAINT IF EXISTS "account_role_check";
