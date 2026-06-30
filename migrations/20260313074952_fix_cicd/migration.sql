/*
  Warnings:

  - You are about to drop the column `purchase_image` on the `order_links` table. All the data in the column will be lost.
  - You are about to drop the column `image_check` on the `orders` table. All the data in the column will be lost.
  - You are about to drop the column `image` on the `warehouse` table. All the data in the column will be lost.
  - You are about to drop the column `image_check` on the `warehouse` table. All the data in the column will be lost.

*/
-- AlterTable
ALTER TABLE "auto_payment" ALTER COLUMN "payment_purpose" DROP NOT NULL;

-- AlterTable
ALTER TABLE "draft_domestic" ALTER COLUMN "carrier" DROP NOT NULL,
ALTER COLUMN "status" DROP NOT NULL;

-- AlterTable
ALTER TABLE "expense_request" ALTER COLUMN "payment_method" DROP NOT NULL,
ALTER COLUMN "status" DROP NOT NULL,
ALTER COLUMN "vat_status" DROP NOT NULL;

-- AlterTable
ALTER TABLE "order_links" DROP COLUMN IF EXISTS "purchase_image";

-- AlterTable
ALTER TABLE "orders" DROP COLUMN IF EXISTS "image_check";

-- AlterTable
ALTER TABLE "voucher" ALTER COLUMN "assign_type" DROP NOT NULL,
ALTER COLUMN "type" DROP NOT NULL;

-- AlterTable
ALTER TABLE "warehouse" DROP COLUMN IF EXISTS "image",
DROP COLUMN IF EXISTS "image_check";
