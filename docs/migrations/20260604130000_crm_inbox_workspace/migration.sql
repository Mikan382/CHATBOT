-- AlterTable: carry the CRM workspace on the inbox-log row (#211 Phase B). Additive,
-- nullable. A human review-reject emits a compensating crm.order.linked { REJECTED }
-- reply, which is workspace-scoped — stash the workspace here (bridge bookkeeping) so we
-- need not add a CRM column to the financial orders table.
ALTER TABLE "crm_inbox_event_log" ADD COLUMN "workspace_id" VARCHAR(64);
