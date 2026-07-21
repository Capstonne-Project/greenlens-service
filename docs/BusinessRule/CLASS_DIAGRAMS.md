# GreenLens — Class Diagrams

> **Dự án:** SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution  
> **Tổng quan:** 8 Class Diagram theo Module / Bounded Context, dựa trên mã nguồn thực tế.  
> **Ký hiệu:** Mermaid UML. Xem bằng bất kỳ Markdown renderer nào hỗ trợ Mermaid (GitHub, VS Code, v.v.)

---

## Chú giải 4 loại Relationship (UML)

```mermaid
classDiagram
    class ParentClass {
        <<Composition>>
    }
    class ChildClass {
        <<Composition>>
    }
    class WholeClass {
        <<Aggregation>>
    }
    class PartClass {
        <<Aggregation>>
    }
    class BaseClass {
        <<Inheritance>>
    }
    class DerivedClass {
        <<Inheritance>>
    }
    class ClassA {
        <<Association>>
    }
    class ClassB {
        <<Association>>
    }

    ParentClass *-- ChildClass : ◆ Composition\nChild không tồn tại\nnếu Parent bị xóa
    WholeClass o-- PartClass : ◇ Aggregation\nPart tồn tại độc lập\nnếu Whole bị xóa
    BaseClass <|-- DerivedClass : △ Inheritance\nDerivedClass kế thừa\ntừ BaseClass
    ClassA --> ClassB : → Association\nTham chiếu đơn giản\ngiữa 2 class
```

| Ký hiệu | Tên | Mermaid syntax | Ý nghĩa | Ví dụ GreenLens |
|----------|-----|----------------|----------|-----------------|
| ◆ (filled diamond) | **Composition** | `A *-- B` | B **không thể tồn tại** nếu A bị xóa | `Report *-- ReportMedia` — xóa Report → xóa luôn Media |
| ◇ (empty diamond) | **Aggregation** | `A o-- B` | B **vẫn tồn tại** độc lập khi A bị xóa | `LocalOffice o-- EnvironmentalTeam` — team có thể transfer sang office khác |
| △ (triangle) | **Inheritance** | `A <\|-- B` | B kế thừa từ A | `AuditableEntity <\|-- User` |
| → (arrow) | **Association** | `A --> B` | A tham chiếu đến B (lookup, FK) | `Report --> User` — Report tham chiếu reporter |

---

## Hệ thống phân cấp Entity Base (chung cho tất cả module)

```mermaid
classDiagram
    class BaseEntity {
        <<abstract>>
        + Id: Guid
        + DomainEvents: IReadOnlyCollection~IDomainEvent~
        + AddDomainEvent(event: IDomainEvent): void
        + ClearDomainEvents(): void
    }

    class AuditableEntity {
        <<abstract>>
        + CreatedAt: DateTime
        + CreatedBy: string?
        + UpdatedAt: DateTime?
        + UpdatedBy: string?
    }

    class SoftDeletableEntity {
        <<abstract>>
        + DeletedAt: DateTime?
        + DeletedBy: string?
        + IsDeleted: bool
        + SoftDelete(deletedBy: string?): void
        + Restore(): void
    }

    BaseEntity <|-- AuditableEntity : Inheritance
    AuditableEntity <|-- SoftDeletableEntity : Inheritance
```

---

## CD-01: User & Authentication Module

**Mô tả:** Quản lý vòng đời tài khoản, xác thực JWT, OTP, social login, lockout, ban.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| User → RefreshToken | **Composition** ◆ | Token không có ý nghĩa nếu User bị xóa |
| User → OtpCode | **Composition** ◆ | OTP gắn chặt với User, xóa User → xóa OTP |
| User → PasswordHistory | **Composition** ◆ | Lịch sử mật khẩu thuộc về User |
| SoftDeletableEntity → User | **Inheritance** △ | User kế thừa SoftDeletableEntity |

```mermaid
classDiagram
    class User {
        <<Aggregate Root>>
        + Email: string
        + PasswordHash: string
        + FullName: string
        + PhoneNumber: string?
        + AvatarUrl: string?
        + Role: UserRole
        + IsEmailVerified: bool
        + IsPhoneVerified: bool
        + MustChangePassword: bool
        + FailedLoginAttempts: int
        + LockoutEnd: DateTime?
        + GoogleId: string?
        + IsBanned: bool
        + HasDataConsent: bool
        + ConsentAcceptedAt: DateTime?
        + FcmDeviceToken: string?
        + Language: string
        + CommentViolationCount: int
        + CommentBannedUntil: DateTime?
        + DepartmentId: Guid?
        + LocalOfficeId: Guid?
        + Create(email, passwordHash, fullName, role): User$
        + CreateByAdmin(email, passwordHash, fullName, role): User$
        + CreateWithTempPassword(email, passwordHash, fullName, role): User$
        + CreateFromGoogle(email, fullName, googleId, avatarUrl?): User$
        + RecordFailedLogin(): void
        + ResetFailedLoginAttempts(): void
        + IsLockedOut(): bool
        + RequiresCaptcha(): bool
        + VerifyEmail(): void
        + ChangePassword(newPasswordHash: string): void
        + Ban(): void
        + Unban(): void
        + AcceptDataConsent(): void
        + RecordCommentViolation(): void
        + IsCommentBanned(): bool
        + AssignToDepartment(departmentId: Guid): void
        + AssignToLocalOffice(localOfficeId: Guid): void
        + ChangeRole(newRole: UserRole): void
    }

    class RefreshToken {
        + UserId: Guid
        + TokenHash: string
        + ExpiresAt: DateTime
        + CreatedAt: DateTime
        + IsRevoked: bool
        + RevokedAt: DateTime?
        + ReplacedByTokenHash: string?
        + IsExpired: bool
        + IsActive: bool
        + Create(userId, tokenHash, expirationDays): RefreshToken$
        + Revoke(replacedByTokenHash?): void
    }

    class OtpCode {
        + Email: string
        + PhoneNumber: string?
        + CodeHash: string
        + Purpose: OtpPurpose
        + ExpiresAt: DateTime
        + CreatedAt: DateTime
        + IsUsed: bool
        + AttemptCount: int
        + IsExpired: bool
        + IsValid: bool
        + Create(email, codeHash, purpose, lifetime): OtpCode$
        + CreateForPhone(phone, codeHash, lifetime): OtpCode$
        + IncrementAttempt(): void
        + MarkUsed(): void
    }

    class PasswordHistory {
        + UserId: Guid
        + PasswordHash: string
        + CreatedAt: DateTime
        + Create(userId, passwordHash): PasswordHistory$
    }

    class UserRole {
        <<enumeration>>
        Citizen
        DEO
        LEO
        Cleaner
        CompanyManager
        CompanyStaff
        Inspector
        Admin
    }

    class OtpPurpose {
        <<enumeration>>
        EmailVerification
        PasswordReset
        PhoneVerification
    }

    SoftDeletableEntity <|-- User : Inheritance
    User "1" *-- "*" RefreshToken : Composition
    User "1" *-- "*" OtpCode : Composition
    User "1" *-- "*" PasswordHistory : Composition
    User --> UserRole : Association
```

