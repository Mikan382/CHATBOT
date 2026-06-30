-- Add link-level cancel request support to order_cancel_request
-- link_id NULL  = cancel order request (existing behaviour)
-- link_id SET   = cancel link request  (new behaviour)

ALTER TABLE "order_cancel_request"
  ADD COLUMN "link_id"              BIGINT,
  ADD COLUMN "previous_link_status" "OrderStatus";

-- FK to order_links
ALTER TABLE "order_cancel_request"
  ADD CONSTRAINT "order_cancel_request_link_id_fkey"
  FOREIGN KEY ("link_id") REFERENCES "order_links"("link_id")
  ON DELETE NO ACTION ON UPDATE NO ACTION;

-- At most 1 pending request per link at a time
CREATE UNIQUE INDEX "order_cancel_request_link_pending_idx"
  ON "order_cancel_request"("link_id")
  WHERE "link_id" IS NOT NULL AND "status" = 'CHO_XU_LY';
