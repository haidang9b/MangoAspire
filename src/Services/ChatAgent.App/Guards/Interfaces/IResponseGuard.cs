using ChatAgent.App.Guards.Grounding;

namespace ChatAgent.App.Guards.Interfaces;

/// <summary>
/// The output guard: verifies a drafted answer against the tool results that produced it,
/// before the customer sees any of it.
/// </summary>
public interface IResponseGuard
{
    Task<ReviewVerdict> ReviewAsync(
        string question,
        string draft,
        GroundingSnapshot grounding,
        CancellationToken cancellationToken = default);
}
