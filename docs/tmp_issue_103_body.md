Part of #3

## User Story Slice

`US-1.1.8` — Hoàn thiện customer-profile HTTP bridge để luồng Social Inbox không chỉ ingest message mà còn nối được vào customer master data.

## Context

`engagement-service` đã ingest được webhook và lưu `platform_contact` / `conversation` / `conversation_message`, nhưng bridge sync sang `customer-profile-service` vẫn chưa production-ready.

Hiện trạng kỹ thuật:

- `engagement-service` đã có `HTTPCustServiceClient`, `CustServiceAutoIdentifier`, `StrangerRetryJob`
- Các thành phần này chỉ được bật khi `ENGAGEMENT_CUST_SERVICE_URL` được set trong `cmd/server/bootstrap.go`
- HTTP client hiện còn thiếu:
  - forward auth header sang `customer-profile-service`
  - decode đúng envelope `{ success, data, meta }`
  - canonical route contract giữa hai service
- Với luồng Facebook Page / bot hiện tại, contact mới từ webhook cần được identify đúng để thread inbox nối sang CRM thật mà không làm gãy conversation

Issue này chốt phần HTTP bridge production-grade cho Sprint 02.

## What to build

### 1. Auth propagation cho cust-service HTTP calls

Hỗ trợ 2 mode:

- Staff-triggered request:
  - forward raw bearer token từ actor/request context sang `customer-profile-service`
- Background/system request:
  - mint inter-service HMAC JWT bằng `ENGAGEMENT_INTER_SERVICE_JWT_KEY`

Áp dụng cho các flow:
- social lookup
- search customer
- create customer
- add social contact
- get profile
- get stats

### 2. Adapt response decoding

`HTTPCustServiceClient` phải decode đúng success envelope của `customer-profile-service`, không decode object phẳng trực tiếp như hiện tại.

### 3. Canonical internal route contract

`engagement-service` phải gọi dedicated internal integration routes của `customer-profile-service`, không dùng chung gateway-facing customer APIs của CRM UI.

### 4. Keep bot-supported inbox flow intact

Luồng webhook Facebook hiện lưu echo reply thành `OUTBOUND/BOT`. HTTP bridge mới không được làm hỏng:
- conversation continuity
- identify flow cho `platform_contact`
- async auto-identify sau khi contact được tạo

## API / Contract

`engagement-service` cần hoạt động được với các route sau của `customer-profile-service`:

```http
GET  /internal/engagement/customers/search?q={query}
GET  /internal/engagement/customers/social-lookup?type={type}&value={platform_user_id}
POST /internal/engagement/customers
POST /internal/engagement/customers/{customer_id}/social-contacts
GET  /internal/engagement/customers/{customer_id}/profile
GET  /internal/engagement/customers/{customer_id}/stats
```

### Auth contract

Staff-triggered:

```http
Authorization: Bearer <raw-user-token>
```

Background/system-triggered:

```http
Authorization: Bearer <engagement-service-hmac-jwt>
```

## Files to touch

| Type | File |
|---|---|
| Wiring | `cmd/server/bootstrap.go` |
| Config | `internal/config/config.go` |
| Auth helper | `internal/auth/*` or adjacent helper |
| HTTP client | `internal/crm/socialinbox/cust_service_http.go` |
| Auto-identify | `internal/crm/socialinbox/autoidentify_cust_service.go` |
| Service tests | `internal/crm/socialinbox/*_test.go` |
| Docs | `README.md` |
| Docs | `docs/dependencies.md` |
| Docs | `docs/architecture.md` |

## Acceptance criteria

- [ ] `HTTPCustServiceClient` sends auth correctly in both modes
- [ ] Staff-triggered requests forward the raw bearer token
- [ ] Background/system requests use inter-service HMAC JWT
- [ ] `HTTPCustServiceClient` decodes cust-service success envelope correctly
- [ ] `CustServiceAutoIdentifier` works against the real cust-service contract
- [ ] `SearchCustomers`, `CreateCustomerFromContact`, `LinkContactToCustomer`, and `GetCustomerProfile` work with the canonical internal cust-service routes
- [ ] `409 duplicate` from cust-service still maps to `ErrCustServiceDuplicate`
- [ ] timeout / `5xx` handling is explicit and tested
- [ ] Facebook echo / bot replies still persist as `OUTBOUND/BOT`
- [ ] `README.md`, `docs/dependencies.md`, and `docs/architecture.md` reflect the final HTTP integration contract

## Out of scope

- GCP Pub/Sub transport
- Customer event subscriber
- UI changes in `crm-admin`
- Business projection from customer events
