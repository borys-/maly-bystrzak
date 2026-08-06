using MalyBystrzak.Cli;
using MalyBystrzak.Core;
using MalyBystrzak.Modules.Kakuro;
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
var reportPath = Path.Combine(cli.OutputDirectory, DifficultyReport.FileName);
if (!cli.Overwrite && (File.Exists(previewPath) || File.Exists(bookletPath) || File.Exists(reportPath)))
{
    Console.Error.WriteLine("Błąd: pliki wynikowe już istnieją. Użyj --overwrite, aby je zastąpić.");
    return 3;
}

try
{
    Directory.CreateDirectory(cli.OutputDirectory);
    var selections = CreateSelections(cli);
    var settings = new BookGenerationSettings(cli.Title, cli.Subtitle, cli.ChildName, cli.Count, cli.Seed,
        selections, cli.ScoreMinimum, cli.ScoreMaximum, cli.RelativeStars, IncludeSolutions: true);
    var registry = new WorksheetModuleRegistry([new SudokuModule(), new KakuroModule()]);
    var progress = new Progress<GenerationProgress>(value => Console.WriteLine(value.Message));
    var book = new BookGenerator(registry).Generate(settings, progress);
    var document = book.CreateDocument();
    var renderer = new BookPdfRenderer();
    File.WriteAllBytes(previewPath, renderer.RenderPreview(document));
    File.WriteAllBytes(bookletPath, renderer.RenderBooklet(document));
    File.WriteAllBytes(reportPath, DifficultyReport.Create(book.Worksheets));

    Console.WriteLine($"Gotowe. Utworzono {document.Pages.Count} stron A5.");
    Console.WriteLine($"Podgląd:  {previewPath}");
    Console.WriteLine($"Broszura: {bookletPath}");
    Console.WriteLine($"Raport:    {reportPath}");
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
    _ => options.Types.Select(type => type switch
    {
        PuzzleType.Sudoku4 => new ModuleSelection("sudoku", "4x4"),
        PuzzleType.Sudoku6 => new ModuleSelection("sudoku", "6x6"),
        PuzzleType.Kakuro3 => new ModuleSelection("kakuro", "3x3"),
        _ => new ModuleSelection("kakuro", "4x4")
    }).ToArray()
};
