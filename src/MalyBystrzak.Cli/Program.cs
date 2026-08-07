using MalyBystrzak.Cli;
using MalyBystrzak.Core;
using MalyBystrzak.Modules.Kakuro;
using MalyBystrzak.Modules.Mazes;
using MalyBystrzak.Modules.Nonograms;
using MalyBystrzak.Modules.Sudoku;
using MalyBystrzak.Pdf;

if (!CliOptions.TryParse(args, out var options, out var error, out var showHelp))
{
    Console.Error.WriteLine($"Błąd: {error}");
    Console.Error.WriteLine("Użyj --help, aby zobaczyć dostępne opcje.");
    return 2;
}
if (showHelp)
{
    Console.WriteLine(CliOptions.HelpText);
    return 0;
}

var cli = options!;
var previewPath = Path.Combine(cli.OutputDirectory, cli.PreviewFileName);
var bookletPath = Path.Combine(cli.OutputDirectory, cli.BookletFileName);
if (!cli.Overwrite && (File.Exists(previewPath) || File.Exists(bookletPath)))
{
    Console.Error.WriteLine("Błąd: pliki wynikowe już istnieją. Użyj --overwrite, aby je zastąpić.");
    return 3;
}

try
{
    Directory.CreateDirectory(cli.OutputDirectory);
    var selections = CreateSelections(cli);
    var settings = new BookGenerationSettings(cli.Title, cli.Subtitle, cli.ChildName, cli.Count, cli.Seed,
        selections, cli.ScoreMinimum, cli.ScoreMaximum, cli.RelativeStars, cli.IncludeSolutions);
    var registry = new WorksheetModuleRegistry([new SudokuModule(), new KakuroModule(), new MazeModule(), new NonogramModule()]);
    var progress = new Progress<GenerationProgress>(value => Console.WriteLine(value.Message));
    var book = new BookGenerator(registry).Generate(settings, progress);
    var document = book.CreateDocument();
    var renderer = new BookPdfRenderer();
    File.WriteAllBytes(previewPath, renderer.RenderPreview(document));
    File.WriteAllBytes(bookletPath, renderer.RenderBooklet(document));

    Console.WriteLine($"Gotowe. Utworzono {document.Pages.Count} stron A5.");
    Console.WriteLine($"Podgląd:  {previewPath}");
    Console.WriteLine($"Broszura: {bookletPath}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Błąd podczas generowania: {exception.Message}");
    return 1;
}

static IReadOnlyList<ModuleSelection> CreateSelections(CliOptions options) => options.Kind switch
{
    PuzzleKind.Sudoku => [new("sudoku", $"{options.Size}x{options.Size}")],
    PuzzleKind.Kakuro => [new("kakuro", $"{options.Size}x{options.Size}")],
    PuzzleKind.Maze => [new("maze", $"{options.Size}x{options.Size}")],
    PuzzleKind.Nonogram => [new("nonogram", $"{options.Size}x{options.Size}")],
    _ => options.Types.Select(type => type switch
    {
        PuzzleType.Sudoku4 => new ModuleSelection("sudoku", "4x4"),
        PuzzleType.Sudoku6 => new ModuleSelection("sudoku", "6x6"),
        PuzzleType.Kakuro3 => new ModuleSelection("kakuro", "3x3"),
        PuzzleType.Kakuro4 => new ModuleSelection("kakuro", "4x4"),
        PuzzleType.Maze9 => new ModuleSelection("maze", "9x9"),
        PuzzleType.Maze15 => new ModuleSelection("maze", "15x15"),
        PuzzleType.Nonogram5 => new ModuleSelection("nonogram", "5x5"),
        PuzzleType.Nonogram7 => new ModuleSelection("nonogram", "7x7"),
        _ => new ModuleSelection("nonogram", "10x10")
    }).ToArray()
};
