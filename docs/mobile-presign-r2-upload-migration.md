# Mobile Migration — Direct R2 Upload (Presigned)

> **Audience:** Mobile FE (`green-lens-app`)  
> **BE change date:** 2026-07-19  
> **Mục đích:** Thay flow cũ `Mobile → BE → R2` bằng `Mobile → R2` (presigned PUT), rồi chỉ gửi URL về BE.  
> **Repo Mobile:** `D:\CapsoneProject\Mobile\green-lens-app`

---

## 0. TL;DR

### Flow mới (bắt buộc cho ảnh report / before / progress / after)

```text
1) POST /v1/media/presign          → nhận uploadUrl + publicUrl + requiredHeaders
2) PUT  {uploadUrl}                → binary ảnh thẳng lên Cloudflare R2
3) Gọi API nghiệp vụ với publicUrl → BE chỉ lưu metadata (không nhận file)
```

### Endpoint đổi / thêm

| Việc cũ | Việc mới |
|---------|----------|
| `POST /v1/media/reports/images` (multipart `file`) | `POST /v1/media/presign` + `PUT` R2 |
| `POST /v1/reports/{id}/before-images` multipart `images` | `POST` JSON `{ imageUrls: [...] }` |
| `PUT /v1/reports/{id}/progress` multipart files | `PUT` JSON `{ progressPercent, progressNote?, imageUrls? }` |
| After: upload multipart rồi resolve | Presign → PUT R2 → `PUT /resolve` với `afterImageUrls` (giống cũ nhưng URL từ R2 trực tiếp) |
| `POST /v1/reports/analyze` multipart | Presign → PUT R2 → `POST /v1/reports/analyze-uploaded` JSON |

### Legacy / chưa migrate

| Endpoint | Lý do |
|----------|-------|
| `POST /v1/reports/analyze` field `image` | Legacy fallback; Mobile mới không dùng |
| `POST /v1/media/reports/videos` | Transcode phía server |
| `POST /v1/users/avatar` | Có thể migrate sau (đã có purpose `Avatar` trên presign) |

`POST /v1/media/reports/images` **vẫn còn** nhưng **DEPRECATED** — chỉ để rollback.

AI classification là tùy chọn trước submit:

- Mobile bật AI → gọi `/reports/analyze-uploaded`, dùng kết quả để auto-fill form.
- Mobile tắt AI → không gọi classify; report được tạo với `aiPending=false`.
- Sau submit không có classify retry. DINOv2 duplicate comparison vẫn là luồng nền riêng.

---

## 1. `POST /v1/media/presign`

### Request

```http
POST /v1/media/presign
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "fileName": "before_1.jpg",
  "contentType": "image/jpeg",
  "purpose": "Before",
  "reportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileSizeBytes": 1456789
}
```

| Field | Required | Note |
|-------|----------|------|
| `fileName` | Yes | Có extension (`.jpg`/`.png`/…) |
| `contentType` | Yes | Phải khớp header khi PUT |
| `purpose` | Yes | Xem bảng dưới |
| `reportId` | **Bắt buộc** nếu `Before` hoặc `Progress` | UUID report |
| `fileSizeBytes` | No | Hint; nếu > max → 400 `IMAGE_TOO_LARGE` |

### `purpose`

| Value | Folder R2 | Max size | Dùng cho |
|-------|-----------|----------|----------|
| `ReportImage` | `reports/images` | 10 MB | Citizen submit images |
| `After` | `reports/images` | 10 MB | Ảnh after trước resolve |
| `Before` | `reports/{reportId}/before` | 20 MB | Ảnh hiện trạng |
| `Progress` | `reports/{reportId}/progress` | 20 MB | Ảnh tiến độ |
| `Comment` | `comments/images` | 5 MB | (sau này) |
| `Avatar` | `users/avatars` | 5 MB | (sau này) |

### Response `data`

```json
{
  "uploadUrl": "https://....r2.cloudflarestorage.com/...?X-Amz-Algorithm=...",
  "publicUrl": "https://pub-xxx.r2.dev/reports/.../abc.jpg",
  "key": "reports/.../abc.jpg",
  "contentType": "image/jpeg",
  "requiredHeaders": {
    "Content-Type": "image/jpeg"
  },
  "expiresInSeconds": 900,
  "maxSizeBytes": 20971520,
  "purpose": "Before"
}
```

### PUT thẳng lên R2

```http
PUT {uploadUrl}
Content-Type: image/jpeg

<binary file bytes>
```

**Bắt buộc:**

- Header `Content-Type` = đúng `requiredHeaders["Content-Type"]` (và khớp lúc presign).
- Không gắn `Authorization: Bearer` vào PUT R2.
- Timeout PUT khuyến nghị **60–120s**.
- Sau PUT 200/204 → dùng `publicUrl` cho bước 3.

