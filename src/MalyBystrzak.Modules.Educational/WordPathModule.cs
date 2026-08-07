using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Educational;

public sealed record GridPoint(int Row, int Column);
public sealed record WordPathPuzzle(int Number, int Tier, string Word, char[] Grid, IReadOnlyList<GridPoint> Path,
    int Columns, int Rows, CognitiveDifficulty Difficulty)
{
    public bool IsValid => Grid.Length == Rows * Columns && Path.Count == Word.Length && Path.Distinct().Count() == Path.Count &&
        Path.All(point => point.Row >= 0 && point.Row < Rows && point.Column >= 0 && point.Column < Columns) &&
        Path.Zip(Path.Skip(1), (a, b) => Math.Abs(a.Row - b.Row) + Math.Abs(a.Column - b.Column) == 1).All(value => value) &&
        new string(Path.Select(point => Grid[point.Row * Columns + point.Column]).ToArray()) == Word;
}

public sealed class WordPathModule : IWorksheetModule
{
    internal static readonly string[] Words = """
        BIEDRONKA PSZCZOLA MOTYLEK CHOMIK KROLIK TYGRYS ZYRAFA DELFIN PINGWIN WIEWIORKA
        JELONEK SARENKA KACZKA KURCZAK KOCIAK SZCZENIAK ZREBIAK BARANEK OWIECZKA MALPKA
        PLANETA GWIAZDA KOMETA KOSMOS RAKIETA CHMURKA TECZOWY WIOSNA JESIEN WAKACJE
        OGRODEK POLANKA LESNIK JEZIORO OCEANY WODOSPAD STRUMYK PAGOREK KAMIEN MUSZELKA
        JAGODY MALINA MORELKA TRUSKAWKA BANANY JABLKO GRUSZKA SLIWKA CYTRYNA ARBUZY
        MARCHEW POMIDOR OGOREK PAPRYKA CEBULKA PIERNIK NALESNIK KANAPKA HERBATA KOMPOT
        KREDKI KLOCKI PUZZLE KARTKA NOZYCZKI PEDZELE PLECAK PIORNIK ZESZYTY TABLICA
        LINIJKA CYRKIEL LITERKI LICZBY ZADANIE ZAGADKA CZYTANIE PISANIE RYSUNEK WYCINANKA
        ROWEREK SAMOLOT BALONIK POCIAG AUTOBUS TRAMWAJ TRAKTOR STATEK SKUTER WAGONIK
        ZAGLOWKA HULAJNOGA KARETKA TAKSOWKA LAWETA PODROZE WEDROWKA BIWAKOWY NAMIOTY LOTNISKO
        RODZINA MAMUSIA TATUSIO BABUNIA DZIADEK SIOSTRA BRACIE KOLEGA PRZYJAZN LEKARZ
        PIEKARZ KUCHARZ MALARZ AKTORKA TANCERZ STRAZAK POLICJANT OGRODNIK PODROZNIK PILOTKA
        ZABAWA PRZYGODA ODKRYCIE TAJEMNICA WYPRAWA PIRACI SKARBY KRAINA ZAMECZEK WROZKA
        RYCERZ KROLEWNA KORONA ZBROJA TARCZA MIECZYK JASKINIA LATARNIA KOMPAS PRZYSTAN
        MUZYKA GITARA PIANINO FUTBOL SPORTY BRAMKA TENISY SIATKOWKA PLYWANIE BIEGANIE
        SKAKANKA WROTKI MECZYK MEDALE PUCHAR DRUZYNA TRENING TANIEC PIOSENKA MELODIA
        PODUSZKA KOLDERKA LAMPKA STOLIK KRZESLO DYWANIK OKIENKO ZEGAREK LUSTERKO SZAFKA
        ZABAWKA LALECZKA MISIEK ROBOTEK PAJACYK KUKIELKA KOSTKI DOMINO UKLADANKA KSIAZKA
        BALWANEK PREZENT CHOINKA SWIETA URODZINY TORCIK BALONY GIRLANDA KONFETTI ZYCZENIA
        PORANEK WIECZOR SOBOTA NIEDZIELA HUMOREK USMIECH RADOSC MARZENIE POMYSL BYSTRZAK
        """.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    private static readonly WorksheetInstruction Rules = new("Ścieżka literowa",
        "Zacznij w oznaczonym polu i idź według strzałek.", "Zapisz odczytane hasło.", "#55a9df");
    public string Id => "word-path";
    public string DisplayName => "Ścieżka literowa";
    public string Symbol => "→";
    public WorksheetInstruction Instruction => Rules;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
    [
        new("5x4", "Ścieżka literowa 5 × 4", "Pierwsza plansza z krótką trasą", WorksheetLayout.HalfPage),
        new("6x5", "Ścieżka literowa 6 × 5", "Więcej liter-pułapek", WorksheetLayout.HalfPage),
        new("7x6", "Ścieżka literowa 7 × 6", "Duża plansza z najdłuższą trasą", WorksheetLayout.FullPage)
    ];
    public IReadOnlyList<string> Validate(ModuleGenerationRequest request) => request.Count <= 0
        ? ["Liczba zadań musi być większa od zera."]
        : Dimensions(request.VariantId) is null ? ["Nieobsługiwany wariant ścieżki literowej."] : [];

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request); if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var random = new Random(request.Seed); var result = new List<GeneratedWorksheet>();
        var (columns, rows, _) = Dimensions(request.VariantId)!.Value;
        var wordOrder = Words.OrderBy(_ => random.Next()).ToArray();
        for (var index = 0; index < request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested(); var tier = PuzzleSupport.Tier(index, request.Count);
            var word = wordOrder[index % wordOrder.Length]; var path = CreatePath(random, word.Length, rows, columns);
            var alphabet = "ABCDEFGHIJKLMNOPRSTUWYZ"; var grid = Enumerable.Range(0, 20)
                .Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray();
            Array.Resize(ref grid, rows * columns);
            for (var i = 20; i < grid.Length; i++) grid[i] = alphabet[random.Next(alphabet.Length)];
            for (var i = 0; i < path.Count; i++) grid[path[i].Row * columns + path[i].Column] = word[i];
            var difficulty = PuzzleSupport.Difficulty(tier, 5, 25 + tier * 7 + (columns - 5) * 8, 35 + word.Length * 4);
            var puzzle = new WordPathPuzzle(index + 1, tier, word, grid, path, columns, rows, difficulty);
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(index + 1, request.Count, $"Ścieżka literowa: {index + 1}/{request.Count}"));
        }
        return result;
    }

    private static IReadOnlyList<GridPoint> CreatePath(Random random, int length, int rows, int columns)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var path = new List<GridPoint> { new(random.Next(rows), random.Next(columns)) };
            while (path.Count < length)
            {
                var last = path[^1];
                var next = new[] { new GridPoint(last.Row - 1, last.Column), new(last.Row + 1, last.Column),
                    new(last.Row, last.Column - 1), new(last.Row, last.Column + 1) }
                    .Where(point => point.Row >= 0 && point.Row < rows && point.Column >= 0 && point.Column < columns && !path.Contains(point))
                    .OrderBy(_ => random.Next()).FirstOrDefault();
                if (next is null) break; path.Add(next);
            }
            if (path.Count == length) return path;
        }
        return Enumerable.Range(0, length).Select(index => new GridPoint(index / columns,
            index / columns % 2 == 0 ? index % columns : columns - 1 - index % columns)).ToArray();
    }

    private static GeneratedWorksheet ToWorksheet(WordPathPuzzle puzzle) => new(puzzle.Number,
        "word-path", $"{puzzle.Columns}x{puzzle.Rows}", $"Ścieżka literowa {puzzle.Columns} × {puzzle.Rows}",
        PuzzleSupport.Fingerprint(puzzle.Number, puzzle.Word, puzzle.Columns, puzzle.Rows, string.Join(',', puzzle.Path)),
        puzzle.Difficulty, puzzle.Difficulty.Stars, Visual(puzzle, false), Visual(puzzle, true), Rules,
        Dimensions($"{puzzle.Columns}x{puzzle.Rows}")!.Value.Layout);

    private static WorksheetVisual Visual(WordPathPuzzle puzzle, bool solution)
    {
        var e = new List<VisualElement>();
        var cell = puzzle.Columns switch { 5 => 13d, 6 => 11d, _ => 9.5d };
        const double x0 = 2; const double y0 = 31;
        for (var row = 0; row < puzzle.Rows; row++) for (var column = 0; column < puzzle.Columns; column++)
        {
            var point = new GridPoint(row, column); var pathIndex = puzzle.Path.IndexOf(point);
            e.Add(new VisualRectangle(x0 + column * cell, y0 + row * cell, cell, cell,
                solution && pathIndex >= 0 ? "#d9f4ee" : pathIndex == 0 ? "#fff1f6" : "#ffffff", "#25316d", .65));
            e.Add(PuzzleSupport.Text(x0 + column * cell + cell / 2, y0 + row * cell + cell * .67,
                puzzle.Grid[row * puzzle.Columns + column].ToString(), Math.Min(6, cell * .44)));
            if (pathIndex == 0) e.Add(new VisualEllipse(x0 + column * cell + cell / 2, y0 + row * cell + cell / 2,
                5.5, 5.5, "none", "#f15a8a", 1.1));
        }
        var arrows = puzzle.Path.Zip(puzzle.Path.Skip(1), (a, b) => b.Row < a.Row ? "↑" : b.Row > a.Row ? "↓" : b.Column < a.Column ? "←" : "→").ToArray();
        var start = puzzle.Path[0];
        e.Add(PuzzleSupport.Text(71, 10, "KOD STRZAŁEK", 4, true, "#25316d"));
        e.Add(PuzzleSupport.Text(39, 23, "START", 4, true, "#f15a8a", "end"));
        e.Add(PuzzleSupport.Text(44, 23, string.Join(' ', arrows), 5, true, "#25316d", "start"));
        const double panelCenter = 108;
        e.Add(PuzzleSupport.Text(panelCenter, 42, "WPISZ HASŁO", 4, true, "#19a88e"));
        const double answerAreaX = 76; const double answerAreaWidth = 64; const double gap = 1;
        var boxWidth = Math.Min(9, (answerAreaWidth - gap * (puzzle.Word.Length - 1)) / puzzle.Word.Length);
        var boxesWidth = puzzle.Word.Length * boxWidth + (puzzle.Word.Length - 1) * gap;
        var boxesX = answerAreaX + (answerAreaWidth - boxesWidth) / 2;
        for (var index = 0; index < puzzle.Word.Length; index++)
            PuzzleSupport.AnswerBox(e, boxesX + index * (boxWidth + gap), 49, boxWidth, 15,
                solution ? puzzle.Word[index].ToString() : null);
        if (solution)
        {
            e.Add(new VisualEllipse(x0 + start.Column * cell + cell / 2, y0 + start.Row * cell + cell / 2,
                5, 5, "none", "#f15a8a", 1.2));
        }
        return new(142, 94, e);
    }

    private static (int Columns, int Rows, WorksheetLayout Layout)? Dimensions(string variantId) => variantId switch
    {
        "5x4" => (5, 4, WorksheetLayout.HalfPage),
        "6x5" => (6, 5, WorksheetLayout.HalfPage),
        "7x6" => (7, 6, WorksheetLayout.FullPage),
        _ => null
    };
}

internal static class PathExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> source, T value) where T : notnull
    {
        for (var index = 0; index < source.Count; index++) if (EqualityComparer<T>.Default.Equals(source[index], value)) return index;
        return -1;
    }
}
