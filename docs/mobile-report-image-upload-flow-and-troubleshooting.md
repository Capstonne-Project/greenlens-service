# Mobile — Report Image Upload Flow & Troubleshooting

> **Audience:** Mobile FE + Backend developer  
> **Mục đích:** Mô tả đúng flow upload ảnh hiện tại, cách tích hợp trên Mobile, và cách xác định nguyên nhân khi upload lâu hoặc báo “Cannot connect to server”.  
> **Cập nhật:** 2026-07-19 — theo implementation hiện tại trong repository  
> **Base API:** `/v1` · Auth: `Authorization: Bearer {accessToken}`  
> **Response envelope:** `{ code, message, status, data }`  
> **Upload mới (preferred):** xem [`mobile-presign-r2-upload-migration.md`](./mobile-presign-r2-upload-migration.md) — FE PUT thẳng R2 qua `POST /v1/media/presign`.

---

## 0. Kết luận quan trọng trước khi debug

### Kiến trúc upload hiện tại

```text
Mobile
  │  multipart/form-data
  ▼
GreenLens API
  │  AWS S3 SDK / PutObject
  ▼
Cloudflare R2
  │
  ▼
Public URL trả về Mobile
```

Mobile **không upload trực tiếp lên R2**. Toàn bộ file đi qua Backend trước:

1. Mobile gửi file lên GreenLens API.
2. API nhận/buffer hoặc stream file.
3. API tiếp tục gửi file lên R2.
4. API chờ R2 hoàn thành rồi mới trả URL.

Vì vậy một ảnh camera 10–20 MB phải đi qua **hai chặng mạng**. Nếu upload nhiều ảnh, thời gian có thể tăng mạnh.

### Các bottleneck đã xác nhận trong code

| Vấn đề | Ảnh hưởng |
|--------|-----------|
| Before/progress/inspection copy toàn bộ `IFormFile` vào `byte[]` rồi `MemoryStream` lại | Tốn RAM/GC; phải chờ nhận hết file trước khi upload R2 |
| Before/progress/inspection upload từng ảnh lên R2 **tuần tự** | 5 ảnh ≈ tổng thời gian của cả 5 lần `PutObject` |
| `LoggingBehavior` log `{@Request}` cả command chứa `byte[]` ảnh | CPU/I/O log phình to; có thể làm request chậm thêm đáng kể |
| Mọi `*Command` bị bọc DB transaction (`TransactionBehavior`) | Transaction mở xuyên lúc gọi AI / upload R2; giữ connection lâu hơn cần thiết |
| Ảnh gốc từ camera thường 5–20 MB | Dễ vượt client timeout, đặc biệt mạng 4G yếu |
| R2 upload không có timeout/retry riêng trong application | Request phụ thuộc timeout SDK, proxy và client |
| Mobile timeout có thể ngắn hơn thời gian server xử lý | BE có thể đang upload nhưng app đã hiện lỗi |
| Manual report submit tải ngược ảnh đầu tiên từ public URL | Submit có thể chờ thêm tối đa khoảng 15 giây để đọc EXIF |
| AI analyze phải gửi ảnh tiếp sang AI Service | Có thêm một network hop; AI timeout hiện tại là 5 giây |
| Partial batch fail không xóa object R2 đã upload | Orphan files; DB rollback không rollback R2 |

### Nhận định về lỗi “Cannot connect to server”

Thông báo này **không đủ để kết luận Backend lỗi**.

- Nếu BE **không có log request**: thường là base URL, Wi-Fi, emulator, TLS/HTTP, token hoặc request chưa rời app.
- Nếu BE có log **“Uploaded file ... to R2”** nhưng app vẫn báo lỗi: upload đã thành công; lỗi thường nằm ở client timeout, response parsing hoặc navigation/state phía Mobile.
- Nếu BE log `STORAGE_UPLOAD_FAILED`: Backend không upload được R2.
- Nếu chỉ `/reports/analyze` lỗi: kiểm tra AI Service; không nên quy kết cho R2.

Muốn xác định chính xác phải có log Mobile theo mẫu ở phần 9.

---

## 1. Các loại ảnh trong report

