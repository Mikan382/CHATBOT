-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_customer_id_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_evidence_image_id_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_mediaId_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_order_id_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_order_link_id_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_parent_transaction_id_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_partial_shipment_id_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_payment_id_fkey";

-- DropForeignKey
ALTER TABLE "transaction" DROP CONSTRAINT "transaction_staff_id_fkey";

-- RenameForeignKey
ALTER TABLE "partial_shipment" RENAME CONSTRAINT "fk8l44qb1ay7rh29pugcb6tp41c" TO "fk8r44qb1ay7rh29pugcb6tp41c";
