namespace ChatAgent.App.Guards.Authorization;

/// <summary>
/// Caps how many state-changing tool calls a single turn may make.
/// </summary>
/// <remarks>
/// <c>MaxToolIterations</c> bounds model round-trips, not writes: one round-trip can invoke
/// several functions in parallel, so a model that decides to add an item repeatedly can still do
/// so within the iteration cap. Scoped, so the budget is per HTTP request, which is per turn.
/// </remarks>
public sealed class ToolWriteBudget
{
    private readonly Lock _gate = new();
    private int _used;

    public int Used
    {
        get
        {
            lock (_gate)
            {
                return _used;
            }
        }
    }

    /// <summary>Consumes one write, or returns false when the turn's budget is spent.</summary>
    public bool TryConsume(int limit)
    {
        lock (_gate)
        {
            if (_used >= limit)
            {
                return false;
            }

            _used++;
            return true;
        }
    }
}
