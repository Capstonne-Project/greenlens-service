# GreenLens — Activity Diagrams (Luồng chính)

> **Dự án:** SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution
> **Chuẩn:** UML Activity Diagram với Swimlanes

---

## Các luồng chính của hệ thống

| #   | Luồng                           | Actors tham gia                                 | Mô tả                                                      |
| --- | ------------------------------- | ----------------------------------------------- | ---------------------------------------------------------- |
| 1   | **Gửi & Xử lý báo cáo ô nhiễm** | Citizen → System → LEO → Cleanup Team → Citizen | Luồng core — từ submit → verify → assign → resolve → close |
| 2   | **Đăng ký & Xác thực**          | Citizen → System                                | Register, login, refresh token, lockout                    |
| 3   | **Quản lý tổ chức**             | Admin → System → LEO                            | Tạo Department → Office → Invitation → Accept              |
| 4   | **Dispatch công ty DVMT**       | LEO → System → Company Manager → Company Team   | Báo cáo nghiêm trọng → dispatch cho CITENCO/công ty        |
| 5   | **Thanh tra xử phạt**           | LEO/Inspector → System                          | Tạo biên bản thanh tra song song với dọn dẹp               |
| 6   | **Gamification**                | System → Citizen                                | Tự động cấp điểm, badge, leaderboard                       |

---

## Ký hiệu UML Activity Diagram

| Ký hiệu             | Mermaid       | Ý nghĩa                             |
| ------------------- | ------------- | ----------------------------------- |
| ● (filled circle)   | `((●))`       | **Initial Node** — Điểm bắt đầu     |
| ◉ (bullseye)        | `((◉))`       | **Final Node** — Điểm kết thúc      |
| ▭ (rounded rect)    | `[Action]`    | **Action/Activity** — Hành động     |
| ◇ (diamond)         | `{Decision?}` | **Decision Node** — Rẽ nhánh        |
| ═ (thick bar)       | `===`         | **Fork/Join** — Tách/Gộp song song  |
| ║ (swimlane border) | `subgraph`    | **Swimlane** — Phân vùng actor      |
| → (arrow)           | `-->`         | **Control Flow** — Luồng điều khiển |
| ⎙ (note)            | `:::note`     | **Note** — Ghi chú bổ sung          |

---

## 1. 📋 Gửi & Xử lý báo cáo ô nhiễm (Main Flow)

> Luồng cốt lõi nhất — từ khi Citizen submit đến khi report được Close.

