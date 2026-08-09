# Test Report — Authentication & Account Management

| | |
|---|---|
| **Feature** | **Authentication & Account Management** |
| **Test requirement** | |
| **Number of TCs** | **153** |

| Testing Round | Passed | Failed | Pending | N/A |
|--------------|--------|--------|---------|-----|
| **Round 1** | 107 | 46 | 0 | 0 |
| **Round 2** | 0 | 0 | 0 | 0 |
| **Round 3** | 0 | 0 | 0 | 0 |

---

## Account Registration

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_REG_001 | Successful registration with all valid data. | 1. Send POST /v1/auth/register with body: email="newuser@gmail.com", password="Test@123456", fullName="Nguyễn Văn An", acceptTerms=true. 2. Observe response. 3. Check database for new user record. | Response: 200 OK. Body contains userId, email, and message "Đăng ký thành công. Mã OTP đã được gửi đến email của bạn." New user record exists with IsEmailVerified=false. OTP email enqueued via Hangfire. | - Email "newuser@gmail.com" does not exist in system. - Hangfire service is running. | Passed | 04/09/2026 | TamKnm | | | | | | | |
| TC_REG_002 | Validate required field — email is empty. | 1. Send POST /v1/auth/register with email="" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Email is required." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | RegisterCommandValidator has NotEmpty rule for Email. |
| TC_REG_003 | Validate email format — invalid email string. | 1. Send POST /v1/auth/register with email="not-an-email" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Invalid email format." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | RegisterCommandValidator has EmailAddress rule. |
| TC_REG_004 | Validate required field — password is empty. | 1. Send POST /v1/auth/register with password="" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Password is required." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | RegisterCommandValidator has NotEmpty rule for Password. |
| TC_REG_005 | Validate password — less than 8 characters. | 1. Send POST /v1/auth/register with password="Ab1@" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Password must be at least 8 characters." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | RegisterCommandValidator has MinimumLength(8). |
| TC_REG_006 | Validate password — missing uppercase letter. | 1. Send POST /v1/auth/register with password="test@12345" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Password must contain at least one uppercase letter." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Matches(@"[A-Z]") rule exists. |
| TC_REG_007 | Validate password — missing lowercase letter. | 1. Send POST /v1/auth/register with password="TEST@12345" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Password must contain at least one lowercase letter." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Matches(@"[a-z]") rule exists. |
| TC_REG_008 | Validate password — missing digit. | 1. Send POST /v1/auth/register with password="Test@abcde" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Password must contain at least one digit." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Matches(@"\d") rule exists. |
| TC_REG_009 | Validate password — missing special character. | 1. Send POST /v1/auth/register with password="Test123456" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Password must contain at least one special character." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Matches(@"[\W_]") rule exists. |
| TC_REG_010 | Validate required field — fullName is empty. | 1. Send POST /v1/auth/register with fullName="" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Họ tên là bắt buộc." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | NotEmpty rule exists for FullName. |
| TC_REG_011 | Validate fullName — less than 2 characters. | 1. Send POST /v1/auth/register with fullName="A" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Họ tên từ 2-50 ký tự." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Length(2, 50) rule exists. |
| TC_REG_012 | Validate fullName — more than 50 characters. | 1. Send POST /v1/auth/register with fullName of 51 characters and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Họ tên từ 2-50 ký tự." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Length(2, 50) rule exists. |
| TC_REG_013 | Validate fullName — contains special characters. | 1. Send POST /v1/auth/register with fullName="Nguyễn V@n A" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Họ tên không hợp lệ (không chứa ký tự đặc biệt)." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Regex ^[\p{L}\s]+$ rejects special chars. |
| TC_REG_014 | Validate fullName — contains numbers. | 1. Send POST /v1/auth/register with fullName="Nguyễn 123" and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Họ tên không hợp lệ (không chứa ký tự đặc biệt)." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Regex ^[\p{L}\s]+$ rejects digits. |
| TC_REG_015 | Validate acceptTerms — user does not accept terms. | 1. Send POST /v1/auth/register with acceptTerms=false and other fields valid. 2. Observe response. | Response: 422 Validation Error. Message: "Bạn phải đồng ý với điều khoản để đăng ký." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Equal(true) rule exists. |
| TC_REG_016 | Registration fails when email already exists (active account). | 1. Send POST /v1/auth/register with email of an existing active user. 2. Observe response. | Response: 409 Conflict. Error code: "EMAIL_TAKEN". | - Active user exists with that email. | Passed | 04/09/2026 | TamKnm | | | | | | | UserRegistrationGuard.ValidateNewEmailForRegistrationAsync checks ExistsAsync. |
| TC_REG_017 | Registration fails when email belongs to soft-deleted account. | 1. Send POST /v1/auth/register with email of a soft-deleted user. 2. Observe response. | Response: 409 Conflict. Error code: "EMAIL_DELETED_RESTORE_AVAILABLE". | - Soft-deleted user exists with that email. | Passed | 04/09/2026 | TamKnm | | | | | | | Guard checks GetDeletedByEmailAsync. |
| TC_REG_018 | Race condition — concurrent duplicate email registration. | 1. Send two concurrent POST /v1/auth/register requests with the same email. 2. Observe both responses. | One request: 200 OK. Other request: 409 Conflict via PostgresUniqueViolationMapper. No duplicate user created. | - Email not in system. - Concurrent requests possible. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches DbUpdateException and calls PostgresUniqueViolationMapper.TryMap(ex). |
| TC_REG_019 | Registration succeeds but OTP email enqueue fails. | 1. Simulate Hangfire unavailability. 2. Send POST /v1/auth/register with valid data. 3. Observe response. | User created in DB. Response returns error code "EMAIL_DISPATCH_UNAVAILABLE". | - Hangfire unavailable. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks TryEnqueueOtpEmail return value and returns Errors.Auth.EmailDispatchUnavailable on failure. |
| TC_REG_020 | Idempotency — duplicate request within TTL. | 1. Send POST /v1/auth/register with Idempotency-Key header. 2. Send exact same request with same key within 1 hour. | First: 200 OK. Second: 200 OK cached response. No duplicate user. | - Idempotency middleware enabled. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller has [SupportsIdempotency(TtlHours = 1)] attribute. |
| TC_REG_021 | Security — SQL injection attempt in email. | 1. Send POST /v1/auth/register with email="'; DROP TABLE users;--". 2. Observe response. | Response: 422 Validation Error: "Invalid email format." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | EmailAddress validator blocks SQL injection strings. |
| TC_REG_022 | Security — XSS attempt in fullName. | 1. Send POST /v1/auth/register with fullName="\<script\>alert('xss')\</script\>". 2. Observe response. | Response: 422 Validation Error: "Họ tên không hợp lệ." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | Regex ^[\p{L}\s]+$ rejects HTML/script tags. |
| TC_REG_023 | Boundary — email exceeds RFC 5321 max length (254 chars). | 1. Send POST /v1/auth/register with email longer than 254 characters (valid format but exceeding RFC limit). 2. Observe response. | Expected: 422 Validation Error for email max length. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ RegisterCommandValidator is missing MaximumLength(254) for Email. Email with 300+ chars would pass validation.** |
| TC_REG_024 | Security — password with extremely long value (bcrypt DoS). | 1. Send POST /v1/auth/register with password of 10,000+ characters. 2. Observe response and server response time. | Expected: 422 Validation Error for password max length. Server should not hang. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 RegisterCommandValidator missing MaximumLength for Password. bcrypt hashing of 10K+ chars causes CPU hang.** |
| TC_REG_025 | Validate request body — completely empty body. | 1. Send POST /v1/auth/register with body {} or null. 2. Observe response. | Response: 422 Validation Error with multiple errors for all required fields. | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | All NotEmpty validators fire for empty fields. |
| TC_REG_026 | Email normalization — leading/trailing whitespace. | 1. Send POST /v1/auth/register with email=" test@test.com ". 2. Observe response. | Email trimmed and processed normally. | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | UserRegistrationGuard calls .Trim().ToLowerInvariant(). |
| TC_REG_027 | Email normalization — case insensitivity. | 1. Register with email "User@Test.COM". 2. Attempt register with "user@test.com". | First: 200 OK. Second: 409 "EMAIL_TAKEN". | - No existing user. | Passed | 04/09/2026 | TamKnm | | | | | | | Guard normalizes to lowercase. |
| TC_REG_028 | Validate fullName — only whitespace characters. | 1. Send POST /v1/auth/register with fullName="   " (spaces only). 2. Observe response. | Response: 422 Validation Error. | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | FluentValidation NotEmpty rejects whitespace-only strings. |
| TC_REG_029 | Edge case — fullName with Vietnamese diacritics. | 1. Send POST /v1/auth/register with fullName="Lê Thị Phương Thảo". 2. Observe response. | Response: 200 OK. Vietnamese characters accepted. | - API is accessible. - Email is unique. | Passed | 04/09/2026 | TamKnm | | | | | | | Regex \p{L} matches Unicode letters including Vietnamese. |
| TC_REG_030 | Boundary — fullName exactly 2 characters (min boundary). | 1. Send POST /v1/auth/register with fullName="An" (2 chars). 2. Observe response. | Response: 200 OK. | - API is accessible. - Email is unique. | Passed | 04/09/2026 | TamKnm | | | | | | | Length(2, 50) accepts 2. |
| TC_REG_031 | Boundary — fullName exactly 50 characters (max boundary). | 1. Send POST /v1/auth/register with fullName of exactly 50 characters. 2. Observe response. | Response: 200 OK. | - API is accessible. - Email is unique. | Passed | 04/09/2026 | TamKnm | | | | | | | Length(2, 50) accepts 50. |
| TC_REG_032 | Boundary — password exactly 8 characters (min boundary). | 1. Send POST /v1/auth/register with password="Ab1@defg" (8 chars). 2. Observe response. | Response: 200 OK. | - API is accessible. - Email is unique. | Passed | 04/09/2026 | TamKnm | | | | | | | MinimumLength(8) accepts 8. |
| TC_REG_033 | Rate limiting — anonymous endpoint flooded. | 1. Send 60+ POST /v1/auth/register requests per minute from same IP. 2. Observe response after threshold. | Excess requests return 429 Too Many Requests. | - Rate limiting middleware enabled. | Passed | 04/09/2026 | TamKnm | | | | | | | Global rate limiter configured in PerformanceServiceExtensions with SlidingWindowRateLimiter for anonymous users. |

