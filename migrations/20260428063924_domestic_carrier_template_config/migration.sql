DO $$
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'public'
      AND table_name = 'domestic_carrier_template'
  ) THEN
    DROP INDEX IF EXISTS "uq_dct_active_carrier_type";

    ALTER TABLE "domestic_carrier_template"
      ALTER COLUMN "updated_at" DROP DEFAULT;
  END IF;
END $$;