---

## CD-02: Report (Pollution Report) Module

**Mô tả:** Vòng đời báo cáo ô nhiễm — state machine, media, assignment, flag, satisfaction, duplicate detection, AI analysis.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| Report → ReportMedia | **Composition** ◆ | Media thuộc về Report, xóa Report → xóa Media |
| Report → ReportAssignment | **Composition** ◆ | Assignment không tồn tại nếu Report bị xóa |
| Report → ReportStatusHistory | **Composition** ◆ | Lịch sử trạng thái gắn chặt với Report |
| Report → ReportFlag | **Composition** ◆ | Flag chỉ có ý nghĩa trong context Report |
| Report → ReportSatisfaction | **Composition** ◆ | Đánh giá gắn chặt với Report |
| Report → ReportWasteTag | **Composition** ◆ | Join table, xóa Report → xóa liên kết |
| Report → ReportDraft | **Composition** ◆ | Draft thuộc về Report |
| Report → User | **Association** → | Report tham chiếu reporter, User tồn tại độc lập |
| Report → PollutionCategory | **Association** → | Report lookup danh mục, Category tồn tại độc lập |
| Report → Report | **Association** → | Self-reference: duplicate → parent |
| ReportAssignment → EnvironmentalTeam | **Association** → | Assignment tham chiếu Team, Team tồn tại độc lập |

```mermaid
classDiagram
    class Report {
        <<Aggregate Root>>
        + Code: string
        + ReporterId: Guid?
        + HideReporterName: bool
        + CategoryId: Guid
        + Severity: Severity
        + SeveritySetBy: SeveritySource
        + Description: string?
        + Latitude: decimal
        + Longitude: decimal
        + Address: string?
        + WardCode: string?
        + ProvinceCode: string?
        + Status: ReportStatus
        + AssignedOfficeId: Guid?
        + AssignedDepartmentId: Guid?
        + VerifiedBy: Guid?
        + AssignedByOfficerId: Guid?
        + AssignedCompanyId: Guid?
        + ParentReportId: Guid?
        + ReporterCount: int
        + IsPossibleDuplicate: bool
        + AiSimilarityScore: decimal?
        + IsSuspicious: bool
        + AiPending: bool
        + AiClassifiedType: string?
        + AiConfidence: decimal?
        + AiEstimatedSeverity: Severity?
        + PriorityScore: decimal
        + VerifiedAt: DateTime?
        + RejectedReason: string?
        + ResolvedAt: DateTime?
        + ClosedAt: DateTime?
        + ReopenedCount: int
        + SlaVerifyBreached: bool
        + SlaResolveBreached: bool
        + IsOverdue: bool
        + IsHidden: bool
        + Create(...): Report$
        + Verify(leoId: Guid, severity?: Severity, categoryId?: Guid): void
        + Reject(reason: string): void
        + Assign(leoId: Guid): void
        + DispatchToCompany(companyId: Guid, leoId: Guid): void
        + AssignByCompanyManager(cmId: Guid): void
        + Resolve(): void
        + Close(): void
        + TryReopen(): bool
        + MarkDuplicate(primaryReportId: Guid): void
        + MarkPossibleDuplicate(...): void
        + DismissDuplicate(): void
        + ApplyAiResults(...): void
        + FlagSuspicious(reasons: string): void
        + ForceStatus(newStatus: ReportStatus): void
        + Hide(adminId: Guid, reason: string): void
        + Unhide(): void
        + CanDelete(): bool
    }

    class ReportMedia {
        + ReportId: Guid
        + Type: MediaType
        + Url: string
        + ThumbnailUrl: string?
        + MimeType: string
        + SizeBytes: long
        + Width: int?
        + Height: int?
        + DurationSeconds: int?
        + PHash: string?
        + ExifData: string?
        + UploadedBy: Guid?
        + UploadedAt: DateTime
        + Create(...): ReportMedia$
        + SetThumbnail(url: string): void
        + SetPHash(pHash: string): void
        + SetDimensions(w: int, h: int): void
        + ChangeType(newType: MediaType): void
        + ReassignToReport(primaryId: Guid): void
    }

    class ReportAssignment {
        + ReportId: Guid
        + TeamId: Guid
        + AssignedById: Guid
        + Status: AssignmentStatus
        + Note: string?
        + DeclineReason: string?
        + AssignedAt: DateTime
        + StartedAt: DateTime?
        + CompletedAt: DateTime?
        + CheckedInAt: DateTime?
        + CheckedInLatitude: decimal?
        + CheckedInLongitude: decimal?
        + ProgressPercent: int
        + ProgressNote: string?
        + Create(...): ReportAssignment$
        + Accept(): void
        + CheckIn(lat: decimal, lng: decimal, note?: string): void
        + Escalate(reason: string): void
        + Complete(): void
        + Decline(reason: string): void
        + ForceDecline(reason: string): void
        + UpdateProgress(percent: int, note: string, userId: Guid): void
    }

    class ReportStatusHistory {
        + ReportId: Guid
        + FromStatus: ReportStatus
        + ToStatus: ReportStatus
        + Reason: string?
        + ChangedBy: Guid?
        + ChangedAt: DateTime
    }

    class ReportDraft {
        + UserId: Guid
        + JsonPayload: string?
        + CreatedAt: DateTime
    }

    class ReportFlag {
        + ReportId: Guid
        + FlaggedBy: Guid
        + Type: FlagType
        + Reason: string?
        + CreatedAt: DateTime
    }

    class ReportSatisfaction {
        + ReportId: Guid
        + UserId: Guid
        + Rating: int
        + Comment: string?
        + CreatedAt: DateTime
    }

    class ReportWasteTag {
        <<join table>>
        + ReportId: Guid
        + WasteTagId: Guid
    }

    class ReportStatus {
        <<enumeration>>
        Submitted
        Verified
        InProgress
        Resolved
        Closed
        Rejected
        Duplicate
    }

    class AssignmentStatus {
        <<enumeration>>
        Assigned
        InProgress
        Completed
        Declined
        Escalated
    }

    class Severity {
        <<enumeration>>
        Low
        Medium
        High
        Critical
    }

    class MediaType {
        <<enumeration>>
        Image
        Video
        Before
        After
        Progress
    }

    SoftDeletableEntity <|-- Report : Inheritance
    SoftDeletableEntity <|-- ReportMedia : Inheritance
    SoftDeletableEntity <|-- ReportAssignment : Inheritance

    Report "1" *-- "1..*" ReportMedia : Composition
    Report "1" *-- "0..*" ReportAssignment : Composition
    Report "1" *-- "0..*" ReportStatusHistory : Composition
    Report "1" *-- "0..1" ReportDraft : Composition
    Report "1" *-- "0..*" ReportFlag : Composition
    Report "1" *-- "0..1" ReportSatisfaction : Composition
    Report "1" *-- "0..*" ReportWasteTag : Composition

    Report "0..*" --> "0..1" User : Association (reporter)
    Report "0..*" --> "1" PollutionCategory : Association (category)
    Report "0..1" --> "0..1" Report : Association (parentReport)
    ReportAssignment "0..*" --> "1" EnvironmentalTeam : Association (team)
    Report --> ReportStatus : Association
    ReportAssignment --> AssignmentStatus : Association
    ReportMedia --> MediaType : Association
    Report --> Severity : Association
```

