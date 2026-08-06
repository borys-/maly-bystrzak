using System.Security.Cryptography;
using System.Text;
using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Sequences;

public sealed class SequenceModule : IWorksheetModule
{
    private static readonly WorksheetInstruction ModuleInstruction = new("Sekwencje",
        "Odkryj regułę, według której ułożono elementy.", "Uzupełnij wszystkie pola oznaczone znakiem zapytania.", "#ff9f68");
    private static readonly string[] Colors = ["#f15a8a", "#88ccf1", "#8edbc4", "#ffd966"];
    public string Id => "sequence";
    public string DisplayName => "Sekwencje";
    public string Symbol => "◇";
    public WorksheetInstruction Instruction => ModuleInstruction;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
    [new("pictures", "Sekwencje obrazkowe", "Figury, kolory i powtarzające się wzory"),
     new("numbers", "Sekwencje liczbowe", "Działania i zależności między liczbami")];

    public IReadOnlyList<string> Validate(ModuleGenerationRequest request)
    {
        var errors = new List<string>();
        if (request.Count <= 0) errors.Add("Liczba zadań musi być większa od zera.");
        if (request.VariantId is not "pictures" and not "numbers") errors.Add("Nieobsługiwany wariant sekwencji.");
        return errors;
    }

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var puzzles = new SequenceGenerator(request.Seed).GenerateBook(request.Count, request.VariantId, cancellationToken);
        return puzzles.Select((puzzle, index) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new(index + 1, request.Count, $"Sekwencje: {index + 1}/{request.Count}"));
            var difficulty = puzzle.Difficulty;
            var name = request.VariantId == "pictures" ? "Sekwencje obrazkowe" : "Sekwencje liczbowe";
            return new GeneratedWorksheet(puzzle.Number, Id, request.VariantId, name, Fingerprint(puzzle), difficulty,
                difficulty.Stars, CreateVisual(puzzle, false), CreateVisual(puzzle, true), ModuleInstruction);
        }).ToArray();
    }

    private static WorksheetVisual CreateVisual(SequencePuzzle puzzle, bool solution)
    {
        var elements = new List<VisualElement>();
        for (var rowIndex = 0; rowIndex < puzzle.Rows.Count; rowIndex++)
        {
            var row = puzzle.Rows[rowIndex];
            var length = row.Numbers?.Length ?? row.Pictures!.Length;
            var spacing = 88d / length;
            var y = 18 + rowIndex * 32;
            elements.Add(new VisualLine(4, y + 12, 96, y + 12, .35, "#dfe1ea"));
            for (var index = 0; index < length; index++)
            {
                var x = 6 + spacing * (index + .5);
                var missing = row.Missing.Contains(index);
                if (missing && !solution)
                {
                    elements.Add(new VisualRectangle(x - 5, y - 7, 10, 14, "#fff9de", "#f15a8a", .8));
                    elements.Add(new VisualText(x, y + 3, "?", 8, "#f15a8a", true));
                    continue;
                }
                if (missing) elements.Add(new VisualRectangle(x - 6, y - 8, 12, 16, "#fff9de", "#f15a8a", .8));
                if (row.Numbers is not null)
                    elements.Add(new VisualText(x, y + 3, row.Numbers[index].ToString(), 7.2, "#25316d", true));
                else
                    DrawPicture(elements, x, y, row.Pictures![index]);
            }
        }
        return new(100, 100, elements);
    }

    private static void DrawPicture(List<VisualElement> elements, double x, double y, PictureToken token)
    {
        var radius = token.Size == 0 ? 4 : 5.5;
        var color = Colors[token.Color];
        switch (token.Shape)
        {
            case PictureShape.Circle:
                elements.Add(new VisualEllipse(x, y, radius, radius, color, "#25316d", .4));
                break;
            case PictureShape.Square:
                elements.Add(new VisualRectangle(x - radius, y - radius, radius * 2, radius * 2, color, "#25316d", .4));
                break;
            case PictureShape.Triangle:
                elements.Add(new VisualPolygon([new(x, y - radius), new(x + radius, y + radius), new(x - radius, y + radius)], color, "#25316d", .4));
                break;
            case PictureShape.Diamond:
                elements.Add(new VisualPolygon([new(x, y - radius), new(x + radius, y), new(x, y + radius), new(x - radius, y)], color, "#25316d", .4));
                break;
        }
    }

    private static string Fingerprint(SequencePuzzle puzzle) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join('|', puzzle.Rows.Select(row => $"{row.Rule}:{string.Join(',', row.Numbers?.Cast<object>() ?? row.Pictures!.Cast<object>())}:{string.Join(',', row.Missing)}")))));
}
