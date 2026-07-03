# 🔐 Seed Accounts — GreenLens Development

> Auto-seeded khi chạy `dotnet run`. Idempotent — chạy lại không tạo duplicate.

## Admin Account

| Field    | Value                    |
| -------- | ------------------------ |
| Email    | `admin@greenlens.com.vn` |
| Password | `Admin@123456`           |
| Role     | `Admin`                  |

## DEO Accounts (34 tài khoản — 1 per Department/Province)

| Pattern     | Value                              |
| ----------- | ---------------------------------- |
| Email       | `deo.{provinceCode}@greenlens.dev` |
| Password    | `Officer@123`                      |
| Role        | `DEO`                              |
| Assigned to | Department của province tương ứng  |

### Danh sách DEO

| Email                  | Province    | Department          |
| ---------------------- | ----------- | ------------------- |
| `deo.01@greenlens.dev` | Hà Nội      | Sở TNMT Hà Nội      |
| `deo.04@greenlens.dev` | Cao Bằng    | Sở TNMT Cao Bằng    |
| `deo.08@greenlens.dev` | Tuyên Quang | Sở TNMT Tuyên Quang |
| `deo.11@greenlens.dev` | Điện Biên   | Sở TNMT Điện Biên   |
| `deo.12@greenlens.dev` | Lai Châu    | Sở TNMT Lai Châu    |
| `deo.14@greenlens.dev` | Sơn La      | Sở TNMT Sơn La      |
| `deo.15@greenlens.dev` | Lào Cai     | Sở TNMT Lào Cai     |
| `deo.19@greenlens.dev` | Thái Nguyên | Sở TNMT Thái Nguyên |
| `deo.20@greenlens.dev` | Lạng Sơn    | Sở TNMT Lạng Sơn    |
| `deo.22@greenlens.dev` | Quảng Ninh  | Sở TNMT Quảng Ninh  |
| `deo.24@greenlens.dev` | Bắc Ninh    | Sở TNMT Bắc Ninh    |
| `deo.25@greenlens.dev` | Phú Thọ     | Sở TNMT Phú Thọ     |
| `deo.31@greenlens.dev` | Hải Phòng   | Sở TNMT Hải Phòng   |
| `deo.33@greenlens.dev` | Hưng Yên    | Sở TNMT Hưng Yên    |
| `deo.37@greenlens.dev` | Ninh Bình   | Sở TNMT Ninh Bình   |
| `deo.38@greenlens.dev` | Thanh Hóa   | Sở TNMT Thanh Hóa   |
| `deo.40@greenlens.dev` | Nghệ An     | Sở TNMT Nghệ An     |
| `deo.42@greenlens.dev` | Hà Tĩnh     | Sở TNMT Hà Tĩnh     |
| `deo.44@greenlens.dev` | Quảng Trị   | Sở TNMT Quảng Trị   |
| `deo.46@greenlens.dev` | Huế         | Sở TNMT Huế         |
| `deo.48@greenlens.dev` | Đà Nẵng     | Sở TNMT Đà Nẵng     |
| `deo.51@greenlens.dev` | Quảng Ngãi  | Sở TNMT Quảng Ngãi  |
| `deo.52@greenlens.dev` | Gia Lai     | Sở TNMT Gia Lai     |
| `deo.56@greenlens.dev` | Khánh Hòa   | Sở TNMT Khánh Hòa   |
| `deo.66@greenlens.dev` | Đắk Lắk     | Sở TNMT Đắk Lắk     |
| `deo.68@greenlens.dev` | Lâm Đồng    | Sở TNMT Lâm Đồng    |
| `deo.75@greenlens.dev` | Đồng Nai    | Sở TNMT Đồng Nai    |
| `deo.79@greenlens.dev` | Hồ Chí Minh | Sở TNMT Hồ Chí Minh |
| `deo.80@greenlens.dev` | Tây Ninh    | Sở TNMT Tây Ninh    |
| `deo.82@greenlens.dev` | Đồng Tháp   | Sở TNMT Đồng Tháp   |
| `deo.86@greenlens.dev` | Vĩnh Long   | Sở TNMT Vĩnh Long   |
| `deo.91@greenlens.dev` | An Giang    | Sở TNMT An Giang    |
| `deo.92@greenlens.dev` | Cần Thơ     | Sở TNMT Cần Thơ     |
| `deo.96@greenlens.dev` | Cà Mau      | Sở TNMT Cà Mau      |

