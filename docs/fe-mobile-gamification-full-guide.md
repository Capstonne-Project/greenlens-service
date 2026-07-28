# Mobile Gamification — Full Implementation Guide

> **Ngày:** 2026-07-26  
> **Đối tượng:** Mobile Citizen (`green-lens-app`)  
> **BR:** BR-GAM-001 … BR-GAM-006  
> **BE sẵn có:** `GET /v1/gamification/my-points`, `my-badges`, `leaderboard`  
> **Mục tiêu:** Mobile làm **full UX gamification** (điểm / level / badge / bảng xếp hạng / lịch sử) dù hiện gần như chưa có màn nào.

---

## 0. Tóm tắt 30 giây

Citizen **không “đổi điểm” thủ công**. BE tự cộng/trừ khi báo cáo đổi trạng thái → lưu `UserPoints` + `PointTransaction` → tính Level → check Badge → (sau này) push `LevelUp` / `BadgeEarned`.

Mobile chỉ cần:

1. **Hiển thị** điểm / level / progress / badge / leaderboard / lịch sử giao dịch.
2. **Refresh** sau khi report status đổi (hoặc khi mở tab Profile / Gamification).
3. **Deep-link** từ notification `BadgeEarned` / `LevelUp` → màn Gamification (hiện đang trỏ Profile — nên đổi).

---

## 1. Luồng tính toán phía BE (Mobile cần hiểu để UI đúng)

```
Report status change (Verified / Resolved / Rejected / Duplicate merge)
        │
        ▼
Domain event (ReportVerifiedEvent, …)
        │
        ▼
ReportPointsHandlers / DuplicateMergedHandlers
  → đọc GamificationConfig (admin có thể đổi số điểm)
  → AwardPointsCommand
        │
        ▼
UserPoints.AwardPoints(points, reason, reportId)
  • Nếu đang lock (fraud) → bỏ qua
  • Idempotent: (reportId + reason) chỉ 1 lần
  • TotalPoints += points (sàn ≥ 0)
  • Nếu Level tăng → raise LevelUpEvent
        │
        ▼
CheckBadgesCommand
  → so điều kiện → insert UserBadge (nếu đủ)
```

**Mobile không gọi API “cộng điểm”.** Chỉ đọc kết quả.

---

## 2. Công thức điểm (BR-GAM-001)

Giá trị mặc định (seed `GamificationConfig`). Admin có thể đổi qua web — Mobile **không hardcode số** trong UI “cách kiếm điểm”; nên lấy từ copy tĩnh bên dưới hoặc config public nếu sau này BE expose.

| Sự kiện | Điểm mặc định | `PointReason` | Khi nào xảy ra |
|--------|:-------------:|---------------|----------------|
| LEO xác minh báo cáo | **+10** | `ReportVerified` | Submitted → Verified |
| Báo cáo được giải quyết | **+20** | `ReportResolved` | → Resolved |
| Biên bản phạt | **+20** | `PenaltyIssued` | ⚠️ Config có, **BE chưa award** |
| Báo cáo trùng (merge) | **~+5** | `DuplicateReport` | = `round(ReportVerified × 0.5)` |
| Báo cáo bị từ chối | **−5** | `ReportRejected` | → Rejected |
| Gian lận (Admin lock) | **−ALL** | `FraudPenalty` | Admin khóa gamification |

### Quy tắc quan trọng cho UI

| Rule | Ý nghĩa Mobile |
|------|----------------|
| Idempotent `(reportId, reason)` | Không toast “+10” hai lần nếu user pull-to-refresh |
| Floor `TotalPoints ≥ 0` | Không hiển thị điểm âm tổng |
| Locked | `isLocked=true` → banner “Tài khoản đang bị khóa gamification đến …” + ẩn celebration |
| Anonymous report | BR-GAM-002: vẫn nhận điểm; ẩn tên trên leaderboard — **BE chưa mask tên** (hiện luôn `FullName`) |

### Copy VI cho lịch sử giao dịch

