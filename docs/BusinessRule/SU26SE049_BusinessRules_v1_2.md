SU26SE049 — Business Rules Specification

BUSINESS RULES SPECIFICATION

Crowdsourced Application for Reporting Environmental Pollution

Ứng dụng báo cáo điểm rác thải và ô nhiễm môi trường

Project Code: SU26SE049

| Document | Business Rules Specification (BR) |
|---|---|
| Version | 1.2 |
| Status | Draft |
| Last Updated | 07/06/2026 |
| Supervisor | Nguyễn Thị Cẩm Hương (huongntc2@fe.edu.vn) |
| Duration | 01/01/2026 – 30/04/2026 |
| Scope | Toàn quốc Việt Nam (đô thị lớn, TP trực thuộc TW, tỉnh lẻ & nông thôn) |
| Change Summary | Bổ sung thực thể & vai trò Công ty Dịch vụ Môi trường (Company Manager + Company Staff) cùng cơ chế onboarding theo hợp đồng; mô hình điều phối dọn dẹp 2 hình thức (công ty đầu nguồn vs đội cộng đồng cấp xã/phường); tách 2 luồng song song theo NHU CẦU (dọn dẹp vs. xử phạt) thay cho phân theo loại ô nhiễm — Inspection Team xử lý xử phạt cho mọi loại ô nhiễm khi LEO lập InspectionReport. |

# 1. Tổng quan (Overview)

Tài liệu này đặc tả toàn bộ Business Rules (BR) của hệ thống Crowdsourced Application for Reporting Environmental Pollution. Các rule được nhóm theo chức năng nghiệp vụ. Phạm vi triển khai là toàn quốc Việt Nam — bao phủ từ đô thị lớn (TP.HCM, Hà Nội), các Thành phố trực thuộc Trung ương, đến tỉnh lẻ và khu vực nông thôn/huyện vùng xa.

Hệ thống có 10 actors: Citizen; Department Environmental Officer (DEO – cấp Tỉnh/Thành phố); Local Environmental Officer (LEO – cấp Xã/Phường); Company Manager (Quản lý Công ty Dịch vụ Môi trường) [NEW v1.2]; Company Staff (Nhân viên Công ty) [NEW v1.2]; Environmental Cleanup Team (Cleaner); Environmental Inspection Team (Inspector); System Administrator; AI Service (automated); và Community Organization (optional).

Trong đó 8 vai trò có thể gán cho người dùng (human roles): Citizen, DEO, LEO, Company Manager, Company Staff, Cleaner, Inspector, Admin. AI Service là dịch vụ tự động và Community Organization là vai trò chỉ-đọc/khai thác open data.

Mỗi rule có định danh duy nhất theo cú pháp BR-<MODULE>-<NNN>, giúp truy vết trong tài liệu phân tích thiết kế và test case.

## 1.1 Quy ước định danh (ID Convention)

- BR-AUTH-xxx: Xác thực, tài khoản

- BR-REP-xxx: Quản lý báo cáo ô nhiễm

- BR-MAP-xxx: Bản đồ & vị trí

- BR-OFF-xxx: Environmental Officer (DEO & LEO)

- BR-CLN-xxx: Environmental Cleanup Team

- BR-INS-xxx: Environmental Inspection Team (xử phạt)

- BR-ORG-xxx: Cơ cấu tổ chức & định tuyến theo địa giới hành chính

- BR-CMP-xxx: Công ty Dịch vụ Môi trường & quản lý hợp đồng  [NEW v1.2]

- BR-NTF-xxx / BR-CMT-xxx: Thông báo & bình luận

- BR-GAM-xxx: Gamification (points, badges)

- BR-AI-xxx: AI Service

- BR-ADM-xxx: Quản trị hệ thống

- BR-DAT-xxx: Dữ liệu, quyền riêng tư, tuân thủ

- BR-SYS-xxx: Phi chức năng, hệ thống

## 1.2 Phân loại Rule Type

Validation: Ràng buộc dữ liệu đầu vào. Business Logic: Công thức, thuật toán nghiệp vụ. Authorization: Phân quyền. State Machine: Quy tắc chuyển trạng thái. Security / Privacy / Compliance: An toàn & tuân thủ. SLA: Thỏa thuận mức dịch vụ. Anti-Spam / Anti-Fraud: Chống gian lận. Auditability: Ghi log. Performance / Scalability / Availability: Phi chức năng.

## 1.3 Các thay đổi chính trong v1.2 (Change Log)

- Thực thể & vai trò Công ty Dịch vụ Môi trường (BR-CMP): bổ sung Environmental Service Company (ESC) với 2 loại — Subsidiary (trực thuộc, chủ lực) và Bidding (tư nhân đấu thầu) — cùng 2 vai trò mới Company Manager và Company Staff.

- Cơ chế onboarding công ty: DEO tạo tài khoản công ty trong phạm vi tỉnh; CM đặt mật khẩu lần đầu qua cơ chế reset-password (token email single-use), KHÔNG dùng module activation riêng. Hiệu lực tác nghiệp dựa trên Company.Status; thời hạn hợp đồng (Bidding) chỉ là metadata, KHÔNG khóa định tuyến.

- Mô hình điều phối dọn dẹp 2 hình thức: (a) Công ty đầu nguồn (đô thị lớn, TP trực thuộc TW, trung tâm tỉnh) và (b) Đội cộng đồng cấp xã/phường (HTX / Tổ tự quản — vùng nông thôn, huyện vùng xa).

- Phân luồng theo NHU CẦU thay cho theo loại ô nhiễm: Khi xác minh, LEO quyết định độc lập: (a) cần dọn dẹp → điều phối Cleanup (công ty hoặc đội cộng đồng); (b) có chủ thể vi phạm cần xử phạt → LEO lập InspectionReport cho Inspection Team. Inspection Team xử lý xử phạt cho MỌI loại ô nhiễm khi LEO lập InspectionReport.

- Thời hạn hợp đồng phân theo LOẠI công ty: chỉ Bidding (đấu thầu) có thời hạn; Subsidiary (trực thuộc chủ lực) VÔ THỜI HẠN. Thời hạn hợp đồng chỉ là metadata, KHÔNG dùng khóa định tuyến — hiệu lực tác nghiệp dựa trên Company.Status (BR-CMP-005). Hệ thống chỉ phân biệt 2 loại công ty, KHÔNG mô hình hóa cơ chế đấu thầu.

- Định tuyến 2 bước & quan hệ Company–Ward là N–N: một xã/phường có NHIỀU đơn vị (công ty đầu mối + đội cộng đồng HTX/rác dân lập, ngang hàng). Định tuyến 2 bước: LEO tiếp nhận → LEO điều phối (app gợi ý đơn vị); một số ít trục lớn gắn cờ 'tuyến cấp TP' → escalate DEO (BR-ORG-016). KHÔNG phân lớp GIS mặt tiền/hẻm, KHÔNG mô hình hóa độc quyền theo tuyến/mét (BR-CMP-014).

- Bỏ truy cập ẩn danh: khách chưa đăng nhập CHỈ xem bản đồ & thông tin công khai (read-only); gửi báo cáo, bình luận và mọi tính năng tương tác đều BẮT BUỘC đăng ký tài khoản (BR-AUTH-017 đảo ngược, BR-CMT-001, BR-GAM-002). "Ẩn danh" nay chỉ còn là tùy chọn ẩn TÊN HIỂN THỊ của một tài khoản đã đăng nhập (BR-REP-012).

- Danh mục loại ô nhiễm còn 3 loại: Rác thải, Nước thải, Hóa chất (đã bỏ Không khí và Tiếng Ồn). Mọi báo cáo đều có thể sinh CleanupTask; xử phạt (InspectionReport) là nhánh tùy chọn khi có chủ thể vi phạm. Cập nhật BR-REP-005, bộ lọc bản đồ, phân loại AI và danh mục Admin.

- Cập nhật: BR-AUTH-008/009 (quyền tạo tài khoản công ty của DEO), BR-ADM-002 (8 human roles), BR-ORG-013, BR-OFF-011, BR-CLN-001, BR-INS-001, BR-REP-014/020/021, Role Matrix và Phụ lục A (state machine).

- Phạm vi: mở rộng chính thức ra toàn quốc Việt Nam. Riêng TP.HCM (thị trường mục tiêu ban đầu) cấu hình 168 Local Environmental Office / LEO tương ứng 168 đơn vị hành chính cấp xã/phường sau sáp nhập (113 phường, 54 xã, 1 đặc khu).

# 2. Actors và ma trận quyền (Role Matrix)

Hệ thống có 10 actors. Ma trận dưới đây tóm tắt quyền của từng actor với các chức năng cốt lõi. Cột viết tắt: Cit=Citizen, DEO, LEO, CM=Company Manager [NEW], CS=Company Staff [NEW], Cln=Cleaner, Ins=Inspector, Adm=Admin, AI=AI Service, Com=Community Organization.

| Chức năng | Cit | DEO | LEO | CM | CS | Cln | Ins | Adm | AI | Com |
|---|---|---|---|---|---|---|---|---|---|---|
| Gửi báo cáo ô nhiễm | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | — | ✗ |
| Xem bản đồ công khai | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| Xác minh & tiếp nhận báo cáo | ✗ | ✓¹ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | — | ✗ |
| Điều phối dọn dẹp (gán Cleanup) | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | — | ✗ |
| Lập InspectionReport (cần xử phạt) | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | — | ✗ |
| Quản lý công ty & nhân viên | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✓ | — | ✗ |
| Nhận & phân công đội công ty | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✓ | — | ✗ |
| Dọn dẹp & cập nhật before/after | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✓ | — | ✗ |
| Lập biên bản & ra QĐ xử phạt | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | — | ✗ |
| Tạo tài khoản công ty (theo HĐ) | ✗ | ✓² | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | — | ✗ |
| Cấu hình hệ thống | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | — | ✗ |
| Phân loại / AI gợi ý | — | — | — | — | — | — | — | — | ✓ | — |
| Xuất open data | ✗ | T.tỉnh | Xã/P | Cty | ✗ | ✗ | ✗ | ✓ | — | ✓ |
| Leaderboard / Badge | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ql | — | ✗ |

Ghi chú:

- ¹ DEO chỉ xác minh báo cáo trong 'Department Common Queue' (xã/phường chưa onboard). Luồng xác minh & điều phối thường ngày do LEO cấp xã/phường đảm nhận.

- ² DEO tạo tài khoản Công ty (Company Manager) trong phạm vi tỉnh/thành phố mình quản lý, theo hợp đồng (BR-CMP-001..004). Admin vẫn có toàn quyền tạo mọi vai trò trên toàn hệ thống.

- Company Staff thực hiện luồng vận hành giống Cleaner nhưng thuộc đội do Company Manager quản lý (không do LEO mời).

- Cleanup được thực hiện bởi đội Công ty (đô thị/đầu nguồn) HOẶC đội cộng đồng cấp xã/phường (HTX/Tổ tự quản — vùng nông thôn).

