# GreenLens — Use Case Diagram (v2.0)

> **Dự án:** SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution
> **Loại:** Use Case Diagram — UML 2.0 style (Mermaid flowchart)
> **Cập nhật:** 2026-08-07 · **Nguồn:** Toàn bộ Feature slices từ src code thực tế
> **Tổng:** 9 Actors · ~170 use cases · 14 modules

---

## Actors

| #   | Actor                  | Vai trò                                                                        | UserRole enum value(s)         |
| --- | ---------------------- | ------------------------------------------------------------------------------ | ------------------------------ |
| 1   | 👤 **Guest**           | Chưa đăng nhập, xem map công khai, đăng ký, quên mật khẩu                     | —                              |
| 2   | 🟢 **Citizen**         | Gửi báo cáo, bình luận, flag, đánh giá, gamification, tham gia community cleanup | Citizen                        |
| 3   | 🔵 **LEO**             | Xác minh, assign team/company, inspect, community cleanup, moderate, SLA       | LEO                            |
| 4   | 🟣 **DEO**             | Quản lý tỉnh, tạo company, gia hạn hợp đồng, dashboard                       | DEO                            |
| 5   | 🟠 **Cleaner**         | Nhận task dọn dẹp, check-in, cập nhật tiến độ, resolve, dẫn community cleanup | Cleaner                        |
| 6   | 🔴 **Inspector**       | Checklist workflow, field investigation, xử phạt, thu tiền phạt                | Inspector                      |
| 7   | 🟡 **Company Manager** | Quản lý nhân sự công ty, team, task dispatch                                   | CompanyManager                 |
| 8   | 🟤 **Company Staff**   | Nhận task từ company, cập nhật tiến độ                                         | CompanyStaff                   |
| 9   | ⚫ **Admin**            | Quản lý user, danh mục, cấu hình, content mod, dashboard                       | Admin                          |
| —   | 🤖 **AI Service**      | Phân loại ảnh, phát hiện trùng, ước lượng severity (automated, không phải actor) | —                              |

---

## UC-01: Authentication & Account (Guest / All Users)

```mermaid
flowchart LR
    Guest((👤 Guest))
    AllUsers((All Users))

    subgraph UC_AUTH["🔐 Authentication & Account"]
        UC01["UC-01 Register<br/>(Email + OTP)"]
        UC02["UC-02 Login"]
        UC03["UC-03 Login Google"]
        UC04["UC-04 Forgot Password"]
        UC05["UC-05 Reset Password"]
        UC06["UC-06 Verify OTP"]
        UC07["UC-07 Refresh Token"]
        UC08["UC-08 Change Password"]
        UC09["UC-09 Request Account<br/>Deletion"]
        UC10["UC-10 Restore Account"]
        UC11["UC-11 View Profile"]
        UC12["UC-12 Update Profile"]
        UC13["UC-13 Upload Avatar"]
        UC14["UC-14 Accept Data Consent"]
        UC15["UC-15 Verify Phone<br/>(Firebase)"]
        UC16["UC-16 Export My Data"]
        UC17["UC-17 View Public<br/>User Profile"]
    end

    Guest --- UC01
    Guest --- UC02
    Guest --- UC03
    Guest --- UC04
    Guest --- UC05
    Guest --- UC06

    AllUsers --- UC07
    AllUsers --- UC08
    AllUsers --- UC09
    AllUsers --- UC10
    AllUsers --- UC11
    AllUsers --- UC12
    AllUsers --- UC13
    AllUsers --- UC14
    AllUsers --- UC15
    AllUsers --- UC16
    AllUsers --- UC17

    UC01 -. "<<include>>" .-> UC06
    UC04 -. "<<include>>" .-> UC06
    UC02 -. "<<extend>>" .-> UC03
```

---

## UC-02: Pollution Report — Citizen

