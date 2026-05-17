using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateDepartment;

public sealed record UpdateDepartmentCommand(Guid Id, string Name) : IRequest<Result>;
