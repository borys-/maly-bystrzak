using MalyBystrzak.Core;

namespace MalyBystrzak.Tests;

public class BookLayoutTests
{
    [Theory]
    [InlineData(12, 2, 12, 12, 100)]
    [InlineData(7, 2, 7, 12, 58)]
    public void EstimatesStandardWorksheetPageUsage(int count, int pages, int used, int capacity, int utilization)
    {
        var estimate = BookLayout.EstimateWorksheetPages(count, [WorksheetLayout.Standard]);
        Assert.Equal((pages, used, capacity, utilization),
            (estimate.PageCount, estimate.UsedSlots, estimate.CapacitySlots, estimate.UtilizationPercentage));
    }

    [Fact]
    public void EstimatesUsageForOneSmallAndTwoLargeVariants()
    {
        var estimate = BookLayout.EstimateWorksheetPages(100,
            [WorksheetLayout.Standard, WorksheetLayout.Large, WorksheetLayout.Large]);
        Assert.Equal((66, 298, 396, 75),
            (estimate.PageCount, estimate.UsedSlots, estimate.CapacitySlots, estimate.UtilizationPercentage));
    }

    [Fact]
    public void FindsNextCountThatFillsEveryWorksheetPage()
    {
        Assert.Equal(12, BookLayout.FindNextFullPageCount(7, [WorksheetLayout.Standard]));
        Assert.Equal(9, BookLayout.FindNextFullPageCount(6,
            [WorksheetLayout.Large, WorksheetLayout.Standard, WorksheetLayout.Standard]));
        Assert.Null(BookLayout.FindNextFullPageCount(4, [WorksheetLayout.Large], 20));
    }

    [Fact]
    public void FindsCountThatUsesBlankBookletPagesWithoutAddingSheets()
    {
        Assert.Equal(36, BookLayout.FindCountToFillWorksheetPages(24, [WorksheetLayout.Standard], 6));
        Assert.Equal(30, BookLayout.FindCountToFillWorksheetPages(24, [WorksheetLayout.Standard], 5));
        Assert.Equal(12, BookLayout.FindCountToFillWorksheetPages(7, [WorksheetLayout.Standard], 2));
        Assert.Null(BookLayout.FindCountToFillWorksheetPages(12, [WorksheetLayout.Standard], 2));
    }

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

    [Fact]
    public void LargeWorksheetOccupiesFourSlots()
    {
        var worksheets = FakeWorksheets(3).ToArray();
        worksheets[2] = worksheets[2] with { Layout = WorksheetLayout.Large };

        var page = Assert.Single(BookLayout.PackWorksheets(worksheets));
        var large = Assert.Single(page, placement => placement.Worksheet.Layout == WorksheetLayout.Large);

        Assert.Equal((0, 2, 2, 4), (large.Column, large.Row, large.ColumnSpan, large.RowSpan));
    }

    [Fact]
    public void LargeWorksheetMovesToNextPageWhenNoTwoByTwoAreaRemains()
    {
        var worksheets = FakeWorksheets(4).ToArray();
        worksheets[3] = worksheets[3] with { Layout = WorksheetLayout.Large };

        var pages = BookLayout.PackWorksheets(worksheets);

        Assert.Equal(2, pages.Count);
        Assert.Equal(3, pages[0].Count);
        Assert.Single(pages[1]);
    }

    [Fact]
    public void FullPagesArePlacedBeforePagesWithFreeSlots()
    {
        var worksheets = FakeWorksheets(6).ToArray();
        worksheets[0] = worksheets[0] with { Layout = WorksheetLayout.Large };
        worksheets[1] = worksheets[1] with { Layout = WorksheetLayout.Large };
        worksheets[2] = worksheets[2] with { Layout = WorksheetLayout.Large };

        var pages = BookLayout.PackWorksheets(BookLayout.ArrangeForFullPages(worksheets));
        var usedSlots = pages.Select(page => page.Sum(item => Weight(item.Worksheet.Layout))).ToArray();

        Assert.Equal(6, usedSlots[0]);
        Assert.Equal(new[] { 6, 5, 4 }, usedSlots);
    }

    [Fact]
    public void LargeAndStandardWorksheetsFillEveryPageWhenCountsMatch()
    {
        var worksheets = FakeWorksheets(12).ToArray();
        for (var index = 0; index < 4; index++) worksheets[index] = worksheets[index] with { Layout = WorksheetLayout.Large };

        var pages = BookLayout.PackWorksheets(BookLayout.ArrangeForFullPages(worksheets));

        Assert.Equal(4, pages.Count);
        Assert.All(pages, page => Assert.Equal(6, page.Sum(item => Weight(item.Worksheet.Layout))));
    }

    [Fact]
    public void HalfAndFullPageWorksheetsUseExpectedSpace()
    {
        var worksheets = FakeWorksheets(3).ToArray();
        worksheets[0] = worksheets[0] with { Layout = WorksheetLayout.HalfPage };
        worksheets[1] = worksheets[1] with { Layout = WorksheetLayout.HalfPage };
        worksheets[2] = worksheets[2] with { Layout = WorksheetLayout.FullPage };

        var pages = BookLayout.PackWorksheets(worksheets);

        Assert.Equal(2, pages.Count);
        Assert.Equal(6, pages[0].Sum(item => Weight(item.Worksheet.Layout)));
        Assert.Equal(6, pages[1].Sum(item => Weight(item.Worksheet.Layout)));
        Assert.Equal((2, 3), (pages[0][0].ColumnSpan, pages[0][0].RowSpan));
        Assert.Equal((2, 6), (pages[1][0].ColumnSpan, pages[1][0].RowSpan));
    }

    private static int Weight(WorksheetLayout layout) => layout switch
    {
        WorksheetLayout.Standard => 1, WorksheetLayout.Large => 4,
        WorksheetLayout.HalfPage => 3, WorksheetLayout.FullPage => 6, _ => 0
    };

    private static IReadOnlyList<GeneratedWorksheet> FakeWorksheets(int count)
    {
        var visual = new WorksheetVisual(100, 100, []);
        var difficulty = CognitiveDifficulty.Create(20, 20, 20, 20, 20, 0);
        var instruction = new WorksheetInstruction("Fake", "Pierwsza linia", "Druga linia", "#25316d");
        return Enumerable.Range(1, count).Select(number => new GeneratedWorksheet(number, "fake", "a", "Fake",
            number.ToString(), difficulty, 2, visual, visual, instruction)).ToArray();
    }
}
