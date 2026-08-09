# Test Report — Map

|                      |                                     |
| -------------------- | ----------------------------------- |
| **Feature**          | **Map — Bản đồ công khai**          |
| **Test requirement** |                                     |
| **Number of TCs**    | **40**                              |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 39     | 1      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## Xem bản đồ — Detail Mode

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MAP_001 | Xem bản đồ detail — zoom gần (default). | 1. Mở app (có thể chưa đăng nhập).<br>2. App hiển thị bản đồ khu vực hiện tại.<br>3. Các pin báo cáo hiển thị trên map. | Danh sách pin báo cáo hiển thị. Mỗi pin có: title, severity color, category icon, ảnh thumbnail. Default limit = 200. Mode = "detail". | - App có GPS permission. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]` endpoint. Default limit 200, max 500. Only Verified/InProgress/Resolved/Closed reports shown. |
| TC_MAP_002 | Xem bản đồ — không đăng nhập (anonymous). | 1. Mở app mà không đăng nhập.<br>2. Bản đồ hiển thị. | Bản đồ hiển thị bình thường với các pin báo cáo. Không cần login. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[AllowAnonymous]` cho cả 2 endpoint map. |
| TC_MAP_003 | Click vào pin — xem preview card. | 1. Mở bản đồ.<br>2. Click vào 1 pin báo cáo. | Preview card hiển thị: ảnh thumbnail, title (category name), description, address, số người báo cáo (reporterCount), category icon. | - Map has report pins. | Passed | 04/09/2026 | TamKnm | | | | | | | Response includes `imageUrl`, `title`, `description`, `address`, `reporterCount`, `categoryIconUrl`. |
| TC_MAP_004 | Tọa độ pin được làm tròn ~11m (privacy). | 1. Mở bản đồ.<br>2. Xem vị trí pin. | Pin hiển thị ở vị trí đã được làm tròn (precision ~11m). Không hiển thị tọa độ chính xác. | - Reports exist. | Passed | 04/09/2026 | TamKnm | | | | | | | `PublicMapCoordinateRounding.RoundLatitude/Longitude` làm tròn 4 chữ số thập phân (≈11m). |
| TC_MAP_005 | Bản đồ — filter theo category. | 1. Mở bản đồ.<br>2. Chọn filter "Rác thải sinh hoạt". | Chỉ hiển thị pin thuộc category đã chọn. | - Reports exist with multiple categories. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `categoryId` filters reports. |
| TC_MAP_006 | Bản đồ — category không tồn tại. | 1. Mở bản đồ.<br>2. Gọi API với categoryId không tồn tại. | Error 404 "Không tìm thấy danh mục" is displayed. | - Invalid categoryId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `categories.ExistsAsync()`. |
| TC_MAP_007 | Bản đồ — category bị inactive. | 1. Mở bản đồ.<br>2. Gọi API với categoryId bị inactive. | Error 404 "Không tìm thấy danh mục" is displayed. | - Category is inactive. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `c.IsActive` in ExistsAsync. |
| TC_MAP_008 | Bản đồ — limit custom (e.g. 50). | 1. Mở bản đồ.<br>2. Gọi API với limit=50. | Chỉ trả về tối đa 50 pin. | - Reports exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler `Math.Clamp(limit, 1, 500)`. |
| TC_MAP_009 | Bản đồ — limit vượt max (> 500). | 1. Mở bản đồ.<br>2. Gọi API với limit=1000. | Error "limit must be between 1 and 500 when provided." is displayed. | - User specifies limit > 500. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator rejects limit > MaxDetailLimit. |
| TC_MAP_010 | Bản đồ — không có report trong viewport. | 1. Mở bản đồ.<br>2. Di chuyển đến vùng biển/đảo không có report. | Map trống, không có pin. items = []. meta.count = 0. | - No reports in viewport. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns empty list. |
| TC_MAP_011 | Bản đồ — report đang Submitted không hiển thị. | 1. Citizen gửi báo cáo mới (status = Submitted).<br>2. Mở bản đồ tại khu vực đó. | Report mới gửi không hiển thị trên map. Chỉ Verified+ mới hiện. | - Report is Submitted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters `PublicMapReportStatuses.Visible` (Verified, InProgress, Resolved, Closed). |
| TC_MAP_012 | Bản đồ — report bị ẩn (moderated) không hiển thị. | 1. Admin ẩn 1 report.<br>2. Mở bản đồ. | Report bị ẩn không hiển thị trên map. | - Report is hidden by admin. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters `!r.IsHidden`. |
| TC_MAP_013 | Bản đồ — pin hiển thị community cleanup event. | 1. Mở bản đồ.<br>2. Xem pin có event dọn rác cộng đồng. | Pin hiển thị icon/badge "Community Cleanup" kèm eventId. `hasCommunityCleanup = true`. | - Report has active community cleanup event. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler joins `CommunityCleanupEvent` to check active events. |