```mermaid
flowchart TD
    Start((●))

    subgraph Citizen["👤 Citizen"]
        A1["Chụp ảnh ô nhiễm"]
        A2["Nhập mô tả + chọn danh mục"]
        A3["Gửi GPS tự động"]
        A4["Submit báo cáo"]
        A12["Xem trạng thái báo cáo"]
        A13{"Hài lòng với kết quả?"}
        A14["Xác nhận đóng báo cáo"]
        A15["Phản hồi không hài lòng"]
    end

    subgraph System["⚙️ System"]
        S1["Validate dữ liệu đầu vào"]
        S2{"Dữ liệu hợp lệ?"}
        S3["Reverse Geocode GPS → Ward"]
        S4{"Ward có Office onboard?"}
        S5["Route tới LocalOffice"]
        S6["Route tới Department Queue"]
        S7["Lưu report - Status: Submitted"]
        S8["AI phân loại ảnh"]
        S9["Gửi notification cho LEO"]
        S14["Auto-close sau 7 ngày"]
    end

    subgraph LEO["👮 LEO (Phường/Xã)"]
        L1["Xem báo cáo trong queue"]
        L2["Xác minh thực địa"]
        L3{"Quyết định?"}
        L4["Verify - xác nhận severity + category"]
        L5["Reject - ghi lý do ≥20 ký tự"]
        L6{"Tuyến cấp TP?"}
        L7["Escalate lên DEO"]
        L8["Phân công Team xử lý"]
    end

    subgraph CleanupTeam["🧹 Cleanup Team"]
        C1["Nhận phân công"]
        C2["Check-in GPS tại hiện trường"]
        C3["Thực hiện dọn dẹp"]
        C4["Cập nhật tiến độ + ảnh"]
        C5["Upload ảnh After"]
        C6["Báo cáo hoàn thành - Resolve"]
    end

    End((◉))
    EndReject((◉))

    Start --> A1
    A1 --> A2 --> A3 --> A4
    A4 --> S1

    S1 --> S2
    S2 -->|"Không hợp lệ"| EndReject
    S2 -->|"Hợp lệ"| S3

    S3 --> S4
    S4 -->|"Có"| S5
    S4 -->|"Chưa onboard"| S6
    S5 --> S7
    S6 --> S7

    S7 --> S8
    S7 --> S9

    S9 --> L1
    L1 --> L2 --> L3

    L3 -->|"Reject"| L5
    L5 --> S6
    L3 -->|"Verify"| L4
    L4 --> L6

    L6 -->|"Có"| L7
    L7 --> S6
    L6 -->|"Không"| L8

    L8 --> C1
    C1 --> C2 --> C3 --> C4 --> C5 --> C6

    C6 --> A12
    A12 --> A13
    A13 -->|"Có"| A14
    A14 --> End
    A13 -->|"Không"| A15
    A15 --> End

    C6 --> S14
    S14 -->|"Citizen không xác nhận"| End

    classDef startEnd fill:#1a1a2e,stroke:#16213e,color:#fff
    classDef action fill:#4A90D9,stroke:#2C5F8A,color:#fff
    classDef decision fill:#F5A623,stroke:#D48B0A,color:#fff
    classDef system fill:#95A5A6,stroke:#7F8C8D,color:#fff
    classDef leo fill:#E67E22,stroke:#D35400,color:#fff
    classDef cleanup fill:#27AE60,stroke:#229954,color:#fff

    class Start,End,EndReject startEnd
    class A1,A2,A3,A4,A12,A14,A15 action
    class S1,S3,S5,S6,S7,S8,S9,S14 system
    class L1,L2,L4,L5,L7,L8 leo
    class C1,C2,C3,C4,C5,C6 cleanup
    class S2,S4,A13,L3,L6 decision
```

---

## 2. 🔐 Đăng ký & Xác thực (Authentication Flow)

```mermaid
flowchart TD
    Start((●))

    subgraph Citizen["👤 Citizen"]
        A1["Mở app / trang đăng ký"]
        A2["Nhập email + mật khẩu + họ tên"]
        A3["Nhập email + mật khẩu đăng nhập"]
        A8["Sử dụng hệ thống"]
    end

    subgraph System["⚙️ System"]
        S1["Validate input"]
        S2{"Email đã tồn tại?"}
        S3["Hash password (bcrypt ≥12)"]
        S4["Tạo User - Role: Citizen"]
        S5["Gửi OTP qua email"]
        S6["Xác minh OTP"]
        S7{"OTP đúng?"}
        S8["Kích hoạt tài khoản"]
        S9["Tìm User theo email"]
        S10{"Tài khoản tồn tại?"}
        S11{"Tài khoản bị ban?"}
        S12{"Tài khoản bị khóa?"}
        S13["Kiểm tra mật khẩu"]
        S14{"Mật khẩu đúng?"}
        S15["Tăng FailedLoginCount"]
        S16{"≥5 lần sai trong 15 phút?"}
        S17["Khóa tài khoản 30 phút"]
        S18["Tạo JWT Access Token 24h"]
        S19["Tạo Refresh Token 30d"]
        S20["Trả về tokens"]
    end

    End((◉))
    EndErr((◉))

    Start --> A1

    %% Register flow
    A1 -->|"Đăng ký"| A2
    A2 --> S1 --> S2
    S2 -->|"Có"| EndErr
    S2 -->|"Không"| S3
    S3 --> S4 --> S5 --> S6 --> S7
    S7 -->|"Sai"| EndErr
    S7 -->|"Đúng"| S8 --> End

    %% Login flow
    A1 -->|"Đăng nhập"| A3
    A3 --> S9 --> S10
    S10 -->|"Không"| EndErr
    S10 -->|"Có"| S11
    S11 -->|"Bị ban"| EndErr
    S11 -->|"Không"| S12
    S12 -->|"Đang khóa"| EndErr
    S12 -->|"Không"| S13 --> S14
    S14 -->|"Sai"| S15 --> S16
    S16 -->|"Có"| S17 --> EndErr
    S16 -->|"Chưa"| EndErr
    S14 -->|"Đúng"| S18 --> S19 --> S20 --> A8 --> End

    classDef startEnd fill:#1a1a2e,stroke:#16213e,color:#fff
    classDef action fill:#3498DB,stroke:#2980B9,color:#fff
    classDef decision fill:#F5A623,stroke:#D48B0A,color:#fff
    classDef system fill:#95A5A6,stroke:#7F8C8D,color:#fff

    class Start,End,EndErr startEnd
    class A1,A2,A3,A8 action
    class S2,S7,S10,S11,S12,S14,S16 decision
    class S1,S3,S4,S5,S6,S8,S9,S13,S15,S17,S18,S19,S20 system
```

