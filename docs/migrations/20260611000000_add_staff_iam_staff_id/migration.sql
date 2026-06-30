-- staff.iam_staff_id — UUID provisioned từ identity-service SCIM (#191 staff-sync).
-- Additive nullable. BẤT BIẾN: KHÔNG bao giờ NOT NULL (staff cũ chưa provision = NULL).
ALTER TABLE "staff" ADD COLUMN "iam_staff_id" VARCHAR(255);
CREATE INDEX "idx_staff_iam_staff_id" ON "staff"("iam_staff_id");
