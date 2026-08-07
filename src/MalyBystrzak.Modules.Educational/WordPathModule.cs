using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Educational;

public sealed record GridPoint(int Row, int Column);
public sealed record WordPathPuzzle(int Number, int Tier, string Word, char[] Grid, IReadOnlyList<GridPoint> Path,
    CognitiveDifficulty Difficulty)
{
    public bool IsValid => Grid.Length == 20 && Path.Count == Word.Length && Path.Distinct().Count() == Path.Count &&
        Path.All(point => point.Row is >= 0 and < 4 && point.Column is >= 0 and < 5) &&
        Path.Zip(Path.Skip(1), (a, b) => Math.Abs(a.Row - b.Row) + Math.Abs(a.Column - b.Column) == 1).All(value => value) &&
        new string(Path.Select(point => Grid[point.Row * 5 + point.Column]).ToArray()) == Word;
}

public sealed class WordPathModule : IWorksheetModule
{
    internal static readonly string[] Words =
    [
        "PLANETA", "PRZYGODA", "MOTYLEK", "KREDKI", "ZABAWA", "ROWEREK", "OGRODEK", "WAKACJE",
        "BALONIK", "SAMOLOT", "RAKIETA", "PIRACI", "KRAINA", "ZAGADKA", "JAGODY", "MALINA",
        "POZIOMKA", "BANANY", "MORELKA", "TRUSKAWKA", "RODZINA", "KLOCKI", "PUZZLE", "KARTKA",
        "WIOSNA", "JESIEN", "DELFIN", "TYGRYS", "ZYRAFA", "MALPKA", "KROLIK", "BIEDRONKA",
        "PSZCZOLA", "CHOMIK", "MUZYKA", "GITARA", "PIANINO", "SPORTY", "PILKARZ", "BRAMKA",
        "KOSMOS", "GWIAZDA", "KOMETA", "TAJEMNICA", "ODKRYCIE", "WYPRAWA", "CHMURKA", "FUTBOL"
    ];
    private static readonly WorksheetInstruction Rules = new("Ścieżka literowa",
        "Zacznij w oznaczonym polu i idź według strzałek.", "Zapisz odczytane hasło.", "#55a9df");
    public string Id => "word-path";
    public string DisplayName => "Ścieżka literowa";
    public string Symbol => "→";
    public WorksheetInstruction Instruction => Rules;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
        [new("5x4", "Ścieżka literowa", "Odczytaj słowo, poruszając się po planszy", WorksheetLayout.HalfPage)];
    public IReadOnlyList<string> Validate(ModuleGenerationRequest request) => request.Count <= 0
        ? ["Liczba zadań musi być większa od zera."]
        : request.VariantId != "5x4" ? ["Nieobsługiwany wariant ścieżki literowej."] : [];

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request); if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var random = new Random(request.Seed); var result = new List<GeneratedWorksheet>();
        var wordOrder = Words.OrderBy(_ => random.Next()).ToArray();
        for (var index = 0; index < request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested(); var tier = PuzzleSupport.Tier(index, request.Count);
            var word = wordOrder[index % wordOrder.Length]; var path = CreatePath(random, word.Length);
            var alphabet = "ABCDEFGHIJKLMNOPRSTUWYZ"; var grid = Enumerable.Range(0, 20)
                .Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray();
            for (var i = 0; i < path.Count; i++) grid[path[i].Row * 5 + path[i].Column] = word[i];
            var difficulty = PuzzleSupport.Difficulty(tier, 5, 30 + tier * 8, 35 + word.Length * 4);
            var puzzle = new WordPathPuzzle(index + 1, tier, word, grid, path, difficulty);
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(index + 1, request.Count, $"Ścieżka literowa: {index + 1}/{request.Count}"));
        }
        return result;
    }

    private static IReadOnlyList<GridPoint> CreatePath(Random random, int length)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var path = new List<GridPoint> { new(random.Next(4), random.Next(5)) };
            while (path.Count < length)
            {
                var last = path[^1];
                var next = new[] { new GridPoint(last.Row - 1, last.Column), new(last.Row + 1, last.Column),
                    new(last.Row, last.Column - 1), new(last.Row, last.Column + 1) }
                    .Where(point => point.Row is >= 0 and < 4 && point.Column is >= 0 and < 5 && !path.Contains(point))
                    .OrderBy(_ => random.Next()).FirstOrDefault();
                if (next is null) break; path.Add(next);
            }
            if (path.Count == length) return path;
        }
        return Enumerable.Range(0, length).Select(index => new GridPoint(index / 5,
            index / 5 % 2 == 0 ? index % 5 : 4 - index % 5)).ToArray();
    }

    private static GeneratedWorksheet ToWorksheet(WordPathPuzzle puzzle) => new(puzzle.Number,
        "word-path", "5x4", "Ścieżka literowa", PuzzleSupport.Fingerprint(puzzle.Number, puzzle.Word, string.Join(',', puzzle.Path)),
        puzzle.Difficulty, puzzle.Difficulty.Stars, Visual(puzzle, false), Visual(puzzle, true), Rules, WorksheetLayout.HalfPage);

    private static WorksheetVisual Visual(WordPathPuzzle puzzle, bool solution)
    {
        var e = new List<VisualElement>(); const double cell = 14; const double x0 = 3; const double y0 = 12;
        for (var row = 0; row < 4; row++) for (var column = 0; column < 5; column++)
        {
            var point = new GridPoint(row, column); var pathIndex = puzzle.Path.IndexOf(point);
            e.Add(new VisualRectangle(x0 + column * cell, y0 + row * cell, cell, cell,
                solution && pathIndex >= 0 ? "#d9f4ee" : pathIndex == 0 ? "#fff1f6" : "#ffffff", "#25316d", .65));
            e.Add(PuzzleSupport.Text(x0 + column * cell + cell / 2, y0 + row * cell + 9, puzzle.Grid[row * 5 + column].ToString(), 6));
            if (pathIndex == 0) e.Add(new VisualEllipse(x0 + column * cell + cell / 2, y0 + row * cell + cell / 2,
                5.5, 5.5, "none", "#f15a8a", 1.1));
        }
        var arrows = puzzle.Path.Zip(puzzle.Path.Skip(1), (a, b) => b.Row < a.Row ? "↑" : b.Row > a.Row ? "↓" : b.Column < a.Column ? "←" : "→").ToArray();
        e.Add(PuzzleSupport.Text(77, 9, "1. START", 4.2, true, "#f15a8a", "start"));
        e.Add(PuzzleSupport.Text(77, 16, "różowe pole", 3.2, false, "#6b7280", "start"));
        e.Add(PuzzleSupport.Text(77, 27, "2. KOD", 3.8, true, "#25316d", "start"));
        e.Add(PuzzleSupport.Text(77, 33, "STRZAŁEK", 3.8, true, "#25316d", "start"));
        e.Add(PuzzleSupport.Text(77, 44, string.Join(' ', arrows.Take(4)), 4.5, true, "#25316d", "start"));
        e.Add(PuzzleSupport.Text(77, 55, string.Join(' ', arrows.Skip(4).Take(4)), 4.5, true, "#25316d", "start"));
        e.Add(PuzzleSupport.Text(77, 66, string.Join(' ', arrows.Skip(8)), 4.5, true, "#25316d", "start"));
        e.Add(PuzzleSupport.Text(25, 83, "3. HASŁO", 4.2, true, "#19a88e", "end"));
        PuzzleSupport.AnswerBox(e, 29, 72, 70, 17, solution ? puzzle.Word : null);
        if (solution)
        {
            var start = puzzle.Path[0];
            e.Add(new VisualEllipse(x0 + start.Column * cell + cell / 2, y0 + start.Row * cell + cell / 2,
                5, 5, "none", "#f15a8a", 1.2));
        }
        return new(102, 94, e);
    }
}

internal static class PathExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> source, T value) where T : notnull
    {
        for (var index = 0; index < source.Count; index++) if (EqualityComparer<T>.Default.Equals(source[index], value)) return index;
        return -1;
    }
}
