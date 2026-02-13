using OutlookSync.Domain.Aggregates;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Infrastructure.Repositories;

public class CredentialRepository(OutlookSyncDbContext context) : Repository<Credential>(context), ICredentialRepository
{
}

