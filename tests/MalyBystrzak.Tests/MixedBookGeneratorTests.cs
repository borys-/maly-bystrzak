using MalyBystrzak;

namespace MalyBystrzak.Tests;

public class MixedBookGeneratorTests
{
    private static readonly PuzzleType[] AllTypes =
        [PuzzleType.Sudoku4, PuzzleType.Sudoku6, PuzzleType.Kakuro3, PuzzleType.Kakuro4];

    [Fact]
    public void MixedBookCyclesThroughSelectedTypesAndKeepsGlobalNumbering()
    {
        var puzzles = new MixedBookGenerator(112233).GenerateBook(60, AllTypes);

        Assert.Equal(60, puzzles.Count);
        Assert.Equal(Enumerable.Range(1, 60), puzzles.Select(puzzle => puzzle.Number));
        Assert.All(AllTypes, type => Assert.Equal(15, puzzles.Count(puzzle => puzzle.Type == type)));
        for (var index = 0; index < puzzles.Count; index++)
            Assert.Equal(AllTypes[index % AllTypes.Length], puzzles[index].Type);
    }

    [Fact]
    public void EveryMixedPuzzleHasOneSolution()
    {
        var puzzles = new MixedBookGenerator(445566).GenerateBook(18, AllTypes);

        Assert.All(puzzles, puzzle =>
        {
            if (puzzle.Sudoku is not null)
                Assert.Equal(1, SudokuSolver.CountSolutions(puzzle.Sudoku.Cells, puzzle.Sudoku.Size,
                    puzzle.Sudoku.BlockRows, puzzle.Sudoku.BlockColumns));
            else
                Assert.Equal(1, KakuroSolver.CountSolutions(puzzle.Kakuro!));
        });
    }

    [Fact]
    public void MixedBookUsesBookletPageCount()
    {
        var puzzles = new MixedBookGenerator(778899).GenerateBook(60, AllTypes);
        var pages = MixedBookLayout.BuildPages(puzzles);

        Assert.Equal(12, pages.Count);
        Assert.Equal(10, pages.Count(page => page.Kind == BookPageKind.Puzzles));
    }

    [Fact]
    public void PersonalizedBookHonorsRangeAndCreatesEqualRelativeStarGroups()
    {
        var puzzles = new MixedBookGenerator(998877).GeneratePersonalizedBook(108, AllTypes, 20, 80);

        Assert.Equal(108, puzzles.Count);
        Assert.All(puzzles, puzzle => Assert.InRange(puzzle.CognitiveScore, 20, 80));
        Assert.Equal(puzzles.Select(puzzle => puzzle.CognitiveScore).Order(),
            puzzles.Select(puzzle => puzzle.CognitiveScore));
        Assert.Equal(new[] { 22, 22, 22, 21, 21 }, Enumerable.Range(1, 5)
            .Select(stars => puzzles.Count(puzzle => puzzle.DisplayStars == stars)));
        Assert.All(AllTypes, type => Assert.Equal(27, puzzles.Count(puzzle => puzzle.Type == type)));
    }

    [Fact]
    public void PersonalizedBookRequiresAtLeastFiveTasks() =>
        Assert.Throws<ArgumentException>(() =>
            new MixedBookGenerator(1).GeneratePersonalizedBook(4, AllTypes, 20, 80));
}

