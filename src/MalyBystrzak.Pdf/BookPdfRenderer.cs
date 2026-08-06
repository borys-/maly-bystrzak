using MalyBystrzak.Core;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace MalyBystrzak.Pdf;

public sealed class BookPdfRenderer : IBookPdfRenderer
{
    private const double A5Width = 419.53;
    private const double A5Height = 595.28;
    private static readonly object FontLock = new();
    private static bool fontInitialized;

    public BookPdfRenderer() => EnsureFont();

    public byte[] RenderPreview(BookDocument document)
    {
        using var pdf = CreateDocument(document, "Podgląd A5");
        for (var index = 0; index < document.Pages.Count; index++)
        {
            var page = pdf.AddPage();
            page.Width = XUnit.FromPoint(A5Width);
            page.Height = XUnit.FromPoint(A5Height);
            using var graphics = XGraphics.FromPdfPage(page);
            DrawLogicalPage(graphics, document.Pages[index], document.Settings, index, 0, 0, 1);
        }
        return Save(pdf);
    }

    public byte[] RenderBooklet(BookDocument document)
    {
        using var pdf = CreateDocument(document, "Broszura A4");
        foreach (var side in BookLayout.CreateBookletOrder(document.Pages.Count))
        {
            var page = pdf.AddPage();
            page.Width = XUnit.FromPoint(A5Width * 2);
            page.Height = XUnit.FromPoint(A5Height);
            using var graphics = XGraphics.FromPdfPage(page);
            DrawLogicalPage(graphics, document.Pages[side.LeftPage - 1], document.Settings, side.LeftPage - 1, 0, 0, 1);
            graphics.DrawLine(new XPen(Color("#e5e7eb"), .5), A5Width, 0, A5Width, A5Height);
            DrawLogicalPage(graphics, document.Pages[side.RightPage - 1], document.Settings, side.RightPage - 1,
                A5Width, 0, 1);
        }
        return Save(pdf);
    }

    private static PdfDocument CreateDocument(BookDocument document, string variant)
    {
        var pdf = new PdfDocument();
        pdf.Info.Title = $"{document.Settings.Title} — {variant}";
        pdf.Info.Subject = "Książeczka z zadaniami dla dzieci";
        pdf.Info.Author = "Mały Bystrzak";
        pdf.Info.Creator = "MalyBystrzak.Pdf";
        return pdf;
    }

