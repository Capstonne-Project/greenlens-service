# FE Guide — Community Cleanup Share (LEO Web / Next.js)

> **Phiên bản:** 2026-08-27 · **Backend:** GreenLens API v1  
> **Audience:** LEO Web (Next.js trên Vercel)  
> **Liên quan:** [`community-cleanup-feature-spec.md`](./community-cleanup-feature-spec.md) · [`community-cleanup-ui-test-guide.md`](./community-cleanup-ui-test-guide.md)

Tài liệu hướng dẫn FE tích hợp **dialog “Tạo chương trình thành công”** + **chia sẻ mạng xã hội** sau khi LEO mở chương trình dọn dẹp cộng đồng, và **landing page public** phục vụ Open Graph (Facebook preview).

---

## 1. Tóm tắt nhanh

| Việc | Ai làm | Ghi chú |
|------|--------|---------|
| Dialog success + nút share | **FE** | Dùng `data.share` từ API create |
| Landing public `/c/community/[eventId]` | **FE** | Route **không** auth — bắt buộc cho Facebook OG |
| `generateMetadata` / OG tags | **FE** | Gọi API public BE (server-side) |
| Block `share` (URL, caption, social links) | **BE** | Trả sẵn trong create/detail |
| API public preview (anonymous) | **BE** | `GET /v1/public/community-cleanups/{id}` |
| Config origin share URL | **BE + FE** | Phải **khớp nhau** (xem §2) |

**Quan trọng:** URL share **không** dùng `/officer/community` — route đó yêu cầu đăng nhập, Facebook crawler chỉ thấy trang login.

---

## 2. Biến môi trường

### 2.1 FE (Next.js)

Tạo / cập nhật `.env.local` (dev) và **Vercel → Settings → Environment Variables** (staging/production).

| Biến | Public? | Môi trường | Giá trị mẫu | Mục đích |
|------|---------|------------|------------|----------|
| `NEXT_PUBLIC_API_BASE_URL` | ✅ Client + Server | Dev | `http://localhost:5089` | Gọi API khi LEO đã login (create, detail) |
| `NEXT_PUBLIC_API_BASE_URL` | ✅ | Production | `https://api.greenlens.online` | URL API production (điều chỉnh theo tunnel/domain thật) |
| `API_BASE_URL` | ❌ Server only | Dev/Prod | Cùng giá trị API trên | **`generateMetadata`** / Server Component gọi public preview — tránh lộ token |
| `NEXT_PUBLIC_WEB_BASE_URL` | ✅ | Dev | `http://localhost:3000` | Origin portal Next.js — **phải khớp BE `PublicWeb__BaseUrl`** |
| `NEXT_PUBLIC_WEB_BASE_URL` | ✅ | Production | `https://greenlens-portal.vercel.app` | Origin Vercel — **không** thêm `/officer/community` |

**Ví dụ `.env.local` (dev):**

```env
# API backend (LEO authenticated calls)
NEXT_PUBLIC_API_BASE_URL=http://localhost:5089
API_BASE_URL=http://localhost:5089

# Portal origin — MUST match BE PublicWeb__BaseUrl
NEXT_PUBLIC_WEB_BASE_URL=http://localhost:3000
```

**Ví dụ Vercel (production):**

```env
NEXT_PUBLIC_API_BASE_URL=https://api.greenlens.online
API_BASE_URL=https://api.greenlens.online
NEXT_PUBLIC_WEB_BASE_URL=https://greenlens-portal.vercel.app
```

> `NEXT_PUBLIC_*` được bundle ra browser. `API_BASE_URL` không prefix `NEXT_PUBLIC_` — chỉ dùng trong Server Component / Route Handler.

### 2.2 BE (tham chiếu — team BE cấu hình)

BE đọc từ `.env` / `.env.production` (repo `greenlens-service`):

```env
PublicWeb__BaseUrl=https://greenlens-portal.vercel.app
PublicWeb__CommunityCleanupPathTemplate=/c/community/{eventId}
```

**Rule đồng bộ:**

```
PublicWeb__BaseUrl  ===  NEXT_PUBLIC_WEB_BASE_URL
```

Nếu lệch → link trong `data.share.url` trỏ sai domain so với trang Next.js thật → Facebook OG broken.

### 2.3 URL share cuối cùng

BE ghép:

```
{PublicWeb__BaseUrl}{CommunityCleanupPathTemplate}
→ https://greenlens-portal.vercel.app/c/community/{eventId}
```

**Không** dùng:

```
❌ https://greenlens-portal.vercel.app/officer/community
❌ https://greenlens-portal.vercel.app/officer/community/{eventId}
```

`/officer/*` là khu vực LEO có middleware auth → không crawl được OG.

---

## 3. API Backend

Envelope chuẩn: `{ code, message, status, data }` — xem `00_API_CONVENTIONS.md`.

### 3.1 Tạo chương trình (LEO — có auth)

```http
POST /v1/reports/{reportId}/community-cleanups
Authorization: Bearer {accessToken}
Content-Type: application/json
Accept-Language: vi-VN
```

**Response `201` — `data` gồm toàn bộ detail + `share`:**

