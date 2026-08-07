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
    [new("5x5", "Nonogram 5 × 5", "Małe ukryte obrazki"),
     new("7x7", "Nonogram 7 × 7", "Więcej grup i szczegółów", WorksheetLayout.Large),
     new("10x10", "Nonogram 10 × 10", "Najbardziej wymagające obrazki", WorksheetLayout.Large)];

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
                difficulty, difficulty.Stars, CreateVisual(puzzle, false), CreateVisual(puzzle, true), ModuleInstruction,
                size >= 7 ? WorksheetLayout.Large : WorksheetLayout.Standard);
        }).ToArray();
    }

    private static WorksheetVisual CreateVisual(NonogramPuzzle puzzle, bool solution)
    {
        const double canvas = 100;
        var clueMargin = puzzle.Size switch { 10 => 26d, 7 => 21d, _ => 20d };
        var gridSize = canvas - clueMargin - 3;
        var cell = gridSize / puzzle.Size;
        var elements = new List<VisualElement>();
        elements.Add(new VisualRectangle(2, clueMargin, clueMargin - 4, gridSize, "#f4f7ff", "none"));
        elements.Add(new VisualRectangle(clueMargin, 2, gridSize, clueMargin - 4, "#f4f7ff", "none"));
        elements.Add(new VisualRectangle(clueMargin, clueMargin, gridSize, gridSize, "#fffefa", "none"));
        for (var row = 0; row < puzzle.Size; row++)
        for (var column = 0; column < puzzle.Size; column++)
        {
            if (!solution || !puzzle.Cells[row * puzzle.Size + column]) continue;
            var x = clueMargin + column * cell;
            var y = clueMargin + row * cell;
            elements.Add(new VisualRectangle(x, y, cell, cell, "#25316d", "none"));
        }
        for (var index = 0; index <= puzzle.Size; index++)
        {
            var width = index is 0 || index == puzzle.Size || (puzzle.Size == 10 && index == 5) ? 1.05 : .38;
            elements.Add(new VisualLine(clueMargin + index * cell, clueMargin,
                clueMargin + index * cell, clueMargin + gridSize, width, "#25316d"));
            elements.Add(new VisualLine(clueMargin, clueMargin + index * cell,
                clueMargin + gridSize, clueMargin + index * cell, width, "#25316d"));
        }
        var font = puzzle.Size == 10 ? 3.5 : 4.5;
        for (var row = 0; row < puzzle.Size; row++)
        {
            var clues = puzzle.RowClues[row];
            for (var index = 0; index < clues.Length; index++)
                elements.Add(new VisualText(clueMargin - 2.5 - (clues.Length - 1 - index) * (font + 1),
                    clueMargin + (row + .64) * cell, clues[index].ToString(), font, "#25316d", true, "end"));
        }
        for (var column = 0; column < puzzle.Size; column++)
        {
            var clues = puzzle.ColumnClues[column];
            var step = puzzle.Size == 10 ? 4 : 5;
            var startY = clueMargin - 3.2 - (clues.Length - 1) * step;
            for (var index = 0; index < clues.Length; index++)
                elements.Add(new VisualText(clueMargin + (column + .5) * cell, startY + index * step,
                    clues[index].ToString(), font, "#25316d", true));
        }
        return new(canvas, canvas, elements);
    }

    private static string Fingerprint(NonogramPuzzle puzzle) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{puzzle.Size}:{string.Concat(puzzle.Cells.Select(value => value ? '1' : '0'))}")));
}
