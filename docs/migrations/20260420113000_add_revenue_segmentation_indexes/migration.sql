-- Indexes for revenue segmentation and delivery-time lookups
CREATE INDEX IF NOT EXISTS idx_order_links_status_shipment_order
  ON order_links (status, shipment_code, order_id);

CREATE INDEX IF NOT EXISTS idx_order_process_log_order_action_ts
  ON order_process_log (order_id, action, timestamp);

CREATE INDEX IF NOT EXISTS idx_order_process_log_order_new_status_ts
  ON order_process_log (order_id, new_status, timestamp);

CREATE INDEX IF NOT EXISTS idx_payment_purpose_status_paid_action
  ON payment (purpose, status, paid_time, action_at);
