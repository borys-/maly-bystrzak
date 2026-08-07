using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<(double Size, bool Bold), XFont> Fonts = new();
    private static readonly ConcurrentDictionary<string, XColor> Colors = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, XBrush> Brushes = new(StringComparer.OrdinalIgnoreCase);
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
            DrawLogicalPage(graphics, document.Pages[index], document.Settings, document.Instructions, index, 0, 0, 1);
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
            DrawLogicalPage(graphics, document.Pages[side.LeftPage - 1], document.Settings, document.Instructions,
                side.LeftPage - 1, 0, 0, 1);
            graphics.DrawLine(new XPen(Color("#e5e7eb", document.Settings.InkSavingMode), .5), A5Width, 0, A5Width, A5Height);
            DrawLogicalPage(graphics, document.Pages[side.RightPage - 1], document.Settings, document.Instructions,
                side.RightPage - 1,
                A5Width, 0, 1);
        }
        return Save(pdf);
    }

    public Task<byte[]> RenderPreviewAsync(BookDocument document, IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default) => RenderAsync(document, false, progress, cancellationToken);

    public Task<byte[]> RenderBookletAsync(BookDocument document, IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default) => RenderAsync(document, true, progress, cancellationToken);

    private static async Task<byte[]> RenderAsync(BookDocument document, bool booklet,
        IProgress<GenerationProgress>? progress, CancellationToken cancellationToken)
    {
        using var pdf = CreateDocument(document, booklet ? "Broszura A4" : "Podgląd A5");
        IReadOnlyList<SheetSide> sides = booklet ? BookLayout.CreateBookletOrder(document.Pages.Count) : [];
        var pageCount = booklet ? sides.Count : document.Pages.Count;
        var total = pageCount + 1;

        for (var index = 0; index < pageCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = pdf.AddPage();
            page.Width = XUnit.FromPoint(booklet ? A5Width * 2 : A5Width);
            page.Height = XUnit.FromPoint(A5Height);
            using var graphics = XGraphics.FromPdfPage(page);
            if (booklet)
            {
                var side = sides[index];
                DrawLogicalPage(graphics, document.Pages[side.LeftPage - 1], document.Settings, document.Instructions,
                    side.LeftPage - 1, 0, 0, 1);
                graphics.DrawLine(new XPen(Color("#e5e7eb", document.Settings.InkSavingMode), .5), A5Width, 0, A5Width, A5Height);
                DrawLogicalPage(graphics, document.Pages[side.RightPage - 1], document.Settings, document.Instructions,
                    side.RightPage - 1, A5Width, 0, 1);
            }
            else
            {
                DrawLogicalPage(graphics, document.Pages[index], document.Settings, document.Instructions,
                    index, 0, 0, 1);
            }

            progress?.Report(new(index + 1, total, $"Wyrenderowano {index + 1} z {pageCount} stron PDF"));
            await Task.Delay(16, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new(pageCount, total, "Zapisuję gotowy plik PDF…"));
        await Task.Delay(16, cancellationToken);
        var bytes = Save(pdf);
        progress?.Report(new(total, total, "PDF jest gotowy"));
        return bytes;
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
        IReadOnlyList<WorksheetInstruction> instructions,
        int pageIndex, double offsetX, double offsetY, double scale)
    {
        var bounds = new XRect(offsetX, offsetY, A5Width * scale, A5Height * scale);
        graphics.DrawRectangle(XBrushes.White, bounds);
        switch (page.Kind)
        {
            case BookPageKind.FrontCover:
                DrawCover(graphics, bounds, settings, instructions);
                break;
            case BookPageKind.Worksheets:
                DrawWorksheetPage(graphics, bounds, page.Placements!, false, pageIndex, settings.InkSavingMode);
                break;
            case BookPageKind.Solutions:
                DrawWorksheetPage(graphics, bounds, page.Placements!, true, pageIndex, settings.InkSavingMode);
                break;
            case BookPageKind.BackCover:
                DrawBackCover(graphics, bounds, settings, instructions);
                break;
        }
    }

    private static void DrawCover(XGraphics graphics, XRect page, BookGenerationSettings settings,
        IReadOnlyList<WorksheetInstruction> instructions)
    {
        var frame = Inset(page, 18);
        graphics.DrawRectangle(new XPen(Color("#25316d", settings.InkSavingMode), 3), frame);
        DrawCentered(graphics, settings.Title, 28, true, "#f15a8a", page.X + 34, page.Y + 95, page.Width - 68, 52, settings.InkSavingMode);
        DrawCentered(graphics, settings.Subtitle, 14, true, "#25316d", page.X + 34, page.Y + 150, page.Width - 68, 34, settings.InkSavingMode);
        var puzzleNames = instructions.Count == 0 ? "Zadania logiczne" : string.Join(" • ", instructions.Select(item => item.Title));
        DrawCentered(graphics, puzzleNames, instructions.Count > 3 ? 11 : 15, true, "#25316d",
            page.X + 34, page.Y + 270, page.Width - 68, 34, settings.InkSavingMode);
        if (!string.IsNullOrWhiteSpace(settings.ChildName))
            DrawCentered(graphics, settings.ChildName!, 27, true, "#25316d",
                page.X + 45, page.Y + 350, page.Width - 90, 48, settings.InkSavingMode);
        var stripeY = page.Bottom - 76;
        var stripeWidth = (page.Width - 100) / 3;
        if (settings.InkSavingMode)
        {
            var pen = new XPen(XColors.Black, .7);
            graphics.DrawRectangle(pen, page.X + 38, stripeY, stripeWidth, 16);
            graphics.DrawRectangle(pen, page.X + 50 + stripeWidth, stripeY, stripeWidth, 16);
            graphics.DrawRectangle(pen, page.X + 62 + stripeWidth * 2, stripeY, stripeWidth, 16);
        }
        else
        {
            graphics.DrawRectangle(Brush("#8edbc4"), page.X + 38, stripeY, stripeWidth, 16);
            graphics.DrawRectangle(Brush("#88ccf1"), page.X + 50 + stripeWidth, stripeY, stripeWidth, 16);
            graphics.DrawRectangle(Brush("#f15a8a"), page.X + 62 + stripeWidth * 2, stripeY, stripeWidth, 16);
        }
    }

    private static void DrawWorksheetPage(XGraphics graphics, XRect page, IReadOnlyList<WorksheetPlacement> placements,
        bool solutions, int pageIndex, bool inkSavingMode)
    {
        var title = solutions ? "Rozwiązania" : "Zadania dla małych bystrzaków";
        graphics.DrawString(title, Font(11, true), Brush("#f15a8a", inkSavingMode), new XPoint(page.X + 18, page.Y + 26));
        const double gap = 8;
        var contentX = page.X + 16;
        var contentY = page.Y + 38;
        var cardWidth = (page.Width - 32 - gap) / 2;
        var cardHeight = (page.Height - 72 - gap * 2) / 3;
        foreach (var placement in placements)
        {
            var card = new XRect(contentX + placement.Column * (cardWidth + gap),
                contentY + placement.Row * (cardHeight + gap),
                cardWidth * placement.ColumnSpan + gap * (placement.ColumnSpan - 1),
                cardHeight * placement.RowSpan + gap * (placement.RowSpan - 1));
            DrawCard(graphics, card, placement.Worksheet, solutions, inkSavingMode);
        }
        DrawCentered(graphics, solutions ? "Sprawdź odpowiedzi po rozwiązaniu zadań." : "Powodzenia! Każde zadanie ma jedno rozwiązanie.",
            6.5, false, "#25316d", page.X + 35, page.Bottom - 24, page.Width - 70, 12, inkSavingMode);
        graphics.DrawString(pageIndex.ToString(), Font(7, true), Brush("#25316d", inkSavingMode),
            new XRect(page.Right - 38, page.Bottom - 27, 20, 12), XStringFormats.CenterRight);
    }

    private static void DrawCard(XGraphics graphics, XRect card, GeneratedWorksheet worksheet, bool solution, bool inkSavingMode)
    {
        var accent = DifficultyColor(worksheet.DisplayStars, inkSavingMode);
        graphics.DrawRoundedRectangle(new XPen(accent, 1.2), card, new XSize(4, 4));
        graphics.DrawString($"Nr {worksheet.Number}  •  {worksheet.TypeName}", Font(7.5, true), Brush("#25316d", inkSavingMode),
            new XPoint(card.X + 7, card.Y + 14));
        var dotsStart = card.Right - 50;
        for (var index = 0; index < worksheet.DisplayStars; index++)
            graphics.DrawEllipse(new XSolidBrush(accent), dotsStart + index * 5, card.Y + 8, 3, 3);
        graphics.DrawString(worksheet.Difficulty.Score.ToString(), Font(6.5, true), new XSolidBrush(accent),
            new XRect(card.Right - 24, card.Y + 4, 18, 14), XStringFormats.CenterRight);
        var visualBounds = new XRect(card.X + 26, card.Y + 25, card.Width - 52, card.Height - 34);
        DrawVisual(graphics, solution ? worksheet.Solution : worksheet.Task, visualBounds, inkSavingMode);
    }

    private static void DrawVisual(XGraphics graphics, WorksheetVisual visual, XRect bounds, bool inkSavingMode)
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
                    if (rectangle.Fill != "none") graphics.DrawRectangle(Brush(rectangle.Fill, inkSavingMode, true), rect);
                    if (rectangle.Stroke != "none" && rectangle.StrokeWidth > 0)
                        graphics.DrawRectangle(new XPen(Color(rectangle.Stroke, inkSavingMode), rectangle.StrokeWidth * scale), rect);
                    break;
                case VisualLine line:
                    graphics.DrawLine(new XPen(Color(line.Color, inkSavingMode), line.Width * scale), originX + line.X1 * scale,
                        originY + line.Y1 * scale, originX + line.X2 * scale, originY + line.Y2 * scale);
                    break;
                case VisualEllipse ellipse:
                    var ellipseRect = new XRect(originX + (ellipse.CenterX - ellipse.RadiusX) * scale,
                        originY + (ellipse.CenterY - ellipse.RadiusY) * scale,
                        ellipse.RadiusX * 2 * scale, ellipse.RadiusY * 2 * scale);
                    if (ellipse.Fill != "none") graphics.DrawEllipse(Brush(ellipse.Fill, inkSavingMode, true), ellipseRect);
                    if (ellipse.Stroke != "none" && ellipse.StrokeWidth > 0)
                        graphics.DrawEllipse(new XPen(Color(ellipse.Stroke, inkSavingMode), ellipse.StrokeWidth * scale), ellipseRect);
                    break;
                case VisualPolygon polygon:
                    var points = polygon.Points.Select(point => new XPoint(originX + point.X * scale,
                        originY + point.Y * scale)).ToArray();
                    if (polygon.Fill != "none") graphics.DrawPolygon(Brush(polygon.Fill, inkSavingMode, true), points, XFillMode.Winding);
                    if (polygon.Stroke != "none" && polygon.StrokeWidth > 0)
                        graphics.DrawPolygon(new XPen(Color(polygon.Stroke, inkSavingMode), polygon.StrokeWidth * scale), points);
                    break;
                case VisualText text:
                    var x = originX + text.X * scale;
                    var y = originY + text.Y * scale;
                    var textBounds = text.Anchor switch
                    {
                        "end" => new XRect(x - 70 * scale, y - text.Size * scale, 70 * scale, text.Size * 1.5 * scale),
                        "start" => new XRect(x, y - text.Size * scale, 70 * scale, text.Size * 1.5 * scale),
                        _ => new XRect(x - 35 * scale, y - text.Size * scale, 70 * scale, text.Size * 1.5 * scale)
                    };
                    var textFormat = text.Anchor switch
                    {
                        "end" => XStringFormats.CenterRight,
                        "start" => XStringFormats.CenterLeft,
                        _ => XStringFormats.Center
                    };
                    graphics.DrawString(text.Text, Font(text.Size * scale, text.Bold), Brush(text.Color, inkSavingMode),
                        textBounds, textFormat);
                    break;
            }
        }
    }

    private static void DrawBackCover(XGraphics graphics, XRect page, BookGenerationSettings settings,
        IReadOnlyList<WorksheetInstruction> instructions)
    {
        graphics.DrawRectangle(new XPen(Color("#25316d", settings.InkSavingMode), 3), Inset(page, 18));
        DrawCentered(graphics, "Brawo!", 30, true, "#f15a8a", page.X + 35, page.Y + 66, page.Width - 70, 48, settings.InkSavingMode);
        DrawCentered(graphics, "Każde rozwiązane zadanie ćwiczy", 12, false, "#25316d",
            page.X + 48, page.Y + 119, page.Width - 96, 24, settings.InkSavingMode);
        DrawCentered(graphics, "spostrzegawczość i logiczne myślenie.", 12, false, "#25316d",
            page.X + 48, page.Y + 143, page.Width - 96, 24, settings.InkSavingMode);

        DrawCentered(graphics, "Jak rozwiązywać zagadki?", 15, true, "#25316d",
            page.X + 42, page.Y + 188, page.Width - 84, 28, settings.InkSavingMode);
        var ruleY = page.Y + 215;
        foreach (var instruction in instructions)
        {
            DrawRuleCard(graphics, page, ruleY, instruction, settings.InkSavingMode);
            ruleY += 54;
        }

        DrawCentered(graphics, "Stwórz kolejną książeczkę:", 9, true, "#25316d",
            page.X + 35, page.Bottom - 105, page.Width - 70, 18, settings.InkSavingMode);
        DrawCentered(graphics, "https://borys-.github.io/maly-bystrzak/", 9, true, "#f15a8a",
            page.X + 35, page.Bottom - 84, page.Width - 70, 18, settings.InkSavingMode);
        DrawCentered(graphics, $"Wygenerowano z ziarnem: {settings.Seed}", 7, false, "#6b7280",
            page.X + 35, page.Bottom - 56, page.Width - 70, 20, settings.InkSavingMode);
    }

    private static void DrawRuleCard(XGraphics graphics, XRect page, double y, WorksheetInstruction instruction, bool inkSavingMode)
    {
        var card = new XRect(page.X + 42, y, page.Width - 84, 46);
        graphics.DrawRoundedRectangle(new XPen(Color(instruction.Accent, inkSavingMode), 1.2), card, new XSize(5, 5));
        if (!inkSavingMode) graphics.DrawRectangle(Brush(instruction.Accent), card.X, card.Y, 7, card.Height);
        graphics.DrawString(instruction.Title, Font(8.5, true), Brush("#25316d", inkSavingMode), new XPoint(card.X + 19, card.Y + 14));
        graphics.DrawString(instruction.FirstLine, Font(6.3), Brush("#25316d", inkSavingMode), new XPoint(card.X + 19, card.Y + 28));
        graphics.DrawString(instruction.SecondLine, Font(6.3), Brush("#25316d", inkSavingMode), new XPoint(card.X + 19, card.Y + 39));
    }

    private static void DrawCentered(XGraphics graphics, string value, double size, bool bold, string color,
        double x, double y, double width, double height, bool inkSavingMode = false) => graphics.DrawString(value, Font(size, bold), Brush(color, inkSavingMode),
        new XRect(x, y, width, height), XStringFormats.Center);
    private static XRect Inset(XRect rect, double value) => new(rect.X + value, rect.Y + value,
        rect.Width - value * 2, rect.Height - value * 2);
    private static XFont Font(double size, bool bold = false) => Fonts.GetOrAdd((Math.Round(size, 3), bold), key =>
        new XFont("Lato", key.Size, key.Bold ? XFontStyleEx.Bold : XFontStyleEx.Regular));
    private static XColor Color(string hex, bool inkSavingMode = false, bool fill = false)
    {
        hex = MapPrintColor(hex, inkSavingMode, fill);
        return Colors.GetOrAdd(hex, value =>
        XColor.FromArgb(Convert.ToInt32(value.TrimStart('#'), 16) | unchecked((int)0xff000000)));
    }
    private static XBrush Brush(string hex, bool inkSavingMode = false, bool fill = false)
    {
        hex = MapPrintColor(hex, inkSavingMode, fill);
        return Brushes.GetOrAdd(hex, value => new XSolidBrush(Color(value)));
    }
    private static XColor DifficultyColor(int stars, bool inkSavingMode = false) => Color(stars switch
    {
        1 => "#8edbc4", 2 => "#74d7b5", 3 => "#88ccf1", 4 => "#ffd966", 5 => "#ff9f68", _ => "#f15a8a"
    }, inkSavingMode);

    private static string MapPrintColor(string hex, bool inkSavingMode, bool fill)
    {
        if (!inkSavingMode) return hex;
        var value = Convert.ToInt32(hex.TrimStart('#'), 16);
        var red = (value >> 16) & 255;
        var green = (value >> 8) & 255;
        var blue = value & 255;
        var luminance = (.2126 * red + .7152 * green + .0722 * blue) / 255;
        if (!fill) return luminance > .84 ? "#a8a8a8" : "#000000";
        if (luminance > .78) return "#ffffff";
        if (luminance < .32) return "#000000";
        return "#dedede";
    }

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
