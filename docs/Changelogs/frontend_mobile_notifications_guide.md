# Hướng dẫn tích hợp Push Notifications (Real-time)

Tài liệu này hướng dẫn cách kết nối hệ thống Real-time Notifications của hệ thống GreenLens. Backend phân chia rõ ràng hai kênh nhận thông báo độc lập:

1. **Web Dashboard (FE)**: Sử dụng **SignalR (WebSockets)**.
2. **Mobile App (Citizen)**: Sử dụng **FCM (Firebase Cloud Messaging)**.

---

## 1. Dành cho Web Dashboard (Sử dụng SignalR)

Web Dashboard bao gồm: Admin, Environmental Officer (LEO, DEO), Company Manager, và Inspector. Backend sử dụng SignalR của ASP.NET Core để đẩy dữ liệu theo thời gian thực (Real-time In-app Notifications).

### 1.1. Cài đặt thư viện

Trên project Web (React/Vue/Angular), bạn cần cài đặt thư viện SignalR của Microsoft:

```bash
npm install @microsoft/signalr
```

### 1.2. Khởi tạo kết nối

Điểm kết nối (Hub URL) là: `https://<api-domain>/hubs/notifications`.
Vì WebSockets chuẩn không hỗ trợ gửi kèm Header `Authorization`, bạn **bắt buộc** phải truyền token JWT qua tham số URL `access_token`.

Ví dụ cách khởi tạo bằng Typescript/Javascript:

```javascript
import * as signalR from "@microsoft/signalr";

const token = "eyJhbGciOiJIUzI1NiIsInR..."; // JWT Access Token hiện tại của user

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.yourdomain.com/hubs/notifications", {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

async function start() {
  try {
    await connection.start();
    console.log("SignalR Connected.");
  } catch (err) {
    console.error(err);
    setTimeout(start, 5000); // Tự động thử lại nếu lỗi
  }
}

// Gọi hàm start
start();
```

### 1.3. Lắng nghe thông báo (Event Listener)

Backend sẽ phát (emit) sự kiện mang tên `"ReceiveNotification"`. Data trả về là một object JSON (kiểu dữ liệu `RealTimeNotificationPayload`).

```javascript
connection.on("ReceiveNotification", (notification) => {
  console.log("New notification received!", notification);

  // Ví dụ cấu trúc payload bạn nhận được:
  // {
  //   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  //   "type": "ReportVerified", // (enum)
  //   "title": "Báo cáo đã được xác minh",
  //   "message": "Báo cáo mã #123 đã được phân công.",
  //   "referenceId": "11111111-2222-3333-4444-555555555555",
  //   "createdAt": "2026-07-13T12:00:00Z"
  // }

  // --> Cập nhật Redux/Context state ở đây để hiển thị thông báo popup (Toast) hoặc chấm đỏ (Badge)
});
```

---

## 2. Dành cho Mobile App (Sử dụng FCM)

Mobile App (Citizen App) sẽ nhận thông báo qua kênh Firebase Cloud Messaging (FCM).

### 2.1. Đăng ký Device Token

Khi người dùng mở App và cho phép (allow) quyền Push Notification, Mobile OS (iOS/Android) sẽ cấp một mã gọi là `FCM Device Token` (qua Firebase SDK).

Mobile App **phải** gửi token này cho Backend để Backend biết "gửi thông báo về cái điện thoại nào".

**API cập nhật Device Token:**

- **Endpoint:** `PUT /api/v1/notifications/device-token`
- **Headers:** `Authorization: Bearer <JWT_Token>`
- **Body:**

```json
{
  "deviceToken": "dL-fA_a2...abc_123"
}
```

_Lưu ý:_ App nên gọi API này mỗi lần User đăng nhập hoặc khi Firebase refresh token (lắng nghe sự kiện `onTokenRefresh`).

### 2.2. Nhận thông báo từ Background / Foreground

- **Khi App đã tắt hoặc chạy nền (Background):**
  Firebase sẽ tự động nhận notification và hiện lên màn hình khoá (System Tray/Lock screen) mà không cần bạn phải viết code xử lý UI.
- **Khi App đang mở (Foreground):**
  Firebase SDK sẽ bắt được sự kiện. Bạn cần lắng nghe sự kiện này (ví dụ `FirebaseMessaging.onMessage` trong Flutter/React Native) để tự vẽ Popup Toast in-app báo cho user.

### 2.3. Cấu trúc Payload FCM Backend gửi

Backend sẽ đính kèm thông tin tham chiếu vào mục `data` của FCM Message. Khi người dùng ấn vào thông báo trên điện thoại, App sẽ đọc được `data` này để thực hiện **Deep link** hoặc chuyển trang.

```json
{
  "notification": {
    "title": "Báo cáo đã được xác minh",
    "body": "Báo cáo mã #123 của bạn hợp lệ."
  },
  "data": {
    "referenceId": "11111111-2222-3333-4444-555555555555",
    "type": "ReportVerified"
  }
}
```

_Frontend Mobile có thể đọc `data.referenceId` để tự động mở màn hình "Chi tiết báo cáo 11111111..." khi user click vào thông báo._