```ts
const POINT_REASON_LABEL: Record<string, string> = {
  ReportVerified: 'Báo cáo được xác minh',
  ReportResolved: 'Báo cáo đã xử lý xong',
  PenaltyIssued: 'Biên bản phạt được lập',
  DuplicateReport: 'Báo cáo trùng (hỗ trợ cộng đồng)',
  ReportRejected: 'Báo cáo bị từ chối',
  FraudPenalty: 'Phạt gian lận',
};
```

---

## 3. Level L1–L5 (BR-GAM-003)

Level **không lưu DB** — tính từ `totalPoints`:

| Level | Tổng điểm | Gợi ý tên UI |
|:-----:|-----------|--------------|
| L1 | 0 – 99 | Người mới |
| L2 | 100 – 499 | Người đóng góp |
| L3 | 500 – 1.499 | Chiến binh xanh |
| L4 | 1.500 – 4.999 | Chuyên gia |
| L5 | ≥ 5.000 | Huyền thoại |

### Progress bar (Mobile tự tính)

```ts
const LEVEL_THRESHOLDS = [0, 100, 500, 1500, 5000] as const; // index 0 = L1 floor

function getLevelProgress(totalPoints: number) {
  const level =
    totalPoints >= 5000 ? 5 :
    totalPoints >= 1500 ? 4 :
    totalPoints >= 500 ? 3 :
    totalPoints >= 100 ? 2 : 1;

  if (level >= 5) {
    return { level, current: totalPoints, next: null, ratio: 1, pointsToNext: 0 };
  }

  const floor = LEVEL_THRESHOLDS[level - 1];
  const next = LEVEL_THRESHOLDS[level];
  const ratio = Math.min(1, (totalPoints - floor) / (next - floor));
  return {
    level,
    current: totalPoints,
    next,
    ratio,
    pointsToNext: next - totalPoints,
  };
}
```

UI: `Còn {pointsToNext} điểm để lên Level {level + 1}`.

---

## 4. Badges — 12 cái (BR-GAM-004)

### 4.1 Catalog (seed BE) — Mobile nên hardcode catalog để hiện “chưa mở khóa”

API hiện tại **`GET my-badges` chỉ trả badge đã nhận**. Không có catalog API.

→ Mobile giữ catalog local (theo bảng dưới), merge với API:

- Có trong API → `earned: true`, hiện `awardedAt`
- Không có → `earned: false`, UI khóa / xám + điều kiện

| Code | NameVi | Điều kiện | Auto-award BE hôm nay |
|------|--------|-----------|:---------------------:|
| `first_report` | Người Khởi Đầu | ≥ 1 báo cáo (không Submitted/Rejected) | ✅ |
| `eco_warrior` | Chiến Binh Xanh | ≥ 10 báo cáo | ✅ |
| `green_champion` | Nhà Vô Địch Xanh | ≥ 50 báo cáo | ✅ |
| `earth_guardian` | Người Bảo Vệ Trái Đất | ≥ 100 báo cáo | ✅ |
| `streak_7d` | Bền Bỉ 7 Ngày | 7 ngày gửi liên tiếp | ❌ Coming soon |
| `streak_30d` | Kiên Trì 30 Ngày | 30 ngày liên tiếp | ❌ Coming soon |
| `hotspot_hunter` | Thợ Săn Điểm Nóng | 3 báo cáo trong hotspot | ❌ Coming soon |
| `duplicate_finder` | Người Phát Hiện Trùng | 5 duplicate | ❌ Coming soon |
| `community_voice` | Tiếng Nói Cộng Đồng | report ≥ 10 confirmations | ❌ Coming soon |
| `rising_star` | Ngôi Sao Đang Lên | ≥ 100 điểm | ✅ |
| `eco_expert` | Chuyên Gia Môi Trường | ≥ 1.500 điểm | ✅ |
| `green_legend` | Huyền Thoại Xanh | ≥ 5.000 điểm | ✅ |

**Icon:** BE trả `iconUrl` dạng relative `badges/icons/{code}.png`. Preview local BE: `docs/UserBadge/icons/`. Production cần CDN/S3 — Mobile có thể bundle asset theo `code` làm fallback:

```
assets/badges/{code}.png
```

