# FE Guide — Facebook JavaScript SDK Share Dialog (Bổ sung)

> **Phiên bản:** 2026-08-27 · **Audience:** LEO Web (Next.js trên Vercel)  
> **Liên quan:** [`fe-leo-community-cleanup-share-guide.md`](./fe-leo-community-cleanup-share-guide.md) (doc chính — API, OG landing, env cơ bản)

Tài liệu này mô tả cách **bổ sung** [Facebook JavaScript SDK Share Dialog](https://developers.facebook.com/docs/sharing/reference/share-dialog/) cho nút “Chia sẻ Facebook” trong dialog success community cleanup.

**Mặc định hiện tại (vẫn hợp lệ production):** BE trả `share.facebookShareUrl` → FE mở `sharer.php` qua `window.open`.  
**Bổ sung tùy chọn:** dùng `FB.ui({ method: 'share', href })` với fallback về `sharer.php` khi SDK không load.

---

## 1. Sharer URL vs FB SDK Share Dialog

| | **Sharer URL** (mặc định) | **FB SDK Share Dialog** (bổ sung) |
|---|---|---|
| Meta App ID | Không cần | Cần |
| Privacy Policy | Không bắt buộc | Bắt buộc khi app **Live** |
| Load `sdk.js` | Không | Có |
| App Review | Không | Không (với `method: 'share'`) |
| Business Verification | Không | Không (chỉ share link) |
| Preview OG | Cần landing public | Cần landing public (giống nhau) |
| Callback sau share | Không | Có (hạn chế) |

**Khuyến nghị:** Primary = SDK (`share.url`), Fallback = `share.facebookShareUrl` (BE đã build sẵn).

Preview Facebook (ảnh, title) **luôn** lấy từ OG trên `share.url` — SDK không thay thế landing `/c/community/[eventId]`. Xem doc chính §6.

---

## 2. Đăng ký Meta App

### 2.1 Tạo app

1. Vào [developers.facebook.com](https://developers.facebook.com) → đăng nhập Facebook cá nhân.
2. **My Apps → Create App**.
3. Điền App details (tên: `GreenLens Portal`, email liên hệ).
4. Bước **Use cases** → filter **Others** → chọn:

   **Create an app without a use case**

   > *Get an app ID without adding any permissions, features or products.*

5. **Không chọn:** Facebook Login, Marketing API, WhatsApp, Fundraisers, ThreatExchange.
6. Bước **Business** → skip hoặc tạo Business portfolio cá nhân (không cần giấy phép DN VN).
7. Hoàn tất → lấy **App ID** tại **Settings → Basic**.

### 2.2 Cấu hình App Dashboard

**Settings → Basic:**

| Field | Giá trị |
|-------|---------|
| App Domains | `greenlens-portal.vercel.app` |
| Privacy Policy URL | `https://greenlens-portal.vercel.app/privacy` |
| Category | `Utilities` (hoặc tương đương) |

**Add Platform → Website:**

```
Site URL: https://greenlens-portal.vercel.app
```

Dev local (tuỳ chọn):

```
http://localhost:3000
```

### 2.3 Chuyển Live (production)

1. Tạo trang **`/privacy`** trên Next.js (public, HTTPS).
2. Toggle app **Development → Live**.
3. **Không cần App Review** cho Share Dialog (`FB.ui` + `method: 'share'` + `href` only).
4. **Không cần Business Verification** cho use case này.

---

## 3. Biến môi trường FE

Bổ sung vào `.env.local` (dev) và **Vercel → Environment Variables** (production):

```env
# Meta App ID — public, dùng init FB SDK
NEXT_PUBLIC_FACEBOOK_APP_ID=123456789012345
```

Các biến khác giữ nguyên theo doc chính:

```env
NEXT_PUBLIC_API_BASE_URL=https://api.greenlens.online
API_BASE_URL=https://api.greenlens.online
NEXT_PUBLIC_WEB_BASE_URL=https://greenlens-portal.vercel.app
```

**Không** đặt **App Secret** trên FE. Share Dialog không cần secret.

**Rule đồng bộ (không đổi):**

```
PublicWeb__BaseUrl (BE)  ===  NEXT_PUBLIC_WEB_BASE_URL (FE)
share.url                ===  https://greenlens-portal.vercel.app/c/community/{eventId}
```

---

## 4. Cấu trúc file đề xuất (Next.js App Router)

```
app/
├── layout.tsx                          ← mount <FacebookSdk />
├── privacy/
│   └── page.tsx                        ← bắt buộc trước khi Meta Live
└── c/community/[eventId]/page.tsx      ← landing OG (doc chính §6)

components/
└── facebook/
    └── FacebookSdk.tsx

lib/share/
    └── facebookShareDialog.ts
```

---

## 5. Load Facebook SDK

```tsx
// components/facebook/FacebookSdk.tsx
'use client';

import { useEffect } from 'react';

declare global {
  interface Window {
    FB?: {
      init: (params: { appId: string; version: string; xfbml?: boolean }) => void;
      ui: (
        params: { method: 'share'; href: string },
        callback?: (response: { error_message?: string }) => void
      ) => void;
    };
    fbAsyncInit?: () => void;
  }
}

export function FacebookSdk() {
  const appId = process.env.NEXT_PUBLIC_FACEBOOK_APP_ID;
  if (!appId) return null;

  useEffect(() => {
    if (document.getElementById('facebook-jssdk')) return;

    window.fbAsyncInit = () => {
      window.FB?.init({
        appId,
        version: 'v21.0', // cập nhật theo doc Meta mới nhất
        xfbml: false,
      });
    };

    const script = document.createElement('script');
    script.id = 'facebook-jssdk';
    script.src = 'https://connect.facebook.net/vi_VN/sdk.js';
    script.async = true;
    script.defer = true;
    script.crossOrigin = 'anonymous';
    document.body.appendChild(script);
  }, [appId]);

  return null;
}
```

Mount trong root layout:

```tsx
// app/layout.tsx
import { FacebookSdk } from '@/components/facebook/FacebookSdk';

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="vi">
      <body>
        <FacebookSdk />
        {children}
      </body>
    </html>
  );
}
```

Nếu chưa có `NEXT_PUBLIC_FACEBOOK_APP_ID` → component return `null` → FE tự fallback `sharer.php`.

---

## 6. Helper Share Dialog + fallback

```tsx
// lib/share/facebookShareDialog.ts

type SharePayload = {
  url: string;
  facebookShareUrl: string;
};

/** Mở popup sharer.php — fallback khi SDK không sẵn sàng. */
export function openFacebookSharerFallback(facebookShareUrl: string) {
  window.open(
    facebookShareUrl,
    '_blank',
    'noopener,noreferrer,width=600,height=640'
  );
}

/**
 * Primary: FB SDK Share Dialog (href = share.url).
 * Fallback: share.facebookShareUrl từ BE (sharer.php).
 */
export function openFacebookShare(share: SharePayload) {
  const href = share.url;

  if (typeof window === 'undefined' || !window.FB) {
    openFacebookSharerFallback(share.facebookShareUrl);
    return;
  }

  window.FB.ui({ method: 'share', href }, (response) => {
    if (response?.error_message) {
      console.warn('[FB Share Dialog]', response.error_message);
      openFacebookSharerFallback(share.facebookShareUrl);
    }
  });
}
```

**Quan trọng:** `href` phải là `share.url` (landing public), **không** phải `/officer/community`.

---

## 7. Tích hợp vào dialog success

Thay nút Facebook trong `CreateSuccessShareDialog` (doc chính §5):

```tsx
import { openFacebookShare } from '@/lib/share/facebookShareDialog';

// Trước (sharer.php only):
// <Button onClick={() => openPopup(share.facebookShareUrl)}>Chia sẻ Facebook</Button>

// Sau (SDK + fallback):
<Button onClick={() => openFacebookShare(share)}>
  Chia sẻ Facebook
</Button>
```

Type `SharePayload` từ API (doc chính §3.1):

```ts
type SharePayload = {
  url: string;
  caption: string;
  imageUrl?: string | null;
  facebookShareUrl: string;
  twitterShareUrl: string;
  linkedInShareUrl: string;
  hashtags: string[];
};
```

Các nút X / LinkedIn / Copy **giữ nguyên** — chỉ Facebook đổi sang SDK.

---

## 8. Trang `/privacy` — hướng dẫn chi tiết

Meta **bắt buộc** Privacy Policy URL trước khi chuyển app **Live**. Trang phải **public**, **HTTPS**, **không** yêu cầu đăng nhập.

```
https://greenlens-portal.vercel.app/privacy
```

Doc này gồm: yêu cầu Meta · cấu trúc nội dung · template copy-paste · code Next.js · cấu hình dashboard.

---

### 8.1 Vì sao cần trang này?

| Ai yêu cầu | Lý do |
|------------|--------|
| **Meta (Facebook)** | App Live + JavaScript SDK |
| **Người dùng / demo capstone** | Minh bạch dữ liệu khi LEO share link công khai |
| **Best practice** | Portal có auth (JWT) — nên có policy riêng cho web |

**Không cần `/privacy`** nếu FE **chỉ** dùng `sharer.php` (không FB SDK, app Meta ở Development). Có SDK + Live → **bắt buộc**.

---

### 8.2 Yêu cầu kỹ thuật (FE)

| Yêu cầu | Chi tiết |
|---------|----------|
| Route | `app/privacy/page.tsx` → URL `/privacy` |
| Auth | **Không** redirect login — exclude khỏi middleware LEO |
| HTTPS | Deploy Vercel production |
| Ngôn ngữ | Tiếng Việt (có thể thêm EN sau) |
| Index | `robots` tuỳ chọn; Meta chỉ cần URL trả `200` + nội dung đọc được |
| Cập nhật | Ghi **Ngày cập nhật** ở đầu trang |

**Middleware Next.js** — đảm bảo public:

```tsx
// middleware.ts — ví dụ matcher exclude
export const config = {
  matcher: ['/((?!privacy|c/community|_next|favicon.ico).*)'],
};
```

Hoặc whitelist rõ `/privacy` trong logic auth hiện có.

---

### 8.3 Cấu trúc nội dung đề xuất

Viết theo thứ tự sau (Meta không bắt buộc đủ từng mục, nhưng capstone + GDPR-lite nên có):

1. **Giới thiệu** — GreenLens Portal là gì, ai vận hành  
2. **Phạm vi** — áp dụng cho portal web LEO (`greenlens-portal.vercel.app`), không nhầm với app mobile Citizen (nếu có policy riêng)  
3. **Dữ liệu thu thập** — email, họ tên, JWT session, log kỹ thuật  
4. **Facebook / Meta** — Share Dialog only, **không** Facebook Login, **không** thu data FB  
5. **Chia sẻ link công khai** — landing `/c/community/[id]` hiển thị gì (title, mô tả, ảnh — không PII participant)  
6. **Mục đích sử dụng** — quản lý báo cáo, chương trình dọn cộng đồng  
7. **Lưu trữ & bảo mật** — HTTPS, JWT, backend API  
8. **Quyền của người dùng** — xem/sửa/xóa tài khoản (theo BR-AUTH-022 nếu có)  
9. **Xóa dữ liệu** — anchor `#data-deletion` cho Meta **User data deletion**  
10. **Liên hệ** — email team  
11. **Thay đổi policy** — có thể cập nhật, ghi ngày hiệu lực  

---

### 8.4 Template nội dung (copy → chỉnh placeholder)

Thay các placeholder trước khi publish:

| Placeholder | Thay bằng |
|-------------|-----------|
| `[NGÀY_CẬP_NHẬT]` | VD: `27/08/2026` |
| `[EMAIL_LIÊN_HỆ]` | VD: `hieutran4525@gmail.com` hoặc email team |
| `[TÊN_NHÓM/TRƯỜNG]` | VD: `SU26SE049 - FPT University` |

---

#### Nội dung Markdown (dùng trong page hoặc CMS)

```markdown
# Chính sách quyền riêng tư — GreenLens Portal

**Cập nhật lần cuối:** [NGÀY_CẬP_NHẬT]

## 1. Giới thiệu

GreenLens Portal (“Chúng tôi”, “Portal”) là cổng web dành cho cán bộ môi trường địa phương (LEO) trong hệ sinh thái ứng dụng **GreenLens** — nền tảng báo cáo và theo dõi ô nhiễm môi trường (dự án [TÊN_NHÓM/TRƯỜNG]).

Chính sách này mô tả cách chúng tôi xử lý thông tin khi bạn truy cập Portal tại:

https://greenlens-portal.vercel.app

## 2. Phạm vi áp dụng

- Áp dụng cho **GreenLens Portal (web LEO)**.
- **Không** thay thế chính sách của ứng dụng di động GreenLens dành cho công dân (nếu được công bố riêng).
- Trang công khai chia sẻ chương trình dọn dẹp (`/c/community/...`) nằm trên cùng domain; mục 5 mô tả dữ liệu hiển thị trên các trang đó.

## 3. Thông tin chúng tôi thu thập

### 3.1 Thông tin bạn cung cấp

- Họ tên, email, số điện thoại (khi đăng ký / quản trị tài khoản LEO).
- Nội dung nghiệp vụ: báo cáo môi trường, chương trình dọn dẹp cộng đồng, ghi chú công việc.

### 3.2 Thông tin thu thập tự động

- Token phiên đăng nhập (JWT) lưu phía trình duyệt theo cơ chế bảo mật của ứng dụng.
- Nhật ký kỹ thuật: thời gian truy cập, loại trình duyệt, mã lỗi (không cố ý ghi mật khẩu).

### 3.3 Thông tin từ Meta (Facebook)

GreenLens Portal **có thể** tích hợp **Facebook JavaScript SDK Share Dialog** để bạn **chủ động** chia sẻ liên kết công khai lên Facebook.

- Chúng tôi **không** sử dụng **Facebook Login** — Portal **không** yêu cầu bạn đăng nhập GreenLens bằng tài khoản Facebook.
- Chúng tôi **không** nhận hoặc lưu trữ dữ liệu cá nhân Facebook (tên FB, friend list, email FB, v.v.) từ SDK.
- Việc đăng bài lên Facebook do **bạn** xác nhận trong giao diện của Facebook; chúng tôi **không** đăng bài tự động thay bạn.

Meta có thể thu thập dữ liệu riêng theo [Chính sách dữ liệu của Meta](https://www.facebook.com/privacy/policy/). Chúng tôi không kiểm soát cách Meta xử lý dữ liệu trên nền tảng của họ.

## 4. Mục đích sử dụng

- Xác thực và phân quyền tài khoản LEO.
- Quản lý vòng đời báo cáo ô nhiễm và chương trình dọn dẹp cộng đồng.
- Gửi thông báo nghiệp vụ (nếu bạn bật).
- Cải thiện độ ổn định và bảo mật hệ thống.

Chúng tôi **không** bán dữ liệu cá nhân cho bên thứ ba.

## 5. Trang công khai và chia sẻ mạng xã hội

Khi LEO chia sẻ chương trình dọn dẹp, người xem link (kể cả qua Facebook) có thể thấy trang công khai, ví dụ:

https://greenlens-portal.vercel.app/c/community/{eventId}

Trang này có thể hiển thị:

- Tiêu đề và mô tả chương trình.
- Thời gian, địa điểm tổng quát (cấp phường/quận).
- Ảnh minh họa (thumbnail).
- Số lượng người tham gia (số đếm, **không** danh sách tên công khai).

Chúng tôi **không** công bố email, số điện thoại hay danh sách participant trên trang công khai này.

## 6. Chia sẻ với bên thứ ba

Chúng tôi có thể sử dụng:

| Bên | Mục đích |
|-----|----------|
| **Vercel** | Hosting Portal |
| **Meta** | SDK Share Dialog (tuỳ chọn trên giao diện) |
| **Nhà cung cấp hạ tầng** (API, lưu trữ ảnh) | Vận hành backend GreenLens |

Các bên này chỉ xử lý dữ liệu trong phạm vi cung cấp dịch vụ cho chúng tôi.

## 7. Lưu trữ và bảo mật

- Kết nối HTTPS giữa trình duyệt và Portal.
- Mật khẩu được băm phía server (bcrypt); chúng tôi không lưu mật khẩu dạng văn bản thuần.
- Phiên đăng nhập có thời hạn; refresh token được bảo vệ theo thiết kế backend.

Không có biện pháp bảo mật nào an toàn tuyệt đối 100%; chúng tôi nỗ lực giảm thiểu rủi ro hợp lý.

## 8. Thời gian lưu giữ

- Dữ liệu tài khoản: trong thời gian bạn sử dụng dịch vụ và theo quy định nội bộ / yêu cầu pháp luật.
- Tài khoản xóa mềm: có thể được xóa vĩnh viễn sau thời gian grace period theo chính sách hệ thống GreenLens.
- Log kỹ thuật: lưu trong thời hạn hạn chế phục vụ vận hành và audit.

## 9. Quyền của bạn

Tùy vai trò và quy định nội bộ, bạn có thể:

- Yêu cầu truy cập / chỉnh sửa thông tin tài khoản.
- Yêu cầu xóa tài khoản qua quy trình trong ứng dụng hoặc liên hệ email bên dưới.
- Ngừng sử dụng Portal và thu hồi cookie/token bằng cách đăng xuất.

## 10. Xóa dữ liệu {#data-deletion}

Nếu bạn muốn **xóa dữ liệu tài khoản LEO** hoặc yêu cầu xóa thông tin liên quan:

1. Gửi email tới **[EMAIL_LIÊN_HỆ]** với tiêu đề: `Yêu cầu xóa dữ liệu GreenLens Portal`.
2. Ghi rõ email đăng ký tài khoản và mô tả yêu cầu.
3. Chúng tôi phản hồi trong vòng **30 ngày làm việc** (dự án học thuật / capstone).

**Dữ liệu Facebook:** Vì chúng tôi không lưu dữ liệu Facebook qua SDK, mọi bài đăng bạn tự chia sẻ trên Facebook do bạn quản lý trực tiếp trên tài khoản Facebook của mình.

## 11. Trẻ em

Portal dành cho cán bộ / người dùng đủ năng lực hành vi dân sự theo quy định pháp luật Việt Nam; không hướng tới trẻ em dưới 16 tuổi.

## 12. Thay đổi chính sách

Chúng tôi có thể cập nhật chính sách này. Phiên bản mới có ngày “Cập nhật lần cuối” ở đầu trang. Việc tiếp tục sử dụng Portal sau khi cập nhật được hiểu là bạn đã biết đến thay đổi.

## 13. Liên hệ

- **Email:** [EMAIL_LIÊN_HỆ]
- **Dự án:** GreenLens — SU26SE049
- **Portal:** https://greenlens-portal.vercel.app
```

---

### 8.5 Triển khai Next.js (App Router)

**Cách đơn giản** — một page tĩnh, không cần API:

```tsx
// app/privacy/page.tsx
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Chính sách quyền riêng tư — GreenLens Portal',
  description: 'Chính sách quyền riêng tư của GreenLens Portal dành cho cán bộ môi trường địa phương.',
  robots: { index: true, follow: true },
};

export default function PrivacyPage() {
  return (
    <main className="mx-auto max-w-3xl px-4 py-10 prose prose-neutral">
      <h1>Chính sách quyền riêng tư — GreenLens Portal</h1>
      <p>
        <strong>Cập nhật lần cuối:</strong> 27/08/2026
      </p>

      {/* Paste các section từ §8.4 — hoặc tách component / MDX */}
      <section id="data-deletion">
        <h2>10. Xóa dữ liệu</h2>
        <p>
          Gửi email tới{' '}
          <a href="mailto:hieutran4525@gmail.com">hieutran4525@gmail.com</a>{' '}
          với tiêu đề &quot;Yêu cầu xóa dữ liệu GreenLens Portal&quot;.
        </p>
      </section>

      {/* ... các section còn lại ... */}
    </main>
  );
}
```

**Tuỳ chọn nâng cao:**

- `content/privacy.vi.md` + `react-markdown` — dễ chỉnh nội dung không đụng JSX.
- Link footer toàn site: `<Link href="/privacy">Chính sách quyền riêng tư</Link>`.

**Kiểm tra sau deploy:**

```bash
curl -I https://greenlens-portal.vercel.app/privacy
# Expect: HTTP/2 200
```

Mở tab ẩn danh — **không** bị redirect login.

---

### 8.6 Cấu hình Meta App Dashboard (sau khi deploy `/privacy`)

**Settings → Basic:**

| Field | Giá trị |
|-------|---------|
| Privacy Policy URL | `https://greenlens-portal.vercel.app/privacy` |
| App domains | `greenlens-portal.vercel.app` *(không thêm `localhost`)* |
| Category | Utilities hoặc Education |

**User data deletion:**

- Chọn **Data deletion instructions URL**
- URL: `https://greenlens-portal.vercel.app/privacy#data-deletion`

**Terms of Service URL** (tuỳ chọn):

- Để trống, hoặc
- `https://greenlens-portal.vercel.app/terms` nếu team viết thêm trang Terms

**Không** dùng placeholder `https://www.facebook.com/` — Meta có thể từ chối hoặc gây hiểu nhầm khi review.

Sau khi Save → toggle **Development → Live**.

---

### 8.7 Checklist `/privacy` trước Live

- [ ] Deploy Vercel — URL `/privacy` trả **200**
- [ ] Tab ẩn danh — không redirect login
- [ ] Có mục **Facebook / Meta** (Share Dialog, không Login)
- [ ] Có anchor **`#data-deletion`** + hướng dẫn email
- [ ] Email liên hệ thật, team monitor được
- [ ] Meta **Privacy Policy URL** trỏ đúng
- [ ] Meta **User data deletion** → `#data-deletion`
- [ ] (Tuỳ chọn) Link `/privacy` ở footer portal

---


## 9. OG landing — vẫn bắt buộc

SDK Share Dialog **không** gửi title/ảnh trực tiếp lên Facebook. Crawler vẫn đọc OG tại `share.url`.

FE phải hoàn thành (doc chính §6):

- [ ] Route public `app/c/community/[eventId]/page.tsx`
- [ ] Middleware **exclude** `/c/community/*`
- [ ] `generateMetadata` → `GET /v1/public/community-cleanups/{id}`
- [ ] `og:image` = HTTPS public (R2 CDN)

Test sau deploy: [Facebook Sharing Debugger](https://developers.facebook.com/tools/debug/)

---

## 10. Checklist tích hợp SDK

### Meta Developer

- [ ] App tạo với **Create an app without a use case**
- [ ] App Domains + Website platform = `greenlens-portal.vercel.app`
- [ ] Privacy Policy URL live
- [ ] App chuyển **Live**

### Next.js

- [ ] `NEXT_PUBLIC_FACEBOOK_APP_ID` trên Vercel
- [ ] `<FacebookSdk />` trong layout
- [ ] `openFacebookShare()` với fallback `share.facebookShareUrl`
- [ ] Trang `/privacy` public

### End-to-end

- [ ] `POST` create → dialog dùng `data.share`
- [ ] Bấm Facebook → popup Share Dialog (hoặc fallback sharer)
- [ ] Preview đúng trên Sharing Debugger với URL production
- [ ] Test khi tắt adblock và khi SDK bị chặn (fallback hoạt động)

---

## 11. FAQ

### Q: Có bắt buộc dùng SDK không?

Không. `sharer.php` qua `share.facebookShareUrl` vẫn production-ready. SDK là **tùy chọn** nâng UX.

### Q: Có cần Facebook Login không?

Không. Share Dialog dùng session Facebook của user trên browser, không cần login vào GreenLens qua Meta.

### Q: BE có cần sửa không?

Không bắt buộc. BE đã trả `share.url` và `share.facebookShareUrl`. FE chỉ đổi cách gọi nút Facebook.

### Q: Callback `FB.ui` có biết user đã đăng bài không?

Không đáng tin cậy. Chỉ dùng log/debug — không dùng cho business logic (award points, v.v.).

### Q: SDK bị chặn (adblock)?

Luôn giữ fallback `openFacebookSharerFallback(share.facebookShareUrl)`.

### Q: Dev local có test Share Dialog được không?

Có với `localhost:3000` nếu đã thêm vào Website platform. OG preview trên localhost thường **không** có ảnh — test OG trên Vercel staging/production.

---

## 12. Tham chiếu

| Tài liệu | Link |
|----------|------|
| Doc share chính (API, env, OG) | [`fe-leo-community-cleanup-share-guide.md`](./fe-leo-community-cleanup-share-guide.md) |
| Meta Share Dialog (Web) | https://developers.facebook.com/docs/sharing/reference/share-dialog/ |
| Meta FB.ui reference | https://developers.facebook.com/docs/javascript/reference/FB.ui/ |
| Facebook Sharing Debugger | https://developers.facebook.com/tools/debug/ |
