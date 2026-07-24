using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.IntegrationTests.Fixtures;

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "admin@test.local";
    public string Role { get; set; } = "Admin";
    public bool IsAuthenticated => true;
}
