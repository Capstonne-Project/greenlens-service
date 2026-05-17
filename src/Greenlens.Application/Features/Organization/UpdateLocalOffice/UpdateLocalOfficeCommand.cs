using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateLocalOffice;

public sealed record UpdateLocalOfficeCommand(Guid Id, string Name) : IRequest<Result>;
