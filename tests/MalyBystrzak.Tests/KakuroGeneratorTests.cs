using MalyBystrzak;

namespace MalyBystrzak.Tests;

public class KakuroGeneratorTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void GeneratedPuzzlesAreValidAndUnique(int size)
    {
        var puzzles = new KakuroGenerator(24680).GenerateBook(12, size);

        Assert.Equal(12, puzzles.Count);
        Assert.All(puzzles, puzzle =>
        {
            Assert.True(KakuroSolver.IsValidSolution(puzzle));
            Assert.Equal(1, KakuroSolver.CountSolutions(puzzle));
            Assert.Equal(size * size - (int)puzzle.Difficulty, puzzle.GivenCount);
            Assert.Equal(size, puzzle.Size);
        });
    }

    [Fact]
    public void SameSeedProducesSameKakuroBook()
    {
        var first = new KakuroGenerator(13579).GenerateBook(6);
        var second = new KakuroGenerator(13579).GenerateBook(6);

        Assert.Equal(first.SelectMany(puzzle => puzzle.Givens), second.SelectMany(puzzle => puzzle.Givens));
        Assert.Equal(first.SelectMany(puzzle => puzzle.Solution), second.SelectMany(puzzle => puzzle.Solution));
    }

    [Fact]
    public void UnsupportedKakuroSizeIsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new KakuroGenerator(1).GenerateBook(1, 5));

    [Fact]
    public void KakuroPagesUseTheSameBookletLayout()
    {
        var pages = KakuroBookLayout.BuildPages(new KakuroGenerator(7).GenerateBook(60));

        Assert.Equal(12, pages.Count);
        Assert.Equal(BookPageKind.FrontCover, pages[0].Kind);
        Assert.Equal(BookPageKind.BackCover, pages[^1].Kind);
        Assert.Equal(10, pages.Count(page => page.Kind == BookPageKind.Puzzles));
    }
}

