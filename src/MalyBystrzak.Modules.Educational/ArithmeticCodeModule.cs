using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Educational;

public sealed record ArithmeticCodePuzzle(int Number, int Tier, string Word, string CodeLetters,
    IReadOnlyList<Equation> Equations, IReadOnlyList<int> OrderedResults, CognitiveDifficulty Difficulty);

public sealed class ArithmeticCodeModule : IWorksheetModule
{
    internal static readonly string[] Words = ["PLANET", "GRUSZA", "KWIATY", "MOTYLE", "OBRAZY", "JESION", "FUTBOL", "KOSZUL"];
    private static readonly WorksheetInstruction Rules = new("Szyfr z działań",
        "Wykonaj działania i przypisz wynikom litery.", "Odczytaj ukryte słowo.", "#19a88e");
    public string Id => "arithmetic-code";
    public string DisplayName => "Szyfr z działań";
    public string Symbol => "A";
    public WorksheetInstruction Instruction => Rules;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
        [new("six-letter", "Szyfr z działań", "Oblicz i odkryj sześcioliterowe hasło", WorksheetLayout.HalfPage)];

    public IReadOnlyList<string> Validate(ModuleGenerationRequest request) => request.Count <= 0
        ? ["Liczba zadań musi być większa od zera."]
        : request.VariantId != "six-letter" ? ["Nieobsługiwany wariant szyfru."] : [];

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request); if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var random = new Random(request.Seed); var result = new List<GeneratedWorksheet>();
        for (var index = 0; index < request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tier = PuzzleSupport.Tier(index, request.Count);
            var word = Words[random.Next(Words.Length)];
            var results = Enumerable.Range(tier <= 2 ? 3 : 5, tier <= 3 ? 16 : 30)
                .OrderBy(_ => random.Next()).Take(6).ToArray();
            var sourceEquations = results.Select(value => PuzzleSupport.CreateEquation(random, value, tier)).ToArray();
            var order = Enumerable.Range(0, 6).OrderBy(_ => random.Next()).ToArray();
            var equations = order.Select(position => sourceEquations[position]).ToArray();
            var codeLetters = new string(order.Select(position => word[position]).ToArray());
            var difficulty = PuzzleSupport.Difficulty(tier, 35 + tier * 9, 28 + tier * 6, 45);
            var puzzle = new ArithmeticCodePuzzle(index + 1, tier, word, codeLetters, equations, results, difficulty);
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(index + 1, request.Count, $"Szyfr z działań: {index + 1}/{request.Count}"));
        }
        return result;
    }

    private static GeneratedWorksheet ToWorksheet(ArithmeticCodePuzzle puzzle) => new(puzzle.Number,
        "arithmetic-code", "six-letter", "Szyfr z działań",
        PuzzleSupport.Fingerprint(puzzle.Number, puzzle.Word, puzzle.CodeLetters, string.Join(';', puzzle.Equations)),
        puzzle.Difficulty, puzzle.Difficulty.Stars, Visual(puzzle, false), Visual(puzzle, true), Rules, WorksheetLayout.HalfPage);

    private static WorksheetVisual Visual(ArithmeticCodePuzzle puzzle, bool solution)
    {
        var e = new List<VisualElement>();
        for (var i = 0; i < 6; i++)
        {
            var column = i % 2; var row = i / 2; var x = 3 + column * 50; var y = 7 + row * 22;
            var equation = puzzle.Equations[i];
            e.Add(new VisualRectangle(x, y, 23, 15, "#f7d6e4", "none"));
            e.Add(PuzzleSupport.Text(x + 11.5, y + 10, $"{equation.Left} {equation.Operator} {equation.Right}", 5.1));
            e.Add(PuzzleSupport.Text(x + 26, y + 10, "=", 4.5));
            PuzzleSupport.AnswerBox(e, x + 29, y, 10, 15, solution ? equation.Result.ToString() : null);
            e.Add(PuzzleSupport.Text(x + 44, y + 10, puzzle.CodeLetters[i].ToString(), 5.5, true, "#19a88e"));
        }
        for (var i = 0; i < 6; i++)
        {
            var x = 6 + i * 15;
            PuzzleSupport.AnswerBox(e, x, 79, 12, 14, solution ? puzzle.Word[i].ToString() : null);
            e.Add(PuzzleSupport.Text(x + 6, 98, puzzle.OrderedResults[i].ToString(), 4.2, true, "#19a88e"));
        }
        return new(100, 103, e);
    }
}