```mermaid
flowchart LR
    Citizen((🟢 Citizen))
    AI((🤖 AI Service))

    subgraph UC_REPORT["📋 Report — Submit & View"]
        UC20["UC-20 Submit Pollution<br/>Report"]
        UC21["UC-21 Save Draft"]
        UC22["UC-22 Delete Draft"]
        UC23["UC-23 View My Drafts"]
        UC24["UC-24 View My Reports"]
        UC25["UC-25 View Report Detail"]
        UC26["UC-26 View Report History"]
        UC27["UC-27 Flag Report"]
        UC28["UC-28 Rate Report<br/>(Satisfaction)"]
        UC29["UC-29 Request Reopen<br/>Report"]
        UC30["UC-30 Delete Report"]
        UC31["UC-31 Upload Report<br/>Photo/Video"]
        UC32["UC-32 Attach GPS<br/>Coordinates"]
        UC33["UC-33 Find Nearby Reports"]
    end

    Citizen --- UC20
    Citizen --- UC21
    Citizen --- UC22
    Citizen --- UC23
    Citizen --- UC24
    Citizen --- UC25
    Citizen --- UC26
    Citizen --- UC27
    Citizen --- UC28
    Citizen --- UC29
    Citizen --- UC30
    Citizen --- UC33

    UC20 -. "<<include>>" .-> UC31
    UC20 -. "<<include>>" .-> UC32
    UC20 -. "<<extend>>" .-> UC21

    UC20 -. "<<include>>" ..-> AI
    AI -.- UC_AI_SUB

    subgraph UC_AI_SUB["🤖 AI (Automated)"]
        UCAI1["Auto-classify Photo"]
        UCAI2["Auto-detect Duplicate"]
        UCAI3["Estimate Severity"]
        UCAI4["Flag Suspicious Content"]
    end
```

---

## UC-03: Report Management — LEO

```mermaid
flowchart LR
    LEO((🔵 LEO))

    subgraph UC_LEO_REPORT["📋 Report Management — LEO"]
        UC40["UC-40 View Officer Queue"]
        UC41["UC-41 Verify Report"]
        UC42["UC-42 Reject Report"]
        UC43["UC-43 View Duplicate<br/>Candidates"]
        UC44["UC-44 Confirm Duplicate"]
        UC45["UC-45 Dismiss Duplicate"]
        UC46["UC-46 Assign Cleanup Team"]
        UC47["UC-47 Reassign Team"]
        UC48["UC-48 Dispatch to Company"]
        UC49["UC-49 Escalate Report<br/>(to DEO)"]
        UC50["UC-50 Close Report"]
        UC51["UC-51 Approve Reopen<br/>Request"]
        UC52["UC-52 Reject Reopen<br/>Request"]
        UC53["UC-53 View Reopen<br/>Requests"]
        UC54["UC-54 View Office Reports"]
        UC55["UC-55 View Officer KPI"]
        UC56["UC-56 Export Reports"]
        UC57["UC-57 Tag Report<br/>Waste Type"]
        UC58["UC-58 View Report<br/>Progress Board"]
        UC59["UC-59 View Violation<br/>Recurrence Candidates"]
        UC60["UC-60 Dismiss Violation<br/>Recurrence"]
    end

    LEO --- UC40
    LEO --- UC41
    LEO --- UC42
    LEO --- UC43
    LEO --- UC44
    LEO --- UC45
    LEO --- UC46
    LEO --- UC47
    LEO --- UC48
    LEO --- UC49
    LEO --- UC50
    LEO --- UC51
    LEO --- UC52
    LEO --- UC53
    LEO --- UC54
    LEO --- UC55
    LEO --- UC56
    LEO --- UC57
    LEO --- UC58
    LEO --- UC59
    LEO --- UC60

    UC46 -. "<<include>>" .-> UC_AVAIL["View Available Teams"]
    UC41 -. "<<include>>" .-> UC_PRIO["Calculate Priority Score"]
```

---

## UC-04: Cleanup Task — Cleaner / Company Staff