| Loại | Actor | `MediaType` | Cách upload |
|------|-------|-------------|-------------|
| Ảnh Citizen gửi ban đầu | Citizen | `Image` | AI flow hoặc manual flow |
| Ảnh trước khi dọn | Cleaner / CompanyStaff leader | `Before` | Multipart nhiều ảnh trong một request |
| Ảnh tiến độ | Cleaner / CompanyStaff leader | `Progress` | Multipart cùng request cập nhật tiến độ |
| Ảnh sau khi dọn | Cleaner / CompanyStaff leader | `After` | Upload từng ảnh lấy URL, sau đó gọi resolve |
| Ảnh bình luận | User đăng nhập | Comment media | Upload riêng, tối đa 5 MB |
| Ảnh bằng chứng inspection | Inspector / LEO | `Inspection` | Multipart `images` — cùng pattern buffer + upload tuần tự như before |
| Video report | Citizen | Video | Upload riêng, server transcode rồi lưu R2 |

---

# PHẦN A — CITIZEN TẠO REPORT

## 2. Citizen có hai flow ảnh — chỉ được chọn một

```text
                  ┌─ AI flow ───── POST /reports/analyze
Chọn ảnh ─────────┤                 → tempImageId
                  │                 → POST /reports với tempImageId
                  │
                  └─ Manual flow ── POST /media/reports/images cho từng ảnh
                                    → URL + MIME + size
                                    → POST /reports với images[]
```

Trong request submit:

- AI flow: có `tempImageId`, không gửi `images`.
- Manual flow: có `images`, không gửi `tempImageId`.
- Gửi cả hai hoặc không gửi cái nào đều bị validation error.

---

## 3. AI flow — phân tích một ảnh trước khi submit

### Bước 1 — Analyze

```http
POST /v1/reports/analyze
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

| Field | Type | Required | Giới hạn |
|-------|------|----------|----------|
| `image` | file | Có | Một ảnh, tối đa 20 MB |

Định dạng chấp nhận:

- `.jpg`, `.jpeg` → `image/jpeg`
- `.png` → `image/png`
- `.webp` → `image/webp`
- `.heic`, `.heif`

### Server thực hiện gì?

```text
Mobile upload
  → API copy toàn bộ ảnh vào RAM
  → API gửi ảnh sang AI Service
  → AI timeout tối đa 5 giây
  → API lưu bytes tạm thời
  → trả tempImageId (TTL 15 phút)
```

Ảnh ở bước này **chưa được lưu vĩnh viễn lên R2**.

### Response 200

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "tempImageId": "32-character-id",
    "expiresInSeconds": 900,
    "aiResult": {},
    "suggestedCategory": {}
  }
}
```

### Bước 2 — Submit bằng `tempImageId`

```http
POST /v1/reports
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "categoryId": "uuid",
  "severity": "Medium",
  "description": "Rác thải tích tụ tại khu vực này",
  "latitude": 10.7626,
  "longitude": 106.6602,
  "address": "Địa chỉ",
  "wardCode": "12345",
  "provinceCode": "79",
  "tempImageId": "32-character-id",
  "images": null,
  "wasteTagIds": [],
  "hideReporterName": false
}
```

Khi submit, BE lấy bytes từ temp store và mới upload ảnh lên R2.

### Lỗi AI flow

| HTTP hiện tại | `code` | Nguyên nhân |
|---------------|--------|-------------|
| 400 | `FILE_REQUIRED` | Field không tên `image`, file rỗng |
| 400 | `INVALID_IMAGE_TYPE` | MIME/extension không được hỗ trợ |
| 413 | `FILE_TOO_LARGE` | Ảnh > 20 MB |
| 500 hiện tại | `AI_SERVICE_UNAVAILABLE` | AI down, connection refused hoặc timeout 5 giây |
| 400 | `TEMP_IMAGE_NOT_FOUND` | Submit sau 15 phút hoặc temp ID sai |
| 500 | `STORAGE_UPLOAD_FAILED` | Submit không đưa được temp image lên R2 |

> Swagger mô tả AI unavailable là 503, nhưng error mapping hiện tại map `Unexpected` thành 500. Mobile nên ưu tiên đọc `response.data.code`, không chỉ đọc HTTP status.

---

## 4. Manual flow — upload 1–5 ảnh trước, submit sau

### Bước 1 — Upload từng ảnh

```http
POST /v1/media/reports/images
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

| Field | Type | Required | Giới hạn |
|-------|------|----------|----------|
| `file` | file | Có | **Một file/request**, tối đa 10 MB |

Endpoint này stream file từ ASP.NET sang R2; không cần copy thành `byte[]` trong handler.

### Response raw từ server

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "url": "https://public-r2.example/reports/images/abc.jpg",
    "key": "reports/images/abc.jpg",
    "message": "Tải ảnh báo cáo thành công.",
    "mimeType": "image/jpeg",
    "sizeBytes": 1234567
  }
}
```

Với Axios:

```ts
const result = response.data.data;
const url = result.url;
```

- `response.data` = envelope.
- `response.data.data` = `UploadReportImageResponse`.
- Nếu API wrapper đã unwrap envelope thì mới dùng `response.data.url`.

Không được trộn hai cách này.

### Bước 2 — Submit JSON

```json
{
  "categoryId": "uuid",
  "severity": "High",
  "description": "Rác thải tích tụ tại khu vực này",
  "latitude": 10.7626,
  "longitude": 106.6602,
  "address": "Địa chỉ",
  "wardCode": "12345",
  "provinceCode": "79",
  "tempImageId": null,
  "images": [
    {
      "url": "https://.../image-1.jpg",
      "mimeType": "image/jpeg",
      "sizeBytes": 1234567
    },
    {
      "url": "https://.../image-2.jpg",
      "mimeType": "image/jpeg",
      "sizeBytes": 2345678
    }
  ],
  "wasteTagIds": [],
  "hideReporterName": false
}
```

Rules:

- 1–5 ảnh.
- Mỗi URL phải là HTTPS absolute URL.
- Mỗi `sizeBytes`: 1 byte đến 10 MB.
- MIME phải thuộc danh sách cho phép.
- Phải dùng đúng metadata server trả về, không tự đoán.

### Vì sao submit manual có thể vẫn chậm sau khi ảnh upload xong?

BE tải ngược **ảnh đầu tiên** từ public URL để kiểm tra EXIF:

```text
POST /reports
  → BE GET public R2 URL của ảnh đầu tiên
  → đọc toàn bộ bytes
  → EXIF analysis
  → lưu report
```

HTTP client `ImageFetch` có timeout 15 giây. Nếu public R2 URL/CDN chậm hoặc chưa truy cập được, submit có thể chờ lâu. Việc fetch thất bại không block report, nhưng vẫn có thể tiêu tốn thời gian đến timeout.

### Lỗi manual upload

| HTTP hiện tại | `code` | Nguyên nhân |
|---------------|--------|-------------|
| 400 | `FILE_REQUIRED` | Không gửi field `file`, file rỗng |
| 400 | `INVALID_IMAGE_TYPE` | MIME/extension không hợp lệ |
| 400 | `IMAGE_TOO_LARGE` | File > 10 MB |
| 401 | Auth error | Thiếu/hết hạn token |
| 500 | `STORAGE_UPLOAD_FAILED` | R2 lỗi hoặc cấu hình storage sai |

> Swagger cũ có thể ghi 422, nhưng Result mapping hiện tại trả lỗi validation bằng HTTP 400.

---

# PHẦN B — TEAM DỌN DẸP

## 5. Before images

```http
POST /v1/reports/{reportId}/before-images
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

| Field | Type | Required | Contract UI |
|-------|------|----------|-------------|
| `images` | file[] | Có | 1–5 ảnh, mỗi ảnh ≤ 20 MB |

Điều kiện nghiệp vụ:

- Role: `Cleaner`, `CompanyStaff`, hoặc `Admin`.
- User phải là Team Leader.
- Report phải `InProgress`.
- Assignment của team phải `InProgress`.

### Response

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "uploadedImageUrls": [
      "https://.../before-1.jpg",
      "https://.../before-2.jpg"
    ]
  }
}
```

### Cách BE xử lý hiện tại

```text
Nhận tất cả IFormFile
  → copy từng file vào byte[]
  → giữ danh sách byte[] trong RAM
  → upload ảnh 1 lên R2 và chờ
  → upload ảnh 2 lên R2 và chờ
  → ...
  → SaveChanges DB một lần
  → trả response
```

Điểm cần lưu ý:

- Code kiểm tra mỗi ảnh ≤ 20 MB.
- Swagger nói tối đa 5 ảnh, nhưng handler hiện chưa enforce `images.Count <= 5`.
- Mobile vẫn phải giới hạn tối đa 5.
- Nếu upload ảnh 1 thành công nhưng ảnh 2 lỗi, request thất bại và có thể để lại file ảnh 1 trên R2 nhưng chưa lưu DB.

---

## 6. Progress images