```json
{
  "code": "SUCCESS",
  "message": "Created",
  "status": 201,
  "data": {
    "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "title": "Dọn rác Hiệp Bình",
    "description": "Cùng dọn sạch khu phố",
    "status": "OpenForJoin",
    "startsAt": "2026-08-28T07:00:00Z",
    "thumbnailUrl": "https://pub-xxx.r2.dev/reports/thumb.jpg",
    "share": {
      "url": "https://greenlens-portal.vercel.app/c/community/f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "caption": "🌿 Dọn rác Hiệp Bình\n...\nTham gia tại: https://...",
      "imageUrl": "https://pub-xxx.r2.dev/reports/thumb.jpg",
      "facebookShareUrl": "https://www.facebook.com/sharer/sharer.php?u=...",
      "twitterShareUrl": "https://twitter.com/intent/tweet?text=...",
      "linkedInShareUrl": "https://www.linkedin.com/sharing/share-offsite/?url=...",
      "hashtags": ["GreenLens", "DonDepCongDong"]
    }
  }
}
```

Sau `201` → mở dialog success, **không cần gọi thêm API** để lấy share payload.

### 3.2 Chi tiết chương trình (auth — optional)

```http
GET /v1/community-cleanups/{eventId}
Authorization: Bearer {accessToken}
```

`data.share` cùng shape như trên (cho nút “Chia sẻ lại” trên trang detail).

### 3.3 Preview public (anonymous — OG / landing page)

```http
GET /v1/public/community-cleanups/{eventId}
Accept-Language: vi-VN
```

- **Không** cần `Authorization`
- Chương trình **Cancelled** → `404`
- Không trả PII participant

**Response `200` — `data`:**

```json
{
  "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "title": "Dọn rác Hiệp Bình",
  "description": "Cùng dọn sạch khu phố",
  "status": "OpenForJoin",
  "startsAt": "2026-08-28T07:00:00Z",
  "endsAt": null,
  "joinClosesAt": "2026-08-27T23:00:00Z",
  "maxParticipants": 40,
  "participantCount": 5,
  "spotsLeft": 35,
  "meetingNote": "Tập trung cổng chào",
  "categoryName": "Rác thải sinh hoạt",
  "reportAddress": "Phường Hiệp Bình, TP.HCM",
  "thumbnailUrl": "https://pub-xxx.r2.dev/reports/thumb.jpg",
  "share": { }
}
```

`share` object giống §3.1.

---

## 4. Route Next.js đề xuất

```
app/
├── (leo)/
│   └── officer/
│       └── community/              ← LEO dashboard (CÓ auth) — đã có
├── c/
│   └── community/
│       └── [eventId]/
│           └── page.tsx            ← Landing PUBLIC (KHÔNG auth) — CẦN TẠO
└── components/
    └── community-cleanup/
        └── CreateSuccessShareDialog.tsx
```

**Middleware:** exclude `/c/community/*` khỏi redirect login.

---

## 5. Dialog “Tạo thành công” (Client Component)

Sau `POST` thành công:

```tsx
'use client';

type SharePayload = {
  url: string;
  caption: string;
  imageUrl?: string | null;
  facebookShareUrl: string;
  twitterShareUrl: string;
  linkedInShareUrl: string;
  hashtags: string[];
};

export function CreateSuccessShareDialog({
  open,
  onClose,
  title,
  share,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  share: SharePayload;
}) {
  const copy = async (text: string) => {
    await navigator.clipboard.writeText(text);
    // toast: Đã copy
  };

  const openPopup = (url: string) => {
    window.open(url, '_blank', 'noopener,noreferrer,width=600,height=640');
  };

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogTitle>Đã tạo chương trình thành công</DialogTitle>
      <p>{title}</p>

      <Button onClick={() => openPopup(share.facebookShareUrl)}>Chia sẻ Facebook</Button>
      <Button onClick={() => openPopup(share.twitterShareUrl)}>Chia sẻ X</Button>
      <Button onClick={() => openPopup(share.linkedInShareUrl)}>LinkedIn</Button>
      <Button onClick={() => copy(share.url)}>Copy link</Button>
      <Button onClick={() => copy(share.caption)}>Copy nội dung</Button>

      {/* Instagram / Threads: không có web sharer — copy caption + tải ảnh */}
      {share.imageUrl && (
        <Button asChild>
          <a href={share.imageUrl} download target="_blank" rel="noreferrer">
            Tải ảnh (đăng Instagram thủ công)
          </a>
        </Button>
      )}

      {typeof navigator !== 'undefined' && 'share' in navigator && (
        <Button onClick={() => navigator.share({ title, text: share.caption, url: share.url })}>
          Chia sẻ khác…
        </Button>
      )}
    </Dialog>
  );
}
```

**Luồng create:**

```tsx
const res = await api.post(`/v1/reports/${reportId}/community-cleanups`, body);
if (res.status === 201) {
  setCreatedEvent(res.data.data);
  setShareDialogOpen(true);
}
```

---

## 6. Landing public + Open Graph

Facebook lấy preview từ **OG meta** trên URL share (`share.url`), không từ API trực tiếp.

