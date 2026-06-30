-- Step 1: Ensure UUID support
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Step 2: Add temporary UUID columns
ALTER TABLE "transaction" ADD COLUMN "new_id" UUID DEFAULT gen_random_uuid();
ALTER TABLE "transaction" ADD COLUMN "new_parent_id" UUID;

-- Step 3: Migrate self-referencing relationship (Parent Transaction Lineage)
UPDATE "transaction" t1
SET "new_parent_id" = t2."new_id"
FROM "transaction" t2
WHERE t1."parent_transaction_id" = t2."transaction_id";

-- Step 4: Handle foreign key constraints and primary key
-- We drop the primary key constraint to allow column replacement
ALTER TABLE "transaction" DROP CONSTRAINT IF EXISTS "transaction_pkey" CASCADE;

-- Step 5: Replace old BigInt columns with new UUID columns
ALTER TABLE "transaction" DROP COLUMN "transaction_id";
ALTER TABLE "transaction" RENAME COLUMN "new_id" TO "transaction_id";

ALTER TABLE "transaction" DROP COLUMN "parent_transaction_id";
ALTER TABLE "transaction" RENAME COLUMN "new_parent_id" TO "parent_transaction_id";

-- Step 6: Set constraints and types correctly
ALTER TABLE "transaction" ALTER COLUMN "transaction_id" SET NOT NULL;
ALTER TABLE "transaction" ADD PRIMARY KEY ("transaction_id");

-- Note: Transaction types in Prisma for String @id usually map to UUID in PG if handled correctly, 
-- but here we explicitly cast if needed or let Prisma handle the String mapping.
ALTER TABLE "transaction" ALTER COLUMN "transaction_id" TYPE UUID USING "transaction_id"::UUID;
ALTER TABLE "transaction" ALTER COLUMN "parent_transaction_id" TYPE UUID USING "parent_transaction_id"::UUID;
