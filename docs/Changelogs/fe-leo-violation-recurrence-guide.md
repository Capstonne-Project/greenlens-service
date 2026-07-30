# FE Guide — LEO Violation Recurrence (BR-REP-034)

> **Audience:** LEO / DEO Web App  
> **Backend branch:** `develop`  
> **Related:** Duplicate flag (BR-REP-030) — có thể cùng true trên một báo cáo

## Khi nào cờ bật?

Citizen submit báo cáo mới → BE so sánh với báo cáo **Closed** trong **30 ngày**, **cùng category**, **≤ 50m** → chọn Closed **mới nhất**.

## API

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/v1/reports/{id}` | Trả `isSuspectedViolationRecurrence`, `suspectedRecurrenceOfReportId`, `priorClosedReport` |
| GET | `/v1/reports/{id}/violation-recurrence-comparison` | So sánh side-by-side current vs prior |
| POST | `/v1/reports/{id}/dismiss-violation-recurrence` | LEO bác cờ (không cần lý do) |
| GET | `/v1/reports/officer-queue?isSuspectedViolationRecurrence=true` | Lọc queue |

## UI gợi ý

1. Badge **"Nghi tái phát"** trên queue/detail (tách badge **"Nghi trùng"** của duplicate).
2. Nút **So sánh** → gọi comparison API, hiển thị 2 cột ảnh/mô tả/timeline.
3. Nút **Bác cờ** → POST dismiss (mirror dismiss duplicate).
4. Cờ **không bắt buộc** tạo InspectionReport — LEO vẫn có thể tạo inspection thủ công.

## Notification

LEO/DEO nhận push/email `ViolationRecurrenceReviewNeeded` khi cờ được gắn lúc submit.

## Submit response (Citizen)

`POST /v1/reports` response thêm:

```json
{
  "isSuspectedViolationRecurrence": true,
  "suspectedRecurrenceOfReportId": "uuid-prior-closed"
}
```

Citizen **không** cần hiển thị cờ này.