---

## 3. 🏢 Quản lý tổ chức & Invitation Flow

```mermaid
flowchart TD
    Start((●))

    subgraph Admin["🔑 Admin"]
        AD1["Tạo Department cho tỉnh"]
        AD2["Tạo LocalOffice cho phường"]
        AD3["Gán DEO cho Department"]
    end

    subgraph System["⚙️ System"]
        S1{"Tỉnh đã có Department?"}
        S2["Lưu Department"]
        S3{"Ward đã có Office?"}
        S4["Lưu LocalOffice"]
        S5["Gán DEO vào Department"]
        S6{"User có role DEO?"}
        S7["Tạo StaffInvitation 7 ngày"]
        S8["Gán role + office + team"]
        S9["Giữ role Citizen"]
        S10{"Invitation hết hạn?"}
        S11{"Đã respond?"}
        S12["Clear role → Citizen"]
        S13["Xoá khỏi tất cả teams"]
    end

    subgraph LEO["👮 LEO"]
        L1["Tìm Citizen qua email"]
        L2["Chọn role + team"]
        L3["Gửi invitation"]
        L4["Release nhân sự"]
    end

    subgraph CitizenActor["👤 Citizen"]
        C1["Nhận notification invitation"]
        C2["Xem chi tiết lời mời"]
        C3{"Quyết định?"}
        C4["Accept — chấp nhận"]
        C5["Decline — từ chối"]
    end

    End((◉))

    Start --> AD1
    AD1 --> S1
    S1 -->|"Có"| End
    S1 -->|"Chưa"| S2 --> AD2
    AD2 --> S3
    S3 -->|"Có"| End
    S3 -->|"Chưa"| S4 --> AD3
    AD3 --> S6
    S6 -->|"Không"| End
    S6 -->|"Có"| S5

    S5 --> L1 --> L2 --> L3 --> S7

    S7 --> C1 --> C2 --> S10
    S10 -->|"Có"| End
    S10 -->|"Chưa"| S11
    S11 -->|"Rồi"| End
    S11 -->|"Chưa"| C3

    C3 -->|"Accept"| C4 --> S8 --> End
    C3 -->|"Decline"| C5 --> S9 --> End

    L4 --> S12 --> S13 --> End

    classDef startEnd fill:#1a1a2e,stroke:#16213e,color:#fff
    classDef admin fill:#8E44AD,stroke:#7D3C98,color:#fff
    classDef system fill:#95A5A6,stroke:#7F8C8D,color:#fff
    classDef leo fill:#E67E22,stroke:#D35400,color:#fff
    classDef citizen fill:#3498DB,stroke:#2980B9,color:#fff
    classDef decision fill:#F5A623,stroke:#D48B0A,color:#fff

    class Start,End startEnd
    class AD1,AD2,AD3 admin
    class L1,L2,L3,L4 leo
    class C1,C2,C4,C5 citizen
    class S1,S3,S6,S10,S11,C3 decision
    class S2,S4,S5,S7,S8,S9,S12,S13 system
```

