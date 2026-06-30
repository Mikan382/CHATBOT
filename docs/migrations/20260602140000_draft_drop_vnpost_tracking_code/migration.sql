-- Contract phase: drop draft_domestic.vnpost_tracking_code (đã thay bằng carrier_tracking_code).
--
-- ⚠️ THỨ TỰ DEPLOY BẮT BUỘC: chỉ chạy migration NÀY *SAU KHI* toàn bộ instance BE đã deploy
-- code mới (không còn đọc/ghi cột này). Nếu chạy sớm trên DB chung, code cũ còn chạy sẽ lỗi
-- "column does not exist". Đây là bước contract của expand/contract.
--
-- LƯU Ý: chỉ drop trên `draft_domestic`. Cột cùng tên trên model `domestic` là FIELD KHÁC
-- (mã đối soát VNPost) — KHÔNG đụng tới.
ALTER TABLE "draft_domestic" DROP COLUMN "vnpost_tracking_code";