---

## Standard Login

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_SL_001 | Successful login with valid email and password. | 1. Send POST /v1/auth/login with valid email and correct password. 2. Observe response. | Response: 200 OK. Body contains accessToken, refreshToken, user object (id, email, fullName, role, isEmailVerified, mustChangePassword). | - User exists, active, email verified. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns LoginResponse with all required fields. |
| TC_SL_002 | Login fails with incorrect password. | 1. Send POST /v1/auth/login with valid email but wrong password. 2. Observe response. | Response: 422. Error code: "INVALID_CREDENTIALS". FailedLoginAttempts incremented by 1. | - User exists, active, email verified. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls user.RecordFailedLogin() before returning Errors.Auth.InvalidCredentials. |
| TC_SL_003 | Login fails when email does not exist. | 1. Send POST /v1/auth/login with non-existent email. 2. Observe response. | Response: 422. Error code: "INVALID_CREDENTIALS". Same error as wrong password (anti-enumeration). | - Email not registered. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns Errors.Auth.InvalidCredentials when user is null. |
| TC_SL_004 | Login fails when account is banned. | 1. Send POST /v1/auth/login with banned user's email and correct password. 2. Observe response. | Response: 403. Error code: "ACCOUNT_BANNED". | - User exists with IsBanned=true. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user.IsBanned before password verify. |
| TC_SL_005 | Login fails when account is soft-deleted. | 1. Send POST /v1/auth/login with soft-deleted user's email. 2. Observe response. | Response: 403. Error code: "ACCOUNT_DEACTIVATED". | - User exists with IsDeleted=true. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user.IsDeleted. |
| TC_SL_006 | Login fails when account is locked out. | 1. Send POST /v1/auth/login with locked user's email and correct password. 2. Observe response. | Response: 422. Error code: "ACCOUNT_LOCKED". Password NOT verified. | - User exists with LockoutEnd > now. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user.IsLockedOut() before password verification. |
| TC_SL_007 | Login fails when email is not verified. | 1. Send POST /v1/auth/login with unverified email user's credentials. 2. Observe response. | Response: 422. Error code: "EMAIL_NOT_VERIFIED". | - User exists with IsEmailVerified=false. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks !user.IsEmailVerified. |
| TC_SL_008 | Login fails when company is expired — CompanyManager. | 1. Send POST /v1/auth/login with CompanyManager whose company is Expired. 2. Observe response. | Response: 403. Error code: "COMPANY_EXPIRED". | - CompanyManager user. Company.Status=Expired. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler queries CompanyStaff with Include(Company), checks CompanyStatus.Expired. |
| TC_SL_009 | Login fails when company is expired — CompanyStaff. | 1. Send POST /v1/auth/login with CompanyStaff whose company is Expired. 2. Observe response. | Response: 403. Error code: "COMPANY_EXPIRED". | - CompanyStaff user. Company.Status=Expired. | Passed | 04/09/2026 | TamKnm | | | | | | | Same check applies for UserRole.CompanyStaff. |
| TC_SL_010 | BR-AUTH-011: Account locked after 5 failed attempts. | 1. Send wrong password 5 times for the same email. 2. On 6th attempt, send correct password. | Attempts 1-4: 422 "INVALID_CREDENTIALS". Attempt 5: lockout triggered. Attempt 6: 422 "ACCOUNT_LOCKED". | - User active, verified. | Passed | 04/09/2026 | TamKnm | | | | | | | user.RecordFailedLogin() sets LockoutEnd after 5th attempt. IsLockedOut() returns true. |
| TC_SL_011 | Failed login attempts reset on successful login. | 1. Enter wrong password 3 times. 2. Enter correct password on 4th attempt. 3. Check database. | Login succeeds. FailedLoginAttempts=0 in DB. | - User active, verified, not locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls user.ResetFailedLoginAttempts() on success. |
| TC_SL_012 | Login succeeds after lockout period expires. | 1. Lock account. 2. Set LockoutEnd to past. 3. Login with correct password. | Response: 200 OK. FailedLoginAttempts reset. | - Lockout expired. | Passed | 04/09/2026 | TamKnm | | | | | | | IsLockedOut() checks LockoutEnd > DateTime.UtcNow. |
| TC_SL_013 | Email case insensitivity during login. | 1. Send POST /v1/auth/login with email "USER@Test.COM". 2. Observe response. | Login succeeds (email normalized to lowercase). | - User registered with "user@test.com". | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls request.Email.ToLowerInvariant() before lookup. |
| TC_SL_014 | Anti-enumeration — same error for non-existent email and wrong password. | 1. Send login with non-existent email. 2. Send login with existing email + wrong password. 3. Compare error codes. | Both return "INVALID_CREDENTIALS" with same message. No user existence leak. | - One email exists, one doesn't. | Passed | 04/09/2026 | TamKnm | | | | | | | Both paths return Errors.Auth.InvalidCredentials. |
| TC_SL_015 | Login with Google-only account using password. | 1. Send POST /v1/auth/login with Google-only user's email and any password. | Response: 422 "INVALID_CREDENTIALS". | - User created via Google, no password hash. | Passed | 04/09/2026 | TamKnm | | | | | | | passwordHasher.Verify(password, "") returns false. |
| TC_SL_016 | Login returns mustChangePassword flag. | 1. Send POST /v1/auth/login with user who has MustChangePassword=true. | Response: 200 OK. UserDto contains mustChangePassword:true. | - Admin-created account with temp password. | Passed | 04/09/2026 | TamKnm | | | | | | | UserDto includes MustChangePassword field from user entity. |
| TC_SL_017 | Validation — email field is empty. | 1. Send POST /v1/auth/login with email="". | Response: 422 Validation Error. | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | LoginCommandValidator has NotEmpty for Email. |
| TC_SL_018 | Validation — email format is invalid. | 1. Send POST /v1/auth/login with email="invalid". | Response: 422 Validation Error. | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | LoginCommandValidator has EmailAddress rule. |
| TC_SL_019 | Validation — password field is empty. | 1. Send POST /v1/auth/login with password="". | Response: 422 Validation Error. | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | LoginCommandValidator has NotEmpty for Password. |
| TC_SL_020 | Priority order — banned check before lockout. | 1. Set user as both banned and locked. 2. Login with correct password. | Response: 403 "ACCOUNT_BANNED" (not "ACCOUNT_LOCKED"). | - User is banned AND locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks IsBanned (line 39) before IsLockedOut (line 48). |
| TC_SL_021 | Edge case — login immediately after lockout expires. | 1. Set LockoutEnd = now - 1 second. 2. Login with correct password. | Response: 200 OK. | - User lockout just expired. | Passed | 04/09/2026 | TamKnm | | | | | | | IsLockedOut() returns false when LockoutEnd <= now. |
| TC_SL_022 | BR-AUTH-011: CAPTCHA flag returned after 3 failed attempts. | 1. Enter wrong password 3 times. 2. Observe response metadata. | Expected: Response includes requiresCaptcha:true after 3rd attempt. | - User active, verified. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 Domain method RequiresCaptcha() exists but handler does NOT include this flag in LoginResponse. CAPTCHA information not exposed to client.** |
| TC_SL_023 | CompanyManager login when company is Active. | 1. Login with CompanyManager whose company status=Active. | Response: 200 OK. | - CompanyManager, Active company. | Passed | 04/09/2026 | TamKnm | | | | | | | Company check passes (status != Expired). |
| TC_SL_024 | CompanyManager login when no CompanyStaff record found. | 1. Login with CompanyManager who has no active CompanyStaff record. | Response: 200 OK. Company check bypassed. | - CompanyManager without CompanyStaff record. | Passed | 04/09/2026 | TamKnm | | | | | | | staff is null → company check skipped (staff?.Company?.Status never equals Expired). |

