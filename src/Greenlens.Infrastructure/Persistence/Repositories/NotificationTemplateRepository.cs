using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class NotificationTemplateRepository(ApplicationDbContext db)
    : GenericRepository<NotificationTemplate>(db), INotificationTemplateRepository;