## LEO Accounts (3.321 tài khoản — 1 per LocalOffice/Ward)

| Pattern     | Value                          |
| ----------- | ------------------------------ |
| Email       | `leo.{wardCode}@greenlens.dev` |
| Password    | `Officer@123`                  |
| Role        | `LEO`                          |
| Assigned to | LocalOffice của ward tương ứng |

### Ví dụ LEO (Hà Nội — province 01)

| Email                     | Ward      | Office            |
| ------------------------- | --------- | ----------------- |
| `leo.00004@greenlens.dev` | Ba Đình   | VP MTĐT Ba Đình   |
| `leo.00008@greenlens.dev` | Ngọc Hà   | VP MTĐT Ngọc Hà   |
| `leo.00025@greenlens.dev` | Giảng Võ  | VP MTĐT Giảng Võ  |
| `leo.00070@greenlens.dev` | Hoàn Kiếm | VP MTĐT Hoàn Kiếm |

### Ví dụ LEO (Hồ Chí Minh — province 79)

| Email                     | Ward | Office           |
| ------------------------- | ---- | ---------------- |
| `leo.27145@greenlens.dev` | 1    | VP MTĐT Phường 1 |
| `leo.27148@greenlens.dev` | 2    | VP MTĐT Phường 2 |

## Mobile Demo Accounts (QA Inspector / CM / Cleaner / Citizen)

> Auto-seeded sau LEO seed. Ward demo: **27145** (TP.HCM Phường 1).  
> Password chung: **`Lualua123@`**

| Role | Email | Sample data |
|------|-------|-------------|
| Citizen | `citizen@greenlens.dev` | Reporter; `REP-MOB-RES001` (Resolved — test close/reopen) |
| Cleaner (leader) | `cleaner@greenlens.dev` | Community team; task `REP-MOB-CLN001` InProgress |
| Cleaner (member) | `cleaner.member@greenlens.dev` | Cùng team, không phải leader |
| Inspector (leader) | `inspector@greenlens.dev` | Inspection queue Draft `REP-MOB-INS001` |
| CompanyManager | `company@greenlens.dev` | Company **GreenLens Demo DVMT**; queue `REP-MOB-CQ001` |
| CompanyStaff (leader) | `staff@greenlens.dev` | Company team task `REP-MOB-TSK001` InProgress 40% |

> MK được **reset về `Lualua123@` mỗi lần** `dotnet run` (Development) cho các email trên.  
> Bản seed cũ (`*.mobile@greenlens.dev`) vẫn dùng được cùng MK sau khi restart API.

**Dev API base:** `http://localhost:5000/v1`

**API guides:** [`fe-inspection-api-guide.md`](./fe-inspection-api-guide.md), [`fe-company-manager-api-guide.md`](./fe-company-manager-api-guide.md)

## Quick Login (Swagger/Postman)

```json
{
  "email": "admin@greenlens.com.vn",
  "password": "Admin@123456"
}
```

```json
{
  "email": "deo.79@greenlens.dev",
  "password": "Officer@123"
}
```

```json
{
  "email": "leo.00004@greenlens.dev",
  "password": "Officer@123"
}
```

```json
{
  "email": "company@greenlens.dev",
  "password": "Lualua123@"
}
```

## Tóm tắt

| Role | Count | Email Pattern | Password |
|------|-------|---------------|----------|
| Admin | 1 | `admin@greenlens.com.vn` | `Admin@123456` |
| DEO | 34 | `deo.{provinceCode}@greenlens.dev` | `Officer@123` |
| LEO | 3,321 | `leo.{wardCode}@greenlens.dev` | `Officer@123` |
| Mobile demo | 6 | `*@greenlens.dev` (xem bảng trên) | `Lualua123@` |
| **Total** | **3,362+** | | |
| Role      | Count     | Email Pattern                      | Password       |
| --------- | --------- | ---------------------------------- | -------------- |
| Admin     | 1         | `admin@greenlens.com.vn`           | `Admin@123456` |
| DEO       | 34        | `deo.{provinceCode}@greenlens.dev` | `Officer@123`  |
| LEO       | 3,321     | `leo.{wardCode}@greenlens.dev`     | `Officer@123`  |
| **Total** | **3,356** |                                    |                |

> ⚠️ **Chỉ dùng cho Development/Staging.** Production KHÔNG seed accounts.
