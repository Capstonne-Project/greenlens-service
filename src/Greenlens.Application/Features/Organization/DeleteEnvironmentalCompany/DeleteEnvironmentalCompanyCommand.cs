using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.DeleteEnvironmentalCompany;

/// <summary>
/// Soft-delete an EnvironmentalServiceCompany. 
/// Only Admin can perform this.
/// </summary>
public sealed record DeleteEnvironmentalCompanyCommand(Guid Id) : IRequest<Result>;
