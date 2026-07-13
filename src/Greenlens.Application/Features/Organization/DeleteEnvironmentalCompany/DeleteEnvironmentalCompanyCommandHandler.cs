using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.DeleteEnvironmentalCompany;

public sealed class DeleteEnvironmentalCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteEnvironmentalCompanyCommandHandler> logger) : IRequestHandler<DeleteEnvironmentalCompanyCommand, Result>
{
    public async Task<Result> Handle(DeleteEnvironmentalCompanyCommand request, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (company is null)
            return Errors.Organization.CompanyNotFound;

        company.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("EnvironmentalServiceCompany {CompanyId} soft-deleted by {UserId}", request.Id, currentUser.UserId);

        return Result.Success();
    }
}