```mermaid
flowchart LR
    Cleaner((🟠 Cleaner))
    CompStaff((🟤 Company Staff))

    subgraph UC_CLEANUP["🧹 Cleanup Task"]
        UC70["UC-70 View Assigned Tasks"]
        UC71["UC-71 Accept Task"]
        UC72["UC-72 Decline Task"]
        UC73["UC-73 Check-in at Site"]
        UC74["UC-74 Upload Before Photos"]
        UC75["UC-75 Update Progress"]
        UC76["UC-76 Upload Progress Image"]
        UC77["UC-77 Mark Task as Resolved"]
        UC78["UC-78 Escalate to LEO"]
        UC79["UC-79 View My Task Detail"]
        UC80["UC-80 View My Progress<br/>History"]
        UC81["UC-81 View My Task<br/>Progress Stats"]
    end

    Cleaner --- UC70
    Cleaner --- UC71
    Cleaner --- UC72
    Cleaner --- UC73
    Cleaner --- UC74
    Cleaner --- UC75
    Cleaner --- UC76
    Cleaner --- UC77
    Cleaner --- UC78
    Cleaner --- UC79
    Cleaner --- UC80
    Cleaner --- UC81

    CompStaff --- UC70
    CompStaff --- UC71
    CompStaff --- UC72
    CompStaff --- UC73
    CompStaff --- UC74
    CompStaff --- UC75
    CompStaff --- UC76
    CompStaff --- UC77
    CompStaff --- UC78

    UC77 -. "<<include>>" .-> UC74
```

---

## UC-05: Inspection & Penalty — LEO / Inspector

```mermaid
flowchart LR
    LEO((🔵 LEO))
    Inspector((🔴 Inspector))

    subgraph UC_INSPECT["⚖️ Inspection & Penalty"]
        UC90["UC-90 Create Inspection<br/>Report"]
        UC91["UC-91 Assign Inspection<br/>Team"]
        UC92["UC-92 Accept Inspection<br/>Task"]
        UC93["UC-93 Decline Inspection"]
        UC94["UC-94 Confirm Arrival"]
        UC95["UC-95 Check-in Inspection<br/>(GPS)"]
        UC96["UC-96 Upload Inspection<br/>Evidence"]
        UC97["UC-97 Update Inspection<br/>Checklist"]
        UC98["UC-98 Submit Field<br/>Investigation"]
        UC99["UC-99 Issue Penalty<br/>Decision"]
        UC100["UC-100 Close No Violation"]
        UC101["UC-101 Record Payment"]
        UC102["UC-102 Delete Payment"]
        UC103["UC-103 Mark Overdue"]
        UC104["UC-104 Close Inspection"]
        UC105["UC-105 View Inspection<br/>Queue"]
        UC106["UC-106 View Inspection<br/>Report Detail"]
        UC107["UC-107 View Inspections<br/>by Report"]
        UC108["UC-108 View Inspection<br/>Team KPI"]
        UC109["UC-109 View Payment<br/>History"]
    end

    subgraph UC_VIOLATOR["🔍 Violating Entity"]
        UC110["UC-110 Create Violating<br/>Entity"]
        UC111["UC-111 Update Violating<br/>Entity"]
        UC112["UC-112 Delete Violating<br/>Entity"]
        UC113["UC-113 Search Violating<br/>Entities"]
        UC114["UC-114 View Violating<br/>Entity Detail"]
    end

    LEO --- UC90
    LEO --- UC91
    LEO --- UC104
    LEO --- UC105
    LEO --- UC106
    LEO --- UC107
    LEO --- UC109

    Inspector --- UC92
    Inspector --- UC93
    Inspector --- UC94
    Inspector --- UC95
    Inspector --- UC96
    Inspector --- UC97
    Inspector --- UC98
    Inspector --- UC99
    Inspector --- UC100
    Inspector --- UC101
    Inspector --- UC102
    Inspector --- UC103
    Inspector --- UC108

    Inspector --- UC110
    Inspector --- UC111
    Inspector --- UC112
    Inspector --- UC113
    Inspector --- UC114

    UC99 -. "<<include>>" .-> UC_CLASS["Classify Violation Level"]
    UC99 -. "<<include>>" .-> UC_DETECT["Detect Repeat Offender"]
```

---

## UC-06: Community Cleanup — LEO / Cleaner / Citizen

