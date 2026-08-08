# Hướng dẫn test UI đầy đủ — Community Cleanup

> Test bằng tay trên UI thật (portal + mobile app), bấm từng nút theo đúng thứ tự workflow. Không dùng API/curl — mục tiêu là xác nhận trải nghiệm người dùng thật.

---

## 0. Chuẩn bị trước khi test

- [ ] Backend đang chạy (`dotnet run` project `Greenlens.Api`), đã restart sau các lần sửa code gần đây.
- [ ] PostGIS đã bật (`CREATE EXTENSION postgis` + migration `EnablePostGisExtension` đã apply) — check-in sẽ lỗi nếu chưa xong bước này.
- [ ] Portal (`greenlens-portal`) và mobile app (Expo Go) đều trỏ đúng vào backend local.

### Tài khoản test có sẵn (dev DB)

| Vai trò | Email | Mật khẩu | Ghi chú |
|---|---|---|---|
| LEO (mở chương trình) | `leo.26743@greenlens.dev` | `Officer@123` | Quản lý office "Ủy Ban Nhân Dân Bến Thành" |
| Leader (Cleaner) | `phuchaulua456@gmail.com` | *(mật khẩu riêng của bạn)* | Thành viên team "Tiểu đội Xe Không Kính", đã có sẵn 1 event test |
| Citizen (join/vote) | dùng tài khoản Citizen bất kỳ bạn có trên mobile | — | Để test Join/Withdraw/Check-in phía citizen |

> Lưu ý: report `RPT-260725-F945CF` đã có 1 community event test ("Test notification flow", bắt đầu 01/08/2026) đang ở trạng thái **OpenForJoin** — dùng report này để test riêng phần "chưa đến giờ bắt đầu", **không dùng để test hết luồng** (vì `StartsAt` ở tương lai, Start/Check-in sẽ luôn báo lỗi cho tới đúng giờ đó). Khi test full luồng end-to-end, tạo **1 chương trình mới** trên **báo cáo Verified khác**, đặt "Bắt đầu dọn" là **giờ hiện tại hoặc quá khứ gần** để không bị chặn.

---

## 1. Portal (LEO) — Mở chương trình dọn cộng đồng

1. [ ] Đăng nhập portal bằng tài khoản LEO ở trên.
2. [ ] Vào danh sách báo cáo, lọc trạng thái **Verified**, chọn 1 báo cáo chưa có assignment/company nào.
3. [ ] Bấm **"Mở chương trình dọn cộng đồng"** → dialog hiện ra.
4. [ ] Điền: Tên chương trình, chọn **Đội Cleanup**, chọn **Leader** (danh sách thành viên phải hiện đủ — nếu "Đội này chưa có thành viên" là bug, báo lại).
5. [ ] Đặt **"Bắt đầu dọn"** = giờ hiện tại hoặc vài phút trước (để test được ngay, không phải chờ).
6. [ ] Bấm **"Mở chương trình"** → toast thành công, dialog đóng.
7. [ ] Vào lại chi tiết báo cáo đó → xác nhận **Status = InProgress**, thấy block Community Cleanup xuất hiện.
8. [ ] Vào **map công khai** (portal hoặc mobile) → tìm đúng báo cáo này → pin phải hiện **màu đen** kèm nhãn **"Cộng đồng"** nổi phía trên (không cần bấm vào mới thấy).

---

## 2. Mobile — Leader nhận thông báo

1. [ ] Đăng nhập mobile bằng tài khoản Leader vừa chọn ở bước 1.4.
2. [ ] Bấm icon chuông (góc trên phải màn hình Home) → badge số phải hiện ngay nếu app đang mở lúc LEO bấm "Mở chương trình" (SignalR realtime); nếu app đóng lúc đó thì mở lại app cũng phải thấy badge.
3. [ ] Vào mục Thông báo → thấy dòng **"Bạn được chỉ định làm Leader"** → bấm vào.
4. [ ] Xác nhận app nhảy đúng sang màn **"Không gian điều phối"** (community-lead workspace) của đúng chương trình vừa tạo, có badge đen **"Cộng đồng"** ở header.
5. [ ] Quay về Home → xem section **"Cộng đồng của bạn"** (thay chỗ "Từ Officer" cũ) → phải thấy đúng chương trình này trong danh sách, kèm % tiến độ + số người tham gia. Bấm vào dòng đó cũng phải mở đúng workspace.

---

## 3. Mobile — Citizen tham gia (Join)

1. [ ] Đăng nhập mobile bằng tài khoản Citizen.
2. [ ] Vào tab bản đồ/cộng đồng → tìm chương trình vừa mở (theo report ở bước 1).
3. [ ] Bấm vào → xem chi tiết chương trình (tên, leader, số chỗ còn lại, điểm tập trung).
4. [ ] Bấm **"Tham gia"** (Join) → xác nhận thành công, participant count tăng lên.
5. [ ] (Tuỳ chọn) Test **"Rút khỏi chương trình"** (Withdraw) trước khi Leader bấm Start — phải cho rút; sau khi status chuyển InProgress thì không được rút nữa (nút phải ẩn/disable hoặc báo lỗi).

