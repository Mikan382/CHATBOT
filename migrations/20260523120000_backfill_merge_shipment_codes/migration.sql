-- Backfill legacy merge sub-codes per #805 policy
--
-- Rationale:
-- Before #805 refactor, declareMergedShipmentCode assigned suffix -A/-B/-C to each purchase
-- in a merge group. New policy: all purchases in a merge share IDENTICAL shipment_code
-- (= external_shipment_code) because 1 physical package from shop = 1 logical package.
--
-- This migration normalizes legacy rows where:
--   shipment_code = "{external_shipment_code}-{A|B|...|AA|AB|...}"
-- to:
--   shipment_code = external_shipment_code.
--
-- Idempotent: re-running has no effect once normalized.

UPDATE order_links
SET shipment_code = external_shipment_code
WHERE external_shipment_code IS NOT NULL
  AND shipment_code IS NOT NULL
  AND shipment_code <> external_shipment_code
  AND shipment_code LIKE external_shipment_code || '-%'
  AND substring(shipment_code from length(external_shipment_code) + 2) ~ '^[A-Z]+$';
