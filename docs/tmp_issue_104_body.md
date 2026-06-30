Part of #3

## User Story Slice

`US-1.1.9` — Thay in-memory bus bằng transport switchable để Social Inbox chạy production-ready với GCP Pub/Sub và nhận được customer events từ ecosystem thật.

## Context

US-1.1 đã có social inbox ingestion và worker wiring, nhưng transport async trong `engagement-service` vẫn đang dừng ở `eventbus.NewInMemoryBus()`.

Hiện trạng kỹ thuật:

- `customer-profile-service` đã dùng Google Cloud Pub/Sub thật:
  - publish từ `cust_outbox`
  - consume từ IAM / CRM / Order / Payment topics
- `engagement-service` hiện có:
  - config `ENGAGEMENT_PUBSUB_PROJECT_ID`
  - nhưng runtime trong `cmd/server/workers.go` vẫn dùng in-memory bus
  - outbox poller và consumers chưa có GCP Pub/Sub path thật
- Comment trong code đã nói rõ hướng “swap to GCP Pub/Sub”, nhưng phần wiring đó chưa được implement

Issue này chốt hạ tầng async production-ready cho Sprint 02, đồng thời thêm subscriber nền cho customer events để service connect hoàn chỉnh hơn với các service xung quanh.

## What to build

### 1. Switchable transport

Thay hardcoded in-memory bus bằng transport switchable:

- local/dev fallback:
  - nếu `ENGAGEMENT_PUBSUB_PROJECT_ID` rỗng thì tiếp tục dùng in-memory bus
- production path:
  - nếu `ENGAGEMENT_PUBSUB_PROJECT_ID` được set thì dùng GCP Pub/Sub publisher/subscriber thật

### 2. Outbox publisher path

Outbox poller của `engagement-service` phải publish được ra GCP Pub/Sub thay vì chỉ publish nội bộ.

### 3. Subscriber path cho existing consumers

Các consumer hiện có phải subscribe được từ transport thật:
- mall
- bod
- shipment

### 4. Add customer topic subscriber skeleton

Thêm subscriber cho customer events từ `customer-profile-service`:
- topic customer
- CloudEvent decode
- typed handler entrypoint
- log + ack + error path rõ ràng

Trong issue này chưa cần full business reaction, nhưng phải có subscriber bootstrap thật.

### 5. Config surface

Bổ sung env/config rõ ràng cho topic/subscription thay vì chỉ có mỗi `ENGAGEMENT_PUBSUB_PROJECT_ID`.

## API / Contract

### Suggested config to add

```text
ENGAGEMENT_PUBSUB_PROJECT_ID
ENGAGEMENT_PUBSUB_TOPIC_CUSTOMER
ENGAGEMENT_PUBSUB_TOPIC_MALL
ENGAGEMENT_PUBSUB_TOPIC_BOD
ENGAGEMENT_PUBSUB_TOPIC_SHIPMENT
ENGAGEMENT_PUBSUB_SUB_CUSTOMER_EVENTS
ENGAGEMENT_PUBSUB_SUB_MALL_EVENTS
ENGAGEMENT_PUBSUB_SUB_BOD_EVENTS
ENGAGEMENT_PUBSUB_SUB_SHIPMENT_EVENTS
```

### Customer topic baseline

`customer-profile-service` đang publish customer domain events lên topic customer. Engagement subscriber trong issue này phải receive/decode được tối thiểu các event family sau:

- `customer.profile.created`
- `customer.profile.updated`
- `customer.profile.deleted`
- `customer.lifecycle.stage_changed`
- `customer.staff.assigned`
- `customer.staff.unassigned`

## Files to touch

| Type | File |
|---|---|
| Config | `internal/config/config.go` |
| Wiring | `cmd/server/bootstrap.go` |
| Worker transport | `cmd/server/workers.go` |
| Consumers | `internal/consumers/crm_consumer.go` |
| New consumer | `internal/consumers/customer_consumer.go` |
| Outbox transport wiring | `internal/outbox/*` and/or transport adapter |
| Tests | `internal/consumers/*_test.go`, worker/transport tests |
| Docs | `docs/events.md` |
| Docs | `docs/architecture.md` |
| Docs | `docs/dependencies.md` |

## Acceptance criteria

- [ ] When `ENGAGEMENT_PUBSUB_PROJECT_ID` is empty, `engagement-service` still runs with in-memory bus in local/dev
- [ ] When `ENGAGEMENT_PUBSUB_PROJECT_ID` is set, outbox/consumers use GCP Pub/Sub transport instead of in-memory bus
- [ ] Existing mall / bod / shipment consumer flows still bootstrap and receive messages correctly
- [ ] A new customer-topic subscriber exists and can decode customer events into a typed entrypoint
- [ ] Transport setup is fully config-driven, not hardcoded
- [ ] Failure path is logged clearly and does not crash the whole server bootstrap
- [ ] `docs/events.md`, `docs/architecture.md`, and `docs/dependencies.md` document the final transport design

## Out of scope

- Full business projection for customer events
- Lifecycle/segment reaction logic
- UI changes
- HTTP bridge/auth propagation
