namespace MalyBystrzak.Core;

public enum BookPageKind { FrontCover, Worksheets, Solutions, Blank, BackCover }
public sealed record BookPage(BookPageKind Kind, IReadOnlyList<GeneratedWorksheet>? Worksheets = null);
public sealed record SheetSide(int LeftPage, int RightPage);
public sealed record BookDocument(BookGenerationSettings Settings, IReadOnlyList<BookPage> Pages);

public static class BookLayout
{
    public static IReadOnlyList<BookPage> BuildPages(IReadOnlyList<GeneratedWorksheet> worksheets, bool includeSolutions = true)
    {
        var pages = new List<BookPage> { new(BookPageKind.FrontCover) };
        pages.AddRange(worksheets.Chunk(6).Select(group => new BookPage(BookPageKind.Worksheets, group)));
        if (includeSolutions)
            pages.AddRange(worksheets.Chunk(6).Select(group => new BookPage(BookPageKind.Solutions, group)));
        while ((pages.Count + 1) % 4 != 0) pages.Add(new BookPage(BookPageKind.Blank));
        pages.Add(new BookPage(BookPageKind.BackCover));
        return pages;
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
