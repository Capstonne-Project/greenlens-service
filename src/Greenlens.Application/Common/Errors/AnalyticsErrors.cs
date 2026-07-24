using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Analytics
    {
        public static Error NotCompanyStaff => new(
            "NOT_COMPANY_STAFF",
            "Tài khoản hiện tại không thuộc công ty nào hoặc đã ngưng hoạt động.",
            ErrorType.Forbidden);
    }
}