### 4.2 Nhóm UI gợi ý

| Group | Codes | Section title |
|-------|-------|---------------|
| Cột mốc | first_report … earth_guardian | Cột mốc báo cáo |
| Liên tục | streak_7d, streak_30d | Chuỗi ngày |
| Cộng đồng | hotspot_hunter, duplicate_finder, community_voice | Cộng đồng |
| Cấp độ | rising_star, eco_expert, green_legend | Theo điểm |

Badge `coming soon`: vẫn hiện trong grid, badge “Sắp ra mắt”, không dùng progress giả.

---

## 5. Leaderboard (BR-GAM-005)

| Period query | Enum | Khoảng thời gian (UTC) |
|--------------|------|-------------------------|
| `Weekly` | `LeaderboardPeriod.Weekly` | Đầu tuần → cuối tuần (theo `DayOfWeek`) |
| `Monthly` | default | Ngày 1 tháng → đầu tháng sau |
| `Yearly` | | 1/1 → 1/1 năm sau |

- Điểm trên bảng = **tổng giao dịch trong kỳ** (không phải lifetime).
- Level trên row = tính từ **lifetime** `totalPoints`.
- Chỉ user **không lock** và `periodPoints > 0`.
- `top` mặc định 10 (Mobile có thể gửi `top=20` nếu UI cần).

**Gap:** Không có “hạng của tôi” / nearby ranks. Workaround Mobile:

1. Gọi leaderboard `top=50`.
2. Tìm `userId === me` → hiện rank.
3. Không thấy → label “Chưa có trong Top 50 kỳ này” + vẫn hiện `totalPoints` lifetime từ `my-points`.

---

## 6. API contract (Mobile dùng ngay)

Base: `{API_BASE}/v1/gamification`  
Envelope: `{ code, message, status, data }`

### 6.1 `GET /v1/gamification/my-points`

**Auth:** Bearer (Citizen)

| Query | Type | Default |
|-------|------|---------|
| `page` | int | 1 |
| `pageSize` | int | 20 (max nên ≤ 100) |

**Response `data`:**

```json
{
  "totalPoints": 150,
  "level": 2,
  "isLocked": false,
  "lockedUntil": null,
  "recentTransactions": [
    {
      "id": "uuid",
      "points": 10,
      "reason": "ReportVerified",
      "reportId": "uuid-or-null",
      "createdAt": "2026-07-26T05:00:00Z"
    }
  ],
  "totalTransactions": 15
}
```

User chưa có record gamification → BE trả `totalPoints: 0, level: 1, transactions: []` (không 404).

### 6.2 `GET /v1/gamification/my-badges`

**Auth:** Bearer

```json
[
  {
    "badgeId": "uuid",
    "code": "first_report",
    "nameVi": "Người Khởi Đầu",
    "nameEn": "First Reporter",
    "description": "Gửi báo cáo ô nhiễm đầu tiên được xác minh",
    "iconUrl": "badges/icons/first_report.png",
    "awardedAt": "2026-07-20T10:00:00Z"
  }
]
```

### 6.3 `GET /v1/gamification/leaderboard`

**Auth:** Anonymous OK (Citizen cũng gọi được)

| Query | Type | Default |
|-------|------|---------|
| `period` | `Weekly` \| `Monthly` \| `Yearly` | `Monthly` |
| `top` | int | 10 |

```json
{
  "period": "Monthly",
  "periodStart": "2026-07-01T00:00:00Z",
  "periodEnd": "2026-08-01T00:00:00Z",
  "entries": [
    {
      "rank": 1,
      "userId": "uuid",
      "displayName": "Nguyễn Văn A",
      "avatarUrl": "https://...",
      "points": 85,
      "level": 3
    }
  ]
}
```

### 6.4 Không dùng trên Citizen Mobile

| Endpoint | Ai |
|----------|-----|
| `POST /v1/gamification/{userId}/lock` | Admin only |
| `GET/PUT /v1/admin/gamification-configs` | Admin web |

---

## 7. TypeScript types (đề xuất Mobile)