- Khách chưa đăng nhập (Guest) KHÔNG phải là actor: chỉ xem được bản đồ & thông tin công khai (read-only). Mọi chức năng ghi/tương tác — gửi báo cáo, bình luận, theo dõi, gamification — đều bắt buộc đăng nhập (BR-AUTH-017 [CẬP NHẬT v1.2]).

- Inspection Team xử lý xử phạt cho mọi loại ô nhiễm khi LEO lập InspectionReport (BR-INS-001 [NEW v1.2]).

# 3. Xác thực & Tài khoản (Authentication & Account)

Nhóm rule quản lý đăng ký, đăng nhập, session và quản lý hồ sơ cho tất cả các loại user.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| 1.1 Đăng ký tài khoản (Registration) | 1.1 Đăng ký tài khoản (Registration) | 1.1 Đăng ký tài khoản (Registration) | 1.1 Đăng ký tài khoản (Registration) | 1.1 Đăng ký tài khoản (Registration) |
| BR-AUTH-001 | Định dạng Email | Email phải đúng định dạng RFC 5322 (có @, domain hợp lệ). | Format | Email không đúng định dạng. |
| BR-AUTH-002 | Email duy nhất | Mỗi email chỉ được đăng ký một tài khoản duy nhất trong hệ thống. | Uniqueness | Email này đã được sử dụng. |
| BR-AUTH-003 | Định dạng SĐT | Chấp nhận định dạng Việt Nam: +84 hoặc 0 + 9 chữ số, bắt đầu bằng 3/5/7/8/9. | Format | Số điện thoại không hợp lệ |
| BR-AUTH-004 | SĐT duy nhất | Mỗi số điện thoại chỉ được gắn với một tài khoản duy nhất. | Uniqueness | Số điện thoại đã được sử dụng |
| BR-AUTH-005 | Độ mạnh mật khẩu | Tối thiểu 8 ký tự, tối đa 32 ký tự, bắt buộc có ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt (!@#$%^&*). | Validation | Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt |
| BR-AUTH-006 | Xác nhận mật khẩu | Trường 'Xác nhận mật khẩu' phải khớp chính xác với trường 'Mật khẩu'. | Validation | Mật khẩu xác nhận không khớp |
| BR-AUTH-007 | Thời hạn mã OTP | Mã xác thực (Email/OTP) có hiệu lực trong vòng 10 phút. Quá hạn mã sẽ mất hiệu lực. | Business Process | Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi lại |
| BR-AUTH-008 | Vai trò mặc định | Tài khoản tự đăng ký qua app mặc định là 'Citizen'. Việc nâng/đổi vai trò (Cleaner, Inspector, Company Staff…) do quy trình mời/onboarding của LEO/Company Manager/Admin thực hiện. | Authorization | N/A (Hệ thống tự gán) |
| BR-AUTH-009 | Quyền quản trị vai trò | Các vai trò nội bộ chỉ được tạo/gán theo phân cấp: • Admin: gán mọi vai trò trên toàn hệ thống. • DEO: tạo tài khoản Company Manager (Công ty Dịch vụ Môi trường) trong phạm vi tỉnh/thành phố theo hợp đồng (BR-CMP-001); onboarding LEO cấp xã/phường. • LEO: mời Citizen trở thành Cleaner/Inspector cho đội cấp xã/phường (BR-ORG-020). • Company Manager: thêm Company Staff cho công ty mình (BR-CMP-010). | Authorization | N/A (Chặn ở backend/UI) |
| BR-AUTH-010 | Trường bắt buộc | Các trường: Họ tên, email, SĐT, mật khẩu là bắt buộc không được để trống. | Validation | Vui lòng điền đầy đủ thông tin bắt buộc |
| BR-AUTH-011 | Định dạng Họ tên | Họ tên từ 2–50 ký tự, không chứa ký tự đặc biệt (trừ dấu tiếng Việt). | Validation | Họ tên không hợp lệ (từ 2-50 ký tự và không chứa ký tự đặc biệt) |
| BR-AUTH-012 | Chấp nhận điều khoản | Người dùng phải tick chọn đồng ý 'Điều khoản sử dụng' và 'Chính sách quyền riêng tư' trước khi hoàn tất. | Compliance | Bạn phải đồng ý với điều khoản để đăng ký |
| 1.2 Đăng nhập (Login) | 1.2 Đăng nhập (Login) | 1.2 Đăng nhập (Login) | 1.2 Đăng nhập (Login) | 1.2 Đăng nhập (Login) |
| BR-AUTH-013 | Thông tin đăng nhập | Người dùng đăng nhập bằng email HOẶC số điện thoại + mật khẩu. Hệ thống không phân biệt chữ hoa/thường với email. | Validation | Thông tin đăng nhập không chính xác |
| BR-AUTH-014 | Giới hạn số lần sai mật khẩu | Nhập sai mật khẩu quá 5 lần liên tiếp trong 15 phút → khóa tài khoản tạm thời 30 phút. Phải gõ CAPTCHA từ lần sai thứ 3. | Security | Tài khoản tạm bị khóa do đăng nhập sai nhiều lần. Thử lại sau 30 phút |
| BR-AUTH-015 | Tài khoản bị khóa | Tài khoản có trạng thái 'Inactive' hoặc 'Banned' không được phép đăng nhập. Tài khoản Company Manager/Company Staff thuộc công ty hết hạn hợp đồng (Status=Expired) cũng bị chặn đăng nhập tác nghiệp (BR-CMP-005). | Authorization | Tài khoản của bạn đã bị vô hiệu hóa. Liên hệ Admin |
| BR-AUTH-016 | Phiên làm việc (session) | Access token hết hạn sau 24 giờ; refresh token 30 ngày. Người dùng không hoạt động quá 30 phút trên web → tự đăng xuất. Refresh bị từ chối nếu công ty đã hết hạn hợp đồng (BR-CMP-005). | Security | Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại |
| BR-AUTH-017 | Phạm vi truy cập của khách (Guest) [CẬP NHẬT v1.2] | Khách CHƯA đăng nhập CHỈ được xem bản đồ công khai và các thông tin hiển thị công khai (read-only). MỌI tính năng tương tác — gửi báo cáo, bình luận, theo dõi/vote, gamification, nhận thông báo, xuất dữ liệu… — đều BẮT BUỘC đăng ký tài khoản & đăng nhập. Hệ thống KHÔNG hỗ trợ gửi báo cáo ẩn danh (không tài khoản). Mọi hành vi ghi đều gắn với một tài khoản đã xác thực để đảm bảo truy vết & chống lạm dụng. | Authorization | Vui lòng đăng nhập để sử dụng tính năng này |
| BR-AUTH-018 | Quên mật khẩu | Cho phép reset qua link email (hiệu lực 15 phút) hoặc OTP SĐT (hiệu lực 5 phút). Link/OTP chỉ dùng 1 lần. | Security | Link đặt lại mật khẩu đã hết hạn hoặc đã được sử dụng |
| 1.3 Quản lý hồ sơ (Profile) | 1.3 Quản lý hồ sơ (Profile) | 1.3 Quản lý hồ sơ (Profile) | 1.3 Quản lý hồ sơ (Profile) | 1.3 Quản lý hồ sơ (Profile) |
| BR-AUTH-019 | Cập nhật thông tin cá nhân | Người dùng chỉ được cập nhật: họ tên, ảnh đại diện, SĐT (phải xác thực lại). Email và vai trò không thể tự đổi. | Authorization | Không thể thay đổi email/vai trò. Liên hệ quản trị viên |
| BR-AUTH-020 | Đổi mật khẩu | Yêu cầu nhập mật khẩu cũ. Mật khẩu mới không được trùng 3 mật khẩu gần nhất. | Security | Mật khẩu mới không được trùng với các mật khẩu đã sử dụng |
| BR-AUTH-021 | Xóa tài khoản | Người dùng có thể yêu cầu xóa tài khoản. Hệ thống ẩn dữ liệu cá nhân (soft delete), giữ lại báo cáo với trạng thái 'Anonymized' trong 90 ngày trước khi xóa vĩnh viễn. | Compliance (GDPR) | Tài khoản sẽ được xóa sau 90 ngày. Bạn có thể khôi phục trước thời hạn |

# 4. Cơ cấu tổ chức & Định tuyến báo cáo (Organization & Routing)

Nhóm rule mô tả mô hình tổ chức 2 cấp (Department of Environmental Management cấp Tỉnh/Thành phố; Local Environmental Office cấp Xã/Phường), cách định tuyến báo cáo theo địa giới hành chính, và cơ chế mời người vào đội cấp xã/phường. Trong v1.2, bước xác minh của LEO được mở rộng để quyết định song song giữa dọn dẹp và xử phạt.

## 4.1 Mô hình tổ chức tổng quan

- Department of Environmental Management (cấp Tỉnh/Thành phố) – đơn vị quản lý cấp cao; do DEO phụ trách.

- Local Environmental Office (cấp Xã/Phường) – trực thuộc Department; mỗi xã/phường có 1 LEO + 0..n Cleanup Team + 0..n Inspection Team. (TP.HCM: 168 đơn vị xã/phường ⇒ 168 LEO/khu vực.)

- Environmental Service Company (ESC) [NEW v1.2] – nhà cung cấp dịch vụ đầu nguồn (dọn dẹp), được DEO onboarding theo hợp đồng; phủ 1..n xã/phường (BR-CMP-*).

- Environmental Cleanup Team – đội dọn dẹp vật lý; thuộc một Company (đô thị) hoặc một Local Office (đội cộng đồng cấp xã/phường vùng nông thôn).

- Environmental Inspection Team – đội xử phạt vi phạm (thuộc Local Office); xử lý xử phạt cho mọi loại ô nhiễm khi được LEO lập InspectionReport.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| Cấu trúc tổ chức 2 cấp | Cấu trúc tổ chức 2 cấp | Cấu trúc tổ chức 2 cấp | Cấu trúc tổ chức 2 cấp | Cấu trúc tổ chức 2 cấp |
| BR-ORG-001 | Cấp 1 – Department of Environmental Management | Department (cấp Tỉnh/Thành phố) là đơn vị cấp cao nhất, quản lý toàn bộ Local Office trực thuộc trong phạm vi tỉnh/thành. Quyền: xem báo cáo toàn tỉnh, quản lý hàng đợi chung, tái phân công LEO, onboarding công ty (BR-CMP), xem KPI tổng hợp. | Authorization | N/A |
| BR-ORG-002 | Cấp 2 – Local Environmental Office | Local Office (cấp Xã/Phường) trực thuộc Department. Mỗi xã/phường có 1 LEO và 0..n Cleanup Team, 0..n Inspection Team. LEO chỉ tiếp nhận báo cáo phát sinh trong phạm vi xã/phường mình phụ trách. | Authorization | N/A |
| BR-ORG-003 | Quan hệ Office – Team | Mỗi Local Office sở hữu 0..n đội cộng đồng (Cleanup/Inspection). Một Team chỉ thuộc 1 chủ thể (Local Office hoặc Company) tại một thời điểm. Điều chuyển Team phải do Admin thực hiện và ghi audit log. | Data Integrity | N/A |
| BR-ORG-004 | Phạm vi địa lý của Office | Mỗi Local Office gắn với 1 ranh giới hành chính (polygon GeoJSON) tương ứng xã/phường. Hệ thống dùng polygon này để định tuyến báo cáo (BR-ORG-010). | Data Integrity | N/A |
| BR-ORG-005 | Mô hình dịch vụ trong một xã/phường — ĐA ĐƠN VỊ [CẬP NHẬT] | Một xã/phường có thể có NHIỀU đơn vị thu gom cùng hoạt động (bình thường, KHÔNG phải 1 phường = 1 công ty): • Công ty đầu mối của phường (ESC) — đội Company Staff, lo các đường/khu vực chính. • Đội cộng đồng cấp xã/phường: HTX / Tổ tự quản / 'đường dây rác dân lập' lo hẻm, ngõ — là Cleanup Team (không thuộc công ty), NGANG HÀNG công ty. • Một số ít trục lớn do đơn vị cấp Thành phố (CITENCO-type) phụ trách → báo cáo trên các tuyến này được escalate lên DEO, ngoài quyền LEO (BR-ORG-016). LEO điều phối từng báo cáo tới đơn vị phù hợp; hệ thống chỉ GỢI Ý, KHÔNG mô hình hóa độc quyền theo tuyến/mét. | Business Rule | N/A |
| Định tuyến báo cáo theo địa giới hành chính | Định tuyến báo cáo theo địa giới hành chính | Định tuyến báo cáo theo địa giới hành chính | Định tuyến báo cáo theo địa giới hành chính | Định tuyến báo cáo theo địa giới hành chính |
| BR-ORG-010 | Định tuyến theo xã/phường (lớp 1) [CẬP NHẬT v1.2] | Khi báo cáo có tọa độ GPS hợp lệ (BR-REP-003), hệ thống xác định xã/phường chứa điểm GPS (point-in-polygon). Đã onboard → gán cho LEO của xã/phường đó. Chưa onboard → hàng đợi chung của Department (BR-ORG-011). LƯU Ý: xác định phường là điều kiện CẦN nhưng CHƯA ĐỦ — phải qua tiếp định tuyến đa lớp theo cấp tuyến đường & loại rác (BR-ORG-016). | Business Logic | N/A |
| BR-ORG-011 | Hàng đợi chung cấp Tỉnh/Thành phố | Báo cáo thuộc xã/phường chưa onboard vào 'Department Common Queue'. Admin/DEO có quyền: (a) gán cho LEO xã/phường gần nhất theo centroid, (b) tự xử lý nếu cấp tỉnh có team/công ty trực thuộc trực tiếp. | Business Logic | N/A |
| BR-ORG-012 | Conflict of interest theo cấp | DEO/LEO không được xác minh báo cáo do chính mình gửi (kế thừa BR-OFF-004). Báo cáo ngoài phạm vi xã/phường mà LEO phụ trách → LEO không có quyền tiếp nhận (trừ khi Admin re-assign). | Authorization | Bạn không có quyền tiếp nhận báo cáo ngoài khu vực |
| BR-ORG-013 | Quyết định xử lý khi xác minh [CẬP NHẬT v1.2] | Khi xác minh, ngoài việc kiểm tra tính hợp lệ, LEO xác minh xem có chủ thể vi phạm/gây ô nhiễm hay không, rồi ra quyết định ĐỘC LẬP cho 2 nhánh (có thể đồng thời): • Cần dọn dẹp vật lý → gán Cleanup (đội Công ty hoặc đội cộng đồng) theo BR-OFF-011. • Có chủ thể cần xử phạt → LEO lập InspectionReport, đẩy cho Inspection Team (BR-INS-001), áp dụng cho MỌI loại ô nhiễm. • Rác đặc thù (AI/LEO nhận diện rác y tế, kim tiêm, hóa chất nguy hại, hoặc xà bần khối lượng lớn): KHÔNG giao đội dọn dẹp thường (sai chức năng) → đẩy cảnh báo để LEO liên hệ đơn vị chuyên trách, đồng thời thường kèm InspectionReport để lập biên bản người xả thải. Loại ô nhiễm (BR-REP-005) chỉ mang tính tham chiếu/gợi ý, KHÔNG còn quyết định cứng đội xử lý. | Business Logic | N/A |
| BR-ORG-014 | SLA tiếp nhận cấp xã/phường | LEO phải phản hồi (Accept/Reject) báo cáo được định tuyến trong vòng 24 giờ. Quá hạn → tự eskalat lên DEO và đánh dấu 'SLA Breach – Acceptance'. | SLA | Cảnh báo: Báo cáo [ID] vượt SLA tiếp nhận 24h |
| BR-ORG-015 | Re-assign khi LEO không thể xử lý | Khi LEO 'Reject' báo cáo: bắt buộc nêu lý do ≥ 20 ký tự. Báo cáo về hàng đợi chung của Department; DEO gán cho LEO khác hoặc giữ xử lý cấp tỉnh. | Business Process | Vui lòng nêu lý do từ chối ≥ 20 ký tự |
| BR-ORG-016 | Định tuyến 2 bước & escalation tuyến cấp TP [CẬP NHẬT] | Bước 1 — Tiếp nhận: point-in-polygon (BR-ORG-010) xác định xã/phường → LEO của phường tiếp nhận & xác minh báo cáo. Bước 2 — Điều phối: LEO chọn đơn vị xử lý; hệ thống GỢI Ý danh sách đơn vị khả dụng trong phường (công ty đầu mối / đội cộng đồng HTX/rác dân lập). LEO là người quyết. Escalation: nếu báo cáo nằm trên một 'tuyến cấp Thành phố' (cờ do Admin/DEO cấu hình cho một số ít trục lớn) → KHÔNG gán cho LEO; đẩy lên hàng đợi cấp Tỉnh/TP để DEO điều phối đơn vị cấp TP (CITENCO-type). Hệ thống KHÔNG phân lớp mặt tiền/hẻm bằng GIS, KHÔNG mô hình hóa hợp đồng theo mét. | Business Logic | N/A (gợi ý đích cho LEO) |
| Mời người vào đội cấp xã/phường (Invitation)   [NEW v1.2] | Mời người vào đội cấp xã/phường (Invitation)   [NEW v1.2] | Mời người vào đội cấp xã/phường (Invitation)   [NEW v1.2] | Mời người vào đội cấp xã/phường (Invitation)   [NEW v1.2] | Mời người vào đội cấp xã/phường (Invitation)   [NEW v1.2] |
| BR-ORG-020 | Mời thành viên đội cộng đồng | LEO thêm người vào đội cấp xã/phường qua cơ chế mời theo email: người dùng đăng ký tài khoản Citizen bình thường → LEO tìm theo email và gửi lời mời (vào một Cleanup/Inspection Team, hoặc vào phụ trách trong phường). Khi người dùng CHẤP NHẬN: vai trò đổi tương ứng (Cleaner cho đội dọn dẹp, Inspector cho đội thanh tra) và được thêm vào đội. Mọi thay đổi vai trò ghi audit log (BR-ADM-010). | Business Process | N/A |
| BR-ORG-021 | Hiệu lực lời mời | Lời mời có hiệu lực 7 ngày, dùng một lần. Quá hạn/đã dùng → phải gửi lại. Người dùng có quyền từ chối; khi đó giữ nguyên vai trò Citizen. | Business Process | Lời mời đã hết hạn hoặc đã được sử dụng |

# 5. Quản lý báo cáo ô nhiễm (Pollution Report Management)

Vòng đời báo cáo từ khởi tạo, chuyển trạng thái đến xử lý trùng lặp. Trong v1.2, một báo cáo có thể đồng thời sinh ra một CleanupTask và một InspectionReport liên kết, do LEO quyết định khi xác minh (BR-ORG-013).

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| 2.1 Tạo báo cáo ô nhiễm (Create Pollution Report) | 2.1 Tạo báo cáo ô nhiễm (Create Pollution Report) | 2.1 Tạo báo cáo ô nhiễm (Create Pollution Report) | 2.1 Tạo báo cáo ô nhiễm (Create Pollution Report) | 2.1 Tạo báo cáo ô nhiễm (Create Pollution Report) |
| BR-REP-001 | Ảnh bắt buộc | Mỗi báo cáo phải có ít nhất 1 ảnh, tối đa 5 ảnh. Định dạng .jpg/.jpeg/.png/.webp, mỗi ảnh ≤ 10MB. | Validation | Vui lòng thêm ít nhất 1 ảnh. Tối đa 5 ảnh, mỗi ảnh ≤ 10MB |
| BR-REP-002 | Video (lựa chọn) | Cho phép tải tối đa 1 video cho mỗi báo cáo. Định dạng hỗ trợ mp4, mov. | Validation | Chỉ hỗ trợ 01 video định dạng MP4, MOV |
| BR-REP-003 | Tọa độ GPS bắt buộc | Tọa độ (lat, lng) phải có và nằm trong lãnh thổ Việt Nam (lat: 8.0–24.0; lng: 102.0–110.0). | Validation | Vui lòng bật GPS. Vị trí phải nằm trong lãnh thổ Việt Nam |
| BR-REP-004 | Mô tả báo cáo | Không bắt buộc, nhưng nếu nhập phải từ 10–1000 ký tự, không chứa ngôn từ tục tĩu (qua bộ lọc từ). | Validation + Moderation | Mô tả phải từ 10–1000 ký tự / chứa nội dung không phù hợp |
| BR-REP-005 | Loại ô nhiễm (3 loại chính thức) [CẬP NHẬT] | Chọn đúng 1 trong 3 loại: Rác thải, Nước thải, Hóa chất. (Đã bỏ Tiếng Ồn và Không khí; không có tùy chọn 'Khác'.) Loại ô nhiễm KHÔNG còn quyết định cứng đội xử lý. Nó là dữ liệu phân tích & gợi ý nhu cầu dọn dẹp; việc gán Cleanup và/hoặc lập InspectionReport do LEO quyết khi xác minh (BR-ORG-013). | Validation | Vui lòng chọn 1 trong 3 loại ô nhiễm |
| BR-REP-006 | Mức độ nghiêm trọng | Chọn 1 trong 4 mức: Low / Medium / High / Critical. Không chọn → mặc định 'Medium', AI đánh giá lại (BR-AI-003). | Default + AI Override | N/A |
| BR-REP-008 | Cảnh báo báo cáo tồn đọng | Báo cáo hợp lệ ở trạng thái Pending quá 72 giờ → tự đánh dấu Overdue và gửi cảnh báo. | Business Logic / Workflow | N/A (Hệ thống tự gửi Notify) |
| BR-REP-009 | Cảnh báo chưa phân công | Báo cáo 'Verified' quá 24 giờ mà Assigned_To trống → cảnh báo điều phối viên (LEO phụ trách hoặc DEO). | Workflow Logic | N/A (Hệ thống tự gửi Notify) |
| BR-REP-010 | Giới hạn spam theo tần suất | Một Citizen tối đa 5 báo cáo/giờ và 20 báo cáo/24 giờ. Vượt ngưỡng → tạm khóa gửi 1 giờ. | Anti-Spam | Bạn đã đạt giới hạn gửi báo cáo. Thử lại sau [X] phút |
| BR-REP-011 | Ảnh phải có metadata hợp lệ | Ưu tiên lấy GPS & timestamp từ EXIF. Ảnh đã chỉnh sửa quá 1 giờ so với thời điểm gửi → cảnh báo & đánh dấu 'Suspicious'. | Data Quality | Ảnh có thể không phản ánh hiện trạng thực tế |
| BR-REP-012 | Ẩn danh tính người gửi (tùy chọn) [CẬP NHẬT v1.2] | Người gửi (đã đăng nhập) có thể chọn ẩn TÊN HIỂN THỊ công khai trên báo cáo để bảo vệ danh tính (vd. tố giác vi phạm). Hệ thống vẫn lưu tài khoản thật phía sau; LEO/DEO và Admin xem được (ghi audit log). Đây KHÔNG phải gửi báo cáo ẩn danh không tài khoản — mọi báo cáo đều gắn với một tài khoản đã xác thực (BR-AUTH-017). | Privacy | N/A |
| BR-REP-013 | Trạng thái khởi tạo | Báo cáo mới luôn có status = 'Submitted'. Không thể set trực tiếp trạng thái khác khi tạo. | State Machine | N/A |
| BR-REP-014 | Ảnh before/after khi Resolved (Cleanup) [CẬP NHẬT v1.2] | Áp dụng cho nhánh dọn dẹp (CleanupTask, bất kể loại ô nhiễm). Khi 'Resolved' phải upload ≥ 1 ảnh 'before' và ≥ 1 ảnh 'after'; ảnh 'after' chụp ≤ 24h trước thời điểm submit. Nhánh xử phạt (InspectionReport) áp dụng BR-INS-* thay thế. | Validation | Cần upload ảnh trước và sau khi xử lý để hoàn tất |
| BR-REP-015 | Xác nhận hài lòng của Citizen | Sau 'Resolved', Citizen có 7 ngày để xác nhận. Không hài lòng → quay về 'In Progress' (tối đa 2 lần re-open). | Business Process | Báo cáo đã được re-open / Đã hết số lần mở lại |
| BR-REP-016 | Auto-close | Báo cáo 'Resolved' không được xác nhận trong 7 ngày → tự chuyển 'Closed'. | Automation | N/A |
| BR-REP-017 | Không thể xóa báo cáo đã verified | Citizen chỉ xóa báo cáo ở trạng thái 'Submitted' và chưa có tương tác AI/Officer. Sau 'Verified' → không được xóa, chỉ yêu cầu ẩn. | Data Integrity | Báo cáo đã được xác nhận. Không thể xóa |
| BR-REP-018 | Đánh giá của Citizen | Sau 'Resolved', citizen có thể đánh giá, bình luận, giao tiếp tại địa điểm đã hoàn thành. | Quality | N/A |
| BR-REP-019 | Lưu nháp (Draft) | Citizen lưu nháp tối đa 3 báo cáo. Nháp tự xóa sau 7 ngày không cập nhật. | Business Process | Bạn đã đạt giới hạn 3 bản nháp. Xóa bớt hoặc gửi đi |
| 2.2 Trạng thái báo cáo (Report Status Lifecycle) | 2.2 Trạng thái báo cáo (Report Status Lifecycle) | 2.2 Trạng thái báo cáo (Report Status Lifecycle) | 2.2 Trạng thái báo cáo (Report Status Lifecycle) | 2.2 Trạng thái báo cáo (Report Status Lifecycle) |
| BR-REP-020 | Quy tắc chuyển trạng thái [CẬP NHẬT v1.2] | Báo cáo (umbrella) đi theo vòng đời dọn dẹp: Submitted → Verified → In Progress → Resolved → Closed. Nhánh phụ: Submitted → Rejected; Verified → Duplicate. Nhánh xử phạt là một InspectionReport LIÊN KẾT, có vòng đời riêng (BR-INS-012/013/020): Draft → Penalty Issued → (Paid/Overdue) → Closed, hoặc Closed – No Violation. Báo cáo chỉ 'Closed' khi nhánh dọn dẹp đã hoàn tất VÀ mọi InspectionReport liên kết đã kết thúc (hoặc không có nhánh xử phạt). Xem Phụ lục A. | State Machine | Không thể chuyển từ trạng thái [X] sang [Y] |
| BR-REP-021 | Ai được chuyển trạng thái [CẬP NHẬT v1.2] | Submitted → Verified/Rejected/Duplicate: LEO (hoặc DEO với queue chung). Verified → In Progress: LEO khi gán Cleanup (đội Công ty hoặc đội cộng đồng). In Progress → Resolved: đội dọn dẹp đang phụ trách (Company Staff hoặc Cleaner). Penalty Issued / No Violation: Inspection Team trên InspectionReport liên kết. Resolved → Closed: Citizen xác nhận HOẶC tự động sau 7 ngày. | Authorization | Bạn không có quyền thay đổi trạng thái này |
| BR-REP-022 | Lý do từ chối (Rejected) | Khi LEO/DEO chuyển 'Rejected', bắt buộc lý do ≥ 20 ký tự; gửi cho người báo cáo qua notification. | Validation | Vui lòng nhập lý do từ chối (≥ 20 ký tự) |
| 2.3 Xử lý trùng lặp (Duplicate Handling) | 2.3 Xử lý trùng lặp (Duplicate Handling) | 2.3 Xử lý trùng lặp (Duplicate Handling) | 2.3 Xử lý trùng lặp (Duplicate Handling) | 2.3 Xử lý trùng lặp (Duplicate Handling) |
| BR-REP-030 | Định nghĩa trùng lặp | Hai báo cáo trùng nếu: (a) cùng loại ô nhiễm; (b) GPS ≤ 50m; (c) trong vòng 24 giờ. | Business Rule | N/A |
| BR-REP-031 | Cờ nghi ngờ trùng lặp | AI tự gán cờ 'possible_duplicate' nếu khớp BR-REP-030. LEO quyết định cuối cùng. | AI-Assisted | N/A |
| BR-REP-032 | Gộp báo cáo trùng | Báo cáo sau gắn với báo cáo gốc (primary); bình luận/ảnh merge vào gốc. Người gửi vẫn được +50% điểm báo cáo gốc và +1 số người báo cáo địa điểm. | Business Rule + Gamification | N/A |
| BR-REP-033 | Flag duplicate bởi người dùng | Citizen có thể flag báo cáo là trùng/không hợp lệ. Cần ≥ 3 flag khác nhau để hệ thống gửi LEO xem xét. | Crowdsourced Moderation | N/A |

# 6. Bản đồ và vị trí (Map & Location)

Hiển thị báo cáo trên bản đồ, heatmap, hotspot và kiểm soát quyền riêng tư về vị trí.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| 3.1 Hiển thị bản đồ | 3.1 Hiển thị bản đồ | 3.1 Hiển thị bản đồ | 3.1 Hiển thị bản đồ | 3.1 Hiển thị bản đồ |
| BR-MAP-001 | Hiển thị mặc định | Bản đồ mặc định hiển thị vị trí hiện tại của người dùng. Không lấy được GPS → hiển thị trung tâm tỉnh/thành theo hồ sơ user, mặc định TP.HCM. | UX | N/A |
| BR-MAP-002 | Phạm vi báo cáo lân cận | 'Nearby reports' hiển thị báo cáo trong bán kính 5 km, tối đa 100 điểm. Mở rộng được lên 10/20/50 km. | Business Rule | N/A |
| BR-MAP-003 | Bộ lọc bản đồ | Lọc theo: loại ô nhiễm (3 loại), trạng thái, mức độ, khoảng thời gian (7/30/90 ngày/custom). Cộng dồn AND. | Business Rule | N/A |
| BR-MAP-004 | Ẩn thông tin riêng tư | Bản đồ công khai không hiển thị tên (nếu người gửi bật ẩn danh tính), SĐT, email. Vị trí làm tròn 10m. | Privacy | N/A |
| BR-MAP-005 | Cụm điểm (Clustering) | Zoom < 13 → gom cụm điểm gần nhau. Màu cụm thể hiện mức độ nghiêm trọng tổng hợp. | UX | N/A |
| 3.2 Hotspot & Heatmap | 3.2 Hotspot & Heatmap | 3.2 Hotspot & Heatmap | 3.2 Hotspot & Heatmap | 3.2 Hotspot & Heatmap |
| BR-MAP-010 | Định nghĩa hotspot | 'Hotspot' nếu có ≥ 10 báo cáo cùng loại trong bán kính 500m và trong 30 ngày gần nhất. | Analytics Rule | N/A |
| BR-MAP-011 | Heatmap cho Officer | DEO/LEO có heatmap theo mật độ, trọng số = severity (Low=1…Critical=4). LEO thấy heatmap trong xã/phường; DEO thấy toàn tỉnh. | Analytics Rule | N/A |
| BR-MAP-012 | Làm mới dữ liệu bản đồ | Dữ liệu bản đồ cache 10 phút. Refresh thủ công rate-limit: 20 lần/phút/user. | Performance | Vui lòng thử lại sau vài giây |

# 7. Environmental Officer (DEO & LEO)

Quy trình xác minh, phân công, đo SLA và KPI. DEO (cấp Tỉnh/Thành phố) tập trung onboarding công ty, quản lý queue chung và KPI tổng hợp; LEO (cấp Xã/Phường) đảm nhận xác minh & điều phối thường ngày.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| 4.1 Xác minh báo cáo (Verification) | 4.1 Xác minh báo cáo (Verification) | 4.1 Xác minh báo cáo (Verification) | 4.1 Xác minh báo cáo (Verification) | 4.1 Xác minh báo cáo (Verification) |
| BR-OFF-001 | Phân công xác minh tự động | Báo cáo mới tự gán cho LEO của xã/phường chứa tọa độ GPS (BR-ORG-010). Chưa onboard → hàng đợi chung của Department (BR-ORG-011). | Business Rule | N/A |
| BR-OFF-002 | SLA xác minh | Báo cáo 'Submitted' phải xác minh trong 24 giờ (làm việc). Quá hạn → 'SLA Breach' và eskalat lên DEO. | SLA | Cảnh báo: Báo cáo đã vượt SLA xác minh |
| BR-OFF-003 | Chỉnh sửa loại/mức độ | LEO có thể chỉnh loại ô nhiễm và severity; mỗi thay đổi lý do ≥ 10 ký tự, ghi audit log. | Auditability | Vui lòng nhập lý do thay đổi |
| BR-OFF-004 | Không tự xử lý báo cáo do mình tạo | LEO/DEO không được tự xác minh báo cáo do chính mình gửi (conflict of interest). Hệ thống chuyển cho Officer khác cùng cấp. | Segregation of Duties | Không thể xử lý báo cáo do bạn tạo |
| 4.1b Quyết định khi xác minh (Triage)   [NEW v1.2] | 4.1b Quyết định khi xác minh (Triage)   [NEW v1.2] | 4.1b Quyết định khi xác minh (Triage)   [NEW v1.2] | 4.1b Quyết định khi xác minh (Triage)   [NEW v1.2] | 4.1b Quyết định khi xác minh (Triage)   [NEW v1.2] |
| BR-OFF-005 | Triage: dọn dẹp & xử phạt [NEW v1.2] | Khi 'Verified', LEO ra 2 quyết định độc lập (có thể cùng lúc): • Cần dọn dẹp → tạo CleanupTask và gán đội (BR-OFF-011). • Có chủ thể vi phạm cần xử phạt → lập InspectionReport và đẩy cho Inspection Team (BR-INS-001). Ít nhất một trong hai nhánh phải được khởi tạo, nếu không phải Reject (BR-REP-022). | Business Logic | Cần chọn ít nhất một hành động (dọn dẹp hoặc xử phạt) |
| 4.2 Ưu tiên và giao việc | 4.2 Ưu tiên và giao việc | 4.2 Ưu tiên và giao việc | 4.2 Ưu tiên và giao việc | 4.2 Ưu tiên và giao việc |
| BR-OFF-010 | Tính điểm ưu tiên | Priority Score = severity*3 + số_báo_cáo_liên_quan*2 + tuổi_báo_cáo(giờ)/24. Điểm cao xếp đầu hàng đợi. | Business Logic | N/A |
| BR-OFF-011 | Gán đội dọn dẹp [CẬP NHẬT] | LEO chọn MỘT đơn vị khả dụng trong phường để xử lý báo cáo (hệ thống gợi ý — BR-ORG-016): • Đội Công ty đầu mối của phường (ESC) — đẩy cho Company Manager phân công Company Staff. • Đội Cleanup Team cộng đồng cấp xã/phường (HTX/Tổ tự quản/rác dân lập — Cleaner). Nếu báo cáo nằm trên tuyến cấp TP → escalate DEO, không thuộc quyền LEO (BR-ORG-016). Mỗi CleanupTask tại một thời điểm chỉ thuộc 1 đội. | State + Authorization | Không có đơn vị khả dụng cho khu vực này |
| BR-OFF-012 | Chuyển giao (Reassign) | Chuyển giao nếu chưa bắt đầu, hoặc nêu lý do ≥ 20 ký tự nếu đang 'In Progress'. Chỉ chuyển giữa các đích cùng loại nhiệm vụ (dọn dẹp ↔ dọn dẹp). | Business Process | Cần nhập lý do chuyển giao |
| BR-OFF-013 | Giới hạn khối lượng | Một đội không quá 10 task 'In Progress' cùng lúc. Cảnh báo khi đạt 8. | Workload Limit | Đội đã đạt giới hạn. Chọn đội khác |
| 4.3 SLA xử lý & báo cáo | 4.3 SLA xử lý & báo cáo | 4.3 SLA xử lý & báo cáo | 4.3 SLA xử lý & báo cáo | 4.3 SLA xử lý & báo cáo |
| BR-OFF-020 | SLA xử lý theo mức độ | Critical: 3 ngày; High: 5 ngày; Medium: 7 ngày; Low: 10 ngày — tính từ 'Verified' (áp dụng cho cả CleanupTask và InspectionReport). | SLA | Báo cáo [ID] sắp vượt SLA |
| BR-OFF-021 | KPI Officer | Tính: tỉ lệ xác minh đúng hạn, tỉ lệ resolved/closed, thời gian phản hồi TB. DEO xem KPI toàn tỉnh; LEO xem KPI của mình. | Reporting | N/A |
| BR-OFF-022 | Xuất dữ liệu | LEO export CSV/Excel báo cáo trong xã/phường; DEO export toàn tỉnh. Không export PII người gửi trừ khi có Request + Admin approval. | Privacy + Authorization | Không đủ quyền xuất dữ liệu cá nhân |

# 8. Environmental Cleanup Team

Quy trình thực địa của đội dọn dẹp — nhận task, check-in, cập nhật tiến độ, đóng task với ảnh before/after. Đội dọn dẹp gồm hai hình thức: đội thuộc Company (Company Staff) và đội cộng đồng cấp xã/phường (Cleaner). Cả hai theo cùng quy trình vận hành dưới đây.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| BR-CLN-001 | Phạm vi tiếp nhận [CẬP NHẬT v1.2] | Đội dọn dẹp tiếp nhận các CleanupTask do LEO gán (BR-OFF-011), thường là ô nhiễm có chất thải vật lý cần xử lý (Rác/Nước/Hóa chất). Việc gán dựa trên nhu cầu dọn dẹp do LEO xác định, không cứng theo loại ô nhiễm. | Authorization | Task này không thuộc phạm vi xử lý của đội |
| BR-CLN-002 | Chỉ xem task được gán | Đội chỉ thấy task được gán cho mình; không truy cập task ngoài phạm vi. | Authorization | Bạn không có quyền xem báo cáo này |
| BR-CLN-003 | Bắt đầu task (Check-in) | Chuyển 'Verified → In Progress' yêu cầu 'Check-in', khoảng cách ≤ 200m so với tọa độ báo cáo (hoặc có lý do ghi chú). | Location Validation | Bạn cần ở gần vị trí báo cáo để check-in |
| BR-CLN-004 | Cập nhật tiến độ | Cập nhật ≥ 1 lần/ngày khi 'In Progress'. > 24h → nhắc lần 1; > 48h → gắn cờ đội (escalate LEO). | SLA | Cảnh báo: Task [ID] chưa được cập nhật |
| BR-CLN-005 | Ảnh after bắt buộc | Khi Mark Resolved phải upload ≥ 2 ảnh 'after' từ các góc khác nhau (kiểm tra khác biệt hash). | Data Quality | Vui lòng upload ảnh khác góc, không trùng lặp |
| BR-CLN-006 | Leo thang (Escalate) | Đội có thể leo thang lên LEO nếu vượt khả năng. Bắt buộc lý do + bằng chứng (ảnh/mô tả). | Business Process | Vui lòng nêu lý do và bằng chứng khi escalate |
| BR-CLN-007 | Từ chối task | Đội có thể từ chối trong 2 giờ (giờ hành chính) sau khi được gán, kèm lý do. Sau 2 giờ coi như chấp nhận. | Business Process | Đã hết thời gian từ chối. Hãy liên hệ LEO/Company Manager |
| BR-CLN-008 | Đội của Company [NEW v1.2] | Với đội thuộc Company: việc tiếp nhận/phân công nội bộ do Company Manager thực hiện (BR-CMP-011). Company Staff chỉ tác nghiệp khi công ty còn hiệu lực hợp đồng (BR-CMP-005). | Authorization | Công ty đã hết hạn hợp đồng, không thể tác nghiệp |

# 9. Environmental Inspection Team (Quy trình xử phạt)

Đội Thanh tra/Kiểm tra môi trường cấp xã/phường. Khác với đội dọn dẹp, Inspection Team không dọn dẹp mà xử phạt vi phạm. Trong v1.2, đội xử lý xử phạt cho MỌI loại ô nhiễm khi LEO lập InspectionReport (không phân theo loại ô nhiễm).

## 9.0 Luồng tổng quan

- Tiếp nhận InspectionReport do LEO lập (cho mọi loại ô nhiễm có chủ thể vi phạm).

- Khảo sát hiện trường → hoàn thiện biên bản (Inspection Report).

- Phân loại mức vi phạm → Ra Quyết định xử phạt (Penalty Decision).

- Theo dõi nộp phạt → đóng vụ việc khi nộp đủ, hoặc eskalat nếu quá hạn.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| 9.1 Phạm vi & tiếp nhận task | 9.1 Phạm vi & tiếp nhận task | 9.1 Phạm vi & tiếp nhận task | 9.1 Phạm vi & tiếp nhận task | 9.1 Phạm vi & tiếp nhận task |
| BR-INS-001 | Phạm vi xử lý [CẬP NHẬT v1.2] | Inspection Team tiếp nhận InspectionReport do LEO lập khi xác minh thấy có chủ thể vi phạm/gây ô nhiễm cần xử phạt — áp dụng cho MỌI loại ô nhiễm (Rác/Nước/Hóa chất). Một báo cáo có thể có cả CleanupTask (dọn dẹp) lẫn InspectionReport (xử phạt) song song. | Authorization | N/A |
| BR-INS-002 | Chỉ xem task được gán | Inspection Team chỉ thấy InspectionReport được gán cho mình trong xã/phường phụ trách; không truy cập ngoài phạm vi. | Authorization | Bạn không có quyền xem hồ sơ này |
| BR-INS-003 | Từ chối task | Có thể từ chối trong 2 giờ (giờ hành chính) sau khi được gán, kèm lý do. Sau 2 giờ coi như chấp nhận. | Business Process | Đã hết thời gian từ chối. Hãy liên hệ LEO |
| BR-INS-004 | Bắt đầu task (Check-in) | Chuyển sang 'In Progress' yêu cầu Check-in hiện trường ≤ 200m. Có thể ghi chú lý do nếu không vào được (ví dụ nhà máy không cho vào). | Location Validation | Bạn cần ở gần vị trí báo cáo để check-in |
| 9.2 Lập biên bản & quyết định xử phạt | 9.2 Lập biên bản & quyết định xử phạt | 9.2 Lập biên bản & quyết định xử phạt | 9.2 Lập biên bản & quyết định xử phạt | 9.2 Lập biên bản & quyết định xử phạt |
| BR-INS-010 | Lập biên bản hiện trường | Bắt buộc biên bản (Inspection Report) gồm: (a) thông tin cơ sở vi phạm (tên, địa chỉ, MST/MSDN nếu có); (b) loại vi phạm; (c) số liệu đo (dB, AQI/PM2.5 hoặc mô tả định tính cho loại khác); (d) ≥ 2 ảnh hiện trường; (e) chữ ký số của Team Leader. | Validation + Evidence | Vui lòng nhập đầy đủ thông tin biên bản & ảnh hiện trường |
| BR-INS-011 | Phân loại mức vi phạm | 4 cấp: Nhẹ (cảnh cáo) / Trung bình / Nặng / Đặc biệt nghiêm trọng. Mỗi mức gắn khung tiền phạt do Admin cấu hình (BR-ADM-008), tính bằng VND, cập nhật theo quy định pháp luật. | Business Rule | N/A |
| BR-INS-012 | Quyết định xử phạt | Inspection Team Leader ban hành 'Penalty Decision': số quyết định, mức phạt (trong khung), thời hạn nộp (mặc định 10 ngày làm việc), hình thức bổ sung. Trạng thái InspectionReport → 'Penalty Issued'. | State Machine + Authorization | Chỉ Inspection Team Leader được ban hành quyết định xử phạt |
| BR-INS-013 | Không đủ căn cứ xử phạt | Nếu không đủ căn cứ, ghi biên bản 'No Violation Found' kèm lý do ≥ 50 ký tự, chuyển InspectionReport → 'Closed – No Violation'. Citizen được thông báo lý do. | Business Process | Vui lòng nêu lý do ≥ 50 ký tự |
| 9.3 Theo dõi nộp phạt & đóng vụ việc | 9.3 Theo dõi nộp phạt & đóng vụ việc | 9.3 Theo dõi nộp phạt & đóng vụ việc | 9.3 Theo dõi nộp phạt & đóng vụ việc | 9.3 Theo dõi nộp phạt & đóng vụ việc |
| BR-INS-020 | Ghi nhận nộp phạt | Cập nhật trạng thái: 'Paid' / 'Partially Paid' / 'Overdue'. 'Paid' đầy đủ → chuyển 'Closed'. Phải kèm bằng chứng (ảnh biên lai/chuyển khoản). | Validation + Evidence | Vui lòng đính kèm bằng chứng nộp phạt |
| BR-INS-021 | Quá hạn nộp phạt | Không nộp trong thời hạn → tự đánh dấu 'Overdue'; chuyển hồ sơ lên LEO/DEO để phối hợp cơ quan chức năng. | Workflow | Hồ sơ đã được eskalat lên cấp trên |
| BR-INS-022 | Tái phạm | Cùng cơ sở bị lập biên bản ≥ 2 lần trong 12 tháng → gắn cờ 'Repeat Offender'; mức phạt tối thiểu nâng 1 bậc (BR-INS-011). | Business Logic | N/A |
| 9.4 SLA & KPI Inspection Team | 9.4 SLA & KPI Inspection Team | 9.4 SLA & KPI Inspection Team | 9.4 SLA & KPI Inspection Team | 9.4 SLA & KPI Inspection Team |
| BR-INS-030 | SLA xử lý theo mức độ | Như BR-OFF-020: Critical 3 / High 5 / Medium 7 / Low 10 ngày làm việc cho Penalty Decision hoặc 'No Violation', tính từ 'Verified'. | SLA | Hồ sơ [ID] sắp vượt SLA xử phạt |
| BR-INS-031 | Cập nhật tiến độ | Cập nhật ≥ 1 lần/ngày khi 'In Progress'. > 24h → nhắc lần 1; > 48h → gắn cờ đội (escalate LEO). | SLA | Cảnh báo: Task chưa được cập nhật |
| BR-INS-032 | KPI Inspection Team | Tính: tỉ lệ ban hành quyết định đúng hạn, tỉ lệ nộp phạt đầy đủ/đúng hạn, số vụ tái phạm. Dashboard tháng/quý. | Reporting | N/A |

# 10. Công ty Dịch vụ Môi trường & Quản lý hợp đồng (Environmental Service Company)

Nhóm rule mới trong v1.2 mô hình hóa các đơn vị thu gom/dọn dẹp đầu nguồn ngoài hệ thống nhà nước trực tiếp. Mỗi Environmental Service Company (ESC) là một thực thể được DEO onboarding theo hợp đồng dịch vụ công ích. Hệ thống KHÔNG xử lý cơ chế đấu thầu và KHÔNG quản lý công ty cuối nguồn (khu xử lý tập trung như VWS, Vietstar, Tâm Sinh Nghĩa, Tasco) — chỉ phân biệt 2 loại công ty đầu nguồn.

## 10.0 Phân loại công ty & vai trò

- Subsidiary (trực thuộc, chủ lực) — Công ty MTV/CP trực thuộc (URENCO ở Hà Nội, CITENCO ở TP.HCM, Cty CP Môi trường Đô thị cấp tỉnh). Hoạt động theo đặt hàng — KHÔNG có thời hạn hợp đồng (vô thời hạn).

- Bidding (đấu thầu) — Công ty tư nhân/cổ phần trúng thầu thu gom tại phường/xã. CÓ thời hạn theo hợp đồng (ShortTerm 1 năm / MediumTerm 3–5 năm) — chỉ là metadata, KHÔNG khóa định tuyến.

- Company Manager (CM) — vai trò quản lý: hồ sơ công ty, thêm/bớt Company Staff, lập & điều phối đội dọn dẹp, nhận CleanupTask do LEO đẩy sang.

- Company Staff — nhân viên hiện trường, hoạt động theo đội (Company Cleanup Team); thực hiện luồng dọn dẹp giống Cleaner (check-in, before/after).

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| 10.1 Onboarding & hợp đồng   [NEW v1.2] | 10.1 Onboarding & hợp đồng   [NEW v1.2] | 10.1 Onboarding & hợp đồng   [NEW v1.2] | 10.1 Onboarding & hợp đồng   [NEW v1.2] | 10.1 Onboarding & hợp đồng   [NEW v1.2] |
| BR-CMP-001 | DEO tạo tài khoản công ty | DEO tạo Company trong phạm vi tỉnh/thành phố mình quản lý, khai báo: tên, loại công ty, email người quản lý, và PHẠM VI PHỤC VỤ = danh sách xã/phường công ty phục vụ. Admin có toàn quyền tương đương trên toàn hệ thống. • Subsidiary (trực thuộc, chủ lực): KHÔNG có thời hạn — bỏ trống ContractEndDate (vô thời hạn). • Bidding (đấu thầu): có ContractType (ShortTerm 1 năm / MediumTerm 3–5 năm), ContractStartDate, ContractEndDate (chỉ là metadata). | Authorization | N/A |
| BR-CMP-002 | CM đặt mật khẩu lần đầu (qua reset-password) | Khi DEO tạo Company, hệ thống gửi email chứa link đặt mật khẩu cho Company Manager, DÙNG CHUNG cơ chế reset-password (token ngẫu nhiên, lưu hash, có hạn, single-use). KHÔNG có module activation riêng; KHÔNG dùng contract key làm credential. Quá hạn/đã dùng → yêu cầu gửi lại. | Security | Liên kết đặt mật khẩu đã hết hạn hoặc đã được sử dụng |
| BR-CMP-003 | Loại hợp đồng & thời hạn | CHỈ áp dụng cho Bidding (đấu thầu): ShortTerm 1 năm hoặc MediumTerm 3–5 năm; ContractEndDate > ContractStartDate. Subsidiary (trực thuộc) vô thời hạn → bỏ trống ContractType/ContractEndDate. Hệ thống KHÔNG xử lý cơ chế đấu thầu; thông tin hợp đồng chỉ là metadata (hiển thị + để job đặt Status), KHÔNG dùng khóa định tuyến. | Validation | Thời hạn hợp đồng không hợp lệ |
| BR-CMP-004 | Trạng thái công ty | Status ∈ {PendingActivation, Active, Suspended, Expired, Terminated}. PendingActivation → Active sau khi CM kích hoạt (BR-CMP-002). Suspended: DEO/Admin tạm ngưng (vi phạm/điều tra). Terminated: chấm dứt sớm. Mọi thay đổi ghi audit log. | State Machine | N/A |
| BR-CMP-005 | Hiệu lực tác nghiệp (Authorization) [CẬP NHẬT] | Yêu cầu tác nghiệp của Company Manager/Company Staff chỉ được chấp nhận khi Company.Status = Active. KHÔNG dùng cửa sổ hợp đồng [ContractStartDate, ContractEndDate] để khóa định tuyến/tác nghiệp — đó chỉ là metadata. Hiệu lực tác nghiệp chỉ phụ thuộc Status. Khi Status ≠ Active (Suspended/Expired/Terminated) → bị chặn đăng nhập tác nghiệp, refresh token bị từ chối (BR-AUTH-016); dữ liệu lịch sử được giữ cho audit. Kiểm soát ở tầng authorization (policy), KHÔNG dựa vào một 'key dài hạn'. | Authorization | Tài khoản công ty không ở trạng thái hoạt động |
| BR-CMP-006 | Gia hạn / tái ký | Khi công ty trúng thầu/ký lại: DEO gia hạn ContractEndDate hoặc tạo kỳ hợp đồng mới và đặt Status = Active. Lịch sử các kỳ hợp đồng được lưu (audit). Không tạo lại tài khoản từ đầu. | Business Process | N/A |
| BR-CMP-007 | Tự động hết hạn [CẬP NHẬT] | CHỈ áp dụng cho Bidding có ContractEndDate: job nền hằng ngày đặt Status → Expired tại thời điểm hết hạn (chặn đăng nhập tác nghiệp, giữ dữ liệu audit), cảnh báo trước 30/7/1 ngày cho DEO và Company Manager. Subsidiary (vô thời hạn) KHÔNG tự hết hạn — chỉ đổi trạng thái khi DEO/Admin chủ động Suspend/Terminate. | Automation | N/A |
| 10.2 Quản lý nhân viên & đội | 10.2 Quản lý nhân viên & đội | 10.2 Quản lý nhân viên & đội | 10.2 Quản lý nhân viên & đội | 10.2 Quản lý nhân viên & đội |
| BR-CMP-010 | Company Manager thêm nhân viên | Company Manager thêm Company Staff cho công ty mình (mời theo email, người dùng đăng ký Citizen → chấp nhận → đổi vai trò Company Staff). Nhân viên chỉ thuộc 1 công ty tại một thời điểm. Ghi audit log. | Authorization | N/A |
| BR-CMP-011 | Lập & điều phối đội | Company Manager lập các Company Cleanup Team từ Company Staff và phân công CleanupTask do LEO đẩy sang (BR-OFF-011). Áp dụng giới hạn khối lượng BR-OFF-013. Một CleanupTask chỉ thuộc 1 đội. | Business Process | N/A |
| BR-CMP-012 | Phạm vi phục vụ [CẬP NHẬT] | Company chỉ nhận CleanupTask thuộc các xã/phường trong phạm vi phục vụ đã khai báo (BR-CMP-001). LEO của phường thấy danh sách công ty phục vụ phường mình khi gán (BR-OFF-011). Quan hệ Company ↔ Ward là N–N (1 công ty phủ nhiều phường, 1 phường có nhiều đơn vị). | Authorization | Công ty không phục vụ khu vực này |
| BR-CMP-013 | Vô hiệu hóa kế thừa | Khi Company chuyển Suspended/Expired/Terminated: toàn bộ Company Staff & đội thuộc công ty mất quyền tác nghiệp (kế thừa BR-CMP-005). CleanupTask đang dở dang được đẩy về LEO để tái điều phối. | State Machine | N/A |
| BR-CMP-014 | Quan hệ Company ↔ Ward (N–N) [CẬP NHẬT] | Company ↔ Ward là N–N: một công ty phủ nhiều phường; một phường có nhiều đơn vị cùng hoạt động — đây là BÌNH THƯỜNG. Hệ thống KHÔNG mô hình hóa độc quyền theo tuyến/mét (đó là việc của hợp đồng/nghiệm thu ngoài phạm vi app). HTX/Tổ tự quản/'đường dây rác dân lập' là đội cộng đồng (CleanupTeam cấp xã/phường, không thuộc công ty) — NGANG HÀNG công ty trong danh sách đơn vị khả dụng của phường mà LEO chọn khi điều phối. | Business Rule | N/A |
| 10.3 Giám sát & KPI | 10.3 Giám sát & KPI | 10.3 Giám sát & KPI | 10.3 Giám sát & KPI | 10.3 Giám sát & KPI |
| BR-CMP-020 | KPI công ty | Hệ thống tính cho mỗi công ty: số task tiếp nhận/hoàn thành, tỉ lệ đúng SLA (BR-OFF-020), thời gian xử lý TB. DEO xem KPI các công ty trong tỉnh; Company Manager xem KPI công ty mình. | Reporting | N/A |
| BR-CMP-021 | Phân tách dữ liệu công ty | Company Manager/Company Staff chỉ truy cập dữ liệu task & đội thuộc công ty mình; không thấy dữ liệu công ty khác hay báo cáo ngoài phạm vi phục vụ. | Authorization | Bạn không có quyền truy cập dữ liệu này |

# 11. Thông báo & Bình luận (Notifications & Comments)

Kênh giao tiếp giữa hệ thống với user và giữa các user với nhau.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| 6.1 Thông báo (Notifications) | 6.1 Thông báo (Notifications) | 6.1 Thông báo (Notifications) | 6.1 Thông báo (Notifications) | 6.1 Thông báo (Notifications) |
| BR-NTF-001 | Kênh thông báo | Hỗ trợ: push (app), email. Người dùng cấu hình bật/tắt theo từng kênh. | Configurability | N/A |
| BR-NTF-002 | Sự kiện kích hoạt | Gửi khi: báo cáo đổi trạng thái, có bình luận mới, đạt badge/level, SLA sắp vượt (LEO/DEO/Company Manager/Team), có báo cáo gần mình (Citizen 2km), Penalty Decision ban hành / đến hạn nộp phạt, hợp đồng công ty sắp hết hạn (BR-CMP-007). | Business Rule | N/A |
| BR-NTF-003 | Không spam | Mỗi người nhận tối đa 20 thông báo/ngày cho một loại sự kiện lặp. Vượt → gộp 'digest' cuối ngày. | Anti-Spam | N/A |
| BR-NTF-004 | Ngôn ngữ thông báo | Theo ngôn ngữ tài khoản (vi-VN / en-US). Template do Admin quản lý (BR-ADM-004). | i18n | N/A |
| 6.2 Bình luận (Comments) | 6.2 Bình luận (Comments) | 6.2 Bình luận (Comments) | 6.2 Bình luận (Comments) | 6.2 Bình luận (Comments) |
| BR-CMT-001 | Quyền bình luận [CẬP NHẬT v1.2] | Chỉ user đã đăng nhập mới được bình luận (khách chưa đăng nhập không bình luận được). Với báo cáo mà người gửi bật ẩn danh tính: chỉ LEO/DEO/Admin và người gửi gốc được bình luận. | Authorization | Vui lòng đăng nhập để bình luận |
| BR-CMT-002 | Độ dài bình luận | Từ 1–500 ký tự. Đính kèm tối đa 2 ảnh (≤ 5MB/ảnh). | Validation | Bình luận quá dài / quá nhiều ảnh |
| BR-CMT-003 | Kiểm duyệt nội dung | Qua bộ lọc từ ngữ + AI moderation. Vi phạm → tự ẩn + cảnh báo. Vi phạm 3 lần → tạm khóa bình luận 7 ngày. | Moderation | Bình luận chứa nội dung không phù hợp |
| BR-CMT-004 | Sửa/xóa bình luận | Người viết sửa/xóa trong 15 phút sau đăng. LEO/Admin có thể ẩn bất kỳ lúc nào (ghi lý do). | Authorization | Đã quá thời gian chỉnh sửa |

# 12. Gamification

Công thức tính điểm, level, badge, bảng xếp hạng và chống gian lận gamification.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| BR-GAM-001 | Công thức tính điểm | Báo cáo Verified: +10đ. Báo cáo Resolved hoặc Penalty Issued: +20đ (cộng thêm). Báo cáo trùng: +5đ. Báo cáo bị reject do sai: -5đ. | Business Logic | N/A |
| BR-GAM-002 | Ẩn danh tính & điểm thưởng [CẬP NHẬT v1.2] | Báo cáo bật ẩn danh tính VẪN cộng điểm vào tài khoản người gửi (vì đã đăng nhập & truy vết được), nhưng KHÔNG hiển thị tên người đó trên leaderboard công khai. | Business Rule | N/A |
| BR-GAM-003 | Cấp độ (Level) | L1: 0–99; L2: 100–499; L3: 500–1,499; L4: 1,500–4,999; L5: ≥ 5,000. Lên level → thông báo + huy hiệu. | Business Rule | N/A |
| BR-GAM-004 | Huy hiệu (Badges) | 12 huy hiệu mặc định chia 4 nhóm: **Milestone** — 'First Reporter' (1 báo cáo), 'Eco Warrior' (10), 'Green Champion' (50), 'Earth Guardian' (100); **Streak** — '7-Day Streak', '30-Day Streak'; **Community** — 'Hotspot Hunter' (3 báo cáo trong hotspot), 'Duplicate Finder' (5 báo cáo trùng), 'Community Voice' (báo cáo có ≥ 10 ReporterCount); **Level** — 'Rising Star' (≥ 100đ), 'Eco Expert' (≥ 1.500đ), 'Green Legend' (≥ 5.000đ). Admin thêm badge tùy chỉnh (BR-ADM-005). | Business Rule | N/A |
| BR-GAM-005 | Bảng xếp hạng | Leaderboard theo tuần/tháng/năm. Chỉ báo cáo KHÔNG bật ẩn danh tính mới hiển thị tên người gửi trên bảng xếp hạng. Top 10 vinh danh trang chủ. | Business Rule | N/A |
| BR-GAM-006 | Chống gian lận gamification | Phát hiện gian lận (báo cáo giả/trùng hàng loạt) → trừ toàn bộ điểm đợt và khóa tính điểm 30 ngày. | Anti-Fraud | Tài khoản của bạn đang bị điều tra vì hoạt động bất thường |

# 13. AI Service

Các rule cho service tự động hóa – phân loại, phát hiện trùng, đánh giá severity, chống gian lận.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| BR-AI-001 | Phân loại ảnh | AI phân loại loại ô nhiễm từ ảnh (3 loại BR-REP-005) với confidence. ≥ 0.8 → auto fill; 0.5–0.8 → gợi ý; < 0.5 → giữ lựa chọn user. | AI Logic | Fine-tuning – third party tự build |
| BR-AI-002 | Phát hiện trùng lặp | AI so khớp GPS (≤ 50m) + thời gian (≤ 24h) + tương đồng ảnh (pHash > 0.85) → gợi ý LEO merge (BR-REP-030). | AI Logic (API Key) | N/A |
| BR-AI-003 | Ước lượng mức độ | AI đánh giá severity theo diện tích ô nhiễm, loại chất, mật độ lân cận. Override severity của Citizen nếu chênh ≥ 2 mức và confidence ≥ 0.85 (kèm log). | AI Logic | N/A |
| BR-AI-004 | Cờ nội dung khả nghi | Đánh flag 'Suspicious' nếu: ảnh đã chỉnh sửa, không có ô nhiễm trong ảnh, hoặc trùng ảnh đã dùng trong 30 ngày. | Anti-Fraud | N/A |
| BR-AI-005 | Ghi nhận kết quả AI | Mọi kết quả AI ghi lại như metadata, không thay kết luận của LEO/DEO. Officer quyết định cuối. | Auditability | N/A |
| BR-AI-006 | Fallback khi AI lỗi | AI không phản hồi trong 5 giây → báo cáo vẫn tạo bình thường, tag 'ai_pending'. Job chạy lại trong 1 giờ. | Resilience | N/A |
| BR-AI-007 | Quyền riêng tư dữ liệu | Ảnh gửi AI phải strip EXIF nhạy cảm (GPS chi tiết, chủ thiết bị) nếu là third-party. Chỉ truyền metadata cần thiết. | Privacy | N/A |

# 14. Quản trị hệ thống (Administration)

Các rule dành cho System Administrator – quản lý user, danh mục, cấu hình, audit.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| BR-ADM-001 | Quản lý người dùng | Admin tạo/sửa/vô hiệu hóa tài khoản. Không xem mật khẩu (chỉ reset). Mọi thao tác ghi audit log. | Authorization + Audit | N/A |
| BR-ADM-002 | Phân quyền (Role) [CẬP NHẬT v1.2] | Hệ thống có 8 vai trò gán cho người dùng: Citizen, DEO, LEO, Company Manager, Company Staff, Cleaner, Inspector, Admin (ngoài ra AI Service automated và Community Organization optional). Admin tạo sub-role tùy chỉnh nhưng không vượt quyền Admin gốc. | Authorization | N/A |
| BR-ADM-003 | Quản lý danh mục ô nhiễm | Admin quản lý 3 loại chính thức (BR-REP-005). Xóa loại đang dùng → chỉ 'Archive' (ẩn khi chọn mới, giữ cho báo cáo cũ). | Data Integrity | Loại này đang được sử dụng, chỉ có thể lưu trữ |
| BR-ADM-004 | Template thông báo | Admin cấu hình template theo ngôn ngữ. Placeholder: {user_name}, {report_id}, {priority}, {status}, {time}, {penalty_amount}, {ward_name}, {company_name}. Test gửi trước khi publish. | Configurability | N/A |
| BR-ADM-005 | Cấu hình gamification | Admin chỉnh công thức điểm, thêm badge, reset leaderboard. Áp dụng từ thời điểm publish (không ảnh hưởng điểm đã cộng). | Business Rule | N/A |
| BR-ADM-006 | Kiểm duyệt nội dung | Admin ẩn/xóa báo cáo, bình luận, ảnh vi phạm. Nội dung bị xóa vẫn giữ trong audit 90 ngày. | Moderation + Audit | N/A |
| BR-ADM-007 | Phát hiện spam | Admin xem dashboard 'Spam suspects' từ AI + heuristic. Có thể ban tài khoản. | Anti-Fraud | N/A |
| BR-ADM-008 | Quản lý khung tiền phạt | Admin cấu hình khung mức phạt cho 4 cấp vi phạm (BR-INS-011) theo loại ô nhiễm, theo VND. Cập nhật khi pháp luật thay đổi; không ảnh hưởng quyết định đã ban hành. | Configurability | N/A |
| BR-ADM-009 | Phân quyền dữ liệu | Admin thiết lập ai xem/sửa/xuất dữ liệu theo phạm vi: cấp Tỉnh/Thành phố (DEO), cấp Xã/Phường (LEO, đội cộng đồng), hoặc theo công ty (Company Manager/Staff — BR-CMP-021). | Authorization | N/A |
| BR-ADM-010 | Audit log | Mọi hành động nhạy cảm (đổi role, xóa data, cấu hình, ban hành QĐ xử phạt, tạo/gia hạn/chấm dứt công ty) được log: actor, action, target, time, IP, user-agent. Giữ ≥ 12 tháng. | Compliance + Audit | N/A |
| BR-ADM-011 | Onboard xã/phường mới | Admin tạo Local Office, nạp polygon ranh giới, gán LEO, tạo ≥ 1 đội cộng đồng (nếu là khu vực cộng đồng). Báo cáo trong queue chung thuộc xã/phường đó tự chuyển về LEO mới (BR-ORG-011). | Business Process | N/A |
| BR-ADM-012 | Giám sát công ty [NEW v1.2] | Admin xem toàn bộ công ty trên hệ thống (mọi tỉnh), trạng thái hợp đồng, KPI; có thể Suspend/Terminate khi cần. DEO chỉ quản lý công ty trong tỉnh mình. | Authorization + Audit | N/A |

# 15. Dữ liệu, quyền riêng tư & tuân thủ (Data, Privacy & Compliance)

Các rule về mã hóa, lưu trữ, quyền dữ liệu cá nhân (phù hợp Nghị định 13/2023/NĐ-CP và GDPR).

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| BR-DAT-001 | Mã hóa dữ liệu | Dữ liệu nhạy cảm (password, token, API key) mã hóa at-rest (AES-256). Mật khẩu bcrypt ≥ 12 rounds. Kết nối TLS 1.2+. | Security | N/A |
| BR-DAT-002 | Thời gian lưu trữ | Báo cáo: 5 năm. Ảnh: 2 năm. Audit log: 12 tháng. Biên bản & quyết định xử phạt: 10 năm. Dữ liệu hợp đồng công ty: tối thiểu 10 năm. Dữ liệu user xóa tài khoản: ẩn 90 ngày → xóa vĩnh viễn. | Retention | N/A |
| BR-DAT-003 | Quyền truy cập dữ liệu cá nhân | Người dùng có quyền xem/tải xuống dữ liệu cá nhân, yêu cầu chỉnh sửa hoặc xóa (GDPR/Nghị định 13/2023/NĐ-CP). | Compliance | N/A |
| BR-DAT-004 | Sao lưu | Backup toàn bộ DB hằng ngày, giữ 30 bản. RPO ≤ 24h, RTO ≤ 4h. | Availability | N/A |
| BR-DAT-005 | Đồng thuận xử lý dữ liệu | Trước khi gửi ảnh/GPS, app hiển thị consent rõ ràng. Từ chối → không gửi được báo cáo; vẫn xem được bản đồ & thông tin công khai (read-only). | Compliance | N/A |

# 16. Phi chức năng & hệ thống (Non-Functional & System Rules)

Hiệu năng, khả năng mở rộng, tính sẵn sàng, khả năng truy cập, và tương thích.

| ID | Rule Name | Description | Type | Error Message / Behavior |
|---|---|---|---|---|
| BR-SYS-001 | Hiệu năng | API < 2s cho 95% request ở tải 5,000 concurrent users. Trang đầu web tải xong < 3s trên 4G. | Performance | N/A |
| BR-SYS-002 | Khả năng mở rộng | Scale ngang xử lý 100,000+ báo cáo. Image storage dùng object storage (S3-compatible). | Scalability | N/A |
| BR-SYS-003 | Tính sẵn sàng | Uptime ≥ 99.5%/tháng cho production. Có trang status. | Availability | N/A |
| BR-SYS-004 | Rate limit API công khai | 60 request/phút/IP cho khách chưa đăng nhập (guest — chỉ xem công khai), 300/phút cho user đã đăng nhập. | Security | Bạn đã đạt giới hạn. Thử lại sau |
| BR-SYS-005 | Tương thích đa nền tảng | Mobile: Android 8.0+, iOS 13+. Web: Chrome/Edge/Safari/Firefox 2 phiên bản gần nhất. | Compatibility | N/A |
| BR-SYS-006 | Ngôn ngữ | Hỗ trợ tối thiểu tiếng Việt và tiếng Anh. Mọi thông báo/lỗi đều i18n. | i18n | N/A |

# Phụ lục A. Sơ đồ trạng thái báo cáo (Report State Machine)

Trong v1.2, báo cáo (umbrella) đi theo vòng đời dọn dẹp; nhánh xử phạt là một InspectionReport liên kết với vòng đời riêng và có thể chạy song song. Mọi chuyển trạng thái phải do đúng role thực hiện (BR-REP-021) và được ghi audit log.

## A.1 Vòng đời báo cáo (nhánh dọn dẹp – umbrella)

Submitted → Verified → In Progress → Resolved → Closed

Nhánh phụ: Submitted → Rejected; Verified → Duplicate; Resolved → In Progress (re-open, tối đa 2 lần).

## A.2 Vòng đời InspectionReport liên kết (nhánh xử phạt) [NEW v1.2]

Draft → Penalty Issued → (Paid / Partially Paid / Overdue) → Closed

Hoặc: Draft → Closed – No Violation (nếu không đủ căn cứ, BR-INS-013). Báo cáo umbrella chỉ 'Closed' khi nhánh dọn dẹp hoàn tất VÀ mọi InspectionReport liên kết đã kết thúc.

## A.3 Bảng chuyển trạng thái chi tiết

| Từ trạng thái | Đến trạng thái | Người thực hiện | Điều kiện |
|---|---|---|---|
| Submitted | Verified | LEO (hoặc DEO – queue chung) | Báo cáo hợp lệ, đủ dữ liệu (BR-OFF-001, BR-ORG-010) |
| Submitted | Rejected | LEO / DEO | Có lý do ≥ 20 ký tự (BR-REP-022) |
| Submitted | Duplicate | LEO / AI | Khớp BR-REP-030 |
| Verified | In Progress | LEO | Gán CleanupTask cho đội Công ty hoặc đội cộng đồng (BR-OFF-011) |
| Verified | [+ InspectionReport] | LEO | Có chủ thể cần xử phạt → lập InspectionReport (BR-OFF-005, BR-INS-001) |
| In Progress (Cleanup) | Resolved | Company Staff / Cleaner | Có ảnh before/after (BR-REP-014, BR-CLN-005) |
| Resolved | Closed | Citizen / System | Citizen xác nhận hoặc auto-close 7 ngày (BR-REP-015/016) + mọi InspectionReport đã kết thúc |
| Resolved | In Progress | Citizen | Không hài lòng, tối đa 2 lần (BR-REP-015) |
| [Inspection] Draft | Penalty Issued | Inspection Team Leader | Có biên bản + quyết định xử phạt (BR-INS-010/012) |
| [Inspection] Draft | Closed – No Violation | Inspection Team | Không đủ căn cứ, lý do ≥ 50 ký tự (BR-INS-013) |
| [Inspection] Penalty Issued | Closed | Inspection Team | Đã nộp phạt đầy đủ (BR-INS-020) |
| [Inspection] Penalty Issued | Overdue | System (tự động) | Quá hạn nộp phạt (BR-INS-021) |

# Phụ lục B. Thuật ngữ (Glossary)

| Thuật ngữ | Định nghĩa |
|---|---|
| SLA | Service Level Agreement – Cam kết về thời gian phản hồi/giải quyết |
| Hotspot | Khu vực có mật độ ô nhiễm cao (≥ 10 báo cáo/500m/30 ngày) |
| Duplicate | Báo cáo trùng lặp theo BR-REP-030 |
| Crowdsourcing | Thu thập dữ liệu từ cộng đồng người dùng |
| PII | Personally Identifiable Information – Thông tin định danh cá nhân |
| Audit log | Nhật ký hệ thống ghi nhận mọi thao tác nhạy cảm |
| EXIF | Metadata đi kèm ảnh (GPS, thời gian, thiết bị) |
| Gamification | Áp dụng yếu tố trò chơi để tăng tương tác |
| Anonymized | Dữ liệu đã loại bỏ thông tin định danh |
| Rate limit | Giới hạn số request trong một khoảng thời gian |
| DEO | Department Environmental Officer – Officer cấp Tỉnh/Thành phố |
| LEO | Local Environmental Officer – Officer cấp Xã/Phường |
| ESC | Environmental Service Company – Công ty Dịch vụ Môi trường đầu nguồn (v1.2) |
| Subsidiary | Loại công ty trực thuộc chủ lực (URENCO, CITENCO, Cty CP MTĐT cấp tỉnh); vô thời hạn (ContractType.Subsidiary) |
| Bidding | Loại công ty tư nhân/cổ phần trúng thầu thu gom; có thời hạn hợp đồng (ContractType.Bidding) |
| Company Manager | Vai trò quản lý công ty: nhân viên, đội, điều phối CleanupTask (v1.2) |
| Company Staff | Nhân viên hiện trường của công ty, hoạt động theo đội dọn dẹp (v1.2) |
| Đặt mật khẩu lần đầu | CM đặt mật khẩu lần đầu qua email token dùng chung cơ chế reset-password (single-use, có hạn); không có module activation riêng (BR-CMP-002) |
| Contract window | Cửa sổ hợp đồng [ContractStartDate, ContractEndDate] của công ty Bidding — chỉ là metadata; KHÔNG dùng khóa định tuyến. Hiệu lực tác nghiệp dựa trên Company.Status (BR-CMP-005). |
| Cleanup Team | Đội dọn dẹp: của Company (Company Staff) hoặc đội cộng đồng cấp xã/phường (Cleaner) |
| Inspection Team | Đội thanh tra/xử phạt cấp xã/phường (Inspector) |
| InspectionReport | Hồ sơ xử phạt liên kết do LEO lập khi cần xử phạt, cho mọi loại ô nhiễm (BR-INS-001) (v1.2) |
| Penalty Decision | Quyết định xử phạt do Inspection Team Leader ban hành (BR-INS-012) |
| Department Common Queue | Hàng đợi chung cấp Tỉnh/Thành phố cho báo cáo thuộc xã/phường chưa onboard (BR-ORG-011) |
| Repeat Offender | Cơ sở vi phạm bị lập biên bản ≥ 2 lần trong 12 tháng (BR-INS-022) |
| Đội cộng đồng | HTX vệ sinh môi trường / Tổ tự quản / 'đường dây rác dân lập'; là CleanupTeam cấp xã/phường (không thuộc công ty), ngang hàng công ty khi LEO điều phối (BR-ORG-020) |
| Tuyến cấp Thành phố | Một số ít trục lớn (đại lộ/quốc lộ xuyên tâm) do đơn vị cấp TP (CITENCO-type) phụ trách; được gắn cờ để escalate báo cáo lên DEO thay vì LEO (BR-ORG-016) |

# Phụ lục C. Ký xác nhận

| Người lập (Team) | Người duyệt (Supervisor) |
|---|---|
|  |  |
