using System.Text.Json;
using MalyBystrzak.Core;
using MalyBystrzak.Modules.Kakuro;
using MalyBystrzak.Modules.Mazes;
using MalyBystrzak.Modules.Nonograms;
using MalyBystrzak.Modules.Sudoku;
using MalyBystrzak.Modules.Educational;

namespace MalyBystrzak.Tests;

public class BookGeneratorTests
{
    private static readonly ModuleSelection[] LegacyTypes =
    [new("sudoku", "4x4"), new("sudoku", "6x6"), new("kakuro", "3x3"), new("kakuro", "4x4")];
    private static readonly ModuleSelection[] AllTypes =
    [.. LegacyTypes, new("maze", "9x9"), new("maze", "15x15"), new("nonogram", "5x5"),
        new("nonogram", "7x7"), new("nonogram", "10x10"), new("picture-equations", "animals"),
        new("arithmetic-code", "six-letter"), new("math-crossword", "chain"), new("product-grid", "3x3"),
        new("word-path", "5x4")];

    [Fact]
    public void RegistryProvidesIndependentModules()
    {
        var registry = Registry();
        Assert.Equal(9, registry.All.Count);
        Assert.Equal(2, registry.GetRequired("sudoku").Variants.Count);
        Assert.Equal(2, registry.GetRequired("kakuro").Variants.Count);
        Assert.Equal(2, registry.GetRequired("maze").Variants.Count);
        Assert.Equal(3, registry.GetRequired("nonogram").Variants.Count);
    }

    [Fact]
    public void MixedBookFillsPagesAndKeepsGlobalNumbering()
    {
        var book = Generate(70, 112233, AllTypes);
        Assert.Equal(Enumerable.Range(1, 70), book.Worksheets.Select(item => item.Number));
        Assert.All(AllTypes, selection => Assert.Equal(5, book.Worksheets.Count(item =>
            item.ModuleId == selection.ModuleId && item.VariantId == selection.VariantId)));
        var pages = BookLayout.PackWorksheets(book.Worksheets);
        Assert.All(pages.Take(pages.Count - 1), page => Assert.Equal(6, page.Sum(item => Weight(item.Worksheet.Layout))));
    }

    [Fact]
    public void CliAndWebCompositionProduceSameMixedBookForSameSeed()
    {
        var settings = Settings(18, 445566, AllTypes);
        var cliGenerator = new BookGenerator(Registry());
        var webGenerator = new BookGenerator(Registry());

        var cliBook = cliGenerator.Generate(settings);
        var webBook = webGenerator.Generate(settings);

        Assert.Equal(cliBook.Worksheets.Select(item => item.Fingerprint), webBook.Worksheets.Select(item => item.Fingerprint));
        Assert.Equal(cliBook.Worksheets.Select(item => item.Difficulty), webBook.Worksheets.Select(item => item.Difficulty));
        Assert.Equal(cliBook.CreateDocument().Pages.Select(page => page.Kind), webBook.CreateDocument().Pages.Select(page => page.Kind));
    }

    [Fact]
    public void PersonalizedBookHonorsRangeAndCreatesEqualRelativeStarGroups()
    {
        var settings = Settings(30, 998877, LegacyTypes) with { ScoreMinimum = 20, ScoreMaximum = 80, RelativeStars = true };
        var book = new BookGenerator(Registry()).Generate(settings);
        Assert.All(book.Worksheets, item => Assert.InRange(item.Difficulty.Score, 20, 80));
        Assert.Equal(book.Worksheets.Select(item => item.Difficulty.Score).Order(), book.Worksheets.Select(item => item.Difficulty.Score));
        Assert.Equal(new[] { 6, 6, 6, 6, 6 }, Enumerable.Range(1, 5).Select(stars => book.Worksheets.Count(item => item.DisplayStars == stars)));
    }