```http
PUT /v1/reports/{reportId}/progress
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

| Field | Type | Required |
|-------|------|----------|
| `progressPercent` | integer 0–100 | Có |
| `progressNote` | string | Không |
| `images` | file[] | Không |

Ví dụ:

```text
progressPercent = 60
progressNote = "Đã dọn xong khu vực A"
images = progress_1.jpg
images = progress_2.jpg
```

BE cũng buffer toàn bộ ảnh thành `byte[]` và upload tuần tự lên R2.

Lưu ý:

- `progressPercent = 100` không tự resolve.
- Code hiện kiểm tra mỗi file ≤ 20 MB.
- Code hiện chưa enforce tối đa 5 ảnh dù Swagger mô tả tối đa 5.
- Flow này hiện không dùng `ReportImageContentTypes` để validate MIME như generic upload.

---

## 7. After images + Resolve

Không có endpoint multipart riêng cho after images.

### Bước 1 — Upload từng ảnh after

Gọi cho từng ảnh:

```http
POST /v1/media/reports/images
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

Field: `file`.

Mỗi ảnh after do đó bị giới hạn **10 MB**, không phải 20 MB.

### Bước 2 — Resolve bằng URLs

```http
PUT /v1/reports/{reportId}/resolve
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "afterImageUrls": [
    "https://.../after-1.jpg",
    "https://.../after-2.jpg"
  ]
}
```

Rules:

- Tối thiểu 2 URLs.
- Phải có ít nhất một `Before` image.
- User phải là Team Leader.
- Assignment phải `InProgress`.

Sau resolve, BE lưu các URL thành `ReportMedia` với `MediaType.After`.

Lưu ý implementation hiện tại của resolve:

- Chỉ kiểm tra `afterImageUrls.Count >= 2`.
- **Không** validate URL rỗng / HTTPS / thuộc R2 / thuộc caller.
- Khi persist after media, BE hard-code `mimeType = "image/jpeg"` và `sizeBytes = 0`.

### Không dùng flow presigned cũ

Một số tài liệu cũ ghi:

```text
POST /v1/media/presign
PUT trực tiếp lên S3
```

Endpoint này **không tồn tại trong implementation hiện tại**. Flow đúng là:

```text
POST /v1/media/reports/images
→ lấy data.url
→ PUT /v1/reports/{reportId}/resolve
```

---

# PHẦN C — CÁCH CODE MOBILE AN TOÀN

## 8. Chuẩn bị file trước khi upload

### Bắt buộc

- Resize ảnh camera trước khi upload.
- Khuyến nghị cạnh dài tối đa: 1600–1920 px.
- JPEG quality khuyến nghị: 0.7–0.85.
- Mục tiêu: khoảng 1–3 MB/ảnh.
- Giữ `name`, `type`, `uri`, `size`.
- Không đưa Base64 vào JSON.
- Không đọc toàn bộ file thành Base64 chỉ để upload; Base64 tăng kích thước khoảng 33%.

### React Native FormData

```ts
type MobileFile = {
  uri: string;
  name: string;
  type: string;
  size?: number;
};

function toFormDataFile(file: MobileFile) {
  return {
    uri: file.uri,
    name: file.name || `photo-${Date.now()}.jpg`,
    type: file.type || 'image/jpeg',
  } as any;
}
```

Android có thể trả `content://...`. Nếu networking library không đọc được URI này:

1. Copy file vào cache/app storage.
2. Dùng URI mới dạng `file://...`.
3. Không gửi local path làm JSON URL.

### Không tự set multipart boundary

Ưu tiên:

```ts
await api.post('/v1/media/reports/images', formData, {
  timeout: 60_000,
});
```

Không hard-code:

```ts
headers: { 'Content-Type': 'multipart/form-data; boundary=...' }
```

Networking library phải tự sinh boundary khớp với body.

Nếu project bắt buộc set header, chỉ dùng:

```ts
headers: { 'Content-Type': 'multipart/form-data' }
```

và kiểm tra library thực sự tự thêm boundary.

---

## 9. Upload helper có timeout, progress và log

```ts
async function uploadReportImage(file: MobileFile) {
  const startedAt = Date.now();
  const form = new FormData();
  form.append('file', toFormDataFile(file));

  console.info('[upload:start]', {
    endpoint: '/v1/media/reports/images',
    name: file.name,
    type: file.type,
    size: file.size,
    uriScheme: file.uri?.split(':')[0],
  });

  try {
    const response = await api.post('/v1/media/reports/images', form, {
      timeout: 60_000,
      onUploadProgress: event => {
        if (!event.total) return;
        console.info('[upload:progress]', {
          name: file.name,
          percent: Math.round((event.loaded * 100) / event.total),
        });
      },
    });

    const envelope = response.data;
    const uploaded = envelope.data;

    console.info('[upload:success]', {
      name: file.name,
      durationMs: Date.now() - startedAt,
      status: response.status,
      code: envelope.code,
      url: uploaded?.url,
    });

    if (!uploaded?.url) {
      throw new Error('UPLOAD_RESPONSE_MISSING_URL');
    }

    return uploaded as {
      url: string;
      key: string;
      mimeType: string;
      sizeBytes: number;
    };
  } catch (error: any) {
    console.error('[upload:failed]', {
      name: file.name,
      durationMs: Date.now() - startedAt,
      message: error?.message,
      code: error?.code,
      httpStatus: error?.response?.status,
      responseCode: error?.response?.data?.code,
      responseMessage: error?.response?.data?.message,
      hasResponse: Boolean(error?.response),
      hasRequest: Boolean(error?.request),
    });
    throw error;
  }
}
```