    private static byte[] Save(PdfDocument pdf)
    {
        using var stream = new MemoryStream();
        pdf.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void DrawLogicalPage(XGraphics graphics, BookPage page, BookGenerationSettings settings,
        int pageIndex, double offsetX, double offsetY, double scale)
    {
        var bounds = new XRect(offsetX, offsetY, A5Width * scale, A5Height * scale);
        graphics.DrawRectangle(XBrushes.White, bounds);
        switch (page.Kind)
        {
            case BookPageKind.FrontCover:
                DrawCover(graphics, bounds, settings);
                break;
            case BookPageKind.Worksheets:
                DrawWorksheetPage(graphics, bounds, page.Worksheets!, false, pageIndex);
                break;
            case BookPageKind.Solutions:
                DrawWorksheetPage(graphics, bounds, page.Worksheets!, true, pageIndex);
                break;
            case BookPageKind.BackCover:
                DrawBackCover(graphics, bounds, settings);
                break;
        }
    }

    private static void DrawCover(XGraphics graphics, XRect page, BookGenerationSettings settings)
    {
        var frame = Inset(page, 18);
        graphics.DrawRectangle(new XPen(Color("#25316d"), 3), frame);
        DrawCentered(graphics, settings.Title, 28, true, "#f15a8a", page.X + 34, page.Y + 95, page.Width - 68, 52);
        DrawCentered(graphics, settings.Subtitle, 14, true, "#25316d", page.X + 34, page.Y + 150, page.Width - 68, 34);
        DrawCentered(graphics, "Sudoku • Kakuro • zadania logiczne", 15, true, "#25316d",
            page.X + 34, page.Y + 270, page.Width - 68, 34);
        DrawCentered(graphics, string.IsNullOrWhiteSpace(settings.ChildName) ? "IMIĘ: ____________________" : settings.ChildName!,
            string.IsNullOrWhiteSpace(settings.ChildName) ? 13 : 27, true, "#25316d",
            page.X + 45, page.Y + 350, page.Width - 90, 48);
        var stripeY = page.Bottom - 76;
        var stripeWidth = (page.Width - 100) / 3;
        graphics.DrawRectangle(Brush("#8edbc4"), page.X + 38, stripeY, stripeWidth, 16);
        graphics.DrawRectangle(Brush("#88ccf1"), page.X + 50 + stripeWidth, stripeY, stripeWidth, 16);
        graphics.DrawRectangle(Brush("#f15a8a"), page.X + 62 + stripeWidth * 2, stripeY, stripeWidth, 16);
    }

    private static void DrawWorksheetPage(XGraphics graphics, XRect page, IReadOnlyList<GeneratedWorksheet> worksheets,
        bool solutions, int pageIndex)
    {
        var title = solutions ? "Rozwiązania" : "Zadania dla małych bystrzaków";
        graphics.DrawString(title, Font(11, true), Brush("#f15a8a"), new XPoint(page.X + 18, page.Y + 26));
        const double gap = 8;
        var contentX = page.X + 16;
        var contentY = page.Y + 38;
        var cardWidth = (page.Width - 32 - gap) / 2;
        var cardHeight = (page.Height - 72 - gap * 2) / 3;
        for (var slot = 0; slot < 6; slot++)
        {
            var row = slot / 2;
            var column = slot % 2;
            var card = new XRect(contentX + column * (cardWidth + gap), contentY + row * (cardHeight + gap), cardWidth, cardHeight);
            if (slot < worksheets.Count) DrawCard(graphics, card, worksheets[slot], solutions);
            else graphics.DrawRectangle(new XPen(Color("#e5e7eb"), 1), card);
        }
        DrawCentered(graphics, solutions ? "Sprawdź odpowiedzi po rozwiązaniu zadań." : "Powodzenia! Każde zadanie ma jedno rozwiązanie.",
            6.5, false, "#25316d", page.X + 35, page.Bottom - 24, page.Width - 70, 12);
        graphics.DrawString(pageIndex.ToString(), Font(7, true), Brush("#25316d"),
            new XRect(page.Right - 38, page.Bottom - 27, 20, 12), XStringFormats.CenterRight);
    }

    private static void DrawCard(XGraphics graphics, XRect card, GeneratedWorksheet worksheet, bool solution)
    {
        var accent = DifficultyColor(worksheet.DisplayStars);
        graphics.DrawRoundedRectangle(new XPen(accent, 1.2), card, new XSize(4, 4));
        graphics.DrawString($"Nr {worksheet.Number}  •  {worksheet.TypeName}", Font(7.5, true), Brush("#25316d"),
            new XPoint(card.X + 7, card.Y + 14));
        var dotsStart = card.Right - 50;
        for (var index = 0; index < worksheet.DisplayStars; index++)
            graphics.DrawEllipse(new XSolidBrush(accent), dotsStart + index * 5, card.Y + 8, 3, 3);
        graphics.DrawString(worksheet.Difficulty.Score.ToString(), Font(6.5, true), new XSolidBrush(accent),
            new XRect(card.Right - 24, card.Y + 4, 18, 14), XStringFormats.CenterRight);
        var visualBounds = new XRect(card.X + 26, card.Y + 25, card.Width - 52, card.Height - 34);
        DrawVisual(graphics, solution ? worksheet.Solution : worksheet.Task, visualBounds);
    }

    private static void DrawVisual(XGraphics graphics, WorksheetVisual visual, XRect bounds)
    {
        var scale = Math.Min(bounds.Width / visual.Width, bounds.Height / visual.Height);
        var originX = bounds.X + (bounds.Width - visual.Width * scale) / 2;
        var originY = bounds.Y + (bounds.Height - visual.Height * scale) / 2;
        foreach (var element in visual.Elements)
        {
            switch (element)
            {
                case VisualRectangle rectangle:
                    var rect = new XRect(originX + rectangle.X * scale, originY + rectangle.Y * scale,
                        rectangle.Width * scale, rectangle.Height * scale);
                    if (rectangle.Fill != "none") graphics.DrawRectangle(Brush(rectangle.Fill), rect);
                    if (rectangle.Stroke != "none" && rectangle.StrokeWidth > 0)
                        graphics.DrawRectangle(new XPen(Color(rectangle.Stroke), rectangle.StrokeWidth * scale), rect);
                    break;
                case VisualLine line:
                    graphics.DrawLine(new XPen(Color(line.Color), line.Width * scale), originX + line.X1 * scale,
                        originY + line.Y1 * scale, originX + line.X2 * scale, originY + line.Y2 * scale);
                    break;
                case VisualText text:
                    var x = originX + text.X * scale;
                    var y = originY + text.Y * scale;
                    graphics.DrawString(text.Text, Font(text.Size * scale, text.Bold), Brush(text.Color),
                        new XRect(x - 35 * scale, y - text.Size * scale, 70 * scale, text.Size * 1.5 * scale), XStringFormats.Center);
                    break;
            }
        }
    }

    private static void DrawBackCover(XGraphics graphics, XRect page, BookGenerationSettings settings)
    {
        graphics.DrawRectangle(new XPen(Color("#25316d"), 3), Inset(page, 18));
        DrawCentered(graphics, "Brawo!", 30, true, "#f15a8a", page.X + 35, page.Y + 185, page.Width - 70, 48);
        DrawCentered(graphics, "Każde rozwiązane zadanie ćwiczy spostrzegawczość i logiczne myślenie.",
            13, false, "#25316d", page.X + 48, page.Y + 245, page.Width - 96, 70);
        DrawCentered(graphics, $"Wygenerowano z ziarnem: {settings.Seed}", 7, false, "#6b7280",
            page.X + 35, page.Bottom - 56, page.Width - 70, 20);
    }

    private static void DrawCentered(XGraphics graphics, string value, double size, bool bold, string color,
        double x, double y, double width, double height) => graphics.DrawString(value, Font(size, bold), Brush(color),
        new XRect(x, y, width, height), XStringFormats.Center);
    private static XRect Inset(XRect rect, double value) => new(rect.X + value, rect.Y + value,
        rect.Width - value * 2, rect.Height - value * 2);
    private static XFont Font(double size, bool bold = false) => new("Lato", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
    private static XColor Color(string hex) => XColor.FromArgb(Convert.ToInt32(hex.TrimStart('#'), 16) | unchecked((int)0xff000000));
    private static XBrush Brush(string hex) => new XSolidBrush(Color(hex));
    private static XColor DifficultyColor(int stars) => Color(stars switch
    {
        1 => "#8edbc4", 2 => "#74d7b5", 3 => "#88ccf1", 4 => "#ffd966", 5 => "#ff9f68", _ => "#f15a8a"
    });

    private static void EnsureFont()
    {
        lock (FontLock)
        {
            if (fontInitialized) return;
            GlobalFontSettings.FontResolver = new LatoFontResolver();
            fontInitialized = true;
        }
    }
}
