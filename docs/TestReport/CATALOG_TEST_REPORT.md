# Test Report — Catalog (Reference Data)

|                      |                                                       |
| -------------------- | ----------------------------------------------------- |
| **Feature**          | **Catalog — Danh mục tham chiếu (Categories, Provinces, Wards)** |
| **Test requirement** |                                                       |
| **Number of TCs**    | **28**                                                |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 27     | 1      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## Danh mục loại ô nhiễm (Pollution Categories)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CAT_001 | Lấy danh mục ô nhiễm — success. | 1. Mở app (không cần đăng nhập).<br>2. Navigate to "Gửi báo cáo" → xem dropdown loại ô nhiễm. | Dropdown hiển thị danh sách: id, code, nameVi, nameEn, iconUrl. Sorted by code. | - Active categories exist in DB. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]` endpoint. Handler returns only `IsActive` categories. |
| TC_CAT_002 | Danh mục — chỉ trả active. | 1. Admin deactivate 1 category.<br>2. Mở dropdown loại ô nhiễm. | Category bị deactivate không hiển thị. | - Some categories inactive. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters `c => c.IsActive`. |
| TC_CAT_003 | Danh mục — không cần đăng nhập. | 1. Mở app không đăng nhập.<br>2. Gọi API pollution-categories. | 200 OK. Danh sách categories trả về bình thường. | - User not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]` on endpoint. |
| TC_CAT_004 | Danh mục — danh sách rỗng (tất cả inactive). | 1. Admin deactivate tất cả categories.<br>2. Gọi API. | 200 OK. List rỗng. | - All categories inactive. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns empty list (not error). |
| TC_CAT_005 | Danh mục — sorted theo code. | 1. Mở danh mục. | Categories hiển thị theo thứ tự code (alphabetical). | - Multiple categories exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler `OrderBy(c => c.Code)`. |
| TC_CAT_006 | Danh mục — icon hiển thị đúng. | 1. Mở danh mục.<br>2. Kiểm tra icon từng category. | Mỗi category hiển thị icon (iconUrl) tương ứng. | - Categories have iconUrl. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler projects `c.IconUrl`. |
| TC_CAT_007 | Danh mục — hiển thị song ngữ (Vi/En). | 1. Chuyển ngôn ngữ app sang English.<br>2. Mở dropdown loại ô nhiễm. | Category hiển thị nameEn thay vì nameVi. | - App supports i18n. | Passed | 04/09/2026 | TamKnm | | | | | | | Response includes both `nameVi` and `nameEn`, FE picks based on locale. |

---

## Danh sách tỉnh/thành (Provinces)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CAT_008 | Lấy danh sách tỉnh — success. | 1. Mở app (không cần đăng nhập).<br>2. Navigate to form cần chọn tỉnh.<br>3. Click dropdown "Tỉnh/Thành". | Dropdown hiển thị 63 tỉnh/thành: code (2 digit), name, boundaryUrl (optional). Sorted by name. | - Province data seeded. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]`. Handler returns all provinces sorted by name. |
| TC_CAT_009 | Tỉnh — sorted theo tên. | 1. Mở dropdown tỉnh. | Tỉnh hiển thị theo thứ tự tên (A-Z: An Giang, Bà Rịa - Vũng Tàu, ...). | - Province data exists. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler `OrderBy(p => p.Name)`. |
| TC_CAT_010 | Tỉnh — không cần đăng nhập. | 1. Mở app không đăng nhập.<br>2. Gọi API provinces. | 200 OK. Danh sách provinces trả về. | - User not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]`. |
| TC_CAT_011 | Tỉnh — boundaryUrl hiển thị (cho map overlay). | 1. Mở dropdown tỉnh.<br>2. Chọn tỉnh có boundaryUrl. | boundaryUrl chứa link GeoJSON cho FE vẽ polygon. | - Province has boundaryUrl. | Passed | 04/09/2026 | TamKnm | | | | | | | Response includes `BoundaryUrl` (nullable). |
| TC_CAT_012 | Tỉnh — boundaryUrl null (province chưa có GeoJSON). | 1. Chọn tỉnh chưa có GeoJSON. | boundaryUrl = null. Map không vẽ polygon cho tỉnh này. | - Province has no boundary data. | Passed | 04/09/2026 | TamKnm | | | | | | | `BoundaryUrl` is nullable. |

---

## Danh sách phường/xã theo tỉnh (Wards by Province)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CAT_013 | Lấy phường/xã — success. | 1. Mở form chọn tỉnh → chọn "Hồ Chí Minh" (code="79").<br>2. Dropdown phường/xã được tải. | Danh sách wards thuộc HCM: code (5 digit), name, unitType (Phường/Xã/Thị trấn), boundaryUrl. Sorted by name. | - Province "79" exists.<br>- Wards seeded for HCM. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates province code → returns wards filtered by `w.ProvinceCode == code`. |
| TC_CAT_014 | Phường/xã — tỉnh không tồn tại. | 1. Gọi API với provinceCode = "99" (không tồn tại). | Error "Province not found" is displayed. 404. | - Invalid province code. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `provinceExists` → `Errors.Catalog.ProvinceNotFound`. |
| TC_CAT_015 | Phường/xã — provinceCode không hợp lệ (3 ký tự). | 1. Gọi API với provinceCode = "123". | Error "ProvinceCode must be a 2-digit official code." is displayed. 422. | - Invalid format. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `Length(2).Matches(@"^\d{2}$")`. |
| TC_CAT_016 | Phường/xã — provinceCode rỗng. | 1. Gọi API với provinceCode = "". | Error "ProvinceCode must not be empty." is displayed. | - Empty code. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()`. |
| TC_CAT_017 | Phường/xã — provinceCode chứa chữ. | 1. Gọi API với provinceCode = "AB". | Error "ProvinceCode must be a 2-digit official code." is displayed. | - Non-numeric code. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `Matches(@"^\d{2}$")`. |
| TC_CAT_018 | Phường/xã — sorted theo tên. | 1. Chọn tỉnh hợp lệ. | Phường/xã sorted alphabetical by name. | - Province has wards. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler `OrderBy(w => w.Name)`. |
| TC_CAT_019 | Phường/xã — không cần đăng nhập. | 1. Mở app không đăng nhập.<br>2. Gọi API wards. | 200 OK. Wards trả về. | - User not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]`. |
| TC_CAT_020 | Phường/xã — hiển thị đơn vị hành chính (unitType). | 1. Chọn tỉnh.<br>2. Xem dropdown phường/xã. | Mỗi ward hiển thị kèm nhãn: "Phường", "Xã", hoặc "Thị trấn". | - Wards have AdministrativeUnit. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler projects `w.AdministrativeUnit!.Abbreviation`. |
| TC_CAT_021 | Phường/xã — tỉnh không có ward nào. | 1. Gọi API cho tỉnh mới (chưa seed ward data). | 200 OK. List rỗng. | - Province exists but no wards seeded. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns empty list, not error. |

---

## Tích hợp với Report Form

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CAT_022 | Tạo báo cáo — chọn category từ catalog. | 1. Login as Citizen.<br>2. Mở form "Gửi báo cáo".<br>3. Chọn loại ô nhiễm từ dropdown. | Dropdown hiển thị active categories. User chọn 1 → categoryId gửi kèm report. | - User on submit report form. | Passed | 04/09/2026 | TamKnm | | | | | | | Catalog is loaded from `GET /v1/catalog/pollution-categories`. |
| TC_CAT_023 | Tạo báo cáo — chọn tỉnh + phường cascading. | 1. Login as Citizen.<br>2. Mở form → chọn tỉnh "Hà Nội".<br>3. Dropdown phường được tải. | Sau khi chọn tỉnh, wards tự động load cho tỉnh đã chọn. | - User on submit report form. | Passed | 04/09/2026 | TamKnm | | | | | | | Cascading: tỉnh → wards API call → ward dropdown. |
| TC_CAT_024 | Thay đổi tỉnh — ward dropdown reset. | 1. Login as Citizen.<br>2. Chọn tỉnh HCM → chọn ward.<br>3. Đổi tỉnh sang Hà Nội. | Ward dropdown reset, tải lại wards của Hà Nội. Ward cũ bị clear. | - User changes province. | Passed | 04/09/2026 | TamKnm | | | | | | | FE clears ward selection on province change. |

---

## Edge Cases

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CAT_025 | Catalog API trả response nhanh (cached). | 1. Gọi API categories 2 lần liên tiếp. | Lần 2 nhanh hơn (nếu có cache). | - Multiple requests. | Passed | 04/09/2026 | TamKnm | | | | | | | Catalog data thay đổi hiếm, FE nên cache client-side. |
| TC_CAT_026 | ProvinceCode trim whitespace. | 1. Gọi API wards với provinceCode = " 79 " (có khoảng trắng). | Xử lý thành "79". Trả danh sách wards của HCM. | - ProvinceCode with whitespace. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller calls `provinceCode.Trim()` before sending to query. |
| TC_CAT_027 | Wards — AdministrativeUnit null (data chưa seed). | 1. Gọi API wards cho tỉnh có ward thiếu AdministrativeUnit. | Server xử lý bình thường hoặc hiện unitType rỗng. | - Ward without AdministrativeUnit. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: Handler projects `w.AdministrativeUnit!.Abbreviation` dùng null-forgiving operator `!`. Nếu `AdministrativeUnit` là null (data chưa seed), sẽ ném `NullReferenceException` → 500 thay vì xử lý graceful. Nên dùng `w.AdministrativeUnit?.Abbreviation ?? ""`. |
| TC_CAT_028 | SQL injection — provinceCode. | 1. Gọi API wards với provinceCode = "'; DROP TABLE--". | Error "ProvinceCode must be a 2-digit official code." is displayed. 422. Không ảnh hưởng DB. | - Malicious input. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator regex `^\d{2}$` blocks non-numeric. EF Core parameterized queries prevent SQL injection. |
