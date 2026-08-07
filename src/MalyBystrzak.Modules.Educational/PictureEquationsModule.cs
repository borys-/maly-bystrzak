using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Educational;

public sealed record PictureEquationsPuzzle(int Number, int Tier, int[] Values, int[] IconKinds,
    int FinalResult, CognitiveDifficulty Difficulty)
{
    public int CountSolutions() => (from a in Enumerable.Range(1, 9)
        from b in Enumerable.Range(1, 9)
        from c in Enumerable.Range(1, 9)
        where 3 * a == 3 * Values[0] && a + 2 * b == Values[0] + 2 * Values[1] && b + c == Values[1] + Values[2]
        select 1).Count();
}

public sealed class PictureEquationsModule : IWorksheetModule
{
    private static readonly WorksheetInstruction Rules = new("Równania obrazkowe",
        "Odkryj wartość każdego obrazka.", "Oblicz ostatnie działanie.", "#f15a8a");
    public string Id => "picture-equations";
    public string DisplayName => "Równania obrazkowe";
    public string Symbol => "◆";
    public WorksheetInstruction Instruction => Rules;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
        [new("animals", "Równania obrazkowe", "Odkryj wartości kolorowych symboli", WorksheetLayout.HalfPage)];

    public IReadOnlyList<string> Validate(ModuleGenerationRequest request) => request.Count <= 0
        ? ["Liczba zadań musi być większa od zera."]
        : request.VariantId != "animals" ? ["Nieobsługiwany wariant równań obrazkowych."] : [];

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request); if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var random = new Random(request.Seed);
        var result = new List<GeneratedWorksheet>();
        for (var index = 0; index < request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tier = PuzzleSupport.Tier(index, request.Count);
            var values = Enumerable.Range(0, 3).Select(_ => random.Next(1, tier <= 2 ? 7 : 10)).ToArray();
            var icons = Enumerable.Range(0, 5).OrderBy(_ => random.Next()).Take(3).ToArray();
            var final = tier <= 2 ? values[2] + values[0] : values[2] * values[0];
            var difficulty = PuzzleSupport.Difficulty(tier, tier <= 2 ? 25 : 55, 45 + tier * 6);
            var puzzle = new PictureEquationsPuzzle(index + 1, tier, values, icons, final, difficulty);
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(index + 1, request.Count, $"Równania obrazkowe: {index + 1}/{request.Count}"));
        }
        return result;
    }

    private static GeneratedWorksheet ToWorksheet(PictureEquationsPuzzle puzzle) => new(puzzle.Number,
        "picture-equations", "animals", "Równania obrazkowe",
        PuzzleSupport.Fingerprint(puzzle.Number, string.Join(',', puzzle.Values), string.Join(',', puzzle.IconKinds), puzzle.Tier),
        puzzle.Difficulty, puzzle.Difficulty.Stars, Visual(puzzle, false), Visual(puzzle, true), Rules, WorksheetLayout.HalfPage);

    private static WorksheetVisual Visual(PictureEquationsPuzzle puzzle, bool solution)
    {
        var e = new List<VisualElement>();
        Row(e, puzzle, 17, [0, 0, 0], "+", 3 * puzzle.Values[0]);
        Row(e, puzzle, 39, [0, 1, 1], "+", puzzle.Values[0] + 2 * puzzle.Values[1]);
        Row(e, puzzle, 61, [1, 2], "+", puzzle.Values[1] + puzzle.Values[2]);
        Row(e, puzzle, 84, [2, 0], puzzle.Tier <= 2 ? "+" : "×", solution ? puzzle.FinalResult : null);
        if (solution)
            for (var i = 0; i < 3; i++)
            {
                PuzzleSupport.Icon(e, puzzle.IconKinds[i], 22 + i * 30, 4, .55);
                e.Add(PuzzleSupport.Text(30 + i * 30, 6, $"={puzzle.Values[i]}", 4.6, true, "#19a88e", "start"));
            }
        return new(100, 100, e);
    }

    private static void Row(ICollection<VisualElement> e, PictureEquationsPuzzle puzzle, double y,
        int[] icons, string operation, int? answer)
    {
        var start = icons.Length == 3 ? 16 : 24;
        for (var i = 0; i < icons.Length; i++)
        {
            PuzzleSupport.Icon(e, puzzle.IconKinds[icons[i]], start + i * 20, y, .65);
            if (i < icons.Length - 1) e.Add(PuzzleSupport.Text(start + i * 20 + 10, y + 2, operation, 6));
        }
        e.Add(PuzzleSupport.Text(76, y + 2, "=", 6));
        PuzzleSupport.AnswerBox(e, 82, y - 7, 15, 14, answer?.ToString());
    }
}