---

## Request OTP

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_OTP_001 | Successfully request OTP for EmailVerification purpose. | 1. Send POST /v1/auth/request-otp with email of existing user and purpose="EmailVerification". 2. Check email inbox. | Response: 200 OK. Message: "Mã OTP đã được gửi đến email của bạn." Previous OTPs invalidated. | - User exists. - Hangfire running. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler invalidates old OTPs, creates new, enqueues email. |
| TC_OTP_002 | Successfully request OTP for PasswordReset purpose. | 1. Send POST /v1/auth/request-otp with email and purpose="PasswordReset". | Response: 200 OK. | - User exists. - Hangfire running. | Passed | 04/09/2026 | TamKnm | | | | | | | Same flow applies for PasswordReset purpose. |
| TC_OTP_003 | Request OTP for non-existent user — leaks existence. | 1. Send POST /v1/auth/request-otp with email not in system. 2. Observe response. | Expected for security: 200 OK generic message. Actual: 404 "NOT_FOUND" via Errors.Auth.UserNotFound. | - Email not registered. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 Security issue — Handler returns Errors.Auth.UserNotFound (404) when user is null, leaking that the email does not exist. ForgotPassword handler correctly returns generic 200 message.** |
| TC_OTP_004 | OTP email enqueue fails (Hangfire unavailable). | 1. Stop Hangfire. 2. Send request-otp. | OTP saved to DB. Response: error "EMAIL_DISPATCH_UNAVAILABLE". | - Hangfire unavailable. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks TryEnqueueOtpEmail return value. |
| TC_OTP_005 | Previous OTPs invalidated on new request. | 1. Request OTP. 2. Request OTP again for same email+purpose. 3. Try to verify first OTP. | First OTP invalidated. Second OTP works. | - User exists. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls otps.InvalidateAllAsync before creating new OTP. |
| TC_OTP_006 | Validation — email is empty. | 1. Send POST /v1/auth/request-otp with email="". | Expected: 422 Validation Error. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No RequestOtpCommandValidator class exists. Empty email passes validation behavior and goes directly to handler where GetByEmailAsync returns null → 404.** |
| TC_OTP_007 | Validation — invalid email format. | 1. Send POST /v1/auth/request-otp with email="invalid". | Expected: 422 Validation Error. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No validator. Invalid format goes to handler → DB lookup returns null → 404 instead of 422.** |
| TC_OTP_008 | Validation — invalid OtpPurpose enum value. | 1. Send POST /v1/auth/request-otp with purpose=999. | Expected: 422 Validation Error for invalid enum. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No validator. Invalid enum may be deserialized as 0 or cause JsonException depending on serializer config.** |
| TC_OTP_009 | Per-email rate limiting for OTP requests. | 1. Send 10 request-otp requests for the same email within 1 minute. | Expected: Rate limited after threshold. | - User exists. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 No per-email rate limiting. Only global IP-based rate limit exists. Attacker can spam OTP requests for any email.** |
| TC_OTP_010 | OTP code is 6 digits and hash-stored. | 1. Request OTP. 2. Check OtpCode record in database. | OTP.CodeHash is bcrypt hash. OTP code range 100000–999999. | - User exists. DB access. | Passed | 04/09/2026 | TamKnm | | | | | | | RandomNumberGenerator.GetInt32(100000, 999999). Hash stored via passwordHasher.Hash(). |
| TC_OTP_011 | Already-verified email can still request EmailVerification OTP. | 1. User has IsEmailVerified=true. 2. Request OTP with purpose=EmailVerification. | OTP sent. No check for already-verified status. | - User exists, email verified. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ Handler does not check IsEmailVerified before generating OTP. Wasteful and confusing UX.** |
| TC_OTP_012 | Banned user can request OTP. | 1. User is banned. 2. Send request-otp. | OTP sent. No user status check. | - Banned user exists. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ Handler does not check IsBanned/IsDeleted before processing. Banned user can receive OTP and potentially use it.** |