### State Machine — Report Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Submitted : Citizen submit

    Submitted --> Verified : LEO verify
    Submitted --> Rejected : LEO reject (reason ≥ 20 chars)
    Submitted --> Duplicate : LEO/AI confirm duplicate

    Verified --> InProgress : LEO/CM assign team
    Verified --> Duplicate : LEO/AI confirm duplicate

    InProgress --> Resolved : Team complete cleanup
    InProgress --> Verified : Company deactivated (revert)

    Resolved --> Closed : Citizen confirm OR Auto-close 7d
    Resolved --> InProgress : Citizen reopen (max 2x)

    Closed --> [*]
    Rejected --> [*]
    Duplicate --> [*]
```

---

## CD-03: Organization Module

**Mô tả:** Cơ cấu tổ chức hành chính (Tỉnh → Phòng ban → Văn phòng phường), Công ty dịch vụ, đội dọn dẹp/thanh tra.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| Department → LocalOffice | **Composition** ◆ | LocalOffice thuộc về Department, không tồn tại độc lập |
| Company → CompanyStaff | **Composition** ◆ | Staff thuộc về Company, xóa Company → xóa Staff |
| Company → CompanyServiceArea | **Composition** ◆ | Vùng phục vụ gắn chặt với Company |
| Company → ContractPeriod | **Composition** ◆ | Lịch sử hợp đồng thuộc về Company |
| Company → StaffInvitation | **Composition** ◆ | Lời mời thuộc về Company |
| LocalOffice → EnvironmentalTeam | **Aggregation** ◇ | Team có thể transfer sang office khác |
| EnvironmentalTeam → TeamMember | **Aggregation** ◇ | Member (User) tồn tại độc lập, có thể chuyển team |
| Company → EnvironmentalTeam | **Aggregation** ◇ | Company team, team có thể tách ra |
| LocalOffice → User (Officer) | **Association** → | Office tham chiếu LEO, User tồn tại độc lập |
| TeamMember → User | **Association** → | Join table tham chiếu User |
| Company → Department | **Association** → | Company tham chiếu Department |

```mermaid
classDiagram
    class Department {
        + Name: string
        + ProvinceCode: string
        + IsActive: bool
        + Create(name: string, provinceCode: string): Department$
        + Update(name: string): void
        + Deactivate(): void
        + Activate(): void
    }

    class LocalOffice {
        + Name: string
        + DepartmentId: Guid
        + WardCode: string
        + OfficerId: Guid?
        + IsOnboarded: bool
        + Create(name, departmentId, wardCode, officerId?): LocalOffice$
        + AssignOfficer(officerId: Guid): void
        + RemoveOfficer(): void
        + Update(name: string): void
        + Deactivate(): void
    }

    class EnvironmentalTeam {
        + Name: string
        + LocalOfficeId: Guid?
        + TeamType: TeamType
        + IsActive: bool
        + CompanyId: Guid?
        + IsCompanyTeam: bool
        + Create(name, localOfficeId, teamType): EnvironmentalTeam$
        + CreateCompanyTeam(name, teamType, companyId): EnvironmentalTeam$
        + Update(name: string): void
        + Deactivate(): void
        + Activate(): void
        + TransferToOffice(newOfficeId: Guid): void
    }

    class TeamMember {
        <<join table>>
        + TeamId: Guid
        + UserId: Guid
        + IsLeader: bool
    }

    class EnvironmentalServiceCompany {
        <<Aggregate Root>>
        + Name: string
        + TaxCode: string?
        + Address: string?
        + Phone: string?
        + Email: string?
        + ContractNumber: string
        + ContractStartDate: DateTime
        + ContractEndDate: DateTime?
        + ContractType: ContractType
        + Status: CompanyStatus
        + ActivatedAt: DateTime?
        + DepartmentId: Guid
        + Create(...): EnvironmentalServiceCompany$
        + Activate(): void
        + Suspend(reason: string): void
        + Terminate(reason: string): void
        + Reactivate(): void
    }

    class CompanyStaff {
        + UserId: Guid
        + CompanyId: Guid
        + Position: string?
        + IsActive: bool
        + Create(userId, companyId, position?): CompanyStaff$
        + Deactivate(): void
        + Activate(): void
    }

    class CompanyServiceArea {
        + CompanyId: Guid
        + WardCode: string
    }

    class ContractPeriod {
        + CompanyId: Guid
        + ContractNumber: string
        + ContractType: ContractType
        + StartDate: DateTime
        + EndDate: DateTime?
        + RenewedByUserId: Guid
        + Note: string?
        + CreatedAt: DateTime
        + Create(...): ContractPeriod$
    }

    class StaffInvitation {
        + CompanyId: Guid
        + Email: string
        + Status: InvitationStatus
        + ExpiresAt: DateTime
        + InvitedByUserId: Guid
    }

    class TeamType {
        <<enumeration>>
        Cleanup
        Inspection
    }

    class ContractType {
        <<enumeration>>
        Subsidiary
        Bidding
    }

    AuditableEntity <|-- Department : Inheritance
    AuditableEntity <|-- LocalOffice : Inheritance
    SoftDeletableEntity <|-- EnvironmentalTeam : Inheritance
    SoftDeletableEntity <|-- EnvironmentalServiceCompany : Inheritance
    AuditableEntity <|-- CompanyStaff : Inheritance

    Department "1" *-- "0..*" LocalOffice : Composition
    EnvironmentalServiceCompany "1" *-- "0..*" CompanyStaff : Composition
    EnvironmentalServiceCompany "1" *-- "0..*" CompanyServiceArea : Composition
    EnvironmentalServiceCompany "1" *-- "0..*" ContractPeriod : Composition
    EnvironmentalServiceCompany "1" *-- "0..*" StaffInvitation : Composition

    LocalOffice "1" o-- "0..*" EnvironmentalTeam : Aggregation
    EnvironmentalTeam "1" o-- "0..*" TeamMember : Aggregation
    EnvironmentalServiceCompany "1" o-- "0..*" EnvironmentalTeam : Aggregation

    LocalOffice "0..*" --> "0..1" User : Association (officer LEO)
    TeamMember --> User : Association
    EnvironmentalServiceCompany --> Department : Association
    EnvironmentalTeam --> TeamType : Association
    ContractPeriod --> ContractType : Association
