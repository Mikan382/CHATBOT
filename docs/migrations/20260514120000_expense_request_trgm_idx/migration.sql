-- Enable pg_trgm extension if not already enabled (idempotent)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- GIN trigram index on expense_request.description
CREATE INDEX IF NOT EXISTS idx_expense_request_description_trgm
  ON expense_request USING GIN (description gin_trgm_ops);

-- GIN trigram index on expense_request.note
CREATE INDEX IF NOT EXISTS idx_expense_request_note_trgm
  ON expense_request USING GIN (note gin_trgm_ops);