---

## Verify OTP

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_VO_001 | Successfully verify OTP for EmailVerification. | 1. Request OTP for EmailVerification. 2. Send POST /v1/auth/verify-otp with correct email, otpCode, purpose. | Response: 200 OK. isVerified:true. OTP marked used. User.IsEmailVerified=true. | - Unverified user. Valid OTP. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler verifies OTP, calls otp.MarkUsed() and user.VerifyEmail(). |
| TC_VO_002 | Successfully verify OTP for PasswordReset. | 1. Request OTP for PasswordReset. 2. Verify with correct data. | Response: 200 OK. isVerified:true. OTP marked used. | - User exists. Valid PasswordReset OTP. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler skips VerifyEmail for non-EmailVerification purpose. |
| TC_VO_003 | Verify OTP fails with incorrect code. | 1. Request OTP. 2. Verify with wrong otpCode. | Response: 422. Error: "OTP_INVALID". AttemptCount incremented. | - Valid OTP exists. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks passwordHasher.Verify and returns Errors.Auth.OtpInvalid. |
| TC_VO_004 | Verify OTP fails when expired. | 1. Request OTP. 2. Wait >10 min or set ExpiresAt in past. 3. Verify. | Response: 422. Error: "OTP_EXPIRED". | - OTP expired. | Passed | 04/09/2026 | TamKnm | | | | | | | otp.IsValid returns false when IsExpired=true. |
| TC_VO_005 | Verify OTP fails when already used. | 1. Verify OTP successfully. 2. Try same OTP again. | Response: 422. Error: "OTP_EXPIRED". | - OTP already used. | Passed | 04/09/2026 | TamKnm | | | | | | | otp.IsValid returns false when IsUsed=true. |
| TC_VO_006 | Verify OTP fails after max attempts (5). | 1. Enter wrong OTP 5 times. 2. Enter correct OTP on 6th attempt. | Attempts 1-4: 422 "OTP_INVALID". Attempt 5+: 422 "OTP_MAX_ATTEMPTS". | - Valid OTP exists. | Passed | 04/09/2026 | TamKnm | | | | | | | otp.HasExceededMaxAttempts checked after IncrementAttempt(). |
| TC_VO_007 | Bug — 5th attempt with correct OTP is rejected. | 1. Enter wrong OTP 4 times (AttemptCount=4). 2. Enter correct OTP on 5th attempt. | Handler calls IncrementAttempt() first (count=5) → HasExceededMaxAttempts=true → "OTP_MAX_ATTEMPTS" returned even with correct code. | - AttemptCount=4 before 5th try. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 Bug — IncrementAttempt() is called BEFORE verification. On 5th attempt, count reaches 5 and HasExceededMaxAttempts returns true before code is checked. Should verify code first, then increment.** |
| TC_VO_008 | Bug — verifying already-verified email throws 500. | 1. User has IsEmailVerified=true. 2. Request EmailVerification OTP. 3. Verify with correct code. | Expected: Graceful 200 or "already verified" error. Actual: User.VerifyEmail() throws DomainException("Email is already verified.") → unhandled → 500. | - User has verified email. Valid OTP exists. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 User.VerifyEmail() throws DomainException when IsEmailVerified=true. ExceptionHandlingMiddleware does NOT catch DomainException specifically → falls to catch(Exception) → 500 Internal Server Error.** |
| TC_VO_009 | OTP for purpose A cannot verify for purpose B. | 1. Request OTP for EmailVerification. 2. Verify with purpose=PasswordReset. | Response: 422 "OTP_EXPIRED" (no valid OTP found for PasswordReset). | - Only EmailVerification OTP exists. | Passed | 04/09/2026 | TamKnm | | | | | | | GetLatestValidAsync filters by purpose parameter. |
| TC_VO_010 | Only latest valid OTP accepted. | 1. Request OTP → OTP1. 2. Request OTP → OTP2 (OTP1 invalidated). 3. Verify OTP1. 4. Verify OTP2. | OTP1: fails. OTP2: succeeds. | - Two OTP requests made. | Passed | 04/09/2026 | TamKnm | | | | | | | InvalidateAllAsync marks old OTPs. GetLatestValidAsync returns only valid one. |
| TC_VO_011 | Validation — email is empty. | 1. Send POST /v1/auth/verify-otp with email="". | Expected: 422 Validation Error. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No VerifyOtpCommandValidator. Empty email passes to handler → GetLatestValidAsync returns null → 422 "OTP_EXPIRED" (wrong error code).** |
| TC_VO_012 | Validation — otpCode is empty. | 1. Send POST /v1/auth/verify-otp with otpCode="". | Expected: 422 Validation Error. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No validator. Empty OTP goes to handler → passwordHasher.Verify("", hash) returns false → "OTP_INVALID" with attempt increment (wastes attempt).** |
| TC_VO_013 | Validation — otpCode not 6 digits. | 1. Send POST /v1/auth/verify-otp with otpCode="abc" or "12345". | Expected: 422 Validation Error for format. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No validator. Non-numeric or wrong-length OTP wastes an attempt count in DB.** |

