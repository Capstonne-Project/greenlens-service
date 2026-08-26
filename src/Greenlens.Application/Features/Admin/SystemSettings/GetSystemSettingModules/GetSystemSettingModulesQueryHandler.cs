using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using Greenlens.Application.Features.Admin.SystemSettings;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.SystemSettings.GetSystemSettingModules;

/// <summary>Returns module catalog for admin sidebar.</summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record GetSystemSettingModulesQuery : IRequest<Result<GetSystemSettingModulesResponse>>;

public sealed record GetSystemSettingModulesResponse(
    IReadOnlyList<SystemSettingModuleDto> Modules);

public sealed record SystemSettingModuleDto(
    string Module,
    string RouteSlug,
    string DisplayNameVi,
    string DescriptionVi);

public sealed class GetSystemSettingModulesQueryHandler
    : IRequestHandler<GetSystemSettingModulesQuery, Result<GetSystemSettingModulesResponse>>
{
    public Task<Result<GetSystemSettingModulesResponse>> Handle(
        GetSystemSettingModulesQuery request,
        CancellationToken ct)
    {
        var modules = SystemSettingModuleCatalog.All
            .Select(m => new SystemSettingModuleDto(
                m.Module.ToString(),
                m.RouteSlug,
                m.DisplayNameVi,
                m.DescriptionVi))
            .ToList();

        return Task.FromResult(Result<GetSystemSettingModulesResponse>.Success(new GetSystemSettingModulesResponse(modules)));
    }
}
