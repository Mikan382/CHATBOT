# <service-name>

| Trường | Giá trị |
|---|---|
| **Loại** | Service documentation |
| **Trạng thái** | Draft / Review / Approved |
| **Phiên bản** | 1.0.0 |
| **Cập nhật** | YYYY-MM-DD |
| **Owner** | <Team / Person> |
| **Bounded Context** | <tên bounded context> |
| **Phase** | 1 / 1.5 / 2 / 3 |
| **Repo** | <link source code> |
| **Slack channel** | `#<channel>` |

## 1. Mục đích

<2-3 câu: service giải quyết vấn đề gì.>

## 2. Trách nhiệm

CHỊU TRÁCH NHIỆM:
- ...
- ...

KHÔNG chịu trách nhiệm:
- ... (xử lý bởi `<other-service>`)

## 3. Bounded Context & Dependencies

### 3.1 Upstream

| Service | Cách tương tác | Mục đích |
|---|---|---|
| `<service-x>` | API call sync | ... |
| `<service-y>` | Subscribe events | ... |

### 3.2 Downstream

| Service | Cách tương tác |
|---|---|
| `<service-z>` | Subscribe events |

### 3.3 External integrations

| Đối tác | Mục đích |
|---|---|
| ... | ... |

## 4. API

Xem [`api.md`](./api.md) hoặc OpenAPI spec tại [`60-reference/api/<service>.openapi.yaml`](../../60-reference/api/).

### 4.1 Endpoints chính

| Method | Path | Mô tả |
|---|---|---|
| `POST` | `/v1/...` | ... |

## 5. Events

### 5.1 Emit

| Event | Khi nào | Consumer chính |
|---|---|---|
| `<domain>.<entity>.<verb>` | ... | ... |

### 5.2 Consume

| Event | Producer | Mục đích |
|---|---|---|
| `<domain>.<entity>.<verb>` | `<service>` | ... |

## 6. Data Model

### 6.1 Entities chính

- `<EntityName>`: ...

### 6.2 Database

| | |
|---|---|
| **Engine** | PostgreSQL |
| **DB name** | `<service>_db` |
| **Migration** | <Flyway/Liquibase/Goose> |

ERD: xem [`data-model.md`](./data-model.md).

## 7. Yêu cầu chức năng

| ID | Mô tả | Priority |
|---|---|---|
| `<DOMAIN>-FR-001` | ... | P0 |

## 8. Yêu cầu phi chức năng

| ID | Yêu cầu | Mục tiêu |
|---|---|---|
| `<DOMAIN>-NFR-001` | Latency p95 | ≤ 500ms |
| `<DOMAIN>-NFR-002` | Uptime | ≥ 99.9% |

## 9. Tech Stack

| Layer | Tech |
|---|---|
| Runtime | <Node/Go/PHP/Python> |
| Framework | <Express/Fiber/Laravel/FastAPI> |
| Database | PostgreSQL |
| Cache | Redis |
| Message | Pub/Sub / NATS |
| Job queue | BullMQ / Laravel Queue |

## 10. Vận hành

### 10.1 Deployment

| | |
|---|---|
| **Platform** | Cloud Run / Fargate |
| **CI/CD** | GitHub Actions |
| **Environments** | dev / staging / production |

### 10.2 Observability

- **Logs**: structured JSON.
- **Metrics**: RED + USE.
- **Tracing**: OpenTelemetry.
- **Alerts**: <link rules>.

### 10.3 SLO

| Metric | SLO | Error budget |
|---|---|---|
| Availability | 99.9% | 43m/tháng |
| Latency p95 | ≤ 500ms | — |

## 11. Liên kết

- ADR: [ADR-NNNN](../../90-adr/...)
- Use case: [UC-NN](../../30-use-cases/...)
- Runbook: [`runbook.md`](./runbook.md)

## Lịch sử thay đổi

| Phiên bản | Ngày | Tác giả | Thay đổi |
|---|---|---|---|
| 1.0.0 | YYYY-MM-DD | @user | Bản đầu tiên |