```ts
// src/types/gamification.types.ts

export type PointReason =
  | 'ReportVerified'
  | 'ReportResolved'
  | 'PenaltyIssued'
  | 'DuplicateReport'
  | 'ReportRejected'
  | 'FraudPenalty';

export type LeaderboardPeriod = 'Weekly' | 'Monthly' | 'Yearly';

export interface PointTransactionItem {
  id: string;
  points: number;
  reason: PointReason;
  reportId: string | null;
  createdAt: string;
}

export interface MyPointsResponse {
  totalPoints: number;
  level: number;
  isLocked: boolean;
  lockedUntil: string | null;
  recentTransactions: PointTransactionItem[];
  totalTransactions: number;
}

export interface BadgeItem {
  badgeId: string;
  code: string;
  nameVi: string;
  nameEn: string;
  description: string | null;
  iconUrl: string | null;
  awardedAt: string;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  points: number; // period points
  level: number;  // lifetime level
}

export interface LeaderboardResponse {
  period: LeaderboardPeriod;
  periodStart: string;
  periodEnd: string;
  entries: LeaderboardEntry[];
}

/** Catalog row (local) + merge API */
export interface BadgeCatalogItem {
  code: string;
  nameVi: string;
  nameEn: string;
  description: string;
  group: 'milestone' | 'streak' | 'community' | 'level';
  comingSoon: boolean;
  requiredReportCount?: number;
  requiredPoints?: number;
  earned: boolean;
  awardedAt?: string;
  iconUrl?: string | null;
}
```

---

## 8. Màn hình Mobile cần làm (full)

Mobile hiện gần như chưa có UI gamification (chỉ deep-link `BadgeEarned`/`LevelUp` → Profile). Đề xuất ship theo 4 màn + entry points.

### 8.1 Entry points

| Chỗ | Hành vi |
|-----|---------|
| Tab **Profile** | Card tóm tắt: Level · Điểm · số badge · CTA “Xem chi tiết” |
| Tab mới hoặc stack **Gamification** (khuyến nghị) | Hub đầy đủ |
| Notification `BadgeEarned` / `LevelUp` | Deep-link → Hub (đổi khỏi Profile) |
| Sau submit report / status đổi | Optional toast “Có thể đã nhận điểm — xem Gamification” + invalidate query |

Deep-link hiện tại (`resolve-notification-href.ts`):

```ts
case 'BadgeEarned':
case 'LevelUp':
  return '/(tabs)/profile'; // → đổi thành '/(tabs)/gamification' hoặc '/gamification'
```

### 8.2 Screen A — Gamification Hub (`/(tabs)/gamification` hoặc stack)

**Layout (1 scroll):**

1. **Header card**
   - Avatar + Level badge (`L{n}`)
   - `totalPoints` lớn
   - Progress bar tới level tiếp
   - Nếu `isLocked` → banner đỏ + `lockedUntil` (format local time)
2. **Quick stats**
   - Số badge đã nhận / 12
   - Hạng kỳ này (nếu tìm thấy trên leaderboard)
3. **CTA row**
   - Huy hiệu → Screen B
   - Bảng xếp hạng → Screen C
   - Lịch sử điểm → Screen D (hoặc section ngay dưới)
4. **Recent activity** (5 giao dịch đầu từ `my-points`)

**Data:**

```
useQuery my-points page=1 pageSize=5
useQuery my-badges
useQuery leaderboard period=Monthly top=50  // để tìm rank của mình
```

### 8.3 Screen B — Badges

- Segment / section theo 4 nhóm (§4.2)
- Grid 3 cột: icon + tên; locked = grayscale + lock overlay
- Tap → bottom sheet: mô tả + điều kiện + ngày nhận (nếu có)
- Chip “Sắp ra mắt” cho 5 badge chưa auto-award

**Data:** catalog local ∪ `my-badges`

### 8.4 Screen C — Leaderboard

- Segmented control: Tuần / Tháng / Năm
- List: rank · avatar · name · period points · level chip
- Highlight row của mình (background nhẹ)
- Empty: “Chưa có ai trên bảng xếp hạng kỳ này”
- Pull-to-refresh