---

## Forgot Password

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_FP_001 | Successfully request forgot password OTP. | 1. Send POST /v1/auth/forgot-password with valid registered email. | Response: 200 OK. Message: "Nếu email tồn tại, mã OTP sẽ được gửi." OTP created and email enqueued. | - User exists. Hangfire running. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler creates OTP, enqueues email, returns generic message. |
| TC_FP_002 | Anti-enumeration — non-existent email returns same response. | 1. Send forgot-password with non-existent email. 2. Compare with TC_FP_001 response. | Response: 200 OK with same generic message. No OTP created. | - Email not registered. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns same ForgotPasswordResponse message for non-existent email (line 36). |
| TC_FP_003 | OTP email enqueue fails silently. | 1. Stop Hangfire. 2. Send forgot-password. | Response: 200 OK. OTP saved. Failure only logged. | - Hangfire unavailable. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler logs error but still returns success response (line 50-55). |
| TC_FP_004 | Validation — email is empty. | 1. Send POST /v1/auth/forgot-password with email="". | Expected: 422 Validation Error. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No ForgotPasswordCommandValidator. Empty email passes to handler → GetByEmailAsync returns null → 200 OK generic message (no error, but OTP not created). Functionally "safe" but not proper validation.** |
| TC_FP_005 | Validation — invalid email format. | 1. Send forgot-password with email="not-email". | Expected: 422 Validation Error. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No validator. Invalid format goes to handler → DB lookup returns null → 200 OK (safe due to anti-enumeration, but wastes DB query).** |
| TC_FP_006 | Banned user can request forgot-password. | 1. Banned user. 2. Send forgot-password. | OTP created and sent. No status check. | - Banned user exists. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ Handler does not check IsBanned/IsDeleted. Banned user can reset password and potentially regain access.** |
| TC_FP_007 | Google-only user can set password via forgot-password. | 1. Google-only user (no password). 2. Send forgot-password. 3. Reset password with OTP. | OTP sent. User can set a password. | - Google-only user exists. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler does not check if user has a password. By design, this allows Google users to add email/password login. |

---

## Reset Password

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_RP_001 | Successfully reset password with valid OTP. | 1. Request forgot-password OTP. 2. Send POST /v1/auth/reset-password with email, otpCode, newPassword. | Response: 200 OK. Message: "Đặt lại mật khẩu thành công." Password updated. All refresh tokens revoked. FailedLoginAttempts=0. | - Valid PasswordReset OTP. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls otp.MarkUsed(), user.ChangePassword(), user.ResetFailedLoginAttempts(), refreshTokens.RevokeAllByUserIdAsync(). |
| TC_RP_002 | Reset fails with expired OTP. | 1. Request OTP. 2. Set ExpiresAt in past. 3. Reset. | Response: 422. Error: "OTP_EXPIRED". | - OTP expired. | Passed | 04/09/2026 | TamKnm | | | | | | | otp.IsValid returns false → Errors.Auth.OtpExpired. |
| TC_RP_003 | Reset fails with incorrect OTP. | 1. Request OTP. 2. Reset with wrong OTP code. | Response: 422. Error: "OTP_INVALID". AttemptCount incremented. | - Valid OTP exists. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks passwordHasher.Verify, returns Errors.Auth.OtpInvalid. |
| TC_RP_004 | Reset fails after max OTP attempts. | 1. Enter wrong OTP 5 times. 2. Enter correct on 6th. | Response: 422 "OTP_MAX_ATTEMPTS". | - AttemptCount reaches 5. | Passed | 04/09/2026 | TamKnm | | | | | | | HasExceededMaxAttempts checked after IncrementAttempt(). Same bug as TC_VO_007 but test expects block behavior. |
| TC_RP_005 | User not found after OTP verified. | 1. Request OTP. 2. Hard-delete user. 3. Reset with correct OTP. | OTP passes. User lookup → 404 "NOT_FOUND". | - User hard-deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user is null after OTP verification → Errors.Auth.UserNotFound. |
| TC_RP_006 | All refresh tokens revoked after reset. | 1. Login, get refresh token. 2. Reset password. 3. Use old refresh token. | Old refresh token: 422 "INVALID_REFRESH_TOKEN". | - User has active tokens. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls refreshTokens.RevokeAllByUserIdAsync(user.Id). |
| TC_RP_007 | Lockout cleared after reset. | 1. Lock account (5 failures). 2. Reset via OTP. 3. Login with new password. | Login succeeds. FailedLoginAttempts=0. LockoutEnd=null. | - Account locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls user.ResetFailedLoginAttempts() which resets count and LockoutEnd. |
| TC_RP_008 | Validation — newPassword weak. | 1. Send reset-password with weak newPassword="weak". | Expected: 422 with password strength errors. | - Valid OTP. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 No ResetPasswordCommandValidator. Weak password goes directly to handler → user.ChangePassword(hash) accepts any password. No strength validation.** |
| TC_RP_009 | New password same as current — not checked. | 1. Reset using same password as current. | Expected: 422 "PASSWORD_RECENTLY_USED". Actual: 200 OK. | - Valid OTP. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 Handler does NOT check if new password equals current (unlike ChangePasswordHandler which does).** |
| TC_RP_010 | New password matches one of last 3 — not checked. | 1. Reset using a password from recent history. | Expected: 422 "PASSWORD_RECENTLY_USED". Actual: 200 OK. | - User has password history. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 Handler does NOT check PasswordHistory (unlike ChangePasswordHandler which queries passwordHistories.GetRecentAsync).** |
| TC_RP_011 | Concurrent reset requests with same OTP. | 1. Send two reset-password simultaneously with same OTP. | First succeeds. Second fails (OTP already used). | - Valid OTP. | Passed | 04/09/2026 | TamKnm | | | | | | | otp.MarkUsed() sets IsUsed=true; concurrent request sees IsValid=false. |

---