### Không log

- Access token.
- File bytes/Base64.
- GPS chính xác ở Information log.
- R2 credentials.

---

## 10. Upload nhiều ảnh: giới hạn concurrency

### Không nên

```ts
await Promise.all(files.map(uploadReportImage));
```

5 ảnh lớn chạy đồng thời có thể:

- bão hòa băng thông thiết bị;
- tăng RAM;
- làm request timeout đồng loạt;
- khó biết ảnh nào thành công.

### Khuyến nghị: concurrency = 2

```ts
async function uploadInBatches(files: MobileFile[]) {
  const uploaded: Array<Awaited<ReturnType<typeof uploadReportImage>>> = [];

  for (let i = 0; i < files.length; i += 2) {
    const batch = files.slice(i, i + 2);
    const batchResults = await Promise.all(batch.map(uploadReportImage));
    uploaded.push(...batchResults);
  }

  return uploaded;
}
```

Lưu kết quả theo từng file:

```ts
type UploadState =
  | { state: 'pending'; file: MobileFile }
  | { state: 'uploading'; file: MobileFile; progress: number }
  | { state: 'success'; file: MobileFile; url: string; mimeType: string; sizeBytes: number }
  | { state: 'failed'; file: MobileFile; errorCode?: string };
```

Retry **chỉ ảnh failed**, không upload lại ảnh đã success.

---

## 11. Retry policy Mobile

Retry tối đa 2 lần với backoff:

```text
Lần 1 thất bại → chờ 1 giây
Lần 2 thất bại → chờ 3 giây
Sau đó hiển thị lỗi thật cho user
```

### Có thể retry

- Network mất tạm thời.
- Timeout client.
- HTTP 500 `STORAGE_UPLOAD_FAILED`.
- HTTP 502/503/504 từ reverse proxy.

### Không retry tự động

- 400 `FILE_REQUIRED`.
- 400 `INVALID_IMAGE_TYPE`.
- 400 `IMAGE_TOO_LARGE`.
- 401/403 — refresh token hoặc yêu cầu đăng nhập lại.
- 413 — phải giảm kích thước ảnh.
- 422 `NOT_TEAM_LEADER`, `ASSIGNMENT_NOT_IN_PROGRESS`.

> Upload hiện chưa có idempotency key. Retry sau timeout có thể tạo thêm một object R2 nếu request trước đã hoàn thành nhưng response bị mất. Vì vậy luôn giữ URL của request đã success và chỉ retry file chưa xác nhận thành công.

---

# PHẦN D — CHẨN ĐOÁN LỖI

## 12. Decision tree 5 phút

```text
App báo upload lỗi
│
├─ BE có nhận request không?
│  │
│  ├─ Không
│  │  ├─ Kiểm tra base URL
│  │  ├─ localhost/10.0.2.2/LAN IP
│  │  ├─ Wi-Fi + firewall
│  │  ├─ HTTP cleartext / HTTPS certificate
│  │  └─ FormData URI/boundary
│  │
│  └─ Có
│     │
│     ├─ HTTP 400/413/401/422?
│     │  └─ Đọc response.code và sửa request
│     │
│     ├─ Log STORAGE_UPLOAD_FAILED?
│     │  └─ Kiểm tra R2/network/config BE
│     │
│     ├─ Log Uploaded file ... to R2?
│     │  ├─ Có → upload BE thành công
│     │  │      Kiểm tra client timeout/response parsing/UI state
│     │  └─ Không → request còn đang upload hoặc lỗi trước R2
│     │
│     └─ Chỉ analyze lỗi?
│        └─ Kiểm tra AI Service localhost:8000 / timeout 5s
```

---

## 13. Các nguyên nhân thường gặp trên Mobile

### 13.1 Base URL sai