```mermaid
flowchart LR
    LEO((🔵 LEO))
    Cleaner((🟠 Cleaner))
    Citizen((🟢 Citizen))

    subgraph UC_COMMUNITY["🤝 Community Cleanup"]
        UC120["UC-120 Create Community<br/>Cleanup Event"]
        UC121["UC-121 Close Join Period"]
        UC122["UC-122 Start Community<br/>Cleanup"]
        UC123["UC-123 Submit Before<br/>Images"]
        UC124["UC-124 Update Community<br/>Progress"]
        UC125["UC-125 Submit Community<br/>Verification"]
        UC126["UC-126 Verify Community<br/>Cleanup"]
        UC127["UC-127 Reject Community<br/>Verification"]
        UC128["UC-128 Cancel Community<br/>Cleanup"]
        UC129["UC-129 Join Community<br/>Cleanup"]
        UC130["UC-130 Withdraw from<br/>Community Cleanup"]
        UC131["UC-131 Check-in Community<br/>Cleanup (GPS)"]
        UC132["UC-132 View Open<br/>Community Cleanups"]
        UC133["UC-133 View My Community<br/>Cleanups"]
        UC134["UC-134 View Led Community<br/>Cleanups"]
        UC135["UC-135 View Community<br/>Cleanup Detail"]
        UC136["UC-136 View Community<br/>Participants"]
        UC137["UC-137 View Office<br/>Community Queue"]
    end

    LEO --- UC120
    LEO --- UC126
    LEO --- UC127
    LEO --- UC128
    LEO --- UC137

    Cleaner --- UC121
    Cleaner --- UC122
    Cleaner --- UC123
    Cleaner --- UC124
    Cleaner --- UC125
    Cleaner --- UC134

    Citizen --- UC129
    Citizen --- UC130
    Citizen --- UC131
    Citizen --- UC132
    Citizen --- UC133
    Citizen --- UC135
    Citizen --- UC136
```

---

## UC-07: Comment — Citizen / LEO

```mermaid
flowchart LR
    Citizen((🟢 Citizen))
    LEO((🔵 LEO))

    subgraph UC_COMMENT["💬 Comment"]
        UC140["UC-140 Add Comment"]
        UC141["UC-141 Edit Comment"]
        UC142["UC-142 Delete Comment"]
        UC143["UC-143 View Report<br/>Comments"]
        UC144["UC-144 Like / Unlike<br/>Comment"]
        UC145["UC-145 Upload Comment<br/>Image"]
        UC146["UC-146 Hide Comment"]
    end

    Citizen --- UC140
    Citizen --- UC141
    Citizen --- UC142
    Citizen --- UC143
    Citizen --- UC144
    Citizen --- UC145

    LEO --- UC146
    LEO --- UC143

    UC140 -. "<<extend>>" .-> UC145
```

---

## UC-08: Organization & Team — LEO

```mermaid
flowchart LR
    LEO((🔵 LEO))

    subgraph UC_ORG["🏛️ Organization & Team"]
        UC150["UC-150 View My<br/>Local Offices"]
        UC151["UC-151 View Office Staff"]
        UC152["UC-152 Create<br/>Environmental Team"]
        UC153["UC-153 View Teams"]
        UC154["UC-154 View Team Detail"]
        UC155["UC-155 Update Team"]
        UC156["UC-156 Add Team Member"]
        UC157["UC-157 Remove Team<br/>Member"]
        UC158["UC-158 Transfer Team<br/>Member"]
        UC159["UC-159 Recruit Staff<br/>(Invite Citizen)"]
        UC160["UC-160 Release Staff"]
        UC161["UC-161 Lookup Citizen<br/>by Email"]
        UC162["UC-162 View My<br/>Team Profile"]
        UC163["UC-163 View My<br/>Invitations"]
        UC164["UC-164 Accept Invitation"]
        UC165["UC-165 Decline Invitation"]
    end

    LEO --- UC150
    LEO --- UC151
    LEO --- UC152
    LEO --- UC153
    LEO --- UC154
    LEO --- UC155
    LEO --- UC156
    LEO --- UC157
    LEO --- UC158
    LEO --- UC159
    LEO --- UC160
    LEO --- UC161

    Citizen((🟢 Citizen)) --- UC162
    Citizen --- UC163
    Citizen --- UC164
    Citizen --- UC165

    UC159 -. "<<include>>" .-> UC161
```

---

## UC-09: Company Management — DEO / Company Manager

