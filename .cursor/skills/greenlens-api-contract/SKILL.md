---
name: greenlens-api-contract
description: >
  Generates API contract documentation for a GreenLens endpoint following 00_API_CONVENTIONS.md.
  Produces request/response schemas, error code catalog, Swagger annotations, and a Postman entry
  with the standard envelope {code, message, status, data}. Use when designing a new endpoint or
  documenting an existing one. Triggers: "API contract", "endpoint spec", "Swagger annotation",
  "Postman entry", "document endpoint".
---

# GreenLens — API Contract

## Inputs

| Field | Example |
|-------|---------|
| Method + path | `POST /v1/pollution-reports` |
| Auth policy | `Policies.CanSubmitReport` |
| Request shape | see template |
| Success response | shape + status code |
| Error codes | list of business codes |
| BR IDs | `BR-REP-001..013, BR-REP-030` |
| Rate limit | `5/h, 20/24h per Citizen (BR-REP-010)` |

## Contract template

```markdown
# `POST /v1/pollution-reports`

## Auth
- **Required:** Yes (Bearer JWT)
- **Policy:** `Policies.CanSubmitReport` (Citizen, Officer, Admin)

## Headers
| Name | Required | Notes |
|------|----------|-------|
| Authorization | Yes | `Bearer {accessToken}` |
| Content-Type | Yes | `application/json` |
| Accept-Language | No | `vi-VN` (default) or `en-US` |
| X-Request-ID | No | UUID; server generates if absent |

## Request
```json
{
  "type": "TRASH",
  "latitude": 10.7626,
  "longitude": 106.6602,
  "mediaIds": ["uuid-1", "uuid-2"],
  "description": "Đống rác lớn cạnh chân cầu"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| type | enum | Yes | `TRASH` \| `WASTEWATER` \| `CHEMICAL` \| `OTHER` |
| latitude | number | Yes | 8.0–24.0 (BR-REP-003) |
| longitude | number | Yes | 102.0–110.0 (BR-REP-003) |
| mediaIds | array | Yes | 1–5 items (BR-REP-001/002) |
| description | string | No | ≤ 2000 chars |

## Success — `201 Created`

```json
{
  "code": "SUCCESS",
  "message": "Báo cáo đã được gửi",
  "status": 201,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

## Errors

| HTTP | code | When |
|------|------|------|
| 401 | UNAUTHORIZED | Missing/invalid token |
| 422 | VALIDATION_ERROR | Field validation failed |
| 422 | INVALID_GPS | Lat/Lng out of Vietnam (BR-REP-003) |
| 422 | TOO_MANY_IMAGES | > 5 mediaIds (BR-REP-002) |
| 409 | DUPLICATE_REPORT | Within 50m + same type + 24h (BR-REP-030) |
| 429 | RATE_LIMIT_EXCEEDED | > 5/h or > 20/24h (BR-REP-010) |
| 503 | AI_UNAVAILABLE | AI service down (BR-AI-006 fallback) |

### Validation error response

```json
{
  "code": "VALIDATION_ERROR",
  "message": "Dữ liệu không hợp lệ",
  "status": 422,
  "data": {
    "errors": [
      { "field": "latitude", "code": "INVALID_GPS", "message": "Latitude must be within Vietnam (8.0 - 24.0)." }
    ]
  }
}
```

## Rate limit headers (always present)
```
X-RateLimit-Limit: 5
X-RateLimit-Remaining: 4
X-RateLimit-Reset: 1715234600
Retry-After: 1800        // only on 429
```

## BR coverage
| BR ID | Where enforced |
|-------|---------------|
| BR-REP-001 | `SubmitReportCommandValidator` |
| BR-REP-003 | `SubmitReportCommandValidator` + `GeoLocation.Create` (defense-in-depth) |
| BR-REP-010 | `SubmitReportCommandHandler` (rate limiter) |
| BR-REP-013 | `Report.Create` (initial state Submitted) |
| BR-REP-030 | `SubmitReportCommandHandler` (deduplicator) |
```

## Swagger annotation snippet

```csharp
/// <summary>Submit a new pollution report.</summary>
/// <remarks>
/// Rate limited: 5/h and 20/24h per Citizen (BR-REP-010).
/// </remarks>
[HttpPost]
[Authorize(Policy = Policies.CanSubmitReport)]
[ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<ValidationErrors>), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
public async Task<IActionResult> SubmitAsync(
    [FromBody] SubmitReportCommand cmd,
    CancellationToken ct)
    => (await sender.Send(cmd, ct)).ToHttp(StatusCodes.Status201Created);
```

## Postman entry

```json
{
  "name": "Submit Pollution Report",
  "request": {
    "method": "POST",
    "url": "{{baseUrl}}/v1/pollution-reports",
    "header": [
      { "key": "Authorization", "value": "Bearer {{accessToken}}" },
      { "key": "Content-Type", "value": "application/json" },
      { "key": "Accept-Language", "value": "vi-VN" }
    ],
    "body": {
      "mode": "raw",
      "raw": "{\n  \"type\": \"TRASH\",\n  \"latitude\": 10.7626,\n  \"longitude\": 106.6602,\n  \"mediaIds\": [\"{{mediaId}}\"],\n  \"description\": \"Test\"\n}"
    }
  }
}
```

## Definition of Done (00_API_CONVENTIONS.md §12)

- [ ] Request/response match this spec 100%
- [ ] Response envelope `{code, message, status, data}` correct
- [ ] All error codes mapped
- [ ] Field-level validation errors
- [ ] Auth policy enforced
- [ ] Rate limit + headers
- [ ] Audit log for sensitive action
- [ ] Swagger annotation complete
- [ ] Postman collection updated
- [ ] BR IDs in commit message
