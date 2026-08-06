using System.Text.Json;
using MalyBystrzak.Core;
using MalyBystrzak.Modules.Kakuro;
using MalyBystrzak.Modules.Sudoku;

namespace MalyBystrzak.Tests;

public class BookGeneratorTests
{
    private static readonly ModuleSelection[] AllTypes =
    [new("sudoku", "4x4"), new("sudoku", "6x6"), new("kakuro", "3x3"), new("kakuro", "4x4")];

    [Fact]
    public void RegistryProvidesIndependentModules()
    {
        var registry = Registry();
        Assert.Equal(2, registry.All.Count);
        Assert.Equal(2, registry.GetRequired("sudoku").Variants.Count);
        Assert.Equal(2, registry.GetRequired("kakuro").Variants.Count);
    }

    [Fact]
    public void MixedBookCyclesThroughSelectedTypesAndKeepsGlobalNumbering()
    {
        var book = Generate(60, 112233, AllTypes);
        Assert.Equal(Enumerable.Range(1, 60), book.Worksheets.Select(item => item.Number));
        Assert.All(AllTypes, selection => Assert.Equal(15, book.Worksheets.Count(item =>
            item.ModuleId == selection.ModuleId && item.VariantId == selection.VariantId)));
        for (var index = 0; index < book.Worksheets.Count; index++)
            Assert.Equal(AllTypes[index % AllTypes.Length].VariantId, book.Worksheets[index].VariantId);
    }

    [Fact]
    public void SameSeedProducesSameMixedBook()
    {
        var first = Generate(18, 445566, AllTypes);
        var second = Generate(18, 445566, AllTypes);
        Assert.Equal(first.Worksheets.Select(item => item.Fingerprint), second.Worksheets.Select(item => item.Fingerprint));
    }

    [Fact]
    public void PersonalizedBookHonorsRangeAndCreatesEqualRelativeStarGroups()
    {
        var settings = Settings(30, 998877, AllTypes) with { ScoreMinimum = 20, ScoreMaximum = 80, RelativeStars = true };
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

        new BookGenerator(Registry()).Generate(Settings(12, 2468, AllTypes), progress);

        Assert.NotEmpty(reports);
        Assert.Equal(12, reports[^1].Completed);
        Assert.All(reports, report => Assert.Equal(12, report.Total));
        Assert.Equal(reports.Select(report => report.Completed).Order(), reports.Select(report => report.Completed));
    }

    [Fact]
    public void ProjectRoundTripsWithPolymorphicVisuals()
    {
        var book = Generate(4, 123, [new("sudoku", "4x4")]);
        var project = new GeneratorProject(1, Guid.NewGuid(), "Test", DateTimeOffset.UtcNow, book);
        var restored = JsonSerializer.Deserialize<GeneratorProject>(JsonSerializer.Serialize(project));
        Assert.NotNull(restored);
        Assert.IsType<VisualRectangle>(restored!.Book.Worksheets[0].Task.Elements[0]);
    }

    private static GeneratedBook Generate(int count, int seed, IReadOnlyList<ModuleSelection> selections) =>
        new BookGenerator(Registry()).Generate(Settings(count, seed, selections));
    private static BookGenerationSettings Settings(int count, int seed, IReadOnlyList<ModuleSelection> selections) =>
        new("Test", "Zadania", null, count, seed, selections, IncludeSolutions: true);
    private static WorksheetModuleRegistry Registry() => new([new SudokuModule(), new KakuroModule()]);

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
