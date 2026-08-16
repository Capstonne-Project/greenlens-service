using FluentAssertions;
using Greenlens.Application.Features.Organization;

namespace Greenlens.Application.UnitTests;

public sealed class CompanyStaffAccountNotificationPlaceholdersTests
{
    [Fact]
    public void ForCreated_IncludesCredentials_BR_CMP_010()
    {
        var placeholders = CompanyStaffAccountNotificationPlaceholders.ForCreated(
            "Cty Môi Trường Xanh",
            "Trần Thị B",
            "staff@example.com",
            "Xy9!kL2mNp");

        placeholders["company_name"].Should().Be("Cty Môi Trường Xanh");
        placeholders["staff_name"].Should().Be("Trần Thị B");
        placeholders["email"].Should().Be("staff@example.com");
        placeholders["temp_password"].Should().Be("Xy9!kL2mNp");
    }
}
