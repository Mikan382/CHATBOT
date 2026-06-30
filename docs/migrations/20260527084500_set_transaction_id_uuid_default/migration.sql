ALTER TABLE "transaction"
  ALTER COLUMN "transaction_id" SET DEFAULT gen_random_uuid();