```

---

## CD-04: Inspection & Penalty Module

**Mô tả:** Quy trình thanh tra vi phạm, lập biên bản xử phạt, quản lý đối tượng vi phạm, theo dõi thanh toán.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| InspectionReport → PenaltyPayment | **Composition** ◆ | Payment gắn chặt với biên bản, xóa biên bản → xóa payment |
| ViolatingEntity → InspectionReport | **Aggregation** ◇ | Đối tượng vi phạm tồn tại độc lập, có thể liên kết nhiều biên bản |
| InspectionReport → Report | **Association** → | Tham chiếu Report gốc |
| InspectionReport → EnvironmentalTeam | **Association** → | Tham chiếu Team thanh tra |
| PenaltyFramework → PollutionCategory | **Association** → | Tham chiếu danh mục |

```mermaid
classDiagram
    class InspectionReport {
        <<Aggregate Root>>
        + ReportId: Guid
        + Status: InspectionStatus
        + AssignedTeamId: Guid?
        + ViolationDescription: string?
        + ViolatorName: string?
        + ViolatorAddress: string?
        + ViolatorIdentity: string?
        + ViolatingEntityId: Guid?
        + ViolationLevel: ViolationLevel?
        + PenaltyAmount: decimal?
        + PenaltyDecisionNumber: string?
        + PenaltyIssuedAt: DateTime?
        + PenaltyDueDate: DateTime?
        + PaidAmount: decimal?
        + AdditionalPenaltyMeasures: string?
        + IsRepeatOffender: bool
        + CreatedByOfficerId: Guid
        + IssuedByInspectorId: Guid?
        + ClosedAt: DateTime?
        + ClosedReason: string?
        + SlaInspectionDueAt: DateTime?
        + SlaInspectionBreached: bool
        + CheckedInAt: DateTime?
        + ProgressPercent: int
        + Create(reportId: Guid, officerId: Guid): InspectionReport$
        + AssignTeam(teamId: Guid): void
        + CheckIn(lat: decimal, lng: decimal, note?: string): void
        + UpdateProgress(percent: int, note: string): void
        + IssuePenalty(...): void
        + RecordPayment(amount: decimal): void
        + CloseNoViolation(reason: string): void
        + Close(): void
        + MarkOverdue(): void
    }

    class ViolatingEntity {
        + Name: string
        + Address: string?
        + TaxCode: string?
        + IdentityNumber: string?
        + PhoneNumber: string?
        + Type: ViolatorType
        + Create(name, type, ...): ViolatingEntity$
        + Update(name?, address?, ...): void
    }

    class PenaltyPayment {
        + InspectionReportId: Guid
        + Amount: decimal
        + PaidAt: DateTime
        + EvidenceUrl: string?
        + Note: string?
        + RecordedByUserId: Guid
        + Create(...): PenaltyPayment$
    }

    class PenaltyFramework {
        + CategoryId: Guid
        + Level: ViolationLevel
        + MinAmount: decimal
        + MaxAmount: decimal
        + Description: string?
        + IsActive: bool
    }

    class InspectionStatus {
        <<enumeration>>
        Draft
        InProgress
        PenaltyIssued
        Paid
        PartiallyPaid
        Overdue
        Closed
        ClosedNoViolation
    }

    class ViolationLevel {
        <<enumeration>>
        Minor
        Moderate
        Severe
        Critical
    }

    class ViolatorType {
        <<enumeration>>
        Individual
        Business
    }

    SoftDeletableEntity <|-- InspectionReport : Inheritance
    SoftDeletableEntity <|-- ViolatingEntity : Inheritance
    SoftDeletableEntity <|-- PenaltyPayment : Inheritance

    InspectionReport "1" *-- "0..*" PenaltyPayment : Composition

    ViolatingEntity "1" o-- "0..*" InspectionReport : Aggregation

    InspectionReport "0..*" --> "1" Report : Association (linked report)
    InspectionReport "0..*" --> "0..1" EnvironmentalTeam : Association (team)
    InspectionReport --> ViolatingEntity : Association (violator)
    InspectionReport --> InspectionStatus : Association
    InspectionReport --> ViolationLevel : Association
    ViolatingEntity --> ViolatorType : Association
    PenaltyFramework --> PollutionCategory : Association
    PenaltyFramework --> ViolationLevel : Association
```

### State Machine — Inspection Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Draft : LEO creates

    Draft --> InProgress : Team check-in at site
    Draft --> ClosedNoViolation : No violation found

    InProgress --> PenaltyIssued : Inspector issues penalty
    InProgress --> ClosedNoViolation : No violation found

    PenaltyIssued --> Paid : Full payment received
    PenaltyIssued --> PartiallyPaid : Partial payment
    PenaltyIssued --> Overdue : Past due date

    PartiallyPaid --> Paid : Remaining paid
    PartiallyPaid --> Overdue : Past due date

    Overdue --> Paid : Late payment received
    Overdue --> Closed : Admin force close

    Paid --> Closed : Auto-close

    Closed --> [*]
    ClosedNoViolation --> [*]
```

---

## CD-05: Comment & Community Interaction Module

**Mô tả:** Bình luận cộng đồng trên báo cáo, reply, like, media đính kèm, kiểm duyệt nội dung.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| Comment → CommentMedia | **Composition** ◆ | Media thuộc về Comment, xóa Comment → xóa Media |
| Comment → CommentLike | **Composition** ◆ | Like gắn chặt với Comment |
| Comment → Comment (replies) | **Composition** ◆ | Reply thuộc về Comment cha, xóa cha → xóa reply |
| Comment → Report | **Association** → | Comment tham chiếu Report |
| Comment → User | **Association** → | Comment tham chiếu Author |
| CommentLike → User | **Association** → | Like tham chiếu User |

