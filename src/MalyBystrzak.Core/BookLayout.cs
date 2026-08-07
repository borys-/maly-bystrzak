namespace MalyBystrzak.Core;

public enum BookPageKind { FrontCover, Worksheets, Solutions, Blank, BackCover }
public sealed record WorksheetPlacement(GeneratedWorksheet Worksheet, int Column, int Row, int ColumnSpan, int RowSpan);
public sealed record BookPage(BookPageKind Kind, IReadOnlyList<WorksheetPlacement>? Placements = null);
public sealed record SheetSide(int LeftPage, int RightPage);
public sealed record BookDocument(BookGenerationSettings Settings, IReadOnlyList<BookPage> Pages,
    IReadOnlyList<WorksheetInstruction> Instructions);
public sealed record WorksheetPageEstimate(int PageCount, int UsedSlots, int CapacitySlots)
{
    public int FreeSlots => CapacitySlots - UsedSlots;
    public int UtilizationPercentage => CapacitySlots == 0 ? 0 :
        (int)Math.Round(UsedSlots * 100d / CapacitySlots);
}

public static class BookLayout
{
    public static WorksheetPageEstimate EstimateWorksheetPages(int count,
        IReadOnlyList<WorksheetLayout> selectionCycle)
    {
        if (count <= 0 || selectionCycle.Count == 0) return new(0, 0, 0);
        var layouts = Enumerable.Range(0, count).Select(index => selectionCycle[index % selectionCycle.Count]).ToArray();
        var pages = PackLayouts(layouts);
        return new(pages, layouts.Sum(LayoutWeight), pages * 6);
    }

    public static int? FindNextFullPageCount(int currentCount, IReadOnlyList<WorksheetLayout> selectionCycle,
        int maximumCount = 180)
    {
        if (selectionCycle.Count == 0) return null;
        for (var candidate = currentCount + 1; candidate <= maximumCount; candidate++)
        {
            var estimate = EstimateWorksheetPages(candidate, selectionCycle);
            if (estimate.FreeSlots == 0) return candidate;
        }
        return null;
    }

    public static int? FindCountToFillWorksheetPages(int currentCount, IReadOnlyList<WorksheetLayout> selectionCycle,
        int targetPageCount, int maximumCount = 180)
    {
        if (selectionCycle.Count == 0 || targetPageCount <= 0) return null;
        for (var candidate = currentCount + 1; candidate <= maximumCount; candidate++)
        {
            var estimate = EstimateWorksheetPages(candidate, selectionCycle);
            if (estimate.PageCount == targetPageCount && estimate.FreeSlots == 0) return candidate;
            if (estimate.PageCount > targetPageCount) return null;
        }
        return null;
    }

    public static IReadOnlyList<GeneratedWorksheet> ArrangeForFullPages(
        IReadOnlyList<GeneratedWorksheet> worksheets)
    {
        return worksheets.OrderByDescending(item => LayoutWeight(item.Layout)).ToArray();
    }

    public static IReadOnlyList<BookPage> BuildPages(IReadOnlyList<GeneratedWorksheet> worksheets, bool includeSolutions = true)
    {
        var pages = new List<BookPage> { new(BookPageKind.FrontCover) };
        var worksheetPages = PackWorksheets(worksheets);
        pages.AddRange(worksheetPages.Select(group => new BookPage(BookPageKind.Worksheets, group)));
        if (includeSolutions)
            pages.AddRange(worksheetPages.Select(group => new BookPage(BookPageKind.Solutions, group)));
        while ((pages.Count + 1) % 4 != 0) pages.Add(new BookPage(BookPageKind.Blank));
        pages.Add(new BookPage(BookPageKind.BackCover));
        return pages;
    }

    public static IReadOnlyList<BookPage> BuildSolutionPages(IReadOnlyList<GeneratedWorksheet> worksheets) =>
        PackWorksheets(worksheets).Select(group => new BookPage(BookPageKind.Solutions, group)).ToArray();