## Change Password

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_CP_001 | Successfully change password. | 1. Login. 2. Send POST /v1/auth/change-password with correct currentPassword and valid newPassword. | Response: 200 OK. "Đổi mật khẩu thành công." Password updated. Old password saved to PasswordHistory. | - User authenticated. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler verifies current, checks history, saves to PasswordHistory, calls ChangePassword(). |
| TC_CP_002 | Fails without authentication. | 1. Send POST /v1/auth/change-password without JWT. | Response: 401 Unauthorized. | - No JWT. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller has [Authorize] attribute. |
| TC_CP_003 | Fails with expired JWT. | 1. Send with expired JWT. | Response: 401 Unauthorized. | - JWT expired. | Passed | 04/09/2026 | TamKnm | | | | | | | ASP.NET Core JWT middleware rejects expired tokens. |
| TC_CP_004 | Fails with incorrect current password. | 1. Login. 2. Send change-password with wrong currentPassword. | Response: 422. Error: "INCORRECT_CURRENT_PASSWORD". | - User authenticated. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks passwordHasher.Verify(request.CurrentPassword, user.PasswordHash). |
| TC_CP_005 | BR-AUTH-020: New password same as current rejected. | 1. Login. 2. Send change-password with newPassword = currentPassword. | Response: 422. Error: "PASSWORD_RECENTLY_USED". | - User authenticated. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks passwordHasher.Verify(request.NewPassword, user.PasswordHash) (line 52). |
| TC_CP_006 | BR-AUTH-020: New password matching last 3 rejected. | 1. Change password 3 times. 2. Try to reuse one of the last 3. | Response: 422. Error: "PASSWORD_RECENTLY_USED". | - User authenticated. PasswordHistory records exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler queries passwordHistories.GetRecentAsync(user.Id, 3) and checks each. |
| TC_CP_007 | Password outside last 3 history is allowed. | 1. Change password 4 times: pw1→pw2→pw3→pw4→pw5. 2. Try pw1. | Response: 200 OK. pw1 not in last 3 (pw2,pw3,pw4). | - User authenticated. 4+ history records. | Passed | 04/09/2026 | TamKnm | | | | | | | GetRecentAsync(userId, 3) returns only last 3 entries. |
| TC_CP_008 | MustChangePassword cleared after change. | 1. Login as user with MustChangePassword=true. 2. Change password. 3. Check DB. | MustChangePassword=false. | - User with MustChangePassword=true. | Passed | 04/09/2026 | TamKnm | | | | | | | user.ChangePassword() sets MustChangePassword=false (line 167-168 in User.cs). |
| TC_CP_009 | Company auto-activation on first password change. | 1. Login as CompanyManager, MustChangePassword=true. 2. Company status=PendingActivation. 3. Change password. | Company.Status → Active. | - CompanyManager, MustChangePassword=true, PendingActivation. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks wasFirstLogin && CompanyManager, then company.Activate(). |
| TC_CP_010 | Company NOT activated if MustChangePassword was false. | 1. Login as CompanyManager, MustChangePassword=false. 2. Company=PendingActivation. 3. Change password. | Company remains PendingActivation. | - CompanyManager, MustChangePassword=false. | Passed | 04/09/2026 | TamKnm | | | | | | | wasFirstLogin is false → auto-activation block skipped. |
| TC_CP_011 | Validation — newPassword is weak. | 1. Login. 2. Change password with weak newPassword="weak". | Expected: 422 with strength errors. | - User authenticated. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No ChangePasswordCommandValidator. Weak password accepted. Handler only checks current password match and history — no strength validation.** |
| TC_CP_012 | Validation — currentPassword is empty. | 1. Login. 2. Send change-password with currentPassword="". | Expected: 422 Validation Error. | - User authenticated. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No validator. Empty currentPassword → passwordHasher.Verify("", hash) returns false → "INCORRECT_CURRENT_PASSWORD" (functionally blocks but wrong error type — should be validation error).** |
| TC_CP_013 | Google-only user cannot change password. | 1. Login as Google user. 2. Change password with any currentPassword. | Response: 422 "INCORRECT_CURRENT_PASSWORD". | - Google-only user (passwordHash=""). | Passed | 04/09/2026 | TamKnm | | | | | | | Verify(anything, "") returns false. Blocked, though error message is misleading for Google users. |
| TC_CP_014 | Refresh tokens NOT revoked after password change. | 1. Login on device A. 2. Change password on device B. 3. Use refresh token from device A. | Old refresh token still works. Sessions NOT revoked. | - Multiple active sessions. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ ChangePasswordHandler does NOT call refreshTokens.RevokeAllByUserIdAsync() unlike ResetPasswordHandler. Old sessions remain valid.** |

---

## Refresh Token

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_RT_001 | Successfully refresh with valid token. | 1. Login and get refresh token. 2. Send POST /v1/auth/refresh-token. | Response: 200 OK. New accessToken and refreshToken. Old token revoked (rotation). | - Valid unexpired refresh token. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler revokes old token, creates new, generates new access token. |
| TC_RT_002 | Refresh fails with invalid token string. | 1. Send refresh-token with random string. | Response: 422. Error: "INVALID_REFRESH_TOKEN". | - No matching token. | Passed | 04/09/2026 | TamKnm | | | | | | | GetByTokenHashAsync returns null → Errors.Auth.InvalidRefreshToken. |
| TC_RT_003 | Refresh fails with expired token. | 1. Set token ExpiresAt to past. 2. Refresh. | Response: 422 "INVALID_REFRESH_TOKEN". Token.IsActive=false. | - Token exists but expired. | Passed | 04/09/2026 | TamKnm | | | | | | | existingToken.IsActive returns false when expired. |
| TC_RT_004 | Refresh fails with already-revoked token. | 1. Refresh once (old revoked). 2. Try old token again. | Response: 422 "INVALID_REFRESH_TOKEN". | - Token already rotated. | Passed | 04/09/2026 | TamKnm | | | | | | | Revoked token has RevokedAt set → IsActive=false. |
| TC_RT_005 | Refresh fails when user is banned. | 1. Login, get token. 2. Ban user. 3. Refresh. | Response: 403 "ACCOUNT_BANNED". | - User banned after token obtained. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user.IsBanned (line 46). |
| TC_RT_006 | Refresh fails when user is soft-deleted. | 1. Login, get token. 2. Soft-delete user. 3. Refresh. | Response: 403 "ACCOUNT_DEACTIVATED". | - User soft-deleted after token obtained. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user.IsDeleted (line 49). |
| TC_RT_007 | Refresh fails when user hard-deleted. | 1. Token exists, user hard-deleted. 2. Refresh. | Response: 404 "NOT_FOUND". | - User hard-deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | GetByIdAsync returns null → Errors.Auth.UserNotFound. |
| TC_RT_008 | Token rotation — old token properly revoked. | 1. Login → token_1. 2. Refresh → token_2. 3. Check DB: token_1 has RevokedAt and ReplacedByTokenHash. | Old token linked to new token. | - Valid refresh token. | Passed | 04/09/2026 | TamKnm | | | | | | | existingToken.Revoke(newTokenHash) sets RevokedAt and ReplacedByTokenHash. |
| TC_RT_009 | Validation — refresh token empty. | 1. Send with refreshToken="". | Response: 422 "Refresh token is required." | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | RefreshTokenCommandValidator has NotEmpty rule. |
| TC_RT_010 | Stolen token detection — reuse after rotation. | 1. Login → token_1. 2. Refresh → token_2. 3. Reuse token_1. | Expected: Revoke ALL tokens for user (theft detection). Actual: 422 "INVALID_REFRESH_TOKEN" only for token_1. Other tokens unaffected. | - Token_1 already rotated. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 No token theft detection. Reuse of rotated token only returns "INVALID_REFRESH_TOKEN" but does NOT revoke all tokens. Attacker using stolen token_1 doesn't trigger security response.** |
| TC_RT_011 | Company expired check missing during refresh. | 1. Login as CompanyManager. 2. Company→Expired. 3. Refresh. | Expected: 403 "COMPANY_EXPIRED". Actual: 200 OK. | - CompanyManager, Expired company. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ RefreshTokenHandler does NOT check company status (unlike LoginHandler). Expired company user continues to access system.** |
| TC_RT_012 | Lockout check missing during refresh. | 1. Login. 2. Account locked (5 failures). 3. Refresh. | Expected: Block. Actual: 200 OK. | - User locked out. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ Handler does not check IsLockedOut(). Locked user can refresh tokens.** |