---

## Xem bản đồ — Aggregate Mode

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MAP_014 | Xem bản đồ aggregate — zoom xa (heatmap). | 1. Mở app.<br>2. Zoom ra xa (toàn thành phố).<br>3. App chuyển sang mode aggregate. | Grid cells hiển thị: mỗi cell có count (số báo cáo) và maxSeverity. Mode = "aggregate". Sorted by count descending. | - Reports exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Aggregate mode groups reports into grid cells by latitude/longitude buckets. |
| TC_MAP_015 | Aggregate — gridLevel default (3). | 1. Mở bản đồ aggregate.<br>2. Không chỉ định gridLevel. | Default gridLevel = 3. Cell size = 0.1 degree. | - Reports exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Default `gridLevel = 3`, cell size 0.1 degree ≈ 11km. |
| TC_MAP_016 | Aggregate — gridLevel = 1 (coarse). | 1. Mở bản đồ zoom rất xa.<br>2. Gọi API với gridLevel=1. | Cells lớn (0.5 degree ≈ 55km). Ít cells hơn, mỗi cell count cao hơn. | - Reports exist. | Passed | 04/09/2026 | TamKnm | | | | | | | gridLevel 1 = cell 0.5 degree. |
| TC_MAP_017 | Aggregate — gridLevel = 5 (fine). | 1. Mở bản đồ zoom gần.<br>2. Gọi API với gridLevel=5. | Cells nhỏ (0.02 degree ≈ 2.2km). Nhiều cells hơn, phân bố chi tiết. | - Reports exist. | Passed | 04/09/2026 | TamKnm | | | | | | | gridLevel 5 = cell 0.02 degree. |
| TC_MAP_018 | Aggregate — gridLevel ngoài phạm vi (> 5). | 1. Gọi API với gridLevel=10. | Error "gridLevel must be between 1 and 5." is displayed. | - User specifies invalid gridLevel. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(1, 5)`. |
| TC_MAP_019 | Aggregate — maxSeverity hiển thị đúng. | 1. Mở bản đồ aggregate.<br>2. Xem cell có nhiều report với severity khác nhau. | Cell hiển thị maxSeverity = severity cao nhất trong cell (Critical > High > Medium > Low). | - Cell contains mixed severity reports. | Passed | 04/09/2026 | TamKnm | | | | | | | `MaxSeverity` function compares severity rank. |
| TC_MAP_020 | Aggregate — performance cap (50,000 rows). | 1. Mở bản đồ aggregate trên viewport có rất nhiều reports. | Hệ thống giới hạn tối đa 50,000 raw points cho aggregate grouping. Kết quả vẫn chính xác cho dữ liệu được load. | - >50,000 reports in viewport. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler `.Take(50_000)` for aggregate grouping. |

---

## Bounding Box Validation

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MAP_021 | Bbox — tọa độ hợp lệ (Hà Nội). | 1. Mở bản đồ tại Hà Nội.<br>2. Bbox: minLat=20.9, maxLat=21.1, minLng=105.7, maxLng=105.9. | Reports trong khu vực Hà Nội được trả về. | - Reports exist in Hanoi. | Passed | 04/09/2026 | TamKnm | | | | | | | Valid bbox within Vietnam bounds. |
| TC_MAP_022 | Bbox — tọa độ hợp lệ (TP.HCM). | 1. Mở bản đồ tại TP.HCM.<br>2. Bbox: minLat=10.7, maxLat=10.9, minLng=106.5, maxLng=106.8. | Reports trong khu vực TP.HCM được trả về. | - Reports exist in HCMC. | Passed | 04/09/2026 | TamKnm | | | | | | | Valid bbox. |
| TC_MAP_023 | Bbox — latitude ngoài Việt Nam (< 8). | 1. Gọi API với minLat=5.0. | Error "MinLat must be between 8 and 24." is displayed. | - Invalid coordinates. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(8, 24)` for latitude. |
| TC_MAP_024 | Bbox — latitude ngoài Việt Nam (> 24). | 1. Gọi API với maxLat=30.0. | Error "MaxLat must be between 8 and 24." is displayed. | - Invalid coordinates. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(8, 24)`. |
| TC_MAP_025 | Bbox — longitude ngoài Việt Nam (< 102). | 1. Gọi API với minLng=90.0. | Error "MinLng must be between 102 and 110." is displayed. | - Invalid coordinates. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(102, 110)` for longitude. |
| TC_MAP_026 | Bbox — longitude ngoài Việt Nam (> 110). | 1. Gọi API với maxLng=120.0. | Error "MaxLng must be between 102 and 110." is displayed. | - Invalid coordinates. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(102, 110)`. |
| TC_MAP_027 | Bbox — minLat > maxLat (đảo ngược). | 1. Gọi API với minLat=21, maxLat=20. | Error "minLat must be less than maxLat and minLng less than maxLng." is displayed. | - Invalid bbox orientation. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `q.MinLat < q.MaxLat && q.MinLng < q.MaxLng`. |
| TC_MAP_028 | Bbox — quá rộng (latitude span > 6 degrees). | 1. Gọi API với minLat=8, maxLat=20 (span = 12). | Error "Bounding box is too large; zoom in." is displayed. | - Bbox too large. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaxBoundingLatSpan = 6`. |
| TC_MAP_029 | Bbox — quá rộng (longitude span > 8 degrees). | 1. Gọi API với minLng=102, maxLng=110 (span = 8 exact). | Hợp lệ (span = 8 bằng đúng max). Request thành công. | - Bbox at max boundary. | Passed | 04/09/2026 | TamKnm | | | | | | | `MaxBoundingLngSpan = 8`. Span == 8 is valid (<=). |
| TC_MAP_030 | Bbox — mode không hợp lệ. | 1. Gọi API với mode="heatmap". | Error "mode must be detail or aggregate." is displayed. | - Invalid mode. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator checks `AllowedModes = ["detail", "aggregate"]`. |

