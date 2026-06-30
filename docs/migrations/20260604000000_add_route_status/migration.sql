-- CreateEnum: trạng thái hoạt động của route. ACTIVE mặc định; INACTIVE dùng để
-- ngưng hoạt động tuyến con (operational route) — issue #1060. Additive.
CREATE TYPE "RouteStatus" AS ENUM ('ACTIVE', 'INACTIVE');

-- AlterTable: thêm cột status NOT NULL DEFAULT 'ACTIVE'.
-- Backfill: toàn bộ route hiện có (cha + con) mặc định 'ACTIVE'.
ALTER TABLE "route" ADD COLUMN "status" "RouteStatus" NOT NULL DEFAULT 'ACTIVE';
