using System.Security.Cryptography;
using System.Text;
using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Mazes;

public sealed class MazeModule : IWorksheetModule
{
    private static readonly WorksheetInstruction ModuleInstruction = new("Labirynt",
        "Znajdź drogę od zielonego wejścia do różowej mety.", "Nie przechodź przez żadną ze ścian.", "#ffd966");
    public string Id => "maze";
    public string DisplayName => "Labirynty";
    public string Symbol => "↝";
    public WorksheetInstruction Instruction => ModuleInstruction;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
    [new("9x9", "Labirynt 9 × 9", "Krótsze trasy i większe pola"), new("15x15", "Labirynt 15 × 15", "Dłuższe trasy i więcej zaułków")];

    public IReadOnlyList<string> Validate(ModuleGenerationRequest request)
    {
        var errors = new List<string>();
        if (request.Count <= 0) errors.Add("Liczba zadań musi być większa od zera.");
        if (request.VariantId is not "9x9" and not "15x15") errors.Add("Nieobsługiwany wariant labiryntu.");
        return errors;
    }

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var size = request.VariantId == "15x15" ? 15 : 9;
        var puzzles = new MazeGenerator(request.Seed).GenerateBook(request.Count, size, cancellationToken);
        return puzzles.Select((puzzle, index) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new(index + 1, request.Count, $"Labirynty: {index + 1}/{request.Count}"));
            var difficulty = puzzle.Difficulty;
            return new GeneratedWorksheet(puzzle.Number, Id, request.VariantId, $"Labirynt {size}x{size}", Fingerprint(puzzle),
                difficulty, difficulty.Stars, CreateVisual(puzzle, false), CreateVisual(puzzle, true), ModuleInstruction);
        }).ToArray();
    }

    private static WorksheetVisual CreateVisual(MazePuzzle puzzle, bool solution)
    {
        const double canvas = 100;
        const double margin = 4;
        var cell = (canvas - margin * 2) / puzzle.Size;
        var elements = new List<VisualElement>();
        if (solution)
        {
            for (var index = 1; index < puzzle.Solution.Length; index++)
            {
                var previous = puzzle.Solution[index - 1];
                var current = puzzle.Solution[index];
                elements.Add(new VisualLine(margin + (previous % puzzle.Size + .5) * cell,
                    margin + (previous / puzzle.Size + .5) * cell, margin + (current % puzzle.Size + .5) * cell,
                    margin + (current / puzzle.Size + .5) * cell, Math.Max(1.2, cell * .24), "#f15a8a"));
            }
        }
        for (var cellIndex = 0; cellIndex < puzzle.Size * puzzle.Size; cellIndex++)
        {
            var row = cellIndex / puzzle.Size;
            var column = cellIndex % puzzle.Size;
            var x = margin + column * cell;
            var y = margin + row * cell;
            if (puzzle.HasWall(cellIndex, 0)) elements.Add(new VisualLine(x, y, x + cell, y, .65, "#25316d"));
            if (puzzle.HasWall(cellIndex, 3)) elements.Add(new VisualLine(x, y, x, y + cell, .65, "#25316d"));
            if (row == puzzle.Size - 1 && puzzle.HasWall(cellIndex, 2)) elements.Add(new VisualLine(x, y + cell, x + cell, y + cell, .65, "#25316d"));
            if (column == puzzle.Size - 1 && puzzle.HasWall(cellIndex, 1)) elements.Add(new VisualLine(x + cell, y, x + cell, y + cell, .65, "#25316d"));
        }
        var entranceY = margin + (puzzle.Entrance / puzzle.Size + .5) * cell;
        var exitY = margin + (puzzle.Exit / puzzle.Size + .5) * cell;
        elements.Add(new VisualEllipse(margin - cell * .25, entranceY, cell * .22, cell * .22, "#50b996", "none"));
        elements.Add(new VisualEllipse(canvas - margin + cell * .25, exitY, cell * .22, cell * .22, "#f15a8a", "none"));
        return new(canvas, canvas, elements);
    }

    private static string Fingerprint(MazePuzzle puzzle) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{puzzle.Size}:{puzzle.Entrance}:{puzzle.Exit}:{string.Join(',', puzzle.Walls.Select(value => value ? 1 : 0))}")));
}