---

## 4. 🏭 Dispatch công ty DVMT (CITENCO Flow)

```mermaid
flowchart TD
    Start((●))

    subgraph LEO["👮 LEO"]
        L1["Xem report đã Verified"]
        L2{"Cần công ty xử lý?"}
        L3["Chọn công ty từ danh sách my-ward"]
        L4["Dispatch tới công ty"]
    end

    subgraph System["⚙️ System"]
        S1["Lọc công ty Active phục vụ ward"]
        S2["Tạo CompanyAssignment"]
        S3["Gửi notification cho CM"]
    end

    subgraph CM["🏭 Company Manager"]
        CM1["Nhận thông báo dispatch"]
        CM2["Xem chi tiết report"]
        CM3["Chọn Company Team phân công"]
        CM4["Phân công team"]
    end

    subgraph CompanyTeam["🧹 Company Team"]
        CT1["Nhận phân công"]
        CT2["Check-in GPS hiện trường"]
        CT3["Dọn dẹp + upload ảnh"]
        CT4["Resolve report"]
    end

    End((◉))

    Start --> L1 --> L2
    L2 -->|"Không — dùng team nội bộ"| End
    L2 -->|"Có"| S1
    S1 --> L3 --> L4 --> S2 --> S3

    S3 --> CM1 --> CM2 --> CM3 --> CM4

    CM4 --> CT1 --> CT2 --> CT3 --> CT4 --> End

    classDef startEnd fill:#1a1a2e,stroke:#16213e,color:#fff
    classDef leo fill:#E67E22,stroke:#D35400,color:#fff
    classDef system fill:#95A5A6,stroke:#7F8C8D,color:#fff
    classDef cm fill:#8E44AD,stroke:#7D3C98,color:#fff
    classDef ct fill:#27AE60,stroke:#229954,color:#fff
    classDef decision fill:#F5A623,stroke:#D48B0A,color:#fff

    class Start,End startEnd
    class L1,L3,L4 leo
    class L2 decision
    class S1,S2,S3 system
    class CM1,CM2,CM3,CM4 cm
    class CT1,CT2,CT3,CT4 ct
```

---

## 5. 🔍 Thanh tra xử phạt (Inspection Flow)

> Luồng song song với dọn dẹp — LEO/Inspector tạo biên bản thanh tra.

```mermaid
flowchart TD
    Start((●))

    subgraph LEO["👮 LEO / Inspector"]
        L1["Xem report đã Verified"]
        L2{"Vi phạm cần xử phạt?"}
        L3["Tạo biên bản thanh tra"]
        L4["Nhập thông tin vi phạm"]
        L5["Nhập thông tin người vi phạm"]
        L6["Chỉ định team thanh tra"]
    end

    subgraph System["⚙️ System"]
        S1["Validate dữ liệu biên bản"]
        S2["Lưu InspectionReport - Status: Pending"]
        S3["Gửi notification cho team"]
    end

    subgraph Team["🔍 Inspection Team"]
        T1["Nhận phân công thanh tra"]
        T2["Đến hiện trường"]
        T3["Ghi nhận vi phạm thực tế"]
        T4["Cập nhật biên bản"]
        T5{"Cần phạt?"}
        T6["Lập quyết định xử phạt"]
        T7["Đóng biên bản - không phạt"]
    end

    End((◉))

    Start --> L1 --> L2
    L2 -->|"Không"| End
    L2 -->|"Có"| L3 --> L4 --> L5 --> L6

    L6 --> S1 --> S2 --> S3

    S3 --> T1 --> T2 --> T3 --> T4 --> T5
    T5 -->|"Có"| T6 --> End
    T5 -->|"Không"| T7 --> End

    classDef startEnd fill:#1a1a2e,stroke:#16213e,color:#fff
    classDef leo fill:#E67E22,stroke:#D35400,color:#fff
    classDef system fill:#95A5A6,stroke:#7F8C8D,color:#fff
    classDef team fill:#2ECC71,stroke:#229954,color:#fff
    classDef decision fill:#F5A623,stroke:#D48B0A,color:#fff

    class Start,End startEnd
    class L1,L3,L4,L5,L6 leo
    class L2,T5 decision
    class S1,S2,S3 system
    class T1,T2,T3,T4,T6,T7 team
```

