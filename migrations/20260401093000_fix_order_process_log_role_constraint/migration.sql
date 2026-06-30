-- Ensure AccountRole enum contains all roles used by code before casting log role.
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'HR';
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'ACCOUNTANT';
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'MARKETING';
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'LEAD_MARKETING';
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'STAFF_FINANCE';
ALTER TYPE "AccountRole" ADD VALUE IF NOT EXISTS 'IT';

-- Drop legacy CHECK constraints on order_process_log.role_at_time (old varchar schema).
DO $$
DECLARE
  c RECORD;
BEGIN
  FOR c IN
    SELECT con.conname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
    WHERE nsp.nspname = current_schema()
      AND rel.relname = 'order_process_log'
      AND con.contype = 'c'
      AND pg_get_constraintdef(con.oid) ILIKE '%role_at_time%'
  LOOP
    EXECUTE format(
      'ALTER TABLE %I.%I DROP CONSTRAINT IF EXISTS %I',
      current_schema(),
      'order_process_log',
      c.conname
    );
  END LOOP;
END
$$;

-- Convert role_at_time to AccountRole when DB is still using text/varchar.
DO $$
DECLARE
  account_role_type REGTYPE;
BEGIN
  account_role_type := to_regtype('"AccountRole"');

  IF account_role_type IS NULL THEN
    RAISE NOTICE 'Type "AccountRole" not found, skip converting order_process_log.role_at_time.';
  ELSIF EXISTS (
    SELECT 1
    FROM pg_attribute a
    JOIN pg_class rel ON rel.oid = a.attrelid
    JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
    WHERE nsp.nspname = current_schema()
      AND rel.relname = 'order_process_log'
      AND a.attname = 'role_at_time'
      AND a.attnum > 0
      AND NOT a.attisdropped
      AND a.atttypid <> account_role_type
  ) THEN
    EXECUTE '
      ALTER TABLE "order_process_log"
      ALTER COLUMN "role_at_time" TYPE "AccountRole"
      USING (
        CASE
          WHEN "role_at_time" IS NULL THEN NULL
          WHEN "role_at_time"::text = '''' THEN NULL
          WHEN "role_at_time"::text = ANY (enum_range(NULL::"AccountRole")::text[]) THEN "role_at_time"::text::"AccountRole"
          ELSE NULL
        END
      )
    ';
  END IF;
END
$$;
