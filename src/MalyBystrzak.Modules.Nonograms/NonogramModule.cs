using System.Security.Cryptography;
using System.Text;
using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Nonograms;

public sealed class NonogramModule : IWorksheetModule
{
    private static readonly WorksheetInstruction ModuleInstruction = new("Nonogram",
        "Zamaluj pola zgodnie ze wskazówkami przy wierszach", "i kolumnach, aby odkryć ukryty obrazek.", "#c9a7eb");
    public string Id => "nonogram";
    public string DisplayName => "Nonogramy";
    public string Symbol => "▦";
    public WorksheetInstruction Instruction => ModuleInstruction;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
    [new("5x5", "Nonogram 5 × 5", "Małe ukryte obrazki"), new("7x7", "Nonogram 7 × 7", "Więcej grup i szczegółów"),
     new("10x10", "Nonogram 10 × 10", "Najbardziej wymagające obrazki")];

    public IReadOnlyList<string> Validate(ModuleGenerationRequest request)
    {
        var errors = new List<string>();
        if (request.Count <= 0) errors.Add("Liczba zadań musi być większa od zera.");
        if (request.VariantId is not "5x5" and not "7x7" and not "10x10") errors.Add("Nieobsługiwany wariant nonogramu.");
        return errors;
    }

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var size = request.VariantId switch { "10x10" => 10, "7x7" => 7, _ => 5 };
        var puzzles = new NonogramGenerator(request.Seed).GenerateBook(request.Count, size, cancellationToken);
        return puzzles.Select((puzzle, index) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new(index + 1, request.Count, $"Nonogramy: {index + 1}/{request.Count}"));
            var difficulty = puzzle.Difficulty;
            return new GeneratedWorksheet(puzzle.Number, Id, request.VariantId, $"Nonogram {size}x{size}", Fingerprint(puzzle),
                difficulty, difficulty.Stars, CreateVisual(puzzle, false), CreateVisual(puzzle, true), ModuleInstruction);
        }).ToArray();
    }

    private static WorksheetVisual CreateVisual(NonogramPuzzle puzzle, bool solution)
    {
        const double canvas = 100;
        var clueMargin = puzzle.Size == 10 ? 27d : 24d;
        var gridSize = canvas - clueMargin - 3;
        var cell = gridSize / puzzle.Size;
        var elements = new List<VisualElement>();
        for (var row = 0; row < puzzle.Size; row++)
        for (var column = 0; column < puzzle.Size; column++)
        {
            var x = clueMargin + column * cell;
            var y = clueMargin + row * cell;
            var filled = solution && puzzle.Cells[row * puzzle.Size + column];
            elements.Add(new VisualRectangle(x, y, cell, cell, filled ? "#25316d" : "#ffffff", "#25316d", .45));
        }
        var font = puzzle.Size == 10 ? 3.7 : 4.8;
        for (var row = 0; row < puzzle.Size; row++)
            elements.Add(new VisualText(clueMargin - 2, clueMargin + (row + .63) * cell,
                string.Join(' ', puzzle.RowClues[row]), font, "#25316d", true, "end"));
        for (var column = 0; column < puzzle.Size; column++)
        {
            var clues = puzzle.ColumnClues[column];
            var step = puzzle.Size == 10 ? 4.2 : 5.2;
            var startY = clueMargin - 3 - (clues.Length - 1) * step;
            for (var index = 0; index < clues.Length; index++)
                elements.Add(new VisualText(clueMargin + (column + .5) * cell, startY + index * step,
                    clues[index].ToString(), font, "#25316d", true));
        }
        return new(canvas, canvas, elements);
    }

    private static string Fingerprint(NonogramPuzzle puzzle) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{puzzle.Size}:{string.Concat(puzzle.Cells.Select(value => value ? '1' : '0'))}")));
}
