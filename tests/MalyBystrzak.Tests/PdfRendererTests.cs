using System.Text;
using MalyBystrzak.Core;
using MalyBystrzak.Modules.Sudoku;
using MalyBystrzak.Pdf;
using PdfSharp.Pdf.IO;

namespace MalyBystrzak.Tests;

public class PdfRendererTests
{
    [Fact]
    public void SharedRendererCreatesPreviewAndBookletBytes()
    {
        var registry = new WorksheetModuleRegistry([new SudokuModule()]);
        var book = new BookGenerator(registry).Generate(new("Moja książeczka", "Łamigłówki", "Zosia", 6, 12345,
            [new("sudoku", "4x4")], IncludeSolutions: true));
        var renderer = new BookPdfRenderer();
        var preview = renderer.RenderPreview(book.CreateDocument());
        var booklet = renderer.RenderBooklet(book.CreateDocument());
        Assert.True(preview.Length > 10_000);
        Assert.True(booklet.Length > 10_000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(preview, 0, 4));
        Assert.Equal("%PDF", Encoding.ASCII.GetString(booklet, 0, 4));
    }

    [Fact]
    public void PreviewAndBookletHaveExpectedPageCountsAndFormats()
    {
        var registry = new WorksheetModuleRegistry([new SudokuModule()]);
        var book = new BookGenerator(registry).Generate(new("Książeczka", "Łamigłówki", "Zosia", 7, 54321,
            [new("sudoku", "4x4")], IncludeSolutions: true));
        var renderer = new BookPdfRenderer();

        using var preview = PdfReader.Open(new MemoryStream(renderer.RenderPreview(book.CreateDocument())));
        using var booklet = PdfReader.Open(new MemoryStream(renderer.RenderBooklet(book.CreateDocument())));

        Assert.Equal(book.CreateDocument().Pages.Count, preview.PageCount);
        Assert.Equal(book.CreateDocument().Pages.Count / 2, booklet.PageCount);
        foreach (var page in Enumerable.Range(0, preview.PageCount).Select(index => preview.Pages[index]))
        {
            Assert.InRange(page.Width.Point, 419, 420);
            Assert.InRange(page.Height.Point, 595, 596);
        }
        foreach (var page in Enumerable.Range(0, booklet.PageCount).Select(index => booklet.Pages[index]))
        {
            Assert.InRange(page.Width.Point, 839, 840);
            Assert.InRange(page.Height.Point, 595, 596);
        }
    }

    [Fact]
    public void CsvContainsEveryWorksheet()
    {
        var worksheets = new SudokuModule().Generate(new("4x4", 6, 7));
        var csv = Encoding.UTF8.GetString(DifficultyReport.Create(worksheets));
        Assert.Contains("Numer;Typ;Wynik_0_100", csv);
        Assert.Equal(7, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
