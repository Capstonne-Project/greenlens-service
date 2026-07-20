using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class NotificationPreferenceRepository(ApplicationDbContext context)
    : GenericRepository<NotificationPreference>(context), INotificationPreferenceRepository;