---

## 2. File Mobile phải sửa

### Core (bắt buộc)

| File | Việc cần làm |
|------|----------------|
| `src/services/pollutionReport.service.ts` | Đổi `uploadReportImage()` → presign + PUT R2 |
| `src/services/cleanupAssignment.service.ts` | `uploadBeforeImages`, `updateProgress`, `uploadAfterImagesForResolve` |
| `src/types/pollution-report.types.ts` | Thêm type Presign response |
| `src/types/cleanup-assignment.types.ts` | Đổi payload before/progress sang `imageUrls` |

### Call sites (review; thường ít đổi nếu giữ signature service)

| File | Note |
|------|------|
| `src/hooks/useSubmitPollutionReport.ts` | Submit `{ url, key, mimeType, sizeBytes }`; `key` giúp BE đọc private object trực tiếp |
| `app/report/create.tsx` / `app/report/form.tsx` | Không đổi nếu hook ổn |
| `app/assignment/before-images.tsx` | Gửi JSON URLs thay multipart |
| `app/assignment/progress.tsx` | Gửi JSON thay multipart |
| `app/assignment/complete.tsx` | After qua presign |
| `src/hooks/useAnalyzeReportImage.ts` | Upload R2 một lần rồi gọi `/reports/analyze-uploaded` |

---

## 3. Code mẫu thay `uploadReportImage`

Thay thân hàm trong `pollutionReport.service.ts`:

```ts
type MediaUploadPurpose =
  | 'ReportImage'
  | 'Before'
  | 'Progress'
  | 'After'
  | 'Comment'
  | 'Avatar';

type PresignResult = {
  uploadUrl: string;
  publicUrl: string;
  key: string;
  contentType: string;
  requiredHeaders: Record<string, string>;
  expiresInSeconds: number;
  maxSizeBytes: number;
  purpose: MediaUploadPurpose;
};

async function uploadViaPresign(input: {
  uri: string;
  mimeType: string;
  fileName: string;
  purpose: MediaUploadPurpose;
  reportId?: string;
  fileSizeBytes?: number;
}): Promise<UploadReportImageResult> {
  // 1) Presign
  const presignRes = await api.post<ApiEnvelope<PresignResult>>('/media/presign', {
    fileName: input.fileName,
    contentType: input.mimeType || 'image/jpeg',
    purpose: input.purpose,
    reportId: input.reportId,
    fileSizeBytes: input.fileSizeBytes,
  });

  const presign = presignRes.data.data;
  if (!presign?.uploadUrl || !presign.publicUrl) {
    throw new Error('Presign response missing uploadUrl/publicUrl');
  }

  // 2) Read local file → blob/arraybuffer (RN: fetch(uri) hoặc FileSystem)
  const fileResponse = await fetch(input.uri);
  const blob = await fileResponse.blob();

  // 3) PUT thẳng R2 — KHÔNG dùng axios instance có Bearer
  const putRes = await fetch(presign.uploadUrl, {
    method: 'PUT',
    headers: {
      ...presign.requiredHeaders,
      // đảm bảo Content-Type khớp
      'Content-Type': presign.contentType,
    },
    body: blob,
  });

  if (!putRes.ok) {
    throw new Error(`R2 PUT failed: HTTP ${putRes.status}`);
  }

  // 4) Trả shape cũ để hook submit không vỡ
  return {
    url: presign.publicUrl,
    key: presign.key,
    message: 'Uploaded to R2',
    mimeType: presign.contentType,
    sizeBytes: input.fileSizeBytes ?? blob.size,
  };
}

// Citizen
export async function uploadReportImage(input: UploadReportImageInput) {
  return uploadViaPresign({ ...input, purpose: 'ReportImage' });
}
```

Citizen submit `images[]` dùng `{ url, key, mimeType, sizeBytes }`. `key` được trả từ presign và phải đi cùng đúng `url`.

---

## 4. Cleanup — before images

### Cũ (xoá)

```ts
// multipart
formData.append('images', file);
await api.post(`/reports/${reportId}/before-images`, formData);
```

### Mới

```ts
// 1) Upload từng ảnh (hoặc batch concurrency 2)
const urls: string[] = [];
for (const img of compressedImages) {
  const uploaded = await uploadViaPresign({
    uri: img.uri,
    mimeType: img.mimeType || 'image/jpeg',
    fileName: img.fileName || `before_${Date.now()}.jpg`,
    purpose: 'Before',
    reportId, // BẮT BUỘC
    fileSizeBytes: img.size,
  });
  urls.push(uploaded.url);
}

// 2) Lưu metadata
await api.post(`/reports/${reportId}/before-images`, {
  imageUrls: urls,
});
```

