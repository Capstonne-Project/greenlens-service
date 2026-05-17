using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateLocalOffice;

public sealed class UpdateLocalOfficeCommandHandler(
    ILocalOfficeRepository offices,
    IUnitOfWork uow) : IRequestHandler<UpdateLocalOfficeCommand, Result>
{
    public async Task<Result> Handle(UpdateLocalOfficeCommand request, CancellationToken ct)
    {
        var office = await offices.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (office is null)
            return Errors.Organization.OfficeNotFound;

        office.Update(request.Name);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
