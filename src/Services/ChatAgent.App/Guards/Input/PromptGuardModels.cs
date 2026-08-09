namespace ChatAgent.App.Guards.Input;

/// <summary>Which structural rule a prompt failed, if any.</summary>
public enum PromptFormatFailure
{
    None = 0,

    /// <summary>Null, empty, or whitespace only.</summary>
    Empty = 1,

    /// <summary>Longer than <see cref="GuardOptions.MaxPromptChars"/>.</summary>
    TooLong = 2,

    /// <summary>More lines than <see cref="GuardOptions.MaxPromptLines"/>.</summary>
    TooManyLines = 3,

    /// <summary>Contains C0/C1 control characters other than tab, carriage return and newline.</summary>
    ControlCharacters = 4,

    /// <summary>
    /// Contains zero-width or bidirectional-override characters — invisible in a chat window,
    /// but enough to hide an instruction from a human reviewer while the model still reads it.
    /// </summary>
    ZeroWidth = 5,
}

/// <param name="RuleId">Stable identifier for logs and metrics; empty when the prompt is valid.</param>
public readonly record struct PromptFormatResult(bool IsValid, PromptFormatFailure Failure, string RuleId)
{
    public static PromptFormatResult Valid { get; } = new(true, PromptFormatFailure.None, string.Empty);

    public static PromptFormatResult Invalid(PromptFormatFailure failure, string ruleId)
        => new(false, failure, ruleId);
}

/// <param name="RuleId">Stable identifier for logs and metrics; empty when nothing matched.</param>
/// <param name="Reason">Short rationale for logs only — never shown to the customer.</param>
public readonly record struct PromptScanResult(bool Blocked, GuardCategory Category, string RuleId, string Reason)
{
    public static PromptScanResult Clean { get; } =
        new(false, GuardCategory.OnTopic, string.Empty, string.Empty);

    public static PromptScanResult Block(GuardCategory category, string ruleId, string reason)
        => new(true, category, ruleId, reason);
}