| Thiết bị | Base URL local |
|----------|----------------|
| Android Emulator | `http://10.0.2.2:{port}` |
| Genymotion | Thường `http://10.0.3.2:{port}` |
| iOS Simulator | Có thể dùng `http://127.0.0.1:{port}` |
| Điện thoại thật | `http://{LAN-IP-máy-chạy-BE}:{port}` |

Trên điện thoại thật, `localhost` là **điện thoại**, không phải máy chạy Backend.

Kiểm tra thêm:

- Điện thoại và máy Backend cùng Wi-Fi.
- Windows Firewall cho phép port API.
- API listen trên interface phù hợp, không chỉ loopback.
- Android cho phép cleartext HTTP trong môi trường dev hoặc dùng HTTPS hợp lệ.

### 13.2 Field name sai

| Endpoint | Field đúng |
|----------|------------|
| `/v1/reports/analyze` | `image` |
| `/v1/media/reports/images` | `file` |
| `/v1/reports/{id}/before-images` | `images` lặp lại nhiều lần |
| `/v1/reports/{id}/progress` | `progressPercent`, `progressNote`, `images` |

`file`, `image`, và `images` **không thay thế cho nhau**.

### 13.3 Client timeout quá ngắn

Khuyến nghị:

| Request | Client timeout |
|---------|----------------|
| Một ảnh đã resize 1–3 MB | 30–60 giây |
| Before/progress nhiều ảnh | 60–120 giây |
| AI analyze | 30 giây |
| Video | 2–5 phút |

Timeout không phải cách sửa upload chậm; vẫn phải resize/compress ảnh trước.

### 13.4 Response parsing sai

Raw Axios response:

```text
response.data.data.url
```

Nếu interceptor đã return `response.data`:

```text
response.data.url
```

Nếu upload thành công nhưng code đọc sai đường dẫn, app có thể báo “upload failed” dù R2 đã có file.

### 13.5 Loading state bị reset sai

```ts
setUploading(true);
try {
  const result = await uploadReportImage(file);
  // update success state
} catch (error) {
  // show actual error
} finally {
  setUploading(false);
}
```

- Disable double-tap khi đang upload.
- Không navigate trước khi request hoàn thành.
- Không dùng một boolean chung cho nhiều file nếu cần hiển thị từng tiến độ.

### 13.6 Ảnh quá lớn hoặc HEIC

- Generic upload chỉ tối đa 10 MB.
- Analyze/before/progress cho phép 20 MB.
- iPhone thường trả HEIC và MIME vendor-specific.
- BE có extension fallback cho generic upload/analyze, nhưng before/progress chưa normalize MIME.
- Cách ổn định nhất cho Mobile: convert ảnh về JPEG trước upload.

---

## 14. Phân biệt Network Error và HTTP Error

```ts
if (error.response) {
  // Server đã trả HTTP — KHÔNG gọi là "không kết nối được"
  show(`${error.response.data?.code}: ${error.response.data?.message}`);
} else if (error.request) {
  // Request gửi đi nhưng không nhận response
  show('Không nhận được phản hồi từ máy chủ. Kiểm tra mạng hoặc timeout.');
} else {
  // Lỗi tạo request/FormData/URI
  show(`Không thể tạo yêu cầu upload: ${error.message}`);
}
```

Không gom mọi lỗi thành “Cannot connect to server”.

---

## 15. Log bắt buộc để tìm nguyên nhân

### Mobile log cho mỗi file

```text
uploadId
endpoint
baseURL (không chứa token)
fileName
fileSizeBytes
mimeType
uriScheme: file/content/ph
startedAt
upload progress %
durationMs
HTTP status
response.code
response.message
Axios error.code
hasRequest
hasResponse
```

### Backend cần đối chiếu

```text
Request start + path
Request completed + HTTP status + elapsed ms
R2 upload success: key
R2 exception type/message
AI timeout/down log (chỉ analyze)
Thời gian từng PutObject (nếu có thêm instrumentation)
```

Lưu ý khi đọc BE log hiện tại:

- `LoggingBehavior` đang `LogInformation("Handling {RequestName} {@Request}", …)` → **không nên** expect thấy binary an toàn; payload ảnh có thể làm log rất lớn.
- Nếu BE “treo” lâu trước khi có dòng `Uploaded file ... to R2`, kiểm tra thêm transaction/logging overhead chứ không chỉ mạng R2.

### Cách kết luận

