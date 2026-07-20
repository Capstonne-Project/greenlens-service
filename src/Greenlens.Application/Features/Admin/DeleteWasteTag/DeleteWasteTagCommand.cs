using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.DeleteWasteTag;

/// <summary>
/// Soft-delete a WasteTag. 
/// Reports referencing this tag will still exist, but the tag won't be listed for new reports.
/// </summary>
public sealed record DeleteWasteTagCommand(Guid Id) : IRequest<Result>;
