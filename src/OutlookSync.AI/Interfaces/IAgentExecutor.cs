using OutlookSync.AI.Models;

namespace OutlookSync.AI.Interfaces;

/// <summary>
/// Defines the contract for executing agent tasks.
/// </summary>
public interface IAgentExecutor
{
    /// <summary>
    /// Executes an agent task and returns the result.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the task execution.</returns>
    Task<AgentResult> ExecuteAsync(AgentTask task, CancellationToken cancellationToken = default);
}
