using MalyBystrzak.Core;
using MalyBystrzak.Modules.Sequences;

namespace MalyBystrzak.Tests;

public class SequenceGeneratorTests
{
    [Theory]
    [InlineData("pictures")]
    [InlineData("numbers")]
    public void GeneratedSequencesAreUniqueAndDeterministic(string variant)
    {
        var first = new SequenceGenerator(3456).GenerateBook(20, variant);
        var second = new SequenceGenerator(3456).GenerateBook(20, variant);
        Assert.Equal(first.Select(Fingerprint), second.Select(Fingerprint));
        Assert.Equal(20, first.Select(Fingerprint).Distinct().Count());
    }

    [Fact]
    public void NumberRowsHaveExactlyOneCatalogSolution()
    {
        var puzzles = new SequenceGenerator(18).GenerateBook(30, "numbers");
        Assert.All(puzzles.SelectMany(puzzle => puzzle.Rows), row =>
            Assert.Equal(1, SequenceGenerator.CountNumberSolutions(row)));
    }

    [Fact]
    public void PictureRowsHaveExactlyOneSupportedPeriod()
    {
        var puzzles = new SequenceGenerator(29).GenerateBook(30, "pictures");
        Assert.All(puzzles.SelectMany(puzzle => puzzle.Rows), row =>
            Assert.Equal(1, SequenceGenerator.CountPicturePeriods(row)));
    }

    [Fact]
    public void ModuleUsesNewNeutralShapes()
    {
        var worksheets = new SequenceModule().Generate(new ModuleGenerationRequest("pictures", 6, 42));
        Assert.Contains(worksheets.SelectMany(item => item.Solution.Elements), element => element is VisualEllipse);
        Assert.Contains(worksheets.SelectMany(item => item.Solution.Elements), element => element is VisualPolygon);
        Assert.All(worksheets, item => Assert.Equal("Sekwencje", item.Instruction.Title));
    }

    private static string Fingerprint(SequencePuzzle puzzle) => string.Join('|', puzzle.Rows.Select(row =>
        $"{row.Rule}:{string.Join(',', row.Numbers?.Cast<object>() ?? row.Pictures!.Cast<object>())}:{string.Join(',', row.Missing)}"));
}