```mermaid
classDiagram
    class Comment {
        <<Aggregate Root>>
        + ReportId: Guid
        + AuthorId: Guid
        + Content: string
        + IsHidden: bool
        + HiddenReason: string?
        + HiddenBy: Guid?
        + HiddenAt: DateTime?
        + ParentCommentId: Guid?
        + Create(reportId, authorId, content, parentId?): Comment$
        + Edit(content: string, editorId: Guid): void
        + DeleteByAuthor(authorId: Guid): void
        + Hide(reason: string, moderatorId: Guid): void
        + Unhide(): void
    }

    class CommentMedia {
        + CommentId: Guid
        + Url: string
        + MimeType: string
        + SizeBytes: long
    }

    class CommentLike {
        <<join table>>
        + CommentId: Guid
        + UserId: Guid
        + CreatedAt: DateTime
    }

    class BlockedWord {
        + Word: string
        + IsActive: bool
        + CreatedAt: DateTime
    }

    SoftDeletableEntity <|-- Comment : Inheritance

    Comment "1" *-- "0..*" CommentMedia : Composition
    Comment "1" *-- "0..*" CommentLike : Composition
    Comment "1" *-- "0..*" Comment : Composition (replies)

    Comment "0..*" --> "1" Report : Association (report)
    Comment "0..*" --> "1" User : Association (author)
    CommentLike --> User : Association (liker)
```

---

## CD-06: Gamification Module

**Mô tả:** Hệ thống điểm thưởng, cấp bậc, huy hiệu, bảng xếp hạng để khuyến khích công dân tham gia.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| User → UserPoints | **Composition** ◆ | UserPoints thuộc về User (1:1), xóa User → xóa Points |
| UserPoints → PointTransaction | **Composition** ◆ | Transaction gắn chặt với UserPoints |
| User ↔ Badge (qua UserBadge) | **Aggregation** ◇ | Badge tồn tại độc lập (seed data), User chỉ "sở hữu" badge |

```mermaid
classDiagram
    class UserPoints {
        <<Aggregate Root>>
        + UserId: Guid
        + TotalPoints: int
        + IsLocked: bool
        + LockedUntil: DateTime?
        + LockedReason: string?
        + Level: int
        + Create(userId: Guid): UserPoints$
        + AddPoints(points: int, reason: PointReason, reportId?: Guid): void
        + DeductPoints(points: int, reason: PointReason, reportId?: Guid): void
        + Lock(reason: string, duration: TimeSpan): void
        + Unlock(): void
        + GetLevelName(): string
    }

    class PointTransaction {
        + UserPointsId: Guid
        + Points: int
        + Reason: PointReason
        + ReportId: Guid?
        + CreatedAt: DateTime
    }

    class Badge {
        + Code: string
        + NameVi: string
        + NameEn: string
        + Description: string?
        + IconUrl: string?
        + IsActive: bool
        + RequiredPoints: int?
        + RequiredReportCount: int?
        + CreatedAt: DateTime
        + Create(code, nameVi, nameEn, ...): Badge$
    }

    class UserBadge {
        <<join table>>
        + UserId: Guid
        + BadgeId: Guid
        + EarnedAt: DateTime
    }

    class GamificationConfig {
        + ActionKey: string
        + Points: int
        + Description: string?
        + IsActive: bool
    }

    class PointReason {
        <<enumeration>>
        ReportVerified
        ReportResolved
        PenaltyIssued
        DuplicateReport
        ReportRejected
        FraudPenalty
    }

    class LeaderboardPeriod {
        <<enumeration>>
        Daily
        Weekly
        Monthly
    }

    SoftDeletableEntity <|-- UserPoints : Inheritance

    User "1" *-- "1" UserPoints : Composition
    UserPoints "1" *-- "0..*" PointTransaction : Composition

    User "0..*" o-- "0..*" Badge : Aggregation (via UserBadge)

    PointTransaction --> PointReason : Association
    UserBadge --> User : Association
    UserBadge --> Badge : Association
```

### Level System

```mermaid
flowchart LR
    L1["L1: Newcomer\n0 – 99 pts"] --> L2["L2: Eco Starter\n100 – 499 pts"]
    L2 --> L3["L3: Green Advocate\n500 – 1,499 pts"]
    L3 --> L4["L4: Eco Warrior\n1,500 – 4,999 pts"]
    L4 --> L5["L5: Earth Guardian\n≥ 5,000 pts"]
```

---

## CD-07: Notification Module

**Mô tả:** Hệ thống thông báo đa kênh (push FCM, email, in-app), cài đặt preference, template quản lý bởi Admin.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| User → Notification | **Composition** ◆ | Notification thuộc về User, xóa User → xóa Notification |
| User → NotificationPreference | **Composition** ◆ | Preference gắn chặt với User |
| NotificationTemplate → NotificationType | **Association** → | Template lookup type |

```mermaid
classDiagram
    class Notification {
        + RecipientId: Guid
        + Type: NotificationType
        + Title: string
        + Message: string
        + ReferenceId: Guid?
        + Channel: NotificationChannel
        + IsRead: bool
        + ReadAt: DateTime?
        + Create(recipientId, type, title, message, channel, refId?): Notification$
        + MarkAsRead(): void
    }

    class NotificationPreference {
        + UserId: Guid
        + Type: NotificationType
        + PushEnabled: bool
        + EmailEnabled: bool
        + Create(userId, type, push, email): NotificationPreference$
        + Update(pushEnabled: bool, emailEnabled: bool): void
    }

    class NotificationTemplate {
        + TemplateKey: string
        + TitleVi: string
        + BodyVi: string
        + TitleEn: string?
        + BodyEn: string?
        + Channel: NotificationChannel
        + Type: NotificationType
        + IsPublished: bool
        + IsActive: bool
        + Create(...): NotificationTemplate$
        + Update(titleVi, bodyVi, titleEn?, bodyEn?): void
        + Publish(): void
        + Unpublish(): void
        + Deactivate(): void
        + Activate(): void
    }

    class NotificationType {
        <<enumeration>>
        ReportStatusChanged
        NewComment
        BadgeEarned
        LevelUp
        SlaBreachWarning
        NearbyReport
        PenaltyIssued
        ContractExpiry
        ReportOverdue
        ReportUnassigned
        ReportAutoClosed
        DuplicateReviewNeeded
    }

    class NotificationChannel {
        <<enumeration>>
        Push
        Email
        Both
    }

    AuditableEntity <|-- Notification : Inheritance
    AuditableEntity <|-- NotificationPreference : Inheritance
    AuditableEntity <|-- NotificationTemplate : Inheritance

    User "1" *-- "0..*" Notification : Composition
    User "1" *-- "0..*" NotificationPreference : Composition

    Notification --> NotificationType : Association
    Notification --> NotificationChannel : Association
    NotificationPreference --> NotificationType : Association
    NotificationTemplate --> NotificationType : Association
    NotificationTemplate --> NotificationChannel : Association
```

---

## CD-08: Catalog & Location Module

**Mô tả:** Dữ liệu tham chiếu — danh mục ô nhiễm, nhãn phân loại rác, đơn vị hành chính Việt Nam, nhật ký kiểm toán.

**Phân loại Relationship:**