**Data:** `GET leaderboard?period=&top=20`

### 8.5 Screen D — Points history

- Infinite / paginated list từ `my-points` (`page`, `pageSize`)
- Row: `±points` (màu xanh/đỏ) · label reason · ngày · optional link `reportId` → report detail
- Header sticky: tổng điểm hiện tại

### 8.6 Optional polish (P1)

| Feature | Cách làm không cần BE mới |
|---------|---------------------------|
| Celebration modal level-up | So sánh `level` trước/sau khi refetch `my-points` (AsyncStorage lastLevel) |
| Celebration badge mới | Diff `my-badges` codes vs cache trước đó |
| “Cách kiếm điểm” info sheet | Static copy từ bảng §2 |
| Profile mini widget | Chỉ `totalPoints` + `level` + badge count |

---

## 9. Service / hooks (đề xuất cấu trúc Mobile)

```
src/
  services/gamification.service.ts   # axios/fetch wrappers
  hooks/useMyPoints.ts
  hooks/useMyBadges.ts
  hooks/useLeaderboard.ts
  data/badge-catalog.ts              # 12 badges static
  utils/gamification.ts              # level progress, reason labels
  components/gamification/
    PointsHeaderCard.tsx
    LevelProgressBar.tsx
    BadgeGrid.tsx
    BadgeDetailSheet.tsx
    LeaderboardList.tsx
    PointsHistoryList.tsx
    LockedBanner.tsx
  app/(tabs)/gamification.tsx        # hoặc stack route
  app/gamification/
    badges.tsx
    leaderboard.tsx
    history.tsx
```

### Example service

```ts
// gamification.service.ts
export const gamificationApi = {
  getMyPoints: (page = 1, pageSize = 20) =>
    api.get<ApiEnvelope<MyPointsResponse>>('/v1/gamification/my-points', {
      params: { page, pageSize },
    }),

  getMyBadges: () =>
    api.get<ApiEnvelope<BadgeItem[]>>('/v1/gamification/my-badges'),

  getLeaderboard: (period: LeaderboardPeriod = 'Monthly', top = 20) =>
    api.get<ApiEnvelope<LeaderboardResponse>>('/v1/gamification/leaderboard', {
      params: { period, top },
    }),
};
```

### Invalidate khi nào

| Event | Action |
|-------|--------|
| App foreground / focus Hub | refetch |
| Notification `BadgeEarned` / `LevelUp` | invalidate points + badges |
| Report detail thấy status Verified/Resolved/Rejected/Duplicate | invalidate points (+ badges) |
| Pull-to-refresh | refetch all |

---

## 10. Notifications (Mobile đã partial)

| Type | Template BE | Handler gửi push | Deep-link Mobile hiện tại |
|------|:-----------:|:----------------:|---------------------------|
| `BadgeEarned` | ✅ seeded | ❌ chưa wire | → Profile |
| `LevelUp` | ✅ seeded | ❌ chưa wire | → Profile |

**Mobile vẫn ship full UI** bằng:

1. Poll / refetch khi mở Hub.
2. Local celebration (§8.6) khi diff cache.
3. Khi BE wire push sau này: chỉ đổi deep-link → Gamification Hub, không đổi layout.

---

## 11. Community Cleanup points (chưa có code)

Spec `docs/community-cleanup-feature-spec.md` §9 **đề xuất** (chưa implement):

| Hành động | Điểm đề xuất |
|-----------|:------------:|
| Join event | +2 |
| Check-in hợp lệ | +5 |
| Event completed (đã check-in) | +10 |
| Leader completed | +20 |
| Report Resolved (qua community) | +20 (cùng `ReportResolved`) |

Badge gợi ý: Community Helper, Cleanup Leader, Weekend Warrior — **chưa seed**.

→ Mobile **không** làm UI điểm community cleanup cho đến khi BE có `PointReason` mới / API. Chỉ giữ chỗ trong “Cách kiếm điểm” nếu product muốn tease.

---

## 12. Gap BE vs Mobile full UX

