using MalyBystrzak.Modules.Kakuro;
using MalyBystrzak.Modules.Sudoku;

namespace MalyBystrzak.Tests;

public class CognitiveDifficultyTests
{
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
