using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Resolves LEO/DEO recipients for inspection escalation alerts (BR-INS-021).</summary>
internal static class InspectionOfficerRecipientResolver
{
    internal static async Task<IReadOnlyList<Guid>> ResolveAsync(
        IOfficerRecipientQuery officers,
        Guid? assignedOfficeId,
        Guid? assignedDepartmentId,
        Guid? createdByOfficerId,
        CancellationToken ct)
    {
        var recipientIds = new HashSet<Guid>();

        if (createdByOfficerId.HasValue)
            recipientIds.Add(createdByOfficerId.Value);

        if (assignedOfficeId.HasValue)
        {
            var leoIds = await officers
                .GetLeoIdsByOfficeAsync(assignedOfficeId.Value, ct)
                .ConfigureAwait(false);

            foreach (var id in leoIds)
                recipientIds.Add(id);
        }

        if (assignedDepartmentId.HasValue)
        {
            var deoIds = await officers
                .GetDeoIdsByDepartmentAsync(assignedDepartmentId.Value, ct)
                .ConfigureAwait(false);

            foreach (var id in deoIds)
                recipientIds.Add(id);
        }

        return recipientIds.ToList();
    }
}
