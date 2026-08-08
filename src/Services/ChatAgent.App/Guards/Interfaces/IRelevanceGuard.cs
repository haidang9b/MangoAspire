namespace ChatAgent.App.Guards.Interfaces;

/// <summary>
/// The quick guard: decides whether a question is worth handing to the agent at all.
/// Runs before any tool is invoked, so a blocked question costs nothing beyond the check
/// itself.
/// </summary>
public interface IRelevanceGuard
{
    /// <param name="recentTurns">
    /// Recent conversation, so a bare follow-up like "and the second one?" is judged in
    /// context rather than dismissed as off-topic.
    /// </param>
    Task<GuardVerdict> EvaluateAsync(
        string question,
        IReadOnlyList<string> recentTurns,
        CancellationToken cancellationToken = default);
}