```mermaid
flowchart LR
    DEO((🟣 DEO))
    CM((🟡 Company<br/>Manager))

    subgraph UC_COMPANY["🏢 Company Management"]
        UC170["UC-170 Create Company"]
        UC171["UC-171 View Companies"]
        UC172["UC-172 View Company Detail"]
        UC173["UC-173 Suspend Company"]
        UC174["UC-174 Reactivate Company"]
        UC175["UC-175 Terminate Company"]
        UC176["UC-176 Delete Company"]
        UC177["UC-177 Renew Contract"]
        UC178["UC-178 View Contract<br/>History"]
        UC179["UC-179 Update Company<br/>Service Areas"]
        UC180["UC-180 View Company<br/>Service Areas"]
        UC181["UC-181 View Company KPI"]
        UC182["UC-182 View Office<br/>Companies"]
        UC183["UC-183 Assign LEO to<br/>Office"]
        UC184["UC-184 Assign DEO to<br/>Department"]
    end

    subgraph UC_CM["🟡 Company Manager"]
        UC190["UC-190 View My Company"]
        UC191["UC-191 Create Company<br/>Staff Account"]
        UC192["UC-192 View Company Staff"]
        UC193["UC-193 Toggle Company<br/>Staff Status"]
        UC194["UC-194 Create Company Team"]
        UC195["UC-195 View Company Teams"]
        UC196["UC-196 Update Company Team"]
        UC197["UC-197 Toggle Company<br/>Team Status"]
        UC198["UC-198 Delete Company<br/>Team"]
        UC199["UC-199 Add Company<br/>Team Member"]
        UC200["UC-200 Remove Company<br/>Team Member"]
        UC201["UC-201 Reset CM Password"]
        UC202["UC-202 View Company<br/>Queue"]
        UC203["UC-203 View Company<br/>Assignments"]
        UC204["UC-204 Assign Company<br/>Team to Task"]
    end

    DEO --- UC170
    DEO --- UC171
    DEO --- UC172
    DEO --- UC173
    DEO --- UC174
    DEO --- UC175
    DEO --- UC176
    DEO --- UC177
    DEO --- UC178
    DEO --- UC179
    DEO --- UC180
    DEO --- UC181
    DEO --- UC182
    DEO --- UC183
    DEO --- UC184

    CM --- UC190
    CM --- UC191
    CM --- UC192
    CM --- UC193
    CM --- UC194
    CM --- UC195
    CM --- UC196
    CM --- UC197
    CM --- UC198
    CM --- UC199
    CM --- UC200
    CM --- UC201
    CM --- UC202
    CM --- UC203
    CM --- UC204
```

---

## UC-10: DEO Department Management

```mermaid
flowchart LR
    DEO((🟣 DEO))

    subgraph UC_DEPT["🏛️ Department Management"]
        UC210["UC-210 View Departments"]
        UC211["UC-211 View Department<br/>Detail"]
        UC212["UC-212 View Department<br/>Reports"]
        UC213["UC-213 View Local Offices"]
        UC214["UC-214 View Local Office<br/>Detail"]
    end

    DEO --- UC210
    DEO --- UC211
    DEO --- UC212
    DEO --- UC213
    DEO --- UC214
```

---

## UC-11: Gamification — Citizen

```mermaid
flowchart LR
    Citizen((🟢 Citizen))

    subgraph UC_GAMIFY["🏆 Gamification"]
        UC220["UC-220 View My Points"]
        UC221["UC-221 View My Badges"]
        UC222["UC-222 Set Featured Badge"]
        UC223["UC-223 View Leaderboard"]
        UC224["UC-224 View Badge Catalog"]
    end

    Citizen --- UC220
    Citizen --- UC221
    Citizen --- UC222
    Citizen --- UC223
    Citizen --- UC224
```

---

## UC-12: Notification — All Users

```mermaid
flowchart LR
    AllUsers((All Users))

    subgraph UC_NOTIF["🔔 Notification"]
        UC230["UC-230 View My<br/>Notifications"]
        UC231["UC-231 Mark Notification<br/>Read"]
        UC232["UC-232 Mark All Read"]
        UC233["UC-233 Update<br/>Notification Preferences"]
        UC234["UC-234 View Notification<br/>Preferences"]
        UC235["UC-235 Update Device<br/>Token (FCM)"]
    end

    AllUsers --- UC230
    AllUsers --- UC231
    AllUsers --- UC232
    AllUsers --- UC233
    AllUsers --- UC234
    AllUsers --- UC235
```

