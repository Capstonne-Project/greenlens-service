using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.ResetCompanyManagerPassword;

/// <summary>
/// DEO/Admin resets a CompanyManager's password to a new temporary password.
/// Used when DEO lost the original TempPassword.
/// </summary>
public sealed record ResetCompanyManagerPasswordCommand(
    Guid CompanyId,
    Guid ManagerUserId) : IRequest<Result<ResetCompanyManagerPasswordResponse>>;

public sealed record ResetCompanyManagerPasswordResponse(
    Guid ManagerUserId,
    string ManagerEmail,
    string TempPassword);
