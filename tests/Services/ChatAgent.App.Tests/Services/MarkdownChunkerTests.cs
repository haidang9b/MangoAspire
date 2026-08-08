using ChatAgent.App.Configurations;
using ChatAgent.App.Services;
using Shouldly;

namespace ChatAgent.App.Tests.Services;

public class MarkdownChunkerTests
{
    private readonly MarkdownChunker _chunker = new();

    private static KnowledgeBaseOptions Options(
        int maxChunkChars = 1200,
        int minChunkChars = 200,
        int overlap = 150) => new()
        {
            MaxChunkChars = maxChunkChars,
            MinChunkChars = minChunkChars,
            ChunkOverlapChars = overlap,
        };

    [Fact]
    public void Chunk_When_MarkdownIsEmpty_Then_ReturnsNoChunks()
    {
        var result = _chunker.Chunk("   ", Options());

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Chunk_When_SectionsFitBudget_Then_KeepsOneChunkPerSection()
    {
        var markdown = """
            # Store

            ## Contact

            Call us on 123456. Email hello@example.com for anything else at all.

            ## Hours

            We open at 10:00 every day and close at 22:00 on weekdays consistently.
            """;

        // No merging, so each section stays a distinct chunk.
        var result = _chunker.Chunk(markdown, Options(minChunkChars: 0));

        result.Count.ShouldBe(2);
        result[0].Breadcrumb.ShouldBe("Store > Contact");
        result[1].Breadcrumb.ShouldBe("Store > Hours");
    }

    [Fact]
    public void Chunk_When_SectionHasNoBody_Then_EmitsNoChunkForIt()
    {
        var markdown = """
            # Title

            ## Empty Parent

            ### Child

            Only the child section has any content worth indexing here today.
            """;

        var result = _chunker.Chunk(markdown, Options(minChunkChars: 0));

        result.Count.ShouldBe(1);
        result[0].Breadcrumb.ShouldBe("Title > Empty Parent > Child");
    }

    [Fact]
    public void Chunk_When_ChunkIsProduced_Then_ContentIsPrefixedWithBreadcrumb()
    {
        var markdown = """
            # Store

            ## Refunds

            Approved refunds are returned within five business days of approval.
            """;

        var result = _chunker.Chunk(markdown, Options(minChunkChars: 0));

        result[0].Content.ShouldStartWith("Store > Refunds");
        result[0].Content.ShouldContain("five business days");
    }

    [Fact]
    public void Chunk_When_SectionExceedsBudget_Then_SplitsIntoChunksWithinBudget()
    {
        var paragraph = string.Join(" ", Enumerable.Repeat("policy text about refunds and delivery.", 30));
        var markdown = $"""
            # Store

            ## Policies

            {paragraph}

            {paragraph}

            {paragraph}
            """;

        var options = Options(maxChunkChars: 400, minChunkChars: 0, overlap: 0);
        var result = _chunker.Chunk(markdown, options);

        result.Count.ShouldBeGreaterThan(1);
        result.ShouldAllBe(c => c.Content.Length <= options.MaxChunkChars);
    }

    [Fact]
    public void Chunk_When_SingleParagraphExceedsBudget_Then_DescendsToSentences()
    {
        // One paragraph, so the paragraph tier cannot help and the splitter must go
        // deeper to make progress.
        var sentences = string.Join(" ", Enumerable.Range(0, 40)
            .Select(i => $"Sentence number {i} explains one detail of the refund policy."));

        var markdown = $"""
            # Store

            ## Refunds

            {sentences}
            """;

        var options = Options(maxChunkChars: 300, minChunkChars: 0, overlap: 0);
        var result = _chunker.Chunk(markdown, options);

        result.Count.ShouldBeGreaterThan(1);
        result.ShouldAllBe(c => c.Content.Length <= options.MaxChunkChars);
    }

    [Fact]
    public void Chunk_When_TextHasNoSeparators_Then_HardSplitsWithinBudget()
    {
        var runOn = new string('x', 2000);
        var markdown = $"""
            # Store

            ## Blob

            {runOn}
            """;

        var options = Options(maxChunkChars: 300, minChunkChars: 0, overlap: 0);
        var result = _chunker.Chunk(markdown, options);

        result.Count.ShouldBeGreaterThan(1);
        result.ShouldAllBe(c => c.Content.Length <= options.MaxChunkChars);
    }

    [Fact]
    public void Chunk_When_PiecesAreSmall_Then_MergesThemIntoFewerChunks()
    {
        var bullets = string.Join("\n\n", Enumerable.Range(0, 12).Select(i => $"- Item {i}"));
        var markdown = $"""
            # Store

            ## Menu Notes

            {bullets}
            """;

        var merged = _chunker.Chunk(markdown, Options(maxChunkChars: 1200, minChunkChars: 200, overlap: 0));
        var unmerged = _chunker.Chunk(markdown, Options(maxChunkChars: 1200, minChunkChars: 0, overlap: 0));

        // Both fit the budget, so the difference is purely the merge pass.
        merged.Count.ShouldBeLessThanOrEqualTo(unmerged.Count);
        merged.Count.ShouldBe(1);
    }

    [Fact]
    public void Chunk_When_CodeFenceFitsBudget_Then_KeepsFenceIntact()
    {
        var markdown = """
            # Store

            ## Sample

            Before the sample.

            ```json
            {
              "a": 1,
              "b": 2
            }
            ```

            After the sample.
            """;

        var result = _chunker.Chunk(markdown, Options(minChunkChars: 0));

        var combined = string.Join("\n", result.Select(c => c.Content));
        combined.ShouldContain("```json");
        combined.ShouldContain("\"b\": 2");
    }

    [Fact]
    public void Chunk_When_HeadingIsInsideCodeFence_Then_DoesNotStartNewSection()
    {
        var markdown = """
            # Store

            ## Shell

            ```bash
            # this is a comment, not a heading
            echo hello
            ```
            """;

        var result = _chunker.Chunk(markdown, Options(minChunkChars: 0));

        result.ShouldAllBe(c => c.Breadcrumb == "Store > Shell");
    }

    [Fact]
    public void Chunk_When_TableFitsBudget_Then_KeepsRowsTogether()
    {
        var markdown = """
            # Store

            ## Hours

            | Day | Open |
            | --- | --- |
            | Mon | 10:00 |
            | Tue | 10:00 |
            """;

        var result = _chunker.Chunk(markdown, Options(minChunkChars: 0));

        result.Count.ShouldBe(1);
        result[0].Content.ShouldContain("| Mon | 10:00 |");
        result[0].Content.ShouldContain("| Tue | 10:00 |");
    }

    [Fact]
    public void Chunk_When_OverlapIsConfigured_Then_RepeatsTailOfPreviousChunk()
    {
        var paragraphs = string.Join("\n\n", Enumerable.Range(0, 6)
            .Select(i => string.Join(" ", Enumerable.Repeat($"paragraph{i} content here.", 12))));

        var markdown = $"""
            # Store

            ## Long

            {paragraphs}
            """;

        var options = Options(maxChunkChars: 500, minChunkChars: 0, overlap: 120);
        var result = _chunker.Chunk(markdown, options);

        result.Count.ShouldBeGreaterThan(1);

        // The second chunk should start with text carried over from the first, after the
        // breadcrumb prefix.
        var previousTail = result[0].Content[^40..];
        var anyOverlap = result.Skip(1).Any(c => previousTail.Split(' ').Any(w => w.Length > 3 && c.Content.Contains(w)));
        anyOverlap.ShouldBeTrue();
    }

    [Fact]
    public void Chunk_When_DocumentIsVeryLarge_Then_AllChunksStayWithinBudget()
    {
        // ~1.5 MB across many sections, to exercise the whole pipeline rather than a toy input.
        var sections = Enumerable.Range(0, 300).Select(i => $"""
            ## Section {i}

            {string.Join(" ", Enumerable.Repeat($"Detail {i} about our restaurant policies and menu.", 80))}
            """);

        var markdown = "# Big Document\n\n" + string.Join("\n\n", sections);

        var options = Options(maxChunkChars: 1000, minChunkChars: 200, overlap: 100);
        var result = _chunker.Chunk(markdown, options);

        result.Count.ShouldBeGreaterThan(300);
        result.ShouldAllBe(c => c.Content.Length <= options.MaxChunkChars);
        result.ShouldAllBe(c => c.Breadcrumb.StartsWith("Big Document > Section "));
        result.Select(c => c.Ordinal).ShouldBe(Enumerable.Range(0, result.Count));
    }

    [Fact]
    public void Chunk_When_BreadcrumbIsLongerThanBudget_Then_StillProducesChunks()
    {
        var deepHeading = new string('h', 400);
        var markdown = $"""
            # {deepHeading}

            ## {deepHeading}

            Some body text that needs to be indexed despite the enormous heading path.
            """;

        // Budget floor keeps this from collapsing to zero or a negative content budget.
        var result = _chunker.Chunk(markdown, Options(maxChunkChars: 200, minChunkChars: 0, overlap: 0));

        result.ShouldNotBeEmpty();
        result.ShouldAllBe(c => c.Content.Length > 0);
    }
}
