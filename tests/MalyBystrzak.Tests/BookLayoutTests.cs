using MalyBystrzak;

namespace MalyBystrzak.Tests;

public class BookLayoutTests
{
    [Theory]
    [InlineData(60, 12, 10)]
    [InlineData(1, 4, 1)]
    [InlineData(7, 4, 2)]
    [InlineData(72, 16, 12)]
    public void PagesArePaddedToCompleteSignatures(int count, int expectedPages, int expectedPuzzlePages)
    {
        var puzzles = FakePuzzles(count);
        var pages = BookLayout.BuildPages(puzzles);

        Assert.Equal(expectedPages, pages.Count);
        Assert.Equal(BookPageKind.FrontCover, pages.First().Kind);
        Assert.Equal(BookPageKind.BackCover, pages.Last().Kind);
        Assert.Equal(expectedPuzzlePages, pages.Count(page => page.Kind == BookPageKind.Puzzles));
        Assert.Equal(0, pages.Count % 4);
        Assert.Equal(count, pages.Where(page => page.Puzzles is not null).Sum(page => page.Puzzles!.Count));
    }

    [Theory]
    [InlineData(4, 4, 1, 2, 3)]
    [InlineData(8, 8, 1, 2, 7)]
    [InlineData(12, 12, 1, 2, 11)]
    public void FirstSheetHasCorrectBookletPairing(int pageCount, int frontLeft, int frontRight, int backLeft, int backRight)
    {
        var sides = BookLayout.CreateBookletOrder(pageCount);

        Assert.Equal(new SheetSide(frontLeft, frontRight), sides[0]);
        Assert.Equal(new SheetSide(backLeft, backRight), sides[1]);
        Assert.Equal(pageCount / 2, sides.Count);
        Assert.Equal(Enumerable.Range(1, pageCount).Order(),
            sides.SelectMany(side => new[] { side.LeftPage, side.RightPage }).Order());
    }

    private static IReadOnlyList<SudokuPuzzle> FakePuzzles(int count) =>
        Enumerable.Range(1, count)
            .Select(number => new SudokuPuzzle(number, 4, 2, 2, Difficulty.Level1, new int[16], new int[16]))
            .ToArray();
}