File: `cleanupAssignment.service.ts` → `uploadBeforeImages()`.

---

## 5. Cleanup — progress

### Cũ (xoá)

```ts
formData.append('progressPercent', String(percent));
formData.append('progressNote', note);
formData.append('images', file);
await api.put(`/reports/${reportId}/progress`, formData);
```

### Mới

```ts
const imageUrls: string[] = [];
for (const img of compressedImages) {
  const uploaded = await uploadViaPresign({
    ...img,
    purpose: 'Progress',
    reportId,
  });
  imageUrls.push(uploaded.url);
}

await api.put(`/reports/${reportId}/progress`, {
  progressPercent: percent,
  progressNote: note ?? null,
  imageUrls, // [] nếu không có ảnh
});
```

File: `cleanupAssignment.service.ts` → `updateProgress()`.

---

## 6. Cleanup — after + resolve

`uploadAfterImagesForResolve()` hiện gọi `uploadReportImage` (multipart). Đổi thành:

```ts
purpose: 'After'  // hoặc 'ReportImage' — cùng folder/limit 10MB
```

`resolve()` **không đổi**:

```ts
await api.put(`/reports/${reportId}/resolve`, {
  afterImageUrls: urls, // >= 2
});
```

BE giờ validate URL phải thuộc CDN R2 (`INVALID_STORAGE_URL` nếu sai).

---

## 7. Checklist implement trên app

- [x] Thêm helper presign + PUT dùng chung
- [x] Citizen: purpose `ReportImage`
- [x] After: purpose `After`
- [x] Before: purpose `Before` + `reportId` + JSON `imageUrls`
- [x] Progress: purpose `Progress` + `reportId` + JSON body
- [x] PUT R2 **không** gắn Bearer token
- [x] PUT R2 gửi đúng `Content-Type` từ `requiredHeaders`
- [x] Timeout PUT 90s; retry một lần với lỗi R2 5xx
- [x] JPEG 1600px, quality 0.72 để cân bằng tốc độ/chất lượng AI
- [x] Concurrency tối đa 2 khi upload nhiều ảnh
- [x] AI dùng `/reports/analyze-uploaded`, không upload multipart lần hai
- [x] Không còn FormData cho before/progress
- [ ] Video / avatar: chưa migrate

---

## 8. Lỗi mới cần handle

| `code` | Khi nào |
|--------|---------|
| `INVALID_UPLOAD_PURPOSE` | Sai `purpose` |
| `INVALID_IMAGE_TYPE` | MIME/extension không hợp lệ |
| `IMAGE_TOO_LARGE` | `fileSizeBytes` > max của purpose |
| `INVALID_STORAGE_URL` | URL không thuộc R2 public base của hệ thống |
| `UPLOAD_NOT_FOUND` | PUT chưa thành công hoặc object key không tồn tại |
| `UPLOAD_METADATA_MISMATCH` | URL/key/size gửi lên không khớp object R2 |
| `TOO_MANY_IMAGES` | > 5 URL before/progress |
| `MISSING_BEFORE_IMAGES` | Resolve khi chưa có before |

PUT R2 fail (403/400): thường do sai `Content-Type` hoặc URL hết hạn (15 phút) — presign lại.

---

## 9. Cloudflare R2 CORS (ops)

Bucket R2 **phải** cho phép PUT từ Expo Web/browser. Native iOS/Android không áp dụng browser CORS, nhưng nên cấu hình để hỗ trợ web build:

```json
[
  {
    "AllowedOrigins": ["*"],
    "AllowedMethods": ["PUT", "GET", "HEAD"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

Nếu CORS chưa cấu hình: Mobile PUT sẽ fail dù presign 200. BE/ops cấu hình trên Cloudflare dashboard.

---

## 10. Thứ tự rollout đề xuất

1. Deploy BE (presign + before/progress JSON).
2. Cấu hình R2 CORS.
3. Mobile ship: citizen + after trước (ít đụng contract).
4. Mobile ship: before + progress.
5. Giữ `POST /media/reports/images` vài sprint rồi gỡ.

---

## 11. Verify nhanh

```bash
# 1) Presign
curl -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"fileName":"t.jpg","contentType":"image/jpeg","purpose":"ReportImage"}' \
  "$API/v1/media/presign"

# 2) PUT file to uploadUrl (từ response)
curl -X PUT -H "Content-Type: image/jpeg" --data-binary @t.jpg "$UPLOAD_URL"

# 3) Submit / resolve / before với publicUrl
```

---

**Kết luận:** BE và Mobile đã dùng direct R2 cho report/before/progress/after. AI phân tích cùng object qua `POST /v1/reports/analyze-uploaded`; ảnh chính không bị upload lần hai. Legacy multipart vẫn còn để rollback.
