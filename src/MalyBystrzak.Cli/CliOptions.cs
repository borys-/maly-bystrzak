using System.Security.Cryptography;
using MalyBystrzak.Core;

namespace MalyBystrzak.Cli;

internal enum PuzzleKind { Sudoku, Kakuro, Mixed }
internal enum PuzzleType { Sudoku4, Sudoku6, Kakuro3, Kakuro4 }

internal sealed record CliOptions(
    PuzzleKind Kind,
    string OutputDirectory,
    int Count,
    int Size,
    string Title,
    string Subtitle,
    string? ChildName,
    int Seed,
    bool Overwrite,
    IReadOnlyList<PuzzleType> Types,
    int? ScoreMinimum,
    int? ScoreMaximum,
    bool RelativeStars)
{
    public string PreviewFileName => Kind switch
    {
        PuzzleKind.Sudoku => "sudoku-podglad-a5.pdf",
        PuzzleKind.Kakuro => "kakuro-podglad-a5.pdf",
        _ => "lamiglowki-podglad-a5.pdf"
    };
    public string BookletFileName => Kind switch
    {
        PuzzleKind.Sudoku => "sudoku-broszura-a4.pdf",
        PuzzleKind.Kakuro => "kakuro-broszura-a4.pdf",
        _ => "lamiglowki-broszura-a4.pdf"
    };

    public static string HelpText => """
        Generator kolorowych książeczek Sudoku dla dzieci

        Użycie:
          maly-bystrzak generate [opcje]   Generuje Sudoku 4x4 lub 6x6
          maly-bystrzak kakuro [opcje]     Generuje dziecięce Kakuro 3x3 lub 4x4
          maly-bystrzak mixed [opcje]      Generuje mieszaną książeczkę

        Opcje:
          --output <katalog>      Katalog wynikowy (domyślnie: ./output)
          --count <liczba>        Liczba zadań (domyślnie: 60)
          --size <liczba>         Sudoku: 4 lub 6; Kakuro: 3 lub 4
          --title <tekst>         Tytuł okładki
          --subtitle <tekst>      Podtytuł okładki
          --child-name <tekst>    Imię dziecka; bez opcji zostaje linia do wpisania
          --seed <liczba>         Ziarno generatora do odtworzenia zestawu
          --types <lista>         Typy dla mixed, np. sudoku4,sudoku6,kakuro
          --score-min <0-100>     Minimalny wskaźnik dla mixed
          --score-max <0-100>     Maksymalny wskaźnik dla mixed
          --relative-stars        Równe grupy 1-5 gwiazdek wewnątrz książeczki
          --overwrite             Zezwól na zastąpienie istniejących plików
          --help, -h              Pokaż tę pomoc

        Przykłady:
          maly-bystrzak generate --count 60 --size 4
          maly-bystrzak generate --child-name "Zosia" --title "Moja książeczka Sudoku"
          maly-bystrzak generate --size 6 --count 72 --seed 12345
        """;

    public static bool TryParse(string[] args, out CliOptions? options, out string? error, out bool showHelp)
    {
        options = null;
        error = null;
        showHelp = args.Any(value => value is "--help" or "-h");
        if (showHelp)
            return true;

        var command = args.Length == 0 ? string.Empty : args[0].ToLowerInvariant();
        if (command is not "generate" and not "kakuro" and not "mixed")
        {
            error = "Brak polecenia. Użyj: generate, kakuro albo mixed.";
            return false;
        }
        var kind = command switch { "kakuro" => PuzzleKind.Kakuro, "mixed" => PuzzleKind.Mixed, _ => PuzzleKind.Sudoku };

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var overwrite = false;
        var relativeStars = false;
        var knownValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "--output", "--count", "--size", "--title", "--subtitle", "--child-name", "--seed", "--types",
            "--score-min", "--score-max"
        };

        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument.Equals("--overwrite", StringComparison.OrdinalIgnoreCase))
            {
                overwrite = true;
                continue;
            }
            if (argument.Equals("--relative-stars", StringComparison.OrdinalIgnoreCase))
            {
                relativeStars = true;
                continue;
            }
            if (!knownValues.Contains(argument))
            {
                error = $"Nieznana opcja: {argument}";
                return false;
            }
            if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Opcja {argument} wymaga wartości.";
                return false;
            }
            values[argument] = args[index];
        }

        if (!TryPositiveInt(values, "--count", 60, out var count, out error) ||
            !TryAllowedSize(values, kind, out var size, out error) ||
            !TryInt(values, "--seed", RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue), out var seed, out error))
            return false;

        if (!TryPuzzleTypes(values, kind, out var types, out error))
            return false;
        if (!TryScoreRange(values, kind, count, relativeStars, out var scoreMinimum, out var scoreMaximum, out error))
            return false;

        var output = Get(values, "--output", Path.Combine(Environment.CurrentDirectory, "output"));
        var defaultTitle = kind switch
        {
            PuzzleKind.Kakuro => "Moja książeczka Kakuro",
            PuzzleKind.Mixed => "Moja książeczka łamigłówek",
            _ => "Moja książeczka Sudoku"
        };
        var title = Get(values, "--title", defaultTitle);
        var subtitle = Get(values, "--subtitle", "Łamigłówki dla małych bystrzaków");
        values.TryGetValue("--child-name", out var childName);
        if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(subtitle))
        {
            error = "Katalog, tytuł i podtytuł nie mogą być puste.";
            return false;
        }

        options = new CliOptions(kind, Path.GetFullPath(output), count, size, title.Trim(), subtitle.Trim(),
            string.IsNullOrWhiteSpace(childName) ? null : childName.Trim(), seed, overwrite, types,
            scoreMinimum, scoreMaximum, relativeStars);
        return true;
    }

    private static string Get(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) ? value : fallback;

    private static bool TryPositiveInt(Dictionary<string, string> values, string key, int fallback, out int value, out string? error)
    {
        if (!TryInt(values, key, fallback, out value, out error))
            return false;
        if (value > 0)
            return true;
        error = $"Wartość {key} musi być większa od zera.";
        return false;
    }

    private static bool TryAllowedSize(Dictionary<string, string> values, PuzzleKind kind, out int size, out string? error)
    {
        if (kind == PuzzleKind.Mixed)
        {
            if (values.ContainsKey("--size"))
            {
                size = 3;
                error = $"Polecenie {kind.ToString().ToLowerInvariant()} nie używa opcji --size.";
                return false;
            }
            size = 3;
            error = null;
            return true;
        }
        if (kind == PuzzleKind.Kakuro)
        {
            if (!TryInt(values, "--size", 3, out size, out error))
                return false;
            if (size is 3 or 4)
                return true;
            error = "Dla Kakuro wartość --size musi wynosić 3 albo 4.";
            return false;
        }
        if (!TryInt(values, "--size", 4, out size, out error))
            return false;
        if (size is 4 or 6)
            return true;
        error = "Wartość --size musi wynosić 4 albo 6.";
        return false;
    }

    private static bool TryPuzzleTypes(Dictionary<string, string> values, PuzzleKind kind,
        out IReadOnlyList<PuzzleType> types, out string? error)
    {
        error = null;
        if (kind != PuzzleKind.Mixed)
        {
            if (values.ContainsKey("--types"))
            {
                types = Array.Empty<PuzzleType>();
                error = "Opcja --types jest dostępna tylko dla polecenia mixed.";
                return false;
            }
            types = Array.Empty<PuzzleType>();
            return true;
        }

        var text = Get(values, "--types", "sudoku4,sudoku6,kakuro3,kakuro4");
        var parsed = new List<PuzzleType>();
        foreach (var item in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var type = item.ToLowerInvariant() switch
            {
                "sudoku4" or "sudoku4x4" => PuzzleType.Sudoku4,
                "sudoku6" or "sudoku6x6" => PuzzleType.Sudoku6,
                "kakuro" or "kakuro3" or "kakuro3x3" => PuzzleType.Kakuro3,
                "kakuro4" or "kakuro4x4" => PuzzleType.Kakuro4,
                _ => (PuzzleType?)null
            };
            if (type is null)
            {
                types = Array.Empty<PuzzleType>();
                error = $"Nieznany typ zadania: {item}. Dostępne: sudoku4, sudoku6, kakuro3, kakuro4.";
                return false;
            }
            if (!parsed.Contains(type.Value))
                parsed.Add(type.Value);
        }
        if (parsed.Count == 0)
        {
            types = Array.Empty<PuzzleType>();
            error = "Opcja --types nie może być pusta.";
            return false;
        }
        types = parsed;
        return true;
    }

    private static bool TryScoreRange(Dictionary<string, string> values, PuzzleKind kind, int count,
        bool relativeStars, out int? minimum, out int? maximum, out string? error)
    {
        minimum = null;
        maximum = null;
        error = null;
        var hasMinimum = values.ContainsKey("--score-min");
        var hasMaximum = values.ContainsKey("--score-max");
        if (kind != PuzzleKind.Mixed && (hasMinimum || hasMaximum || relativeStars))
        {
            error = "Zakres wyniku i --relative-stars są dostępne tylko dla polecenia mixed.";
            return false;
        }
        if (!hasMinimum && !hasMaximum && !relativeStars)
            return true;
        if (!hasMinimum || !hasMaximum || !relativeStars)
        {
            error = "Personalizowana książeczka wymaga --score-min, --score-max i --relative-stars.";
            return false;
        }
        if (!int.TryParse(values["--score-min"], out var min) || !int.TryParse(values["--score-max"], out var max) ||
            min < 0 || max > 100 || min >= max)
        {
            error = "Zakres wskaźnika musi spełniać: 0 <= score-min < score-max <= 100.";
            return false;
        }
        if (count < 5)
        {
            error = "Przy --relative-stars liczba zadań musi wynosić co najmniej 5.";
            return false;
        }
        minimum = min;
        maximum = max;
        return true;
    }

    private static bool TryInt(Dictionary<string, string> values, string key, int fallback, out int value, out string? error)
    {
        error = null;
        if (!values.TryGetValue(key, out var text))
        {
            value = fallback;
            return true;
        }
        if (int.TryParse(text, out value))
            return true;
        error = $"Wartość {key} musi być liczbą całkowitą.";
        return false;
    }
}


