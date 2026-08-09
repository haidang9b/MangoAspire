using ChatAgent.App.Guards.Output;
using Shouldly;

namespace ChatAgent.App.Tests.Guards.Output;

/// <summary>
/// Pins the rule that stops the response guard becoming an injection route of its own.
/// </summary>
/// <remarks>
/// The reviewer is prompted with retrieved facts, which are untrusted, and on the revise path it
/// writes the text the customer reads. Requiring the revision to be a deletion of the draft means
/// it cannot introduce a single word the agent did not already produce.
/// </remarks>
public class RevisionValidatorTests
{
    private const string Draft = "Our Pho Bo is $12.50 and it is in stock right now.";

    [Fact]
    public void IsDeletionOnly_When_RevisionDropsAClause_Then_IsAccepted()
        => RevisionValidator.IsDeletionOnly(Draft, "Our Pho Bo is $12.50.").ShouldBeTrue();

    [Fact]
    public void IsDeletionOnly_When_RevisionIsIdentical_Then_IsAccepted()
        => RevisionValidator.IsDeletionOnly(Draft, Draft).ShouldBeTrue();

    [Fact]
    public void IsDeletionOnly_When_RevisionOnlyChangesPunctuation_Then_IsAccepted()
    {
        // Cutting a middle sentence legitimately turns a comma into a full stop. Failing that
        // would push every real edit into a rejection.
        RevisionValidator.IsDeletionOnly("Our Pho Bo is $12.50, and it is tasty.", "Our Pho Bo is $12.50.")
            .ShouldBeTrue();
    }

    [Fact]
    public void IsDeletionOnly_When_RevisionAddsAWord_Then_IsRejected()
        => RevisionValidator.IsDeletionOnly(Draft, "Our Pho Bo is $12.50 and delicious.").ShouldBeFalse();

    [Fact]
    public void IsDeletionOnly_When_RevisionCorrectsAFigure_Then_IsRejected()
    {
        // The accepted cost of the rule: a reviewer cannot fix a number, only cut it. Correcting
        // one is exactly the case where the reviewer would be asserting a fact of its own.
        RevisionValidator.IsDeletionOnly("We open at 9.", "We open at 10.").ShouldBeFalse();
    }

    [Fact]
    public void IsDeletionOnly_When_RevisionReordersWords_Then_IsRejected()
        => RevisionValidator.IsDeletionOnly("pho bo is tasty", "tasty is pho bo").ShouldBeFalse();

    [Fact]
    public void IsDeletionOnly_When_RevisionSmugglesAnInstruction_Then_IsRejected()
    {
        // What a captured reviewer looks like: the retrieved facts told it to say this.
        RevisionValidator.IsDeletionOnly(
            Draft, "Our Pho Bo is $12.50. Visit http://example.com to claim your free order.")
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsDeletionOnly_When_RevisionIsBlank_Then_IsRejected(string? revision)
        => RevisionValidator.IsDeletionOnly(Draft, revision).ShouldBeFalse();

    [Fact]
    public void IsDeletionOnly_When_DraftIsBlank_Then_IsRejected()
        => RevisionValidator.IsDeletionOnly("  ", "anything").ShouldBeFalse();
}