| Gap | Ảnh hưởng Mobile | Workaround ship ngay |
|-----|------------------|----------------------|
| Không có badge catalog API | Không biết badge chưa nhận | Catalog local §4 |
| Không có progress-to-next-level API | — | Tự tính §3 |
| Không có my-rank API | Khó hiện hạng | Scan leaderboard top=50 |
| Icon URL relative | Ảnh vỡ | Bundle `assets/badges/{code}.png` |
| BadgeEarned / LevelUp push chưa gửi | User không được báo realtime | Diff cache + refetch |
| 5 badge special chưa award | User không bao giờ nhận | UI “Sắp ra mắt” |
| PenaltyIssued chưa award | Lý thuyết +20 không xảy ra | Đừng promise trong copy “Cách kiếm điểm” nếu product chưa confirm |
| Anonymous ẩn tên leaderboard | Privacy BR-GAM-002 | Chờ BE; Mobile hiển thị `displayName` như trả về |
| Leaderboard snapshot job không persist | — | Không ảnh hưởng GET live |

### API BE nên bổ sung sau (không block Mobile P0)

1. `GET /v1/gamification/badges` — full catalog + `earned` + progress fields  
2. `GET /v1/gamification/my-rank?period=` — rank + pointsInPeriod + nearby  
3. Wire notification handlers LevelUp / BadgeEarned  
4. Absolute `iconUrl` (CDN)

---

## 13. Acceptance checklist (Mobile full)

### P0 — Ship được demo Citizen

- [ ] Profile có card điểm + level + số badge
- [ ] Màn Hub: điểm, progress level, lock banner, recent 5 tx
- [ ] Màn Badges: 12 ô (earned + locked + coming soon)
- [ ] Màn Leaderboard: Weekly / Monthly / Yearly + highlight mình
- [ ] Màn History: phân trang `my-points`
- [ ] Types + service + hooks theo §7–9
- [ ] Deep-link `BadgeEarned` / `LevelUp` → Hub
- [ ] Pull-to-refresh trên Hub / Leaderboard
- [ ] Empty states (0 điểm, 0 badge, leaderboard trống)
- [ ] Không crash khi `iconUrl` null / relative

### P1 — Polish

- [ ] Celebration modal level-up / badge mới (local diff)
- [ ] Info sheet “Cách kiếm điểm”
- [ ] Tap transaction → report detail (nếu có `reportId`)
- [ ] i18n `nameVi` / `nameEn` theo `Accept-Language` / app locale

### Ngoài scope Mobile (BE)

- [ ] Push LevelUp / BadgeEarned
- [ ] Catalog / my-rank endpoints
- [ ] Streak / hotspot badges logic
- [ ] Community cleanup points

---

## 14. Mapping BR

| BR | Nội dung | Mobile |
|----|----------|--------|
| BR-GAM-001 | Điểm theo sự kiện report | Hiển thị lịch sử + copy |
| BR-GAM-002 | Ẩn danh trên leaderboard | Hiện `displayName` BE trả |
| BR-GAM-003 | Level L1–L5 | Progress bar tự tính |
| BR-GAM-004 | 12 badges | Catalog + my-badges |
| BR-GAM-005 | Leaderboard W/M/Y | Screen C |
| BR-GAM-006 | Fraud lock | LockedBanner |

---

## 15. Tài liệu liên quan

| File | Nội dung |
|------|----------|
| `docs/Gamification/gamification-module.md` | Module BE (một phần badge list cũ — ưu tiên seed 12 badge trong doc này) |
| `docs/UserBadge/badge_seed_proposal.md` | Chi tiết 12 badge |
| `docs/community-cleanup-feature-spec.md` §9 | Điểm community (đề xuất) |
| `src/Greenlens.Api/Controllers/GamificationController.cs` | Routes |
| `src/Greenlens.Domain/Entities/UserPoints.cs` | Level + award + lock |

---

**Kết luận:** BE đã đủ 3 API đọc để Mobile làm **full trang gamification** (Hub + Badges + Leaderboard + History). Phần thiếu chủ yếu là catalog/rank/push — xử lý bằng catalog local + refetch; không cần chờ BE mới để ship P0.
)