    [Fact]
    public void CancellationIsObservedBeforeGeneration()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() => new BookGenerator(Registry()).Generate(
            Settings(12, 1, AllTypes), cancellationToken: source.Token));
    }

    [Fact]
    public void ProgressReportsCompletedWorksheetCount()
    {
        var reports = new List<GenerationProgress>();
        var progress = new InlineProgress<GenerationProgress>(reports.Add);

        new BookGenerator(Registry()).Generate(Settings(28, 2468, AllTypes), progress);

        Assert.NotEmpty(reports);
        Assert.Equal(28, reports[^1].Completed);
        Assert.All(reports, report => Assert.Equal(28, report.Total));
        Assert.Equal(reports.Select(report => report.Completed).Order(), reports.Select(report => report.Completed));
    }

    [Fact]
    public async Task AsyncGenerationKeepsSeedAndReportsRealBatches()
    {
        var settings = Settings(18, 13579, AllTypes);
        var generator = new BookGenerator(Registry());
        var reports = new List<GenerationProgress>();

        var asyncBook = await generator.GenerateAsync(settings, new InlineProgress<GenerationProgress>(reports.Add));
        var syncBook = generator.Generate(settings);

        Assert.Equal(syncBook.Worksheets.Select(item => item.Fingerprint),
            asyncBook.Worksheets.Select(item => item.Fingerprint));
        Assert.Equal(0, reports[0].Completed);
        Assert.Equal(settings.Count, reports[^1].Completed);
        Assert.Contains(reports, report => report.Completed is > 0 and < 18);
        Assert.Equal(reports.Select(report => report.Completed).Order(), reports.Select(report => report.Completed));
    }

    [Fact]
    public void ProjectRoundTripsWithPolymorphicVisuals()
    {
        var book = Generate(4, 123, [new("sudoku", "4x4")]);
        var project = new GeneratorProject(GeneratorProject.CurrentSchemaVersion, Guid.NewGuid(), "Test", DateTimeOffset.UtcNow, book);
        var restored = JsonSerializer.Deserialize<GeneratorProject>(JsonSerializer.Serialize(project));
        Assert.NotNull(restored);
        Assert.IsType<VisualRectangle>(restored!.Book.Worksheets[0].Task.Elements[0]);
    }

    [Fact]
    public void RegeneratingOneWorksheetKeepsOrderAndReplacesOnlyThatPuzzle()
    {
        var generator = new BookGenerator(Registry());
        var book = generator.Generate(Settings(6, 98765, [new("sudoku", "4x4")]));
        var originalFingerprints = book.Worksheets.Select(item => item.Fingerprint).ToArray();

        var regenerated = generator.RegenerateWorksheet(book, 3);

        Assert.Equal(Enumerable.Range(1, 6), regenerated.Worksheets.Select(item => item.Number));
        Assert.NotEqual(originalFingerprints[2], regenerated.Worksheets[2].Fingerprint);
        Assert.Equal(originalFingerprints.Where((_, index) => index != 2),
            regenerated.Worksheets.Where((_, index) => index != 2).Select(item => item.Fingerprint));
        Assert.Equal(6, regenerated.Worksheets.Select(item => item.Fingerprint).Distinct().Count());
    }

    private static GeneratedBook Generate(int count, int seed, IReadOnlyList<ModuleSelection> selections) =>
        new BookGenerator(Registry()).Generate(Settings(count, seed, selections));
    private static BookGenerationSettings Settings(int count, int seed, IReadOnlyList<ModuleSelection> selections) =>
        new("Test", "Zadania", null, count, seed, selections, IncludeSolutions: true);
    private static WorksheetModuleRegistry Registry() => new(
        [new SudokuModule(), new KakuroModule(), new MazeModule(), new NonogramModule(), new PictureEquationsModule(),
            new ArithmeticCodeModule(), new MathCrosswordModule(), new ProductGridModule(), new WordPathModule()]);

    private static int Weight(WorksheetLayout layout) => layout switch
    {
        WorksheetLayout.Standard => 1, WorksheetLayout.Large => 4,
        WorksheetLayout.HalfPage => 3, WorksheetLayout.FullPage => 6, _ => 0
    };

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