| Relationship | Loại | Lý do |
|---|---|---|
| Province → Ward | **Composition** ◆ | Ward thuộc về Province, không tồn tại ngoài Province |
| Province → AdministrativeRegion | **Association** → | Province tham chiếu Region |
| Province → AdministrativeUnit | **Association** → | Province tham chiếu Unit |
| Report → PollutionCategory | **Association** → | Report lookup Category |
| Report ↔ WasteTag | **Association** → | Nhiều-Nhiều qua join table |

```mermaid
classDiagram
    class PollutionCategory {
        + Name: string
        + Description: string?
        + IconUrl: string?
        + IsActive: bool
        + SortOrder: int
    }

    class WasteTag {
        + Code: string
        + NameVi: string
        + NameEn: string
        + Description: string?
        + IconUrl: string?
        + IsActive: bool
        + SortOrder: int
    }

    class Province {
        + Code: string
        + Name: string
        + NameEn: string
        + FullName: string
        + FullNameEn: string
        + CodeName: string?
        + AdministrativeRegionId: Guid
        + AdministrativeUnitId: Guid
    }

    class Ward {
        + Code: string
        + Name: string
        + NameEn: string
        + FullName: string
        + FullNameEn: string
        + CodeName: string?
        + ProvinceCode: string
        + AdministrativeUnitId: Guid?
    }

    class AdministrativeRegion {
        + Id: int
        + Name: string
        + NameEn: string
    }

    class AdministrativeUnit {
        + Id: int
        + FullName: string
        + FullNameEn: string
        + ShortName: string
        + ShortNameEn: string
    }

    class AuditLog {
        + Action: string
        + EntityType: string
        + EntityId: Guid?
        + OldValues: string?
        + NewValues: string?
        + PerformedBy: Guid?
        + CreatedAt: DateTime
        + IpAddress: string?
    }

    Province "1" *-- "0..*" Ward : Composition

    Province --> AdministrativeRegion : Association (region)
    Province --> AdministrativeUnit : Association (unit)
    Ward --> AdministrativeUnit : Association (unit)

    Report "0..*" --> "1" PollutionCategory : Association
    Report "0..*" --> "0..*" WasteTag : Association (via ReportWasteTag)
```

---

## Tổng hợp quan hệ liên module (Cross-Module Overview)

> Diagram tổng quan cấp cao, hiển thị các entity chính và quan hệ cross-module với đầy đủ 4 loại relationship.

```mermaid
classDiagram
    class User {
        <<CD-01: Auth>>
    }
    class Report {
        <<CD-02: Report>>
    }
    class ReportAssignment {
        <<CD-02: Report>>
    }
    class ReportMedia {
        <<CD-02: Report>>
    }
    class EnvironmentalTeam {
        <<CD-03: Organization>>
    }
    class LocalOffice {
        <<CD-03: Organization>>
    }
    class Department {
        <<CD-03: Organization>>
    }
    class EnvironmentalServiceCompany {
        <<CD-03: Organization>>
    }
    class InspectionReport {
        <<CD-04: Inspection>>
    }
    class PenaltyPayment {
        <<CD-04: Inspection>>
    }
    class Comment {
        <<CD-05: Comment>>
    }
    class UserPoints {
        <<CD-06: Gamification>>
    }
    class Notification {
        <<CD-07: Notification>>
    }
    class PollutionCategory {
        <<CD-08: Catalog>>
    }

    %% Inheritance (△)
    SoftDeletableEntity <|-- User : Inheritance
    SoftDeletableEntity <|-- Report : Inheritance

    %% Composition (◆ child dies with parent)
    Report "1" *-- "1..*" ReportMedia : Composition
    Report "1" *-- "0..*" ReportAssignment : Composition
    InspectionReport "1" *-- "0..*" PenaltyPayment : Composition
    User "1" *-- "1" UserPoints : Composition
    User "1" *-- "0..*" Notification : Composition
    Department "1" *-- "0..*" LocalOffice : Composition

    %% Aggregation (◇ child exists independently)
    LocalOffice "1" o-- "0..*" EnvironmentalTeam : Aggregation
    EnvironmentalServiceCompany "1" o-- "0..*" EnvironmentalTeam : Aggregation

    %% Association (→ simple reference)
    User "1" --> "0..*" Report : Association (submits)
    Report "0..*" --> "1" PollutionCategory : Association (categorizedAs)
    Report --> LocalOffice : Association (routedTo)
    ReportAssignment --> EnvironmentalTeam : Association (assignedTo)
    InspectionReport --> Report : Association (linkedTo)
    Comment --> Report : Association (commentOn)
    Comment --> User : Association (writtenBy)
    EnvironmentalServiceCompany --> Department : Association (contracted)
```

---

## CD-09: Architecture Layer — Clean Architecture & Dependency Inversion

**Mô tả:** Thể hiện kiến trúc Clean Architecture 4 layer, nguyên tắc Dependency Inversion (interface ở Application, implementation ở Infrastructure), MediatR pipeline, và quan hệ giữa các layer.

### Dependency Rule

```mermaid
flowchart TB
    subgraph API ["🌐 Greenlens.Api (Composition Root)"]
        direction LR
        Controllers
        Middlewares
        Filters
    end

    subgraph APP ["📋 Greenlens.Application (Use Cases)"]
        direction LR
        Handlers["Handlers\n(Command/Query)"]
        Validators["FluentValidation\nValidators"]
        Interfaces["Interfaces\n(Contracts)"]
        Behaviors["Pipeline\nBehaviors"]
    end

    subgraph DOM ["🏛️ Greenlens.Domain (Core Business)"]
        direction LR
        Entities
        ValueObjects["Value Objects"]
        DomainEvents["Domain Events"]
        Enums
    end

    subgraph INFRA ["⚙️ Greenlens.Infrastructure (Adapters)"]
        direction LR
        Persistence["Persistence\n(EF Core + PostGIS)"]
        ExternalSvc["External Services\n(S3, AI, FCM, SMTP)"]
        Identity["Identity\n(JWT, OAuth)"]
        BackgroundJobs["Background Jobs\n(Hangfire)"]
    end

    API -->|depends on| APP
    API -.->|DI registration| INFRA
    APP -->|depends on| DOM
    INFRA -->|implements| APP
    INFRA -->|depends on| DOM

    style DOM fill:#2d5016,stroke:#4a8c2a,color:#fff
    style APP fill:#1a3a5c,stroke:#2980b9,color:#fff
    style INFRA fill:#5c3a1a,stroke:#b97829,color:#fff
    style API fill:#3a1a5c,stroke:#8e44ad,color:#fff
```

### Class Diagram — Interface ↔ Implementation (Dependency Inversion)