| Quan sát | Kết luận gần nhất |
|----------|-------------------|
| Mobile fail, BE không có request | Mobile/network/base URL/FormData |
| BE trả 400/413 | Request hoặc file không hợp lệ |
| BE `Uploaded file ... to R2`, Mobile timeout | Client timeout hoặc mất response |
| BE R2 upload mất nhiều giây | R2/network/server region |
| BE upload nhanh, tổng request lâu | Xử lý sau upload hoặc client/UI |
| Manual submit lâu ~15 giây | BE fetch public image để EXIF có thể timeout |
| Analyze lỗi đúng 5 giây | AI Service timeout/down |
| Before/progress càng nhiều ảnh càng lâu tuyến tính | Upload tuần tự hiện tại |

---

# PHẦN E — CURL TEST TÁCH MOBILE KHỎI BACKEND

## 16. Test generic image upload

```bash
curl -v \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test.jpg;type=image/jpeg" \
  "http://YOUR_API/v1/media/reports/images"
```

Nếu curl/Postman upload nhanh và ổn định nhưng Mobile lỗi, tập trung debug Mobile.

## 17. Test analyze

```bash
curl -v \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "image=@test.jpg;type=image/jpeg" \
  "http://YOUR_API/v1/reports/analyze"
```

## 18. Test before images

```bash
curl -v \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "images=@before-1.jpg;type=image/jpeg" \
  -F "images=@before-2.jpg;type=image/jpeg" \
  "http://YOUR_API/v1/reports/REPORT_ID/before-images"
```

## 19. Test progress

```bash
curl -v -X PUT \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "progressPercent=60" \
  -F "progressNote=Đã dọn xong khu vực A" \
  -F "images=@progress-1.jpg;type=image/jpeg" \
  "http://YOUR_API/v1/reports/REPORT_ID/progress"
```

---

# PHẦN F — KNOWN GAPS / BACKLOG

## 20. Backend gaps đã thấy

| Priority | Gap | Tác động |
|----------|-----|----------|
| High | Upload proxy qua BE, chưa có presigned R2 upload | Hai chặng mạng, tăng tải BE |
| High | Before/progress/inspection buffer toàn bộ ảnh trong RAM | Tốn RAM, chậm với file lớn |
| High | Before/progress/inspection upload R2 tuần tự | Thời gian tăng theo số ảnh |
| High | `LoggingBehavior` destructure toàn bộ command (`{@Request}`) kể cả `byte[]` | Log nặng, chậm request upload |
| High | `TransactionBehavior` mở DB transaction xuyên AI/R2 upload | Giữ connection lâu; rollback DB không xóa R2 |
| Medium | Chưa enforce tối đa 5 ảnh trong before/progress/inspection | Request có thể rất lớn |
| Medium | Chưa validate/normalize MIME cho before/progress/inspection | File không chuẩn có thể lọt vào |
| Medium | Không rollback/xóa R2 objects khi batch upload lỗi giữa chừng | Có orphan files |
| Medium | Generic upload chưa có idempotency key | Retry có thể tạo duplicate object |
| Medium | Manual submit fetch lại ảnh đầu tiên từ public URL | Có thể thêm tới 15 giây |
| Medium | Resolve không validate URL after; hard-code MIME/size | Metadata after images kém chính xác |
| Medium | `GET /teams/my-tasks/{reportId}` chỉ trả `MediaType.Image` (ảnh citizen) | Team detail không hiện before/progress/after |
| Low | Swagger status một số lỗi khác HTTP thực tế | FE xử lý nhầm status |
| Low | `UploadProgressImage` slice tồn tại nhưng không được controller dùng | Dễ gây nhầm khi đọc code |
| Low | Doc cũ ghi decline window 2h; code hiện tại 24h | FE countdown sai |

### Hướng cải thiện dài hạn

Flow tối ưu:

```text
Mobile
  → xin presigned URL từ BE
  → upload trực tiếp R2
  → confirm metadata với BE
```

Lợi ích:

- Không truyền bytes qua API server.
- Giảm RAM/CPU/bandwidth BE.
- Upload nhanh hơn.
- Dễ multipart/resumable upload.

Đây là thay đổi contract và security, cần thiết kế riêng; không nên tự chuyển Mobile sang upload trực tiếp khi BE chưa cấp presigned URL.

---

## 21. Documentation cũ không còn đúng

Không dùng các hướng dẫn sau nếu thấy trong tài liệu cũ:

