using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Educational;

public sealed record MathCrosswordPuzzle(int Number, int Tier, IReadOnlyList<Equation> Chain,
    CognitiveDifficulty Difficulty)
{
    public int CountSolutions() => Chain.Count > 0 && Chain.All(item => item.IsValid) &&
        Chain.Zip(Chain.Skip(1), (left, right) => left.Result == right.Left).All(value => value) ? 1 : 0;
}

public sealed class MathCrosswordModule : IWorksheetModule
{
    private static readonly WorksheetInstruction Rules = new("Krzyżówka matematyczna",
        "Uzupełnij połączone działania.", "Każdy wynik rozpoczyna następne działanie.", "#7058b3");
    public string Id => "math-crossword";
    public string DisplayName => "Krzyżówka matematyczna";
    public string Symbol => "×";
    public WorksheetInstruction Instruction => Rules;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
        [new("chain", "Krzyżówka matematyczna", "Łańcuch połączonych działań", WorksheetLayout.FullPage)];
    public IReadOnlyList<string> Validate(ModuleGenerationRequest request) => request.Count <= 0
        ? ["Liczba zadań musi być większa od zera."]
        : request.VariantId != "chain" ? ["Nieobsługiwany wariant krzyżówki."] : [];

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request); if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var random = new Random(request.Seed); var result = new List<GeneratedWorksheet>();
        for (var index = 0; index < request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tier = PuzzleSupport.Tier(index, request.Count); var chain = BuildChain(random, tier);
            var difficulty = PuzzleSupport.Difficulty(tier, 35 + tier * 10, 50 + tier * 6, 55 + tier * 5);
            var puzzle = new MathCrosswordPuzzle(index + 1, tier, chain, difficulty);
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(index + 1, request.Count, $"Krzyżówka matematyczna: {index + 1}/{request.Count}"));
        }
        return result;
    }

    private static Equation[] BuildChain(Random random, int tier)
    {
        var chain = new List<Equation>(); var current = random.Next(2, 7 + tier);
        for (var i = 0; i < 8; i++)
        {
            Equation equation;
            if (current > 50)
            {
                var subtract = random.Next(5, Math.Min(20, current));
                equation = new(current, subtract, "−", current - subtract);
            }
            else if (tier >= 5 && i % 4 == 3)
            {
                var divisors = Enumerable.Range(2, 7).Where(value => current % value == 0).ToArray();
                if (divisors.Length > 0)
                {
                    var divisor = divisors[random.Next(divisors.Length)];
                    equation = new(current, divisor, "÷", current / divisor);
                }
                else equation = new(current, 2, "+", current + 2);
            }
            else if (tier >= 3 && i % 3 == 2)
            {
                var factor = random.Next(2, tier >= 5 ? 6 : 4);
                equation = current * factor <= 60
                    ? new(current, factor, "×", current * factor)
                    : new(current, random.Next(2, 8), "−", current - random.Next(2, 8));
                if (!equation.IsValid) equation = new(current, 2, "+", current + 2);
            }
            else
            {
                var add = random.Next(1, 4 + tier); equation = new(current, add, "+", current + add);
            }
            chain.Add(equation); current = equation.Result;
        }
        return chain.ToArray();
    }

    private static GeneratedWorksheet ToWorksheet(MathCrosswordPuzzle puzzle) => new(puzzle.Number,
        "math-crossword", "chain", "Krzyżówka matematyczna",
        PuzzleSupport.Fingerprint(puzzle.Number, string.Join(';', puzzle.Chain)), puzzle.Difficulty, puzzle.Difficulty.Stars,
        Visual(puzzle, false), Visual(puzzle, true), Rules, WorksheetLayout.FullPage);

    private static WorksheetVisual Visual(MathCrosswordPuzzle puzzle, bool solution)
    {
        var e = new List<VisualElement>();
        for (var i = 0; i < puzzle.Chain.Count; i++)
        {
            var equation = puzzle.Chain[i]; var horizontal = i % 2 == 0;
            var x = horizontal ? 8 : 53; var y = 4 + i * 12;
            if (!horizontal) x = 50 - (i % 4) * 7;
            DrawEquation(e, x, y, equation, solution, i == puzzle.Chain.Count - 1);
            if (i < puzzle.Chain.Count - 1)
                e.Add(new VisualLine(x + 42, y + 5, horizontal ? 48 : x + 42, y + 12, .7, "#c8c8d8"));
        }
        return new(100, 104, e);
    }

    private static void DrawEquation(ICollection<VisualElement> e, double x, double y, Equation equation, bool solution, bool terminal)
    {
        var values = new[] { equation.Left.ToString(), equation.Operator, equation.Right.ToString(), "=", equation.Result.ToString() };
        for (var i = 0; i < values.Length; i++)
        {
            var answerCell = !solution && (terminal ? i == 4 : i == 2);
            e.Add(new VisualRectangle(x + i * 9, y, 8, 10, answerCell ? "#ffffff" : "#f4f2fb", "#b9b5d0", .55));
            if (!answerCell) e.Add(PuzzleSupport.Text(x + i * 9 + 4, y + 7, values[i], 4.5));
        }
    }
}
