# Session Handoff — GreenLens v1.7: API Documentation & E2E Onboarding

> **Cập nhật lần cuối:** 2026-06-17 20:19 · **Phiên bản:** 7 · **Agent:** Antigravity

## 1. Mục tiêu & Trạng thái hiện tại
Đã hoàn tất kiểm kê, cập nhật tài liệu API, và **chạy thành công E2E test 20 bước** cho luồng Company Management.

- **Đã hoàn thành:** 
  - Cập nhật tài liệu `docs/API_DASHBOARD_AND_FLOW.md` (Version 1.7, 106 endpoints).
  - E2E test toàn bộ luồng onboarding: DEO→Company→CM→Staff→Team (20/20 PASS).
  - Script test tự động tại `e2e-test.ps1`.

## 2. Quyết định đã chốt (Locked Decisions)
- API Documentation v1.7 là "Source of Truth" cho cấu trúc endpoint hiện tại.
- `CompanyStaff.IsActive` chỉ enforce ở tầng task assignment, KHÔNG block login.
- `contractType` phải gửi dạng **numeric enum** (0=Subsidiary, 1=Bidding).
- `PUT /companies/my/staff/{id}/status` cần body `{ isActive: bool }`, KHÔNG phải toggle.

## 3. Kết quả E2E Test
| Nhóm | Steps | Kết quả |
|------|-------|---------|
| Auth (DEO/CM/Staff) | 1,3,4,5,11,11b,12,14,16 | ✅ All PASS |
| Company CRUD | 2,6 | ✅ All PASS |
| Staff MGMT | 8,9,13,15,19 | ✅ All PASS |
| Team CRUD | 7,10,17,18,20 | ✅ All PASS |

## 4. Việc tiếp theo (Next Steps)
1. **Bổ sung login-level blocking**: Nếu muốn staff bị deactivate không login được, cần sửa `LoginCommandHandler`.
2. **Xử lý ServiceScope/ContractType mở rộng**: Cần theo dõi nếu có thêm enum values.
3. **Hoàn thiện test coverage**: Thêm E2E cho LEO flow (recruit, inspect, dispatch).

## 5. File & Artefact quan trọng
| Đường dẫn | Trạng thái |
|---|---|
| `docs/API_DASHBOARD_AND_FLOW.md` | **Đã cập nhật (v1.7)** |
| `e2e-test.ps1` | **E2E test script (20 steps, all PASS)** |
| `src/Greenlens.Api/Controllers/CompaniesController.cs` | Ổn định |
| `src/Greenlens.Api/Controllers/TeamsController.cs` | Ổn định |

---
*Tệp này đã được lưu tại `.agents/memory/SESSION_HANDOFF.md`.*
