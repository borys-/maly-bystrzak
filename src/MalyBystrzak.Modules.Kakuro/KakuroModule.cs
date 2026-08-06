using System.Security.Cryptography;
using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Kakuro;

public sealed class KakuroModule : IWorksheetModule
{
    private static readonly WorksheetInstruction ModuleInstruction = new("Kakuro",
        "Wpisz cyfry 1-9, aby otrzymać podane sumy.", "W jednej grupie cyfry nie mogą się powtarzać.", "#8edbc4");
    public string Id => "kakuro";
    public string DisplayName => "Kakuro";
    public string Symbol => "+";
    public WorksheetInstruction Instruction => ModuleInstruction;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
    [new("3x3", "Kakuro 3 × 3", "Pierwsze zadania z sumami"), new("4x4", "Kakuro 4 × 4", "Większe zadania z sumami")];

    public IReadOnlyList<string> Validate(ModuleGenerationRequest request)
    {
        var errors = new List<string>();
        if (request.Count <= 0) errors.Add("Liczba zadań musi być większa od zera.");
        if (request.VariantId is not "3x3" and not "4x4") errors.Add("Nieobsługiwany wariant Kakuro.");
        return errors;
    }

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        var size = request.VariantId == "4x4" ? 4 : 3;
        var puzzles = new KakuroGenerator(request.Seed).GenerateBook(request.Count, size);
        var result = new List<GeneratedWorksheet>(puzzles.Count);
        foreach (var puzzle in puzzles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(result.Count, request.Count, $"Kakuro: {result.Count}/{request.Count}"));
        }
        return result;
    }

    private static GeneratedWorksheet ToWorksheet(KakuroPuzzle puzzle)
    {
        var metrics = puzzle.CognitiveDifficulty;
        return new(puzzle.Number, "kakuro", $"{puzzle.Size}x{puzzle.Size}", $"Kakuro {puzzle.Size}x{puzzle.Size}",
            Fingerprint(puzzle), metrics, metrics.Stars, CreateVisual(puzzle, false), CreateVisual(puzzle, true), ModuleInstruction);
    }

    private static WorksheetVisual CreateVisual(KakuroPuzzle puzzle, bool solution)
    {
        const double size = 100;
        var cells = puzzle.Size + 1;
        var cell = size / cells;
        var elements = new List<VisualElement>();
        for (var row = 0; row < cells; row++)
        for (var column = 0; column < cells; column++)
        {
            var x = column * cell;
            var y = row * cell;
            var header = row == 0 || column == 0;
            elements.Add(new VisualRectangle(x, y, cell, cell,
                row == 0 && column == 0 ? "#25316d" : header ? "#e8eef8" : "#ffffff", "#25316d", .75));
            if (row == 0 && column > 0)
                elements.Add(new VisualText(x + cell / 2, y + cell * .62, $"↓ {puzzle.ColumnSums[column - 1]}", 5.5, "#25316d", true));
            else if (column == 0 && row > 0)
                elements.Add(new VisualText(x + cell / 2, y + cell * .62, $"→ {puzzle.RowSums[row - 1]}", 5.5, "#25316d", true));
            else if (row > 0 && column > 0)
            {
                var index = (row - 1) * puzzle.Size + column - 1;
                var value = solution ? puzzle.Solution[index] : puzzle.Givens[index];
                if (value != 0)
                {
                    if (!solution) elements.Add(new VisualRectangle(x, y, cell, cell, "#fff9de", "#25316d", .75));
                    elements.Add(new VisualText(x + cell / 2, y + cell * .65, value.ToString(), 8.5, "#25316d", true));
                }
            }
        }
        return new(size, size, elements);
    }

    private static string Fingerprint(KakuroPuzzle puzzle) => Convert.ToHexString(SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(string.Join(',', puzzle.RowSums.Concat(puzzle.ColumnSums).Concat(puzzle.Givens)))));
}