```mermaid
classDiagram
    %% ══════════════════════════════════════════
    %% APPLICATION LAYER — Interfaces (Contracts)
    %% ══════════════════════════════════════════

    class IApplicationDbContext {
        <<interface>>
        + Reports: DbSet~Report~
        + Users: DbSet~User~
        + Comments: DbSet~Comment~
        + InspectionReports: DbSet~InspectionReport~
        + Notifications: DbSet~Notification~
        + ...other DbSets
        + SaveChangesAsync(ct: CancellationToken): Task~int~
    }

    class ICurrentUser {
        <<interface>>
        + Id: Guid?
        + Role: UserRole?
        + IsAuthenticated: bool
    }

    class IJwtService {
        <<interface>>
        + GenerateAccessToken(user: User): string
        + GenerateRefreshToken(): string
    }

    class IFileStorageService {
        <<interface>>
        + GeneratePresignedUploadUrl(...): Task~PresignedUrlResult~
        + GeneratePresignedDownloadUrl(key: string): Task~string~
        + DeleteAsync(key: string, ct: CancellationToken): Task
    }

    class IEmailSender {
        <<interface>>
        + SendAsync(to: string, subject: string, body: string): Task
        + SendTemplateAsync(to: string, template: string, data: object): Task
    }

    class IPushNotificationSender {
        <<interface>>
        + SendAsync(deviceToken: string, title: string, body: string): Task
    }

    class INotificationService {
        <<interface>>
        + NotifyAsync(recipientId: Guid, type: NotificationType, ...): Task
        + NotifyBulkAsync(recipientIds: List~Guid~, ...): Task
    }

    class IAiClassificationService {
        <<interface>>
        + ClassifyImageAsync(imageUrl: string): Task~AiClassificationResult~
        + EstimateSeverityAsync(imageUrl: string): Task~Severity~
    }

    class IAiImageCompareService {
        <<interface>>
        + ComputePHashAsync(imageUrl: string): Task~string~
        + CompareImagesAsync(hash1: string, hash2: string): Task~decimal~
    }

    class IGeoDistanceService {
        <<interface>>
        + IsWithinDistance(lat1, lng1, lat2, lng2, meters: double): bool
    }

    class IPasswordHasher {
        <<interface>>
        + Hash(password: string): string
        + Verify(password: string, hash: string): bool
    }

    class IDateTimeProvider {
        <<interface>>
        + UtcNow: DateTime
    }

    class ITransactionManager {
        <<interface>>
        + BeginTransactionAsync(ct: CancellationToken): Task
        + CommitAsync(ct: CancellationToken): Task
        + RollbackAsync(ct: CancellationToken): Task
    }

    class IAuditLogger {
        <<interface>>
        + LogAsync(action: string, entityType: string, entityId: Guid?, ...): Task
    }

    class IReportSubmissionRateLimiter {
        <<interface>>
        + IsAllowedAsync(userId: Guid): Task~bool~
        + RecordSubmissionAsync(userId: Guid): Task
    }

    class IGoogleAuthService {
        <<interface>>
        + VerifyTokenAsync(idToken: string): Task~GoogleUserInfo~
    }

    %% ══════════════════════════════════════════
    %% INFRASTRUCTURE LAYER — Implementations
    %% ══════════════════════════════════════════

    class ApplicationDbContext {
        <<EF Core>>
        + Reports: DbSet~Report~
        + Users: DbSet~User~
        + ...other DbSets
        + OnModelCreating(builder: ModelBuilder): void
    }

    class CurrentUser {
        - _httpContextAccessor: IHttpContextAccessor
    }

    class JwtService {
        - _options: JwtOptions
        - _dateTimeProvider: IDateTimeProvider
    }

    class R2FileStorageService {
        <<Cloudflare R2 / S3>>
        - _s3Client: IAmazonS3
        - _options: R2Options
    }

    class SmtpEmailSender {
        <<SMTP>>
        - _smtpOptions: SmtpOptions
    }

    class FcmPushNotificationSender {
        <<Firebase FCM>>
        - _messaging: FirebaseMessaging
    }

    class NotificationService {
        - _dbContext: IApplicationDbContext
        - _pushSender: IPushNotificationSender
        - _emailSender: IEmailSender
    }

    class AiClassificationService {
        <<External AI API>>
        - _httpClient: HttpClient
        - _options: AiOptions
    }

    class AiImageCompareService {
        <<pHash comparison>>
        - _httpClient: HttpClient
    }

    class PostGisDistanceService {
        <<PostGIS>>
        - _dbContext: IApplicationDbContext
    }

    class BcryptPasswordHasher {
        <<bcrypt ≥ 12 rounds>>
    }

    class DateTimeProvider {
    }

    class TransactionManager {
        - _dbContext: ApplicationDbContext
    }

    %% ══════════════════════════════════════════
    %% Dependency Inversion — Implementation relationships
    %% ══════════════════════════════════════════

    IApplicationDbContext <|.. ApplicationDbContext : implements
    ICurrentUser <|.. CurrentUser : implements
    IJwtService <|.. JwtService : implements
    IFileStorageService <|.. R2FileStorageService : implements
    IEmailSender <|.. SmtpEmailSender : implements
    IPushNotificationSender <|.. FcmPushNotificationSender : implements
    INotificationService <|.. NotificationService : implements
    IAiClassificationService <|.. AiClassificationService : implements
    IAiImageCompareService <|.. AiImageCompareService : implements
    IGeoDistanceService <|.. PostGisDistanceService : implements
    IPasswordHasher <|.. BcryptPasswordHasher : implements
    IDateTimeProvider <|.. DateTimeProvider : implements
    ITransactionManager <|.. TransactionManager : implements
```

### Class Diagram — MediatR Pipeline (Request Flow)

