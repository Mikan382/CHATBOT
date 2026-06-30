-- Drop legacy CHECK constraint on order_process_log.action
-- Column has already been converted to OrderLogAction enum type,
-- so the check constraint is redundant and blocks new enum values.
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
      AND pg_get_constraintdef(con.oid) ILIKE '%action%'
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
