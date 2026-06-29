using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class StaffInvitationRepository(ApplicationDbContext db)
    : GenericRepository<StaffInvitation>(db), IStaffInvitationRepository;
