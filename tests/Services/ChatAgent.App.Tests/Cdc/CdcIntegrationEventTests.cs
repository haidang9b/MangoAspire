using ChatAgent.App.Cdc;
using Shouldly;

namespace ChatAgent.App.Tests.Cdc;

/// <summary>
/// The replay fence in isolation. Everything about rebuilding a read-model from a replayable
/// log rests on this predicate being right.
/// </summary>
public class CdcIntegrationEventTests
{
    private static ProductCdcEvent Event(long? lsn = null, long? timestampMs = null) => new()
    {
        ProductId = Guid.NewGuid(),
        Name = "Pho Bo",
        Description = "Beef noodle soup",
        CategoryName = "Soups",
        ImageUrl = "https://example.com/pho.jpg",
        Price = 12.5m,
        SourceLsn = lsn,
        SourceTimestampMs = timestampMs,
    };

    [Fact]
    public void IsStaleAgainst_When_IncomingLsnIsLower_Then_ReturnsTrue()
        => Event(lsn: 100).IsStaleAgainst(rowLsn: 200, rowTimestamp: null).ShouldBeTrue();

    [Fact]
    public void IsStaleAgainst_When_IncomingLsnIsHigher_Then_ReturnsFalse()
        => Event(lsn: 300).IsStaleAgainst(rowLsn: 200, rowTimestamp: null).ShouldBeFalse();

    [Fact]
    public void IsStaleAgainst_When_IncomingLsnIsEqual_Then_ReturnsTrue()
    {
        // An exact redelivery is the common case during a replay. Treating it as stale keeps
        // the rebuild cheap: no rewrite, and no needless embedding invalidation.
        Event(lsn: 200).IsStaleAgainst(rowLsn: 200, rowTimestamp: null).ShouldBeTrue();
    }

    [Fact]
    public void IsStaleAgainst_When_NeitherSideHasMetadata_Then_ReturnsFalse()
    {
        // Messages published before add.fields was configured must still be applied.
        Event().IsStaleAgainst(rowLsn: null, rowTimestamp: null).ShouldBeFalse();
    }

    [Fact]
    public void IsStaleAgainst_When_RowHasNoLsn_Then_FallsBackToTimestamp()
    {
        var older = Event(timestampMs: 1_000);
        older.IsStaleAgainst(rowLsn: null, rowTimestamp: new DateTime(2026, 1, 1, 0, 0, 2, DateTimeKind.Utc))
            .ShouldBeTrue();
    }

    [Fact]
    public void IsStaleAgainst_When_TimestampsAreEqual_Then_ReturnsFalse()
    {
        // ts_ms has millisecond resolution and several rows commonly share a commit
        // timestamp, so equality must not be treated as stale — that would drop real changes.
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var sameInstant = Event(timestampMs: new DateTimeOffset(timestamp).ToUnixTimeMilliseconds());

        sameInstant.IsStaleAgainst(rowLsn: null, rowTimestamp: timestamp).ShouldBeFalse();
    }

    [Fact]
    public void IsStaleAgainst_When_LsnIsPresentOnBothSides_Then_IgnoresTimestamp()
    {
        // LSN is the authoritative order; a misleading timestamp must not override it.
        var newerLsnOlderTimestamp = Event(lsn: 300, timestampMs: 1_000);

        newerLsnOlderTimestamp
            .IsStaleAgainst(rowLsn: 200, rowTimestamp: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .ShouldBeFalse();
    }

    [Fact]
    public void SourceTimestamp_When_TimestampMsIsSet_Then_ConvertsFromUnixMilliseconds()
        => Event(timestampMs: 1_767_225_600_000).SourceTimestamp
            .ShouldBe(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void SourceTimestamp_When_TimestampMsIsAbsent_Then_ReturnsNull()
        => Event().SourceTimestamp.ShouldBeNull();

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void IsDeleted_When_DeletedRawVaries_Then_ParsesCaseInsensitively(string? deletedRaw, bool expected)
    {
        var cdcEvent = Event();
        cdcEvent.DeletedRaw = deletedRaw;

        cdcEvent.IsDeleted.ShouldBe(expected);
    }
}
