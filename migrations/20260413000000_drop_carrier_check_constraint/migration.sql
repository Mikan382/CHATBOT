-- Drop manually-added CHECK constraints that block the J&T carrier enum value.
-- These constraints were created outside Prisma migrations and only allowed
-- VNPOST and OTHER. The DraftDomesticCarrier enum was extended with J&T
-- (migration 20260409024527_jandt), but these constraints were never updated.
-- The enum type itself already enforces valid values, so the CHECK constraints
-- are redundant and must be removed.
ALTER TABLE "draft_domestic" DROP CONSTRAINT IF EXISTS "chk_draft_domestic_carrier";
ALTER TABLE "domestic"       DROP CONSTRAINT IF EXISTS "chk_domestic_carrier";
