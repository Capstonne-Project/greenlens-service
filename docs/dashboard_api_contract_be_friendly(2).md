# Dashboard API Contract

## Base Response
```json
{
  "success": true,
  "message": "Success",
  "data": {}
}
```

# A. ADMIN DASHBOARD

## 1. Dashboard Overview

**Endpoint**
```http
GET /v1/dashboard/admin/overview
```

**Description**
Returns overview KPIs.

**Query Parameters**
- `from`
- `to`

**Response**
```json
{
  "totalUsers":12540,
  "totalReports":8245,
  "pendingReports":152,
  "resolvedReports":7680,
  "activeCompanies":24,
  "activeTeams":86,
  "slaComplianceRate":97.8,
  "averageResolutionHours":13.6
}
```

## 2. Report Status Distribution

**Endpoint**
```http
GET /v1/dashboard/admin/report-status
```

**Description**
Distribution by status.

**Query Parameters**
- `from`
- `to`

**Response**
```json
[
 {"status":"Submitted","count":120,"percentage":12.5},
 {"status":"Verified","count":220,"percentage":22.9}
]
```

## 3. Report Trend

**Endpoint**
```http
GET /v1/dashboard/admin/report-trend
```

**Description**
Trend created/resolved.

**Query Parameters**
- `groupBy`
- `from`
- `to`

**Response**
```json
[{"date":"2026-07-01","created":20,"resolved":18}]
```

## 4. Pollution Analytics

**Endpoint**
```http
GET /v1/dashboard/admin/pollution-analytics
```

**Description**
Category counts.

**Query Parameters**
- `from`
- `to`

**Response**
```json
[{"category":"Trash","count":120}]
```

## 5. Geographic Heatmap

**Endpoint**
```http
GET /v1/dashboard/admin/geographic
```

**Description**
Heatmap and markers.

**Query Parameters**
- `from`
- `to`

**Response**
```json
{"heatmap":[{"latitude":10.7,"longitude":106.6,"weight":5}],"markers":[{"reportId":"uuid","latitude":10.7,"longitude":106.6,"status":"Pending"}]}
```

## 6. Report Funnel

**Endpoint**
```http
GET /v1/dashboard/admin/report-funnel
```

**Description**
Lifecycle counts.

**Query Parameters**
- `from`
- `to`

**Response**
```json
[{"stage":"Submitted","count":100}]
```

## 7. Company Performance

**Endpoint**
```http
GET /v1/dashboard/admin/company-performance
```

**Description**
Company KPIs.

**Query Parameters**
- `from`
- `to`

**Response**
```json
[{"companyId":"uuid","companyName":"Green Co","assignedTasks":50,"completedTasks":45,"onTimeRate":95,"slaRate":97,"performanceScore":94}]
```

## 8. Officer Performance

**Endpoint**
```http
GET /v1/dashboard/admin/officer-performance
```

**Description**
Officer KPIs.

**Query Parameters**
- `from`
- `to`

**Response**
```json
[{"officerId":"uuid","officerName":"John","verifiedReports":45,"averageHours":4.5,"slaRate":98,"score":96}]
```

## 9. Queue Aging

**Endpoint**
```http
GET /v1/dashboard/admin/queue-aging
```

**Description**
Queue age.

**Query Parameters**
- None

**Response**
```json
[{"range":"0-6h","count":20}]
```

## 10. Resolution Distribution

**Endpoint**
```http
GET /v1/dashboard/admin/resolution-distribution
```

**Description**
Resolution histogram.

**Query Parameters**
- `from`
- `to`

**Response**
```json
[{"range":"<2h","count":30}]
```

## 11. Recent Activities

**Endpoint**
```http
GET /v1/dashboard/admin/recent-activities
```

**Description**
Recent events.

**Query Parameters**
- `page`
- `pageSize`

**Response**
```json
[{"time":"2026-07-24T10:00:00Z","type":"OfficerVerified","description":"Officer verified report #123"}]
```

## 12. Alerts

**Endpoint**
```http
GET /v1/dashboard/admin/alerts
```

**Description**
System alerts.

**Query Parameters**
- None

**Response**
```json
[{"type":"SLA_BREACH","severity":"High","message":"12 reports exceeded SLA"}]
```

# B. COMPANY DASHBOARD

## 1. Overview

**Endpoint**
```http
GET /v1/dashboard/company/overview
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

## 2. Workload Trend

**Endpoint**
```http
GET /v1/dashboard/company/workload-trend
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

## 3. Task Status

**Endpoint**
```http
GET /v1/dashboard/company/task-status
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

## 4. Team Performance

**Endpoint**
```http
GET /v1/dashboard/company/team-performance
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

## 5. Staff Performance

**Endpoint**
```http
GET /v1/dashboard/company/staff-performance
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

## 6. Queue Aging

**Endpoint**
```http
GET /v1/dashboard/company/queue-aging
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

## 7. Recent Activities

**Endpoint**
```http
GET /v1/dashboard/company/recent-activities
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

## 8. Upcoming Deadlines

**Endpoint**
```http
GET /v1/dashboard/company/upcoming-deadlines
```

**Query Parameters**
- from (optional)
- to (optional)

**Sample Response**
```json
{"success":true,"message":"Success","data":[]}
```

