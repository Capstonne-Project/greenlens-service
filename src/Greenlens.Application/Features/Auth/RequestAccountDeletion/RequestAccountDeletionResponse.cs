namespace Greenlens.Application.Features.Auth.RequestAccountDeletion;

public sealed record RequestAccountDeletionResponse(string Message, DateTime? WillBeDeletedAt);