```tsx
// app/c/community/[eventId]/page.tsx
import type { Metadata } from 'next';

const API = process.env.API_BASE_URL!;

async function fetchPreview(eventId: string) {
  const res = await fetch(`${API}/v1/public/community-cleanups/${eventId}`, {
    next: { revalidate: 60 },
  });
  if (!res.ok) return null;
  const json = await res.json();
  return json.data as {
    title: string;
    description?: string | null;
    thumbnailUrl?: string | null;
    share: { url: string };
  };
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ eventId: string }>;
}): Promise<Metadata> {
  const { eventId } = await params;
  const data = await fetchPreview(eventId);
  if (!data) return { title: 'Chương trình không tồn tại' };

  return {
    title: data.title,
    description: data.description ?? undefined,
    openGraph: {
      title: data.title,
      description: data.description ?? undefined,
      url: data.share.url,
      type: 'website',
      images: data.thumbnailUrl
        ? [{ url: data.thumbnailUrl, width: 1200, height: 630, alt: data.title }]
        : [],
    },
    twitter: {
      card: 'summary_large_image',
      title: data.title,
      description: data.description ?? undefined,
      images: data.thumbnailUrl ? [data.thumbnailUrl] : [],
    },
  };
}

export default async function CommunityCleanupPublicPage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  const data = await fetchPreview(eventId);
  if (!data) notFound();

  return (
    <main>
      {/* UI public: title, mô tả, thời gian, spotsLeft, CTA mở app mobile */}
      <h1>{data.title}</h1>
      {/* Deep link hoặc link store — tùy product */}
    </main>
  );
}
```

**Kiểm tra OG sau deploy:**

- [Facebook Sharing Debugger](https://developers.facebook.com/tools/debug/)
- Paste URL: `https://greenlens-portal.vercel.app/c/community/{eventId}`

---

## 7. Nền tảng mạng xã hội

| Nền tảng | Cách FE | Ghi chú |
|----------|---------|---------|
| **Facebook** | `share.facebookShareUrl` hoặc `window.open` | Preview lấy từ OG trên landing public |
| **X (Twitter)** | `share.twitterShareUrl` | Text + URL encoded sẵn |
| **LinkedIn** | `share.linkedInShareUrl` | Share by URL |
| **Copy link** | `share.url` | Clipboard |
| **Copy caption** | `share.caption` | Cho Zalo / Messenger manual |
| **Instagram** | Copy caption + tải `share.imageUrl` | **Không** có API web pre-fill post |
| **Threads** | Giống Instagram | Chưa có sharer web chính thức |
| **Web Share API** | `navigator.share({ title, text, url })` | Mobile browser |

BE **không** đăng bài tự động lên Facebook/Instagram (cần Meta App Review — ngoài scope capstone).

---

## 8. Checklist tích hợp FE

- [ ] `.env.local` / Vercel: `NEXT_PUBLIC_API_BASE_URL`, `API_BASE_URL`, `NEXT_PUBLIC_WEB_BASE_URL`
- [ ] Xác nhận BE production: `PublicWeb__BaseUrl` = `NEXT_PUBLIC_WEB_BASE_URL`
- [ ] Dialog success sau `POST` 201 — dùng `data.share`
- [ ] Route public `app/c/community/[eventId]/page.tsx` — **không** auth middleware
- [ ] `generateMetadata` gọi `GET /v1/public/community-cleanups/{id}` server-side
- [ ] `thumbnailUrl` / `share.imageUrl` là HTTPS public (R2 CDN) — Facebook từ chối localhost image trên production
- [ ] Nút Instagram/Threads = copy + download (không expect auto-post)
- [ ] Test Facebook Debugger với URL production thật
- [ ] **Không** share link `/officer/community`

---

## 9. FAQ

### Q: Production portal là `https://greenlens-portal.vercel.app/officer/community` — set env thế nào?

Chỉ set **origin**:

```env
NEXT_PUBLIC_WEB_BASE_URL=https://greenlens-portal.vercel.app
PublicWeb__BaseUrl=https://greenlens-portal.vercel.app
```

`/officer/community` là trang quản lý **sau login**, không phải URL share.

### Q: FE có tự ghép `share.url` được không?

Được (`${NEXT_PUBLIC_WEB_BASE_URL}/c/community/${id}`), nhưng **nên dùng `data.share` từ BE** để caption và social URLs luôn đồng bộ.

### Q: Citizen mobile deep link?

Landing public có thể CTA → Universal Link / custom scheme → màn Join trong app. Phần mobile ngoài scope doc này.

### Q: Lỗi Facebook không hiện ảnh?

1. URL ảnh phải public HTTPS  
2. Landing `/c/community/[id]` phải render OG server-side  
3. Chạy lại Facebook Debugger để refresh cache  

---

## 10. Liên hệ BE

| Thay đổi cần BE | Endpoint / config |
|-----------------|-------------------|
| Đổi path landing | `PublicWeb__CommunityCleanupPathTemplate` + FE route cùng path |
| Thêm field preview public | `GET /v1/public/community-cleanups/{id}` |
| Caption template | `CommunityCleanupShareBuilder` (BE) |