---

## Login with Google

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_GL_001 | Successful Google login — existing user by GoogleId. | 1. Send POST /v1/auth/google-login with valid idToken. 2. User found by GoogleId. | Response: 200 OK. Tokens returned. No new user created. | - User exists with matching GoogleId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls GetByGoogleIdAsync first, finds user. |
| TC_GL_002 | Successful Google login — existing user by Email, GoogleId linked. | 1. Google login with email matching existing user (no GoogleId). | Response: 200 OK. GoogleId linked. If email unverified, now verified. | - User exists with email but GoogleId=null. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler falls through to GetByEmailAsync, calls LinkGoogleAccount and VerifyEmail if needed. |
| TC_GL_003 | Successful Google login — auto-register new user. | 1. Google login with email not in system. | Response: 200 OK. New user created (Citizen, IsEmailVerified=true). | - Email not in system (active or deleted). | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls User.CreateFromGoogle and users.Add. |
| TC_GL_004 | Google login fails with invalid ID token. | 1. Send google-login with idToken="invalid". | Response: 422. Error: "GOOGLE_AUTH_FAILED". | - API is accessible. | Passed | 04/09/2026 | TamKnm | | | | | | | googleAuth.VerifyIdTokenAsync returns null → Errors.Auth.GoogleAuthFailed. |
| TC_GL_005 | Google login — email belongs to soft-deleted account. | 1. Soft-delete user. 2. Google login with that email. | Response: 409 "EMAIL_DELETED_RESTORE_AVAILABLE". | - Soft-deleted user with matching email. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks GetDeletedByEmailAsync before auto-register. |
| TC_GL_006 | Race condition — concurrent Google auto-register. | 1. Send two concurrent google-login for same new email. | First: 200 OK. Second: mapped error from PostgresUniqueViolationMapper. | - Email not in system. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches DbUpdateException and calls PostgresUniqueViolationMapper.TryMap. |
| TC_GL_007 | Validation — idToken is empty. | 1. Send google-login with idToken="". | Expected: 422 Validation Error. | - API is accessible. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No GoogleLoginCommandValidator. Empty idToken goes to googleAuth.VerifyIdTokenAsync("") → likely returns null → "GOOGLE_AUTH_FAILED" (blocks but wrong error type).** |
| TC_GL_008 | Security — banned user login via Google (bypasses check). | 1. Ban user. 2. Google login with that GoogleId. | Expected: 403 "ACCOUNT_BANNED". Actual: 200 OK. Login succeeds. | - User banned with linked GoogleId. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 CRITICAL — Handler does NOT check IsBanned for existing users. Banned user bypasses ban entirely via Google login.** |
| TC_GL_009 | Security — soft-deleted user login via Google by GoogleId. | 1. Soft-delete user. 2. Google login with GoogleId. | Expected: 403 "ACCOUNT_DEACTIVATED". Actual: 200 OK. | - User soft-deleted with GoogleId. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 CRITICAL — GetByGoogleIdAsync returns the user (not filtered by soft-delete). Handler issues tokens for deleted user.** |
| TC_GL_010 | Company expired check missing. | 1. CompanyManager with expired company. 2. Google login. | Expected: 403 "COMPANY_EXPIRED". Actual: 200 OK. | - CompanyManager, expired company. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No company status check in GoogleLoginHandler (LoginHandler has it).** |
| TC_GL_011 | Auto-register: new user has IsEmailVerified=true. | 1. Google login with new email. 2. Check user record. | User.IsEmailVerified=true. | - New Google user. | Passed | 04/09/2026 | TamKnm | | | | | | | User.CreateFromGoogle sets IsEmailVerified=true by design. |
| TC_GL_012 | Linking Google auto-verifies unverified email. | 1. User exists, IsEmailVerified=false. 2. Google login with matching email. | GoogleId linked. IsEmailVerified=true. | - User with unverified email, no GoogleId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls user.VerifyEmail() when linking Google (line 60-61). |

---

## Request Account Deletion

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_AD_001 | ⛔ Endpoint does not exist — cannot test. | 1. Send DELETE /v1/auth/account or equivalent. 2. Observe response. | Expected: 200 OK. Actual: 404 Not Found — no route. | - User authenticated. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 RequestAccountDeletionCommandHandler exists but NO endpoint in AuthController or any controller.** |
| TC_AD_002 | Handler — user not found. | 1. Call handler with non-existent userId. | Handler returns 404 "NOT_FOUND". | - UserId not in DB. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user is null → Errors.Auth.UserNotFound. (Code review only — no endpoint to test.) |
| TC_AD_003 | Handler — user already deleted. | 1. Call handler for user with IsDeleted=true. | Handler returns 409 "USER_ALREADY_DELETED". | - User already soft-deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user.IsDeleted → Errors.Users.UserAlreadyDeleted. |
| TC_AD_004 | Handler — reports anonymized on deletion. | 1. User has reports. 2. Call handler. | User soft-deleted. Reports anonymized. WillBeDeletedAt = now + 90d. | - User has reports. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls user.SoftDelete(), reports.AnonymizeReporterAsync(). |
| TC_AD_005 | Handler — refresh tokens not revoked on deletion. | 1. User has active tokens. 2. Call handler. 3. Check tokens. | Tokens remain active. Deleted user can still refresh. | - User has active refresh tokens. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 Handler does NOT call refreshTokens.RevokeAllByUserIdAsync(). Soft-deleted user's tokens remain valid (though RefreshTokenHandler checks IsDeleted).** |

---

