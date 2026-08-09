using ChatAgent.App.Guards.Grounding;

namespace ChatAgent.App.Guards.Output;

/// <summary>
/// Checks a drafted answer against the tool results actually captured during the turn, without
/// consulting a model.
/// </summary>
public interface IAnswerFactChecker
{
    FactCheckResult Check(string? answer, GroundingSnapshot grounding);
}