---

## Viewport Summary (Home Card)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MAP_031 | Viewport summary — default 30 ngày. | 1. Mở app (không cần đăng nhập).<br>2. Xem card "Khu vực đang xem" trên trang chủ. | Card hiển thị: tổng số báo cáo trong viewport, biểu đồ daily count 30 ngày. | - Reports exist in viewport. | Passed | 04/09/2026 | TamKnm | | | | | | | Default `days = 30`. Returns `reportCount` and `dailyCounts` array. |
| TC_MAP_032 | Viewport summary — anonymous access. | 1. Mở app không đăng nhập.<br>2. Xem card "Khu vực đang xem". | Card hiển thị bình thường. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]` endpoint. |
| TC_MAP_033 | Viewport summary — custom days (7 ngày). | 1. Mở app.<br>2. Chuyển sang "7 ngày gần đây". | Biểu đồ hiển thị 7 ngày. | - Reports exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `days=7`. |
| TC_MAP_034 | Viewport summary — filter by category. | 1. Mở app.<br>2. Chọn filter category "Rác thải xây dựng".<br>3. Xem card summary. | Summary chỉ tính báo cáo thuộc category đã chọn. | - Reports exist with multiple categories. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `categoryId`. |
| TC_MAP_035 | Viewport summary — category không tồn tại. | 1. Gọi API với categoryId không tồn tại. | Error 404 "Không tìm thấy danh mục" is displayed. | - Invalid categoryId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `categories.ExistsAsync()`. |
| TC_MAP_036 | Viewport summary — không có report trong 30 ngày. | 1. Mở app tại khu vực ít dân cư.<br>2. Xem card summary. | reportCount = 0. Biểu đồ daily counts toàn 0. | - No reports in last 30 days in viewport. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns 0 counts, daily array filled with zeros. |
| TC_MAP_037 | Viewport summary — daily counts fill missing dates with 0. | 1. Mở app.<br>2. Xem biểu đồ daily (có ngày không có report). | Biểu đồ hiển thị 0 cho ngày không có report. Không có gap. | - Some days have no reports. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler loops `periodStart → periodEnd`, fills `count = 0` for missing dates. |
| TC_MAP_038 | Viewport summary — report bị ẩn không đếm. | 1. Admin ẩn report.<br>2. Xem card summary tại khu vực đó. | Report bị ẩn không được tính trong reportCount và dailyCounts. | - Report is hidden. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters `!r.IsHidden`. |
| TC_MAP_039 | Viewport summary — bbox validation same as map reports. | 1. Gọi API với minLat > maxLat. | Error validation tương tự GET /v1/map/reports. | - Invalid bbox. | Passed | 04/09/2026 | TamKnm | | | | | | | Shares same bbox validation rules as reports endpoint. |
| TC_MAP_040 | Viewport summary — spurious warning log khi có categoryId hợp lệ. | 1. Gọi summary API với categoryId hợp lệ. | Response trả về đúng nhưng server log chứa warning "Category not found" dù category tồn tại. | - Valid categoryId. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: Handler line `logger.LogWarning("Category {CategoryId} not found", request.CategoryId.Value)` được gọi ngay sau `if (request.CategoryId.HasValue)` — luôn log warning khi có categoryId dù category hợp lệ. Đây là lỗi logic copy-paste từ block phía trên. Nên xóa dòng log warning thừa này. |