---

## UC-13: Map — Guest / Citizen

```mermaid
flowchart LR
    Guest((👤 Guest))
    Citizen((🟢 Citizen))

    subgraph UC_MAP["🗺️ Map & Location"]
        UC240["UC-240 View Pollution Map"]
        UC241["UC-241 View Map Clusters"]
        UC242["UC-242 View Heatmap"]
        UC243["UC-243 Filter Map"]
        UC244["UC-244 View Map Viewport<br/>Summary"]
    end

    Guest --- UC240
    Guest --- UC241
    Guest --- UC244
    Citizen --- UC240
    Citizen --- UC241
    Citizen --- UC242
    Citizen --- UC243
    Citizen --- UC244

    UC240 -. "<<extend>>" .-> UC241
    UC240 -. "<<extend>>" .-> UC242
    UC240 -. "<<extend>>" .-> UC243
```

---

## UC-14: Administration — Admin

```mermaid
flowchart LR
    Admin((⚫ Admin))

    subgraph UC_ADMIN["⚙️ Administration"]
        direction TB

        subgraph UC_USER_MGMT["User Management"]
            UC250["UC-250 View All Users"]
            UC251["UC-251 Create Account"]
            UC252["UC-252 Update User"]
            UC253["UC-253 Update User Role"]
            UC254["UC-254 Delete User"]
            UC255["UC-255 Ban / Unban User"]
            UC256["UC-256 Lock Gamification<br/>Points"]
        end

        subgraph UC_CAT_MGMT["Catalog Management"]
            UC260["UC-260 Create Pollution<br/>Category"]
            UC261["UC-261 Update Pollution<br/>Category"]
            UC262["UC-262 Archive Category"]
            UC263["UC-263 Delete Category"]
            UC264["UC-264 Create Waste Tag"]
            UC265["UC-265 Update Waste Tag"]
            UC266["UC-266 Toggle Waste Tag"]
            UC267["UC-267 Delete Waste Tag"]
        end

        subgraph UC_CONFIG["Configuration"]
            UC270["UC-270 Manage Penalty<br/>Frameworks"]
            UC271["UC-271 Manage Gamification<br/>Config"]
            UC272["UC-272 Manage Blocked<br/>Words"]
            UC273["UC-273 Manage Notification<br/>Templates"]
        end

        subgraph UC_MODERATION["Content Moderation"]
            UC280["UC-280 Hide / Unhide<br/>Report"]
            UC281["UC-281 Force Update<br/>Report Status"]
            UC282["UC-282 View Spam<br/>Dashboard"]
        end

        subgraph UC_AUDIT["Audit & Reporting"]
            UC290["UC-290 View Audit Log"]
        end
    end

    Admin --- UC250
    Admin --- UC251
    Admin --- UC252
    Admin --- UC253
    Admin --- UC254
    Admin --- UC255
    Admin --- UC256
    Admin --- UC260
    Admin --- UC261
    Admin --- UC262
    Admin --- UC263
    Admin --- UC264
    Admin --- UC265
    Admin --- UC266
    Admin --- UC267
    Admin --- UC270
    Admin --- UC271
    Admin --- UC272
    Admin --- UC273
    Admin --- UC280
    Admin --- UC281
    Admin --- UC282
    Admin --- UC290
```

---

## UC-15: Dashboard & Analytics

