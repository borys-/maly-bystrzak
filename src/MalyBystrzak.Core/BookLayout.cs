namespace MalyBystrzak.Core;

public enum BookPageKind { FrontCover, Worksheets, Solutions, Blank, BackCover }
public sealed record WorksheetPlacement(GeneratedWorksheet Worksheet, int Column, int Row, int ColumnSpan, int RowSpan);
public sealed record BookPage(BookPageKind Kind, IReadOnlyList<WorksheetPlacement>? Placements = null);
public sealed record SheetSide(int LeftPage, int RightPage);
public sealed record BookDocument(BookGenerationSettings Settings, IReadOnlyList<BookPage> Pages,
    IReadOnlyList<WorksheetInstruction> Instructions);

public static class BookLayout
{
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
        var pages = new List<IReadOnlyList<WorksheetPlacement>>();
        var placements = new List<WorksheetPlacement>();
        var occupied = new bool[3, 2];
        foreach (var worksheet in worksheets)
        {
            var span = worksheet.Layout == WorksheetLayout.Large ? 2 : 1;
            if (!TryPlace(worksheet, span, occupied, placements))
            {
                pages.Add(placements.ToArray());
                placements = [];
                occupied = new bool[3, 2];
                if (!TryPlace(worksheet, span, occupied, placements))
                    throw new InvalidOperationException("Zadanie nie mieści się na stronie książeczki.");
            }
        }
        if (placements.Count > 0) pages.Add(placements.ToArray());
        return pages;
    }

    private static bool TryPlace(GeneratedWorksheet worksheet, int span, bool[,] occupied,
        ICollection<WorksheetPlacement> placements)
    {
        for (var row = 0; row <= 3 - span; row++)
        for (var column = 0; column <= 2 - span; column++)
        {
            var available = true;
            for (var checkedRow = row; checkedRow < row + span; checkedRow++)
            for (var checkedColumn = column; checkedColumn < column + span; checkedColumn++)
                available &= !occupied[checkedRow, checkedColumn];
            if (!available) continue;
            for (var occupiedRow = row; occupiedRow < row + span; occupiedRow++)
            for (var occupiedColumn = column; occupiedColumn < column + span; occupiedColumn++)
                occupied[occupiedRow, occupiedColumn] = true;
            placements.Add(new(worksheet, column, row, span, span));
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
