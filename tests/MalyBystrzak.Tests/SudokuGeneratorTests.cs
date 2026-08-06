using MalyBystrzak;

namespace MalyBystrzak.Tests;

public class SudokuGeneratorTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public void GeneratedPuzzlesHaveValidUniqueSolutions(int size)
    {
        var puzzles = new SudokuGenerator(12345).GenerateBook(9, size);

        Assert.Equal(9, puzzles.Count);
        foreach (var puzzle in puzzles)
        {
            Assert.True(SudokuSolver.IsValidComplete(
                puzzle.Solution, puzzle.Size, puzzle.BlockRows, puzzle.BlockColumns));
            Assert.Equal(1, SudokuSolver.CountSolutions(
                puzzle.Cells, puzzle.Size, puzzle.BlockRows, puzzle.BlockColumns));
        }
    }

    [Fact]
    public void SameSeedProducesSamePuzzles()
    {
        var first = new SudokuGenerator(9876).GenerateBook(6, 4);
        var second = new SudokuGenerator(9876).GenerateBook(6, 4);

        Assert.Equal(first.SelectMany(puzzle => puzzle.Cells), second.SelectMany(puzzle => puzzle.Cells));
        Assert.Equal(first.SelectMany(puzzle => puzzle.Solution), second.SelectMany(puzzle => puzzle.Solution));
    }

    [Fact]
    public void DifficultyProgressesInSixStarLevels()
    {
        var puzzles = new SudokuGenerator(42).GenerateBook(60, 4);

        var levels = Enum.GetValues<Difficulty>();
        for (var index = 0; index < levels.Length; index++)
        {
            var group = puzzles.Skip(index * 10).Take(10).ToArray();
            Assert.All(group, puzzle => Assert.Equal(levels[index], puzzle.Difficulty));
            Assert.All(group, puzzle => Assert.Equal(index + 1, (int)puzzle.Difficulty));
        }

        for (var boundary = 10; boundary < puzzles.Count; boundary += 10)
            Assert.True(puzzles[boundary - 1].ClueCount > puzzles[boundary].ClueCount);

        Assert.Equal(11, puzzles[0].ClueCount);
        Assert.Equal(6, puzzles[^1].ClueCount);
    }

    [Fact]
    public void UnsupportedSizeIsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SudokuGenerator(1).GenerateBook(1, 9));
}