---

## 4. Mobile — Leader vận hành chương trình

Ở màn "Không gian điều phối" của Leader:

1. [ ] **Test validate giờ bắt đầu (quan trọng):** nếu bạn đang dùng event test có sẵn (StartsAt = 01/08/2026, còn tương lai) → bấm **"Check-in tại điểm tập trung"** hoặc **"Bắt đầu dọn dẹp"** → phải hiện toast lỗi đỏ dạng *"Chưa đến giờ bắt đầu dọn dẹp. Dự kiến bắt đầu lúc HH:mm ngày dd/MM."* — không được gọi API, không đổi trạng thái.
2. [ ] Chuyển qua event mới tạo ở mục 1 (StartsAt = hiện tại/quá khứ) → bấm **"Check-in tại điểm tập trung"**:
   - App xin quyền vị trí (nếu lần đầu) → cho phép.
   - Nếu vị trí thật của bạn cách xa điểm tập trung > 200m → phải báo lỗi *"Vị trí check-in cách điểm tập trung hơn 200m"*.
   - Nếu trong bán kính 200m → toast "Check-in thành công!".
3. [ ] Bấm **"Bắt đầu dọn dẹp"** → toast thành công, status chuyển **InProgress**, block ảnh before/tiến độ/ảnh after hiện ra.
4. [ ] **Ảnh trước khi dọn:** bấm "Chụp ảnh" hoặc "Thư viện" → chọn ít nhất 1 ảnh → xem ảnh hiện đúng dạng lưới (UI giống hệt flow assignment thường) → bấm **"Lưu ảnh trước khi dọn"** → thành công, block này biến mất (đã có before media).
5. [ ] **Cập nhật tiến độ:** nhập % (thử 50%) + ghi chú → bấm **"Cập nhật tiến độ"** → toast thành công, thanh progress cập nhật.
6. [ ] Cập nhật tiến độ lần 2 lên **100%**.
7. [ ] **Ảnh sau khi dọn:** thêm **ít nhất 2 ảnh** → nút **"Nộp xác thực hoàn thành"** phải **disabled** nếu chưa đủ điều kiện (chưa 100% hoặc chưa đủ 2 ảnh after) — thử xoá bớt ảnh để xác nhận nút tự disable lại.
8. [ ] Đủ điều kiện (100% + ≥2 ảnh after) → bấm **"Nộp xác thực hoàn thành"** → toast thành công, status chuyển **PendingVerification**, màn hiện "Đang chờ LEO duyệt xác thực".

---

## 5. Portal (LEO) — Duyệt xác thực

1. [ ] Đăng nhập lại portal bằng LEO → vào **hàng đợi chương trình cộng đồng** (office queue, mặc định lọc PendingVerification).
2. [ ] Mở đúng chương trình vừa nộp → xem lại ảnh before/after, tiến độ.
3. [ ] **Test Reject trước:** bấm **"Từ chối xác thực"**, nhập lý do ≥ 20 ký tự → xác nhận status quay lại **InProgress**, Leader trên mobile phải thấy lại được các nút cập nhật tiến độ/ảnh after (test nộp lại lần 2 nếu muốn).
4. [ ] Nộp xác thực lại từ mobile (lặp bước 4.7–4.8) → quay lại portal, lần này bấm **"Duyệt xác thực"** (Approve) → status chuyển **Completed**.
5. [ ] Vào lại chi tiết báo cáo gốc → xác nhận **Report Status = Resolved**.
6. [ ] Kiểm tra map: pin báo cáo này không còn hiện marker đen "Cộng đồng" nữa (vì event đã Completed, không còn active).

---

## 6. Test case phụ (nếu còn thời gian)

- [ ] **Hủy chương trình:** tạo 1 event mới, LEO bấm "Hủy chương trình" với lý do ≥20 ký tự → report quay về Verified, participants đang Joined/CheckedIn bị rút tự động.
- [ ] **Đóng đăng ký sớm:** LEO bấm "Đóng đăng ký" khi đang OpenForJoin → status chuyển JoinClosed, Citizen không Join được nữa nhưng Leader vẫn check-in/start bình thường.
- [ ] **Chương trình đầy chỗ:** set `maxParticipants` nhỏ (vd. 1), Join tới khi đầy → Citizen tiếp theo bấm Join phải bị chặn với lỗi "đã đủ người".
- [ ] **2 người cùng làm Leader 2 report khác nhau song song** — xác nhận không đụng dữ liệu chéo nhau (danh sách "Cộng đồng của bạn" của mỗi Leader chỉ hiện đúng event của mình).

---

## 7. Ghi lại kết quả

Khi test xong, ghi chú lại:
- Bước nào fail (kèm ảnh chụp màn hình nếu có).
- Thông báo lỗi hiển thị có dễ hiểu không.
- Có bước nào UX rối/thiếu hướng dẫn không.

Gửi lại danh sách này để xử lý tiếp.
