-- AlterTable
ALTER TABLE "domestic" ADD COLUMN     "carrier_id" BIGINT;

-- CreateTable
CREATE TABLE "domestic_carrier" (
    "carrier_id" BIGSERIAL NOT NULL,
    "carrier_name" VARCHAR(255) NOT NULL,
    "carrier_code" VARCHAR(50) NOT NULL,
    "is_active" BOOLEAN NOT NULL DEFAULT true,
    "created_at" TIMESTAMPTZ(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMPTZ(6) NOT NULL,

    CONSTRAINT "domestic_carrier_pkey" PRIMARY KEY ("carrier_id")
);

-- CreateIndex
CREATE UNIQUE INDEX "domestic_carrier_carrier_code_key" ON "domestic_carrier"("carrier_code");

-- AddForeignKey
ALTER TABLE "domestic" ADD CONSTRAINT "domestic_carrier_id_fkey" FOREIGN KEY ("carrier_id") REFERENCES "domestic_carrier"("carrier_id") ON DELETE NO ACTION ON UPDATE NO ACTION;