- `POST /v1/media/presign` — hiện không có endpoint.
- `POST /v1/pollution-reports` — route hiện tại là `POST /v1/reports`.
- Upload report image anonymous — `MediaController` hiện có `[Authorize]`.
- Submit anonymous / `isAnonymous` — submit hiện yêu cầu đăng nhập; field hiện là `hideReporterName`.
- Resolve trả 204 — controller hiện trả envelope HTTP 200 qua `ToHttpNoContent`.
- Invalid image luôn 422 — mapping hiện tại trả validation error bằng HTTP 400.
- Progress field `percent`/`note` — field thật là `progressPercent` / `progressNote`.
- Decline window 2 giờ — code hiện tại dùng **24 giờ** (`declineDeadlineAt = assignedAt + 24h`).
- Before/progress “max 5” trong Swagger/doc — handler **chưa enforce** count.
- Local base URL cố định `localhost:5000` — xem `launchSettings.json` (thường `5162`/`7041`) và IP LAN/emulator tương ứng.

---

## 22. Checklist bàn giao cho Mobile FE

### Trước upload

- [ ] Resize/compress về JPEG, mục tiêu 1–3 MB.
- [ ] Kiểm tra file không rỗng và đúng URI.
- [ ] Generic/after: file ≤ 10 MB.
- [ ] Analyze/before/progress: file ≤ 20 MB.
- [ ] Không chọn quá 5 ảnh.
- [ ] Token còn hạn.

### Khi upload

- [ ] Field name đúng endpoint.
- [ ] Không hard-code multipart boundary.
- [ ] Timeout 60 giây cho image upload.
- [ ] Hiển thị progress theo từng file.
- [ ] Concurrency tối đa 2 cho upload từng ảnh.
- [ ] Disable double-tap.
- [ ] Log duration/status/error theo mẫu.

### Sau upload

- [ ] Đọc đúng `response.data.data`.
- [ ] Kiểm tra `url` tồn tại trước khi đánh dấu success.
- [ ] Giữ URL đã upload thành công trong state.
- [ ] Chỉ retry file failed.
- [ ] Submit report/resolve chỉ khi đủ URL.
- [ ] Sau mutation, refetch detail.

---

## 23. Checklist tìm nguyên nhân cho lỗi đang gặp

Thực hiện theo đúng thứ tự:

1. Chọn **một ảnh JPEG khoảng 500 KB–1 MB**.
2. Upload bằng curl/Postman tới cùng API.
3. Upload cùng ảnh đó bằng Mobile.
4. Ghi lại Mobile `durationMs`, HTTP status, `response.code`.
5. Đối chiếu timestamp với BE.
6. Kiểm tra có log `Uploaded file ... to R2` không.
7. Nếu BE success nhưng Mobile fail: tăng timeout 60 giây và sửa response parsing.
8. Nếu BE không nhận request: sửa base URL/network/FormData URI.
9. Nếu file nhỏ thành công, file camera lỗi: bắt buộc resize/compress.
10. Nếu chỉ before/progress nhiều ảnh lỗi: giảm còn 1–2 ảnh để xác nhận bottleneck batch tuần tự.
11. Nếu chỉ analyze lỗi: chạy AI Service hoặc dùng manual flow.
12. Không hiển thị chung “Cannot connect”; hiển thị `response.code` thật.

---

## 24. Code backend tham chiếu

| Thành phần | File |
|------------|------|
| Generic image endpoint | `src/Greenlens.Api/Controllers/MediaController.cs` |
| Generic image handler | `src/Greenlens.Application/Features/Media/UploadReportImage/UploadReportImageCommandHandler.cs` |
| Analyze endpoint | `src/Greenlens.Api/Controllers/ReportsController.cs` |
| Analyze handler | `src/Greenlens.Application/Features/Reports/AnalyzeReportImage/AnalyzeReportImageCommandHandler.cs` |
| Submit report | `src/Greenlens.Application/Features/Reports/SubmitPollutionReport/` |
| Before images | `src/Greenlens.Application/Features/Reports/UploadBeforeImages/` |
| Progress images | `src/Greenlens.Application/Features/Reports/UpdateProgress/` |
| R2 adapter | `src/Greenlens.Infrastructure/Storage/R2FileStorageService.cs` |
| MIME normalization | `src/Greenlens.Application/Common/ReportImageContentTypes.cs` |
| HTTP response mapping | `src/Greenlens.Api/Extensions/ResultExtensions.cs` |

---

**Kết luận:** trước khi sửa Backend, Mobile phải bổ sung resize/compress, timeout hợp lý, per-file progress, log thật và phân biệt network error với HTTP error. Nếu BE đã log upload R2 thành công nhưng app vẫn báo lỗi, ưu tiên sửa Mobile response handling/timeout. Backend vẫn có backlog hiệu năng rõ ràng ở before/progress và kiến trúc proxy upload.
