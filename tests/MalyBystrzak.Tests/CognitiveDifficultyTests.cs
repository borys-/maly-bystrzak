using MalyBystrzak.Modules.Kakuro;
using MalyBystrzak.Modules.Sudoku;

using MalyBystrzak.Core;
using MalyBystrzak.Modules.Mazes;
using MalyBystrzak.Modules.Nonograms;
using MalyBystrzak.Modules.Educational;

namespace MalyBystrzak.Tests;

public class CognitiveDifficultyTests
{
    private static readonly ModuleSelection[] EveryVariant =
    [new("sudoku", "4x4"), new("sudoku", "6x6"), new("kakuro", "3x3"), new("kakuro", "4x4"),
        new("maze", "9x9"), new("maze", "15x15"), new("nonogram", "5x5"),
        new("nonogram", "7x7"), new("nonogram", "10x10"), new("picture-equations", "animals"),
        new("arithmetic-code", "six-letter"), new("math-crossword", "chain"), new("product-grid", "3x3"),
        new("word-path", "5x4")];

    [Fact]
    public void EveryVariantUsesComparableNormalizedDistribution()
    {
        var generator = new BookGenerator(new WorksheetModuleRegistry(
            [new SudokuModule(), new KakuroModule(), new MazeModule(), new NonogramModule(),
                new PictureEquationsModule(), new ArithmeticCodeModule(), new MathCrosswordModule(),
                new ProductGridModule(), new WordPathModule()]));
        foreach (var selection in EveryVariant)
        {
            var book = generator.Generate(new("Test", "Test", null, 180, 314159, [selection]));
            var scores = book.Worksheets.Select(item => item.Difficulty.Score).Order().ToArray();
            Assert.InRange(scores[44], 20, 40);
            Assert.InRange(scores[89], 40, 60);
            Assert.InRange(scores[134], 60, 80);
        }
    }

    [Fact]
    public void NonogramTechnicalScoreCanExceedVisibleScale()
    {
        var worksheets = new NonogramModule().Generate(new("10x10", 180, 314159));
        Assert.Contains(worksheets, item => item.Difficulty.RawScore > 100);
    }

    [Fact]
    public void SudokuScoresStayInRangeAndIncreaseWithLevels()
    {
        var puzzles = new SudokuGenerator(314159).GenerateBook(60, 4);
        var levelAverages = puzzles.Chunk(10)
            .Select(group => group.Average(puzzle => puzzle.CognitiveDifficulty.Score))
            .ToArray();

        Assert.All(puzzles, puzzle => Assert.InRange(puzzle.CognitiveDifficulty.Score, 0, 100));
        Assert.True(levelAverages.SequenceEqual(levelAverages.Order()));
        Assert.Equal(6, puzzles.Select(puzzle => puzzle.CognitiveDifficulty.Stars).Distinct().Count());
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void KakuroMetricsIncludeArithmeticLoad(int size)
    {
        var puzzles = new KakuroGenerator(271828).GenerateBook(12, size);

        Assert.All(puzzles, puzzle =>
        {
            var metrics = puzzle.CognitiveDifficulty;
            Assert.InRange(metrics.Score, 0, 100);
            Assert.InRange(metrics.InformationGap, 0, 100);
            Assert.InRange(metrics.ChoiceLoad, 0, 100);
            Assert.InRange(metrics.ArithmeticLoad, 1, 100);
        });
    }
}
