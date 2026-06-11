<!-- VÍ DỤ ĐÃ ĐIỀN — minh họa mức độ chi tiết phù hợp. Đây là mẫu, không phải file thật của workspace. -->

# Session Handoff — SU26SE049 Business Rules & OVERVIEW

> Cập nhật lần cuối: 2026-06-09 16:40 · Phiên bản: 4 · Agent: Claude

## 0. TL;DR
Đang hoàn thiện tài liệu Business Rules v1.2 (.docx) và OVERVIEW.md cho app báo
cáo ô nhiễm môi trường. Vừa siết quyền truy cập (bỏ ẩn danh) và sửa mô hình hợp
đồng công ty. Việc tiếp theo: rà lại Role Matrix và chờ người dùng xác nhận tên
2 vai trò công ty.

## 1. Mục tiêu & Bối cảnh
- **Mục tiêu:** Cập nhật bộ Business Rules từ v1.1 → v1.2 (file Word) và đồng bộ
  OVERVIEW.md (tài liệu quy ước backend) cho đồ án SU26SE049.
- **Phạm vi:** Toàn quốc VN; TP.HCM = 168 phường/xã sau sáp nhập (113 phường,
  54 xã, 1 đặc khu).
- **Tech stack:** .NET 9, Clean Architecture, CQRS. Tài liệu sinh bằng docx-js.
- **Ngôn ngữ làm việc:** Tiếng Việt.

## 2. Quyết định đã chốt (Locked Decisions)
| # | Quyết định | Lý do | Ngày |
|---|---|---|---|
| 1 | 2 vai trò mới = Company Manager (CM) + Company Staff (CS); thực thể = Environmental Service Company (ESC) | Mô hình hóa công ty dịch vụ môi trường | 06-07 |
| 2 | Onboarding công ty dùng activation token một-lần (7 ngày), KHÔNG dùng contract key làm credential | An toàn, tách biệt onboarding vs authorization | 06-07 |
| 3 | Điều phối theo NHU CẦU, không theo loại ô nhiễm; LEO lập InspectionReport cho mọi loại | Sát thực tế vận hành | 06-07 |
| 4 | Danh mục ô nhiễm còn 4 loại (bỏ Không khí) | Yêu cầu người dùng | 06-09 |
| 5 | Bỏ truy cập ẩn danh: guest chỉ xem map/public; mọi tính năng ghi cần đăng nhập | Yêu cầu người dùng | 06-09 |
| 6 | Thời hạn HĐ chỉ áp cho PrivateContractor; StateAffiliated vô thời hạn | Yêu cầu người dùng (đúng thực tế) | 06-09 |
| 7 | 1 xã/phường ↔ tối đa 1 công ty; 1 công ty phủ nhiều xã/phường (BR-CMP-014) | Yêu cầu người dùng | 06-09 |

## 3. Trạng thái hiện tại
- **Đã hoàn thành:** docx v1.2 build & validate PASSED (25 trang); OVERVIEW.md
  đồng bộ tới v1.3; đã kiểm tra trực quan các trang BR-AUTH-017, BR-REP-005,
  BR-CMP-001/003/005/007/014.
- **Đang dở:** chưa có; chờ phản hồi người dùng.

## 4. Việc tiếp theo (Next Steps)
- [ ] Chờ người dùng xác nhận tên 2 vai trò công ty (CM/CS) — có thể đổi.
- [ ] Cân nhắc có giữ tùy chọn "ẩn tên hiển thị" (BR-REP-012) hay bỏ hẳn.
- [ ] (Tùy chọn) Cân nhắc bỏ loại "Tiếng ồn" nếu muốn app thuần rác/nước/hóa chất.

## 5. File & Artefact quan trọng
| Đường dẫn | Vai trò | Trạng thái |
|---|---|---|
| `build1.js` | Sinh bìa + §1 Overview + Role Matrix | đã sửa |
| `build2.js` | Sinh §3..§16 + phụ lục, đóng gói .docx | đã sửa |
| `gen.js` | Helper/style cho docx-js | ổn định |
| `SU26SE049_BusinessRules_v1_2.docx` | Deliverable chính | mới |
| `OVERVIEW.md` | Quy ước backend (v1.3) | đã sửa |

## 6. Kiến thức nền & Quy ước
- Build docx: `node build2.js` (require build1.js → gen.js).
- Validate: `python /mnt/skills/public/docx/scripts/office/validate.py <file>`.
- QA trực quan: convert PDF bằng `soffice --headless --convert-to pdf`, render
  trang bằng `pdftoppm`.
- Nhóm BR ID: AUTH, REP, MAP, OFF, CLN, INS, ORG, CMP, NTF/CMT, GAM, AI, ADM,
  DAT, SYS. 10 actors. State machine 2 nhánh: dọn dẹp (umbrella) + InspectionReport.

## 7. Câu hỏi mở / Cần xác nhận
- Tên 2 vai trò công ty có giữ "Company Manager/Company Staff" không?
- Có giữ tùy chọn ẩn tên hiển thị của người gửi (BR-REP-012) không?

## 9. Change Log
- 2026-06-07 — Dựng v1.2: thêm ESC/CM/CS, token onboarding, điều phối theo nhu cầu.
- 2026-06-09 — Bỏ ô nhiễm Không khí (5→4 loại).
- 2026-06-09 — Bỏ truy cập ẩn danh; siết quyền guest.
- 2026-06-09 — Hợp đồng theo loại công ty + độc quyền địa bàn (BR-CMP-014).
