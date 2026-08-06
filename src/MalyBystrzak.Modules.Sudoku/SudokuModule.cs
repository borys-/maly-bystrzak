using System.Security.Cryptography;
using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Sudoku;

public sealed class SudokuModule : IWorksheetModule
{
    private static readonly WorksheetInstruction ModuleInstruction = new("Sudoku",
        "W każdym wierszu, kolumnie i oznaczonym bloku", "każda cyfra może wystąpić tylko raz.", "#88ccf1");
    public string Id => "sudoku";
    public string DisplayName => "Sudoku";
    public string Symbol => "#";
    public WorksheetInstruction Instruction => ModuleInstruction;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
    [new("4x4", "Sudoku 4 × 4", "Plansze z blokami 2 × 2"), new("6x6", "Sudoku 6 × 6", "Plansze z blokami 2 × 3")];

    public IReadOnlyList<string> Validate(ModuleGenerationRequest request)
    {
        var errors = new List<string>();
        if (request.Count <= 0) errors.Add("Liczba zadań musi być większa od zera.");
        if (request.VariantId is not "4x4" and not "6x6") errors.Add("Nieobsługiwany wariant Sudoku.");
        return errors;
    }

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        var size = request.VariantId == "6x6" ? 6 : 4;
        var puzzles = new SudokuGenerator(request.Seed).GenerateBook(request.Count, size);
        var result = new List<GeneratedWorksheet>(puzzles.Count);
        foreach (var puzzle in puzzles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(result.Count, request.Count, $"Sudoku: {result.Count}/{request.Count}"));
        }
        return result;
    }

    private static GeneratedWorksheet ToWorksheet(SudokuPuzzle puzzle)
    {
        var metrics = puzzle.CognitiveDifficulty;
        return new(puzzle.Number, "sudoku", $"{puzzle.Size}x{puzzle.Size}", $"Sudoku {puzzle.Size}x{puzzle.Size}",
            Fingerprint(puzzle.Cells), metrics, metrics.Stars, CreateVisual(puzzle, false), CreateVisual(puzzle, true), ModuleInstruction);
    }

    private static WorksheetVisual CreateVisual(SudokuPuzzle puzzle, bool solution)
    {
        const double size = 100;
        var elements = new List<VisualElement>();
        var cell = size / puzzle.Size;
        var values = solution ? puzzle.Solution : puzzle.Cells;
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] == 0) continue;
            var row = index / puzzle.Size;
            var column = index % puzzle.Size;
            if (!solution) elements.Add(new VisualRectangle(column * cell, row * cell, cell, cell, "#fff9de", "none"));
        }
        elements.Add(new VisualRectangle(0, 0, size, size, "none", "#25316d", 1.8));
        for (var index = 1; index < puzzle.Size; index++)
        {
            elements.Add(new VisualLine(index * cell, 0, index * cell, size,
                index % puzzle.BlockColumns == 0 ? 1.8 : .55, "#25316d"));
            elements.Add(new VisualLine(0, index * cell, size, index * cell,
                index % puzzle.BlockRows == 0 ? 1.8 : .55, "#25316d"));
        }
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] == 0) continue;
            var row = index / puzzle.Size;
            var column = index % puzzle.Size;
            elements.Add(new VisualText((column + .5) * cell, (row + .62) * cell, values[index].ToString(),
                puzzle.Size == 4 ? 13 : 10, "#25316d", true));
        }
        return new(size, size, elements);
    }

    private static string Fingerprint(int[] cells) => Convert.ToHexString(SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(string.Join(',', cells))));
}
