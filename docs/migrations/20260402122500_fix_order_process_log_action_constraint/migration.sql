-- Ensure enum value exists for partial-delivery logs.
ALTER TYPE "OrderLogAction" ADD VALUE IF NOT EXISTS 'GIAO_THIEU_HANG';

-- Drop legacy CHECK constraints for action on older schemas.
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
      AND (
        con.conname ILIKE '%order_process_log_action%'
        OR pg_get_constraintdef(con.oid) ILIKE '%"action"%'
      )
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

-- Convert action column to enum when database is still on varchar/text.
DO $$
DECLARE
  order_log_action_type REGTYPE;
BEGIN
  order_log_action_type := to_regtype('"OrderLogAction"');

  IF order_log_action_type IS NULL THEN
    RAISE NOTICE 'Type "OrderLogAction" not found, skip converting order_process_log.action.';
  ELSIF EXISTS (
    SELECT 1
    FROM pg_attribute a
    JOIN pg_class rel ON rel.oid = a.attrelid
    JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
    WHERE nsp.nspname = current_schema()
      AND rel.relname = 'order_process_log'
      AND a.attname = 'action'
      AND a.attnum > 0
      AND NOT a.attisdropped
      AND a.atttypid <> order_log_action_type
  ) THEN
    EXECUTE '
      ALTER TABLE "order_process_log"
      ALTER COLUMN "action" TYPE "OrderLogAction"
      USING (
        CASE
          WHEN "action" IS NULL THEN NULL
          WHEN "action"::text = '''' THEN NULL
          WHEN "action"::text = ANY (enum_range(NULL::"OrderLogAction")::text[]) THEN "action"::text::"OrderLogAction"
          ELSE ''DA_CHINH_SUA''::"OrderLogAction"
        END
      )
    ';
  END IF;
END
$$;
