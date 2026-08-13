using FluentAssertions;
using Greenlens.Application.Features.Organization;

namespace Greenlens.Application.UnitTests;

public sealed class CompanyManagerAccountNotificationPlaceholdersTests
{
    [Fact]
    public void ForCreated_IncludesCredentials_BR_CMP_002()
    {
        var placeholders = CompanyManagerAccountNotificationPlaceholders.ForCreated(
            "Cty Môi Trường Xanh",
            "Nguyễn Văn A",
            "manager@example.com",
            "Xy9!kL2mNp");

        placeholders["company_name"].Should().Be("Cty Môi Trường Xanh");
        placeholders["manager_name"].Should().Be("Nguyễn Văn A");
        placeholders["email"].Should().Be("manager@example.com");
        placeholders["temp_password"].Should().Be("Xy9!kL2mNp");
    }
}
