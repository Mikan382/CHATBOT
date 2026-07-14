# Runbook: <Tên runbook>

| Trường | Giá trị |
|---|---|
| **Loại** | Runbook |
| **Trạng thái** | Active |
| **Severity** | P0 / P1 / P2 / P3 |
| **Service liên quan** | `<service-name>` |
| **Owner** | <Team / On-call rotation> |
| **Cập nhật** | YYYY-MM-DD |

## 1. Trigger / Symptom

<Khi nào dùng runbook này. Symptom cụ thể.>

- Alert: `<alert name>`.
- Symptom: ...
- Người báo: ...

## 2. Tác động (Impact)

| Đối tượng | Ảnh hưởng |
|---|---|
| Customer | ... |
| Internal | ... |
| Revenue | ... |

## 3. Diagnosis

Bước 1: Kiểm tra ...
```bash
<command>
```

Bước 2: Xem dashboard ...
- Link: <URL>.

Bước 3: Check log ...
```bash
<command>
```

## 4. Mitigation

### Bước 1: <Action>
```bash
<command>
```
Kỳ vọng: ...

### Bước 2: <Action>
...

### Bước 3: Verify
- Check ...

## 5. Rollback Plan

<Cách undo nếu mitigation gây vấn đề.>

```bash
<command>
```

## 6. Escalation

| Mức | Khi nào | Liên hệ |
|---|---|---|
| L1 | Mitigation không work sau 15p | On-call platform team |
| L2 | Sau 30p | Tech lead |
| L3 | Customer impact > 1h | CTO |

## 7. Post-mortem

Sau khi fix:
- Tạo post-mortem trong 48h.
- Template: <link>.
- Action items: ...

## 8. Liên kết

- Runbook liên quan: ...
- Dashboard: ...
- Code repo: ...