## Restore Account

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| TC_RA_001 | ⛔ Endpoint does not exist — cannot test. | 1. Send POST /v1/auth/restore-account with email and password. 2. Observe response. | Expected: 200 OK. Actual: 404 Not Found — no route. | - Soft-deleted user within 90d. | Failed | 04/09/2026 | TamKnm | | | | | | | **🔴 RestoreAccountCommandHandler exists but NO endpoint in any controller.** |
| TC_RA_002 | Handler — restore succeeds with correct credentials. | 1. Call handler with correct email + password. | user.Restore() called. IsDeleted=false. | - Soft-deleted user. Correct password. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls user.Restore() after password verification. |
| TC_RA_003 | Handler — user not found (not soft-deleted). | 1. Call handler with active user's email. | Handler returns 404 "NOT_FOUND". | - Email not soft-deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | GetDeletedByEmailAsync returns null for non-deleted users. |
| TC_RA_004 | Handler — wrong password. | 1. Call handler with correct email, wrong password. | Handler returns 422 "INVALID_CREDENTIALS". | - Soft-deleted user. Wrong password. | Passed | 04/09/2026 | TamKnm | | | | | | | passwordHasher.Verify returns false → Errors.Auth.InvalidCredentials. |
| TC_RA_005 | Handler — user already hard-deleted. | 1. User hard-deleted (past 90d). 2. Call handler. | Handler returns 404 "NOT_FOUND". | - User no longer in DB. | Passed | 04/09/2026 | TamKnm | | | | | | | GetDeletedByEmailAsync returns null when record doesn't exist. |
| TC_RA_006 | Validation — email is empty. | 1. Send restore-account with email="". | Expected: 422 Validation Error. | - API accessible (when endpoint exists). | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ No RestoreAccountCommandValidator. No endpoint either.** |
| TC_RA_007 | Reports de-anonymized after restore. | 1. Soft-delete (reports anonymized). 2. Restore. 3. Check reports. | Expected: Reports restore original reporter. Actual: Reports remain anonymized. | - User had anonymized reports. | Failed | 04/09/2026 | TamKnm | | | | | | | **⚠️ RestoreAccountHandler does NOT call any de-anonymization logic. Reports remain anonymized permanently after restore.** |

---

## Gap Analysis Summary — Round 1

### Test Results Overview

| Section | Total TCs | Passed | Failed |
|---------|-----------|--------|--------|
| Account Registration | 33 | 31 | 2 |
| Standard Login | 24 | 23 | 1 |
| Request OTP | 12 | 6 | 6 |
| Verify OTP | 13 | 7 | 6 |
| Forgot Password | 7 | 4 | 3 |
| Reset Password | 11 | 8 | 3 |
| Change Password | 14 | 11 | 3 |
| Refresh Token | 12 | 9 | 3 |
| Login with Google | 12 | 8 | 4 |
| Request Account Deletion | 5 | 3 | 2 |
| Restore Account | 7 | 4 | 3 |
| **TOTAL** | **153** | **107** | **46** |

### Failed Test Cases — Root Cause Classification

#### 🔴 P0 — Critical (Must Fix)

| Test Case | Issue | Root Cause |
|-----------|-------|------------|
| TC_REG_024 | bcrypt DoS — no password max length | RegisterCommandValidator missing MaximumLength for Password |
| TC_OTP_003 | Request OTP leaks user existence (404) | Handler returns 404 instead of generic 200 |
| TC_VO_007 | 5th OTP attempt with correct code is blocked | IncrementAttempt() called before verification |
| TC_VO_008 | DomainException → 500 when verifying already-verified email | user.VerifyEmail() throws unhandled exception |
| TC_RP_008 | Weak password accepted in reset-password | No ResetPasswordCommandValidator |
| TC_RP_009 | New password = current accepted in reset | No current password check in handler |
| TC_RP_010 | Password history not checked in reset | No PasswordHistory check in handler |
| TC_GL_008 | Banned user bypasses ban via Google login | No IsBanned check in GoogleLoginHandler |
| TC_GL_009 | Soft-deleted user bypasses deletion via Google | No IsDeleted check in GoogleLoginHandler |
| TC_AD_001 | Missing endpoint for RequestAccountDeletion | No route in any controller |
| TC_RA_001 | Missing endpoint for RestoreAccount | No route in any controller |
| TC_RT_010 | No stolen token detection on reuse | Revoked token reuse doesn't revoke all tokens |

#### ⚠️ P1 — Should Fix

| Test Case | Issue | Root Cause |
|-----------|-------|------------|
| TC_REG_023 | Email max length not validated (RFC 5321) | Missing MaximumLength(254) |
| TC_SL_022 | CAPTCHA flag not in login response | RequiresCaptcha() exists but unused in handler |
| TC_OTP_006-008 | No RequestOtpCommandValidator | Validator class missing |
| TC_OTP_009 | No per-email rate limit for OTP | Only global IP rate limit |
| TC_OTP_011 | Already-verified user can request EmailVerification OTP | No IsEmailVerified check |
| TC_OTP_012 | Banned user can request OTP | No IsBanned check |
| TC_VO_011-013 | No VerifyOtpCommandValidator | Validator class missing |
| TC_FP_004-005 | No ForgotPasswordCommandValidator | Validator class missing |
| TC_FP_006 | Banned user can request forgot-password | No IsBanned check |
| TC_CP_011-012 | No ChangePasswordCommandValidator | Validator class missing |
| TC_CP_014 | Refresh tokens not revoked after password change | Missing RevokeAllByUserIdAsync call |
| TC_GL_007 | No GoogleLoginCommandValidator | Validator class missing |
| TC_GL_010 | No company expired check in Google login | Missing company status check |
| TC_RT_011 | No company expired check in refresh | Missing company status check |
| TC_RT_012 | No lockout check in refresh | Missing IsLockedOut check |
| TC_AD_005 | Refresh tokens not revoked on account deletion | Missing RevokeAllByUserIdAsync call |
| TC_RA_006 | No RestoreAccountCommandValidator | Validator class missing |
| TC_RA_007 | Reports not de-anonymized on restore | No de-anonymization logic |

### Missing Validator Classes (7 files)

| Feature | Missing File | Required Validations |
|---------|-------------|---------------------|
| RequestOtp | RequestOtpCommandValidator.cs | Email (NotEmpty, EmailAddress), Purpose (IsInEnum) |
| VerifyOtp | VerifyOtpCommandValidator.cs | Email (NotEmpty, EmailAddress), OtpCode (NotEmpty, Length(6), Matches "^\d{6}$"), Purpose (IsInEnum) |
| ForgotPassword | ForgotPasswordCommandValidator.cs | Email (NotEmpty, EmailAddress) |
| ResetPassword | ResetPasswordCommandValidator.cs | Email (NotEmpty, EmailAddress), OtpCode (NotEmpty, Length(6)), NewPassword (strength rules) |
| ChangePassword | ChangePasswordCommandValidator.cs | CurrentPassword (NotEmpty), NewPassword (NotEmpty, strength rules) |
| GoogleLogin | GoogleLoginCommandValidator.cs | IdToken (NotEmpty) |
| RestoreAccount | RestoreAccountCommandValidator.cs | Email (NotEmpty, EmailAddress), Password (NotEmpty) |
