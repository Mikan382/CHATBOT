-- Reset all BIGSERIAL sequences to max(current_id).
-- Fixes sequence desync when rows are inserted with explicit IDs
-- (pg_restore, direct SQL imports) bypassing the sequence.
-- Safe: only advances a sequence, never lowers below its current last_value.

DO $$
DECLARE
  _seq TEXT;
  _max BIGINT;
  _last BIGINT;
BEGIN

  -- account_route  ← reported broken, fix first
  _seq := pg_get_serial_sequence('"account_route"', 'account_route_id');
  SELECT COALESCE(MAX(account_route_id), 0) INTO _max FROM account_route;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- account
  _seq := pg_get_serial_sequence('"account"', 'account_id');
  SELECT COALESCE(MAX(account_id), 0) INTO _max FROM account;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- address
  _seq := pg_get_serial_sequence('"address"', 'address_id');
  SELECT COALESCE(MAX(address_id), 0) INTO _max FROM address;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- orders
  _seq := pg_get_serial_sequence('"orders"', 'order_id');
  SELECT COALESCE(MAX(order_id), 0) INTO _max FROM orders;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- order_links
  _seq := pg_get_serial_sequence('"order_links"', 'link_id');
  SELECT COALESCE(MAX(link_id), 0) INTO _max FROM order_links;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- purchases
  _seq := pg_get_serial_sequence('"purchases"', 'purchase_id');
  SELECT COALESCE(MAX(purchase_id), 0) INTO _max FROM purchases;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- order_process_log
  _seq := pg_get_serial_sequence('"order_process_log"', 'log_id');
  SELECT COALESCE(MAX(log_id), 0) INTO _max FROM order_process_log;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- payment
  _seq := pg_get_serial_sequence('"payment"', 'payment_id');
  SELECT COALESCE(MAX(payment_id), 0) INTO _max FROM payment;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- route
  _seq := pg_get_serial_sequence('"route"', 'route_id');
  SELECT COALESCE(MAX(route_id), 0) INTO _max FROM route;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- warehouse
  _seq := pg_get_serial_sequence('"warehouse"', 'warehouse_id');
  SELECT COALESCE(MAX(warehouse_id), 0) INTO _max FROM warehouse;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- warehouse_location
  _seq := pg_get_serial_sequence('"warehouse_location"', 'location_id');
  SELECT COALESCE(MAX(location_id), 0) INTO _max FROM warehouse_location;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- packing
  _seq := pg_get_serial_sequence('"packing"', 'packing_id');
  SELECT COALESCE(MAX(packing_id), 0) INTO _max FROM packing;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- partial_shipment
  _seq := pg_get_serial_sequence('"partial_shipment"', 'id');
  SELECT COALESCE(MAX(id), 0) INTO _max FROM partial_shipment;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- expense_request
  _seq := pg_get_serial_sequence('"expense_request"', 'expense_request_id');
  SELECT COALESCE(MAX(expense_request_id), 0) INTO _max FROM expense_request;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- voucher
  _seq := pg_get_serial_sequence('"voucher"', 'voucher_id');
  SELECT COALESCE(MAX(voucher_id), 0) INTO _max FROM voucher;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- product_type
  _seq := pg_get_serial_sequence('"product_type"', 'product_type_id');
  SELECT COALESCE(MAX(product_type_id), 0) INTO _max FROM product_type;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

  -- route_exchange_rate
  _seq := pg_get_serial_sequence('"route_exchange_rate"', 'route_exchange_rate_id');
  SELECT COALESCE(MAX(route_exchange_rate_id), 0) INTO _max FROM route_exchange_rate;
  IF _seq IS NOT NULL AND _max > 0 THEN
    EXECUTE format('SELECT last_value FROM %s', _seq) INTO _last;
    IF _max > _last THEN PERFORM setval(_seq, _max); END IF;
  END IF;

END $$;
