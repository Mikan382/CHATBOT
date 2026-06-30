-- AlterEnum
ALTER TYPE "MediaFolder" ADD VALUE 'claims';

-- AlterEnum
ALTER TYPE "OrderLogAction" ADD VALUE 'MO_CLAIM';
ALTER TYPE "OrderLogAction" ADD VALUE 'DONG_CLAIM';
ALTER TYPE "OrderLogAction" ADD VALUE 'DA_BOI_THUONG';

-- AlterEnum
ALTER TYPE "PaymentPurpose" ADD VALUE 'PHI_BAO_HIEM';
ALTER TYPE "PaymentPurpose" ADD VALUE 'BOI_THUONG_MAT_HANG';

-- AlterTable: route
ALTER TABLE "route"
  ADD COLUMN "insurance_rate"           DECIMAL(5,2),
  ADD COLUMN "insured_compensation_pct" DECIMAL(5,2),
  ADD COLUMN "no_ins_ship_pct"          DECIMAL(5,2),
  ADD COLUMN "no_ins_max_amount"        DECIMAL(18,2),
  ADD COLUMN "claim_window_days"        INTEGER,
  ADD COLUMN "sla_max_working_days"     INTEGER;

-- AlterTable: orders
ALTER TABLE "orders"
  ADD COLUMN "is_insuranced"                 BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN "declared_value"                DECIMAL(18,2),
  ADD COLUMN "insurance_fee"                 DECIMAL(18,2),
  ADD COLUMN "insurance_payment_id"          BIGINT,
  ADD COLUMN "snap_insurance_rate_pct"       DECIMAL(5,2),
  ADD COLUMN "snap_insured_compensation_pct" DECIMAL(5,2),
  ADD COLUMN "snap_no_ins_ship_pct"          DECIMAL(5,2),
  ADD COLUMN "snap_no_ins_max_amount"        DECIMAL(18,2),
  ADD COLUMN "snap_claim_window_days"        INTEGER,
  ADD COLUMN "sla_max_working_days"          INTEGER,
  ADD COLUMN "reported_lost_at"              TIMESTAMPTZ(6),
  ADD COLUMN "confirmed_lost_at"             TIMESTAMPTZ(6);

-- CreateIndex
CREATE UNIQUE INDEX "orders_insurance_payment_id_key" ON "orders"("insurance_payment_id");

-- AddForeignKey
ALTER TABLE "orders" ADD CONSTRAINT "orders_insurance_payment_id_fkey"
  FOREIGN KEY ("insurance_payment_id") REFERENCES "payment"("payment_id")
  ON DELETE SET NULL ON UPDATE CASCADE;
