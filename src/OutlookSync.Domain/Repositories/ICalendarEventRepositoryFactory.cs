using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Domain.Repositories;

/// <summary>
/// Factory for creating calendar event repositories
/// </summary>
public interface ICalendarEventRepositoryFactory
{
    /// <summary>
    /// Creates a repository instance for the specified credential
    /// </summary>
    /// <param name="credential">The credential to use for authentication</param>
    /// <returns>Repository instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when credential is null, token is invalid, or status data is missing</exception>
    ICalendarEventRepository Create(Credential credential);
}