```mermaid
classDiagram
    class ISender {
        <<interface / MediatR>>
        + Send~TResponse~(request: IRequest~TResponse~, ct: CancellationToken): Task~TResponse~
    }

    class IRequest~TResponse~ {
        <<interface / MediatR>>
    }

    class IRequestHandler~TRequest_TResponse~ {
        <<interface / MediatR>>
        + Handle(request: TRequest, ct: CancellationToken): Task~TResponse~
    }

    class IPipelineBehavior~TRequest_TResponse~ {
        <<interface / MediatR>>
        + Handle(request: TRequest, next: RequestHandlerDelegate, ct: CancellationToken): Task~TResponse~
    }

    class ValidationBehavior {
        <<Pipeline Behavior>>
        - _validators: IEnumerable~IValidator~
        + Handle(...): Task~TResponse~
    }

    class LoggingBehavior {
        <<Pipeline Behavior>>
        - _logger: ILogger
        + Handle(...): Task~TResponse~
    }

    class TransactionBehavior {
        <<Pipeline Behavior>>
        - _transactionManager: ITransactionManager
        + Handle(...): Task~TResponse~
    }

    class AuditLogBehavior {
        <<Pipeline Behavior>>
        - _auditLogger: IAuditLogger
        - _currentUser: ICurrentUser
        + Handle(...): Task~TResponse~
    }

    class SubmitReportCommand {
        <<example Command>>
        + CategoryId: Guid
        + Description: string?
        + Latitude: decimal
        + Longitude: decimal
        + MediaKeys: List~string~
    }

    class SubmitReportCommandHandler {
        <<example Handler>>
        - _dbContext: IApplicationDbContext
        - _currentUser: ICurrentUser
        - _rateLimiter: IReportSubmissionRateLimiter
        + Handle(cmd, ct): Task~Result~Guid~~
    }

    class SubmitReportCommandValidator {
        <<FluentValidation>>
    }

    IPipelineBehavior <|.. ValidationBehavior : implements
    IPipelineBehavior <|.. LoggingBehavior : implements
    IPipelineBehavior <|.. TransactionBehavior : implements
    IPipelineBehavior <|.. AuditLogBehavior : implements

    IRequest <|.. SubmitReportCommand : implements
    IRequestHandler <|.. SubmitReportCommandHandler : implements

    ISender --> IPipelineBehavior : 1. dispatches through
    IPipelineBehavior --> IRequestHandler : 2. delegates to

    SubmitReportCommandHandler --> IApplicationDbContext : uses
    SubmitReportCommandHandler --> ICurrentUser : uses
    SubmitReportCommandHandler --> IReportSubmissionRateLimiter : uses
```

### Class Diagram — API Layer (Controllers)

```mermaid
classDiagram
    class ControllerBase {
        <<ASP.NET Core>>
    }

    class AuthController {
        - _sender: ISender
        + Register(cmd: RegisterCommand): Task~IActionResult~
        + Login(cmd: LoginCommand): Task~IActionResult~
        + RefreshToken(cmd: RefreshTokenCommand): Task~IActionResult~
        + ForgotPassword(cmd: ForgotPasswordCommand): Task~IActionResult~
    }

    class ReportsController {
        - _sender: ISender
        + Submit(cmd: SubmitReportCommand): Task~IActionResult~
        + GetById(id: Guid): Task~IActionResult~
        + GetNearby(query: GetNearbyReportsQuery): Task~IActionResult~
        + Verify(id: Guid, cmd: VerifyReportCommand): Task~IActionResult~
        + Reject(id: Guid, cmd: RejectReportCommand): Task~IActionResult~
    }

    class InspectionsController {
        - _sender: ISender
        + Create(cmd: CreateInspectionCommand): Task~IActionResult~
        + IssuePenalty(id: Guid, cmd: IssuePenaltyCommand): Task~IActionResult~
        + RecordPayment(id: Guid, cmd: RecordPaymentCommand): Task~IActionResult~
    }

    class CompaniesController {
        - _sender: ISender
        + Create(cmd: CreateCompanyCommand): Task~IActionResult~
        + Activate(id: Guid): Task~IActionResult~
        + Suspend(id: Guid, cmd: SuspendCompanyCommand): Task~IActionResult~
    }

    class CommentsController {
        - _sender: ISender
        + Create(cmd: CreateCommentCommand): Task~IActionResult~
        + Delete(id: Guid): Task~IActionResult~
    }

    class AdminController {
        - _sender: ISender
        + ManageUsers(...): Task~IActionResult~
        + ManageCategories(...): Task~IActionResult~
        + ViewAuditLogs(...): Task~IActionResult~
    }

    class NotificationsController {
        - _sender: ISender
        + GetMyNotifications(query): Task~IActionResult~
        + MarkAsRead(id: Guid): Task~IActionResult~
    }

    class GamificationController {
        - _sender: ISender
        + GetMyPoints(query): Task~IActionResult~
        + GetLeaderboard(query): Task~IActionResult~
    }

    class TeamsController {
        - _sender: ISender
        + Create(cmd: CreateTeamCommand): Task~IActionResult~
        + AddMember(teamId: Guid, cmd): Task~IActionResult~
    }

    class MapController {
        - _sender: ISender
        + GetHeatmap(query): Task~IActionResult~
        + GetNearby(query): Task~IActionResult~
    }

    ControllerBase <|-- AuthController : Inheritance
    ControllerBase <|-- ReportsController : Inheritance
    ControllerBase <|-- InspectionsController : Inheritance
    ControllerBase <|-- CompaniesController : Inheritance
    ControllerBase <|-- CommentsController : Inheritance
    ControllerBase <|-- AdminController : Inheritance
    ControllerBase <|-- NotificationsController : Inheritance
    ControllerBase <|-- GamificationController : Inheritance
    ControllerBase <|-- TeamsController : Inheritance
    ControllerBase <|-- MapController : Inheritance

    AuthController --> ISender : Association (dispatches)
    ReportsController --> ISender : Association (dispatches)
    InspectionsController --> ISender : Association (dispatches)
    CompaniesController --> ISender : Association (dispatches)
    CommentsController --> ISender : Association (dispatches)
    AdminController --> ISender : Association (dispatches)
    NotificationsController --> ISender : Association (dispatches)
    GamificationController --> ISender : Association (dispatches)
    TeamsController --> ISender : Association (dispatches)
    MapController --> ISender : Association (dispatches)
```

### Request Flow tổng quan (API → Handler → Domain)

```mermaid
flowchart LR
    Client["📱 Client\n(Mobile/Web)"] -->|HTTP Request| Controller["🌐 Controller\n(Api Layer)"]
    Controller -->|ISender.Send| Pipeline["⚙️ MediatR Pipeline"]

    subgraph Pipeline["MediatR Pipeline (Application Layer)"]
        direction TB
        B1["1️⃣ LoggingBehavior\n→ log request"] --> B2["2️⃣ ValidationBehavior\n→ FluentValidation"]
        B2 --> B3["3️⃣ TransactionBehavior\n→ begin transaction"]
        B3 --> B4["4️⃣ AuditLogBehavior\n→ audit trail"]
        B4 --> Handler["✅ Handler\n→ business logic"]
    end

    Handler -->|"uses"| Domain["🏛️ Domain\nEntities"]
    Handler -->|"uses"| Infra["⚙️ Infrastructure\n(DB, S3, AI, FCM)"]
    Handler -->|"returns"| Result["📦 Result~T~"]
    Result -->|"ToActionResult()"| Response["📤 HTTP Response\n(ProblemDetails)"]

    style Client fill:#34495e,color:#fff
    style Controller fill:#8e44ad,color:#fff
    style Handler fill:#2980b9,color:#fff
    style Domain fill:#27ae60,color:#fff
    style Infra fill:#d35400,color:#fff
    style Result fill:#2c3e50,color:#fff
    style Response fill:#16a085,color:#fff
```