---

## 6. 🎮 Gamification (Tự động cấp điểm & badge)

```mermaid
flowchart TD
    Start((●))

    subgraph Trigger["🔔 Domain Event Trigger"]
        E1["ReportVerifiedEvent"]
        E2["ReportResolvedEvent"]
        E3["ReportClosedEvent"]
    end

    subgraph System["⚙️ Gamification Engine"]
        S1["Nhận DomainEvent"]
        S2["Tính điểm theo rule"]
        S3["Cộng điểm vào UserPoints"]
        S4["Ghi PointTransaction"]
        S5{"Đạt mốc badge?"}
        S6["Cấp Badge tự động"]
        S7["Ghi UserBadge"]
        S8["Gửi notification badge mới"]
        S9{"Đủ điểm lên level?"}
        S10["Cập nhật Level"]
        S11{"Đạt mốc Level badge?"}
        S12["Cấp Level Badge"]
    end

    subgraph Background["⏰ Background Job"]
        B1["LeaderboardSnapshotJob chạy Daily"]
        B2["Snapshot top users theo period"]
        B3["Lưu LeaderboardEntry"]
    end

    End((◉))

    Start --> E1 & E2 & E3
    E1 & E2 & E3 --> S1
    S1 --> S2 --> S3 --> S4

    S4 --> S5
    S5 -->|"Có"| S6 --> S7 --> S8
    S5 -->|"Chưa"| S9

    S8 --> S9
    S9 -->|"Có"| S10 --> S11
    S9 -->|"Chưa"| End

    S11 -->|"Có"| S12 --> End
    S11 -->|"Chưa"| End

    B1 --> B2 --> B3 --> End

    classDef startEnd fill:#1a1a2e,stroke:#16213e,color:#fff
    classDef trigger fill:#E74C3C,stroke:#C0392B,color:#fff
    classDef system fill:#9B59B6,stroke:#7D3C98,color:#fff
    classDef bg fill:#34495E,stroke:#2C3E50,color:#fff
    classDef decision fill:#F5A623,stroke:#D48B0A,color:#fff

    class Start,End startEnd
    class E1,E2,E3 trigger
    class S1,S2,S3,S4,S6,S7,S8,S10,S12 system
    class S5,S9,S11 decision
    class B1,B2,B3 bg
```

---

## Tổng hợp Coverage

| Luồng                        | Actors   | Decision Points | Ký hiệu UML sử dụng                                   |
| ---------------------------- | -------- | :-------------: | ----------------------------------------------------- |
| 1. Submit & Process Report   | 4 actors |        6        | Initial, Final, Action, Decision, Swimlane            |
| 2. Authentication            | 2 actors |        7        | Initial, Final, Action, Decision, Swimlane            |
| 3. Organization & Invitation | 4 actors |        5        | Initial, Final, Action, Decision, Swimlane            |
| 4. Company Dispatch          | 4 actors |        1        | Initial, Final, Action, Decision, Swimlane            |
| 5. Inspection                | 3 actors |        2        | Initial, Final, Action, Decision, Swimlane            |
| 6. Gamification              | 2 actors |        3        | Initial, Final, Action, Decision, Fork/Join, Swimlane |
