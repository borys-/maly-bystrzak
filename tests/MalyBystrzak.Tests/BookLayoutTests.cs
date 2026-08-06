using MalyBystrzak.Core;

namespace MalyBystrzak.Tests;

public class BookLayoutTests
{
    [Theory]
    [InlineData(60, false, 12, 10)]
    [InlineData(1, false, 4, 1)]
    [InlineData(7, false, 4, 2)]
    [InlineData(6, true, 4, 1)]
    [InlineData(60, true, 24, 10)]
    public void PagesArePaddedToCompleteSignatures(int count, bool solutions, int expectedPages, int expectedTaskPages)
    {
        var pages = BookLayout.BuildPages(FakeWorksheets(count), solutions);
        Assert.Equal(expectedPages, pages.Count);
        Assert.Equal(BookPageKind.FrontCover, pages.First().Kind);
        Assert.Equal(BookPageKind.BackCover, pages.Last().Kind);
        Assert.Equal(expectedTaskPages, pages.Count(page => page.Kind == BookPageKind.Worksheets));
        Assert.Equal(solutions ? expectedTaskPages : 0, pages.Count(page => page.Kind == BookPageKind.Solutions));
        Assert.Equal(0, pages.Count % 4);
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
        Assert.Equal(Enumerable.Range(1, pageCount).Order(), sides.SelectMany(side => new[] { side.LeftPage, side.RightPage }).Order());
    }

    private static IReadOnlyList<GeneratedWorksheet> FakeWorksheets(int count)
    {
        var visual = new WorksheetVisual(100, 100, []);
        var difficulty = CognitiveDifficulty.Create(20, 20, 20, 20, 20, 0);
        var instruction = new WorksheetInstruction("Fake", "Pierwsza linia", "Druga linia", "#25316d");
        return Enumerable.Range(1, count).Select(number => new GeneratedWorksheet(number, "fake", "a", "Fake",
            number.ToString(), difficulty, 2, visual, visual, instruction)).ToArray();
    }
}