```mermaid
flowchart LR
    Admin((⚫ Admin))
    CM((🟡 Company<br/>Manager))

    subgraph UC_DASH_ADMIN["📊 Admin Dashboard"]
        UC300["UC-300 Admin Overview"]
        UC301["UC-301 Report Trend"]
        UC302["UC-302 Status Distribution"]
        UC303["UC-303 Report Funnel"]
        UC304["UC-304 Geographic Analytics"]
        UC305["UC-305 Officer Performance"]
        UC306["UC-306 Company Performance"]
        UC307["UC-307 Pollution Analytics"]
        UC308["UC-308 Queue Aging"]
        UC309["UC-309 Resolution Distribution"]
        UC310["UC-310 Recent Activities"]
        UC311["UC-311 Admin Alerts"]
    end

    subgraph UC_DASH_CM["📊 Company Dashboard"]
        UC320["UC-320 Company Overview"]
        UC321["UC-321 Task Status"]
        UC322["UC-322 Team Performance"]
        UC323["UC-323 Staff Performance"]
        UC324["UC-324 Workload Trend"]
        UC325["UC-325 Upcoming Deadlines"]
        UC326["UC-326 Company Queue Aging"]
        UC327["UC-327 Company Recent<br/>Activities"]
    end

    Admin --- UC300
    Admin --- UC301
    Admin --- UC302
    Admin --- UC303
    Admin --- UC304
    Admin --- UC305
    Admin --- UC306
    Admin --- UC307
    Admin --- UC308
    Admin --- UC309
    Admin --- UC310
    Admin --- UC311

    CM --- UC320
    CM --- UC321
    CM --- UC322
    CM --- UC323
    CM --- UC324
    CM --- UC325
    CM --- UC326
    CM --- UC327
```

---

## UC-16: Media Upload

```mermaid
flowchart LR
    Citizen((🟢 Citizen))
    Cleaner((🟠 Cleaner))
    Inspector((🔴 Inspector))

    subgraph UC_MEDIA["📸 Media"]
        UC340["UC-340 Presign Media<br/>Upload (S3)"]
        UC341["UC-341 Upload Report Image"]
        UC342["UC-342 Upload Report Video"]
        UC343["UC-343 Upload Comment<br/>Image"]
    end

    Citizen --- UC341
    Citizen --- UC342
    Citizen --- UC343
    Cleaner --- UC340
    Inspector --- UC340
```

---

## Tổng hợp Use Cases theo Actor

| Actor                  | Use Cases (count) | Modules chính                                                                    |
| ---------------------- | :---------------: | -------------------------------------------------------------------------------- |
| 👤 **Guest**           |         8         | Auth (Register, Login, Google, Forgot, Reset, OTP), Map (Public View)            |
| 🟢 **Citizen**         |        38         | Report, Comment, Gamification, Community Cleanup, Map, Media, Notification       |
| 🔵 **LEO**             |        35         | Report Mgmt, Inspection, Community Cleanup, Team, Comment (mod), Organization    |
| 🟣 **DEO**             |        20         | Department, Company, Contract, Assignment                                        |
| 🟠 **Cleaner**         |        18         | Cleanup Task, Community Cleanup (Leader), Team                                   |
| 🔴 **Inspector**       |        22         | Inspection, Penalty, Violating Entity, Evidence                                  |
| 🟡 **Company Manager** |        23         | Company Staff, Company Team, Queue, Assignments, Dashboard                       |
| 🟤 **Company Staff**   |         8         | Cleanup Task (accept, progress, resolve)                                         |
| ⚫ **Admin**            |        35         | User CRUD, Catalog, Config, Content Moderation, Dashboard Analytics, Audit       |
| 🤖 **AI Service**      |         4         | Auto-classify, Duplicate Detection, Severity Estimation, Suspicious Flag         |
| **Tổng (unique)**      |      **~170**     |                                                                                  |

---

## So sánh v1.0 → v2.0

| Metric               | v1.0 (drawio) | v2.0            |
| --------------------- | :-----------: | :-------------: |
| Actors                |       8       | **9** (+Company Staff)    |
| Total use cases       |     ~120      | **~170** (+50)  |
| Community Cleanup UCs |       0       | **18** (NEW)    |
| Inspection UCs        |      12       | **28** (+16)    |
| Company Manager UCs   |       8       | **23** (+15)    |
| Dashboard/Analytics   |       0       | **20** (NEW)    |
| Comment Like          |       ❌      | ✅              |
| Reopen Request flow   |       1       | **3** (req/approve/reject) |
| Violation Recurrence  |       ❌      | **2** (NEW)     |
| Content Moderation    |       2       | **3** (+spam)   |