    public static IReadOnlyList<IReadOnlyList<WorksheetPlacement>> PackWorksheets(
        IReadOnlyList<GeneratedWorksheet> worksheets)
    {
        var pagePlacements = new List<List<WorksheetPlacement>>();
        var pageOccupancy = new List<bool[,]>();
        foreach (var worksheet in worksheets)
        {
            var (columnSpan, rowSpan) = LayoutSpan(worksheet.Layout);
            var placed = false;
            for (var pageIndex = 0; pageIndex < pagePlacements.Count && !placed; pageIndex++)
                placed = TryPlace(worksheet, columnSpan, rowSpan, pageOccupancy[pageIndex], pagePlacements[pageIndex]);
            if (!placed)
            {
                var occupied = new bool[6, 2];
                var placements = new List<WorksheetPlacement>();
                if (!TryPlace(worksheet, columnSpan, rowSpan, occupied, placements))
                    throw new InvalidOperationException("Zadanie nie mieści się na stronie książeczki.");
                pageOccupancy.Add(occupied);
                pagePlacements.Add(placements);
            }
        }
        return pagePlacements.Select(page => (IReadOnlyList<WorksheetPlacement>)page.ToArray()).ToArray();
    }

    private static bool TryPlace(GeneratedWorksheet worksheet, int columnSpan, int rowSpan, bool[,] occupied,
        ICollection<WorksheetPlacement> placements)
    {
        for (var row = 0; row <= 6 - rowSpan; row++)
        for (var column = 0; column <= 2 - columnSpan; column++)
        {
            var available = true;
            for (var checkedRow = row; checkedRow < row + rowSpan; checkedRow++)
            for (var checkedColumn = column; checkedColumn < column + columnSpan; checkedColumn++)
                available &= !occupied[checkedRow, checkedColumn];
            if (!available) continue;
            for (var occupiedRow = row; occupiedRow < row + rowSpan; occupiedRow++)
            for (var occupiedColumn = column; occupiedColumn < column + columnSpan; occupiedColumn++)
                occupied[occupiedRow, occupiedColumn] = true;
            placements.Add(new(worksheet, column, row, columnSpan, rowSpan));
            return true;
        }
        return false;
    }

    private static (int Columns, int Rows) LayoutSpan(WorksheetLayout layout) => layout switch
    {
        WorksheetLayout.Standard => (1, 2),
        WorksheetLayout.Large => (2, 4),
        WorksheetLayout.HalfPage => (2, 3),
        WorksheetLayout.FullPage => (2, 6),
        _ => throw new ArgumentOutOfRangeException(nameof(layout))
    };

    private static int LayoutWeight(WorksheetLayout layout) => layout switch
    {
        WorksheetLayout.Standard => 1,
        WorksheetLayout.Large => 4,
        WorksheetLayout.HalfPage => 3,
        WorksheetLayout.FullPage => 6,
        _ => 0
    };

    private static int PackLayouts(IReadOnlyList<WorksheetLayout> layouts)
    {
        var pages = new List<bool[,]>();
        foreach (var layout in layouts.OrderByDescending(LayoutWeight))
        {
            var (columns, rows) = LayoutSpan(layout);
            if (pages.Any(page => TryPlaceLayout(columns, rows, page))) continue;
            var occupied = new bool[6, 2];
            _ = TryPlaceLayout(columns, rows, occupied); pages.Add(occupied);
        }
        return pages.Count;
    }

    private static bool TryPlaceLayout(int columnSpan, int rowSpan, bool[,] occupied)
    {
        for (var row = 0; row <= 6 - rowSpan; row++)
        for (var column = 0; column <= 2 - columnSpan; column++)
        {
            var available = true;
            for (var y = row; y < row + rowSpan; y++)
            for (var x = column; x < column + columnSpan; x++) available &= !occupied[y, x];
            if (!available) continue;
            for (var y = row; y < row + rowSpan; y++)
            for (var x = column; x < column + columnSpan; x++) occupied[y, x] = true;
            return true;
        }
        return false;
    }

    public static IReadOnlyList<SheetSide> CreateBookletOrder(int pageCount)
    {
        if (pageCount <= 0 || pageCount % 4 != 0)
            throw new ArgumentException("Liczba stron broszury musi być dodatnią wielokrotnością czterech.", nameof(pageCount));
        var result = new List<SheetSide>(pageCount / 2);
        for (var sheet = 0; sheet < pageCount / 4; sheet++)
        {
            result.Add(new SheetSide(pageCount - 2 * sheet, 1 + 2 * sheet));
            result.Add(new SheetSide(2 + 2 * sheet, pageCount - 1 - 2 * sheet));
        }
        return result;
    }
}
