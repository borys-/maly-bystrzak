namespace MalyBystrzak.Core;

public sealed record BookGenerationSettings(
    string Title, string Subtitle, string? ChildName, int Count, int Seed,
    IReadOnlyList<ModuleSelection> Selections, int? ScoreMinimum = null, int? ScoreMaximum = null,
    bool RelativeStars = false, bool IncludeSolutions = true);

public sealed record GeneratedBook(BookGenerationSettings Settings, IReadOnlyList<GeneratedWorksheet> Worksheets)
{
    private IReadOnlyList<WorksheetInstruction> Instructions => Worksheets.Select(item => item.Instruction).Distinct().ToArray();
    public BookDocument CreateDocument() => new(Settings, BookLayout.BuildPages(Worksheets, Settings.IncludeSolutions), Instructions);
    public BookDocument CreateSolutionsDocument() => new(Settings, BookLayout.BuildSolutionPages(Worksheets), Instructions);
}

public sealed record GeneratorProject(
    int SchemaVersion,
    Guid Id,
    string Name,
    DateTimeOffset UpdatedAt,
    GeneratedBook Book)
{
    public const int CurrentSchemaVersion = 5;
}

public sealed record ProjectSummary(Guid Id, string Name, DateTimeOffset UpdatedAt, int WorksheetCount);

public interface IProjectStore
{
    ValueTask<IReadOnlyList<ProjectSummary>> ListAsync();
    ValueTask<GeneratorProject?> GetAsync(Guid id);
    ValueTask SaveAsync(GeneratorProject project);
    ValueTask DeleteAsync(Guid id);
}

public sealed class BookGenerator(WorksheetModuleRegistry registry)
{
    public GeneratedBook Generate(BookGenerationSettings settings, IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => GenerateCoreAsync(settings, progress, cancellationToken, false).GetAwaiter().GetResult();

    public Task<GeneratedBook> GenerateAsync(BookGenerationSettings settings,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
        => GenerateCoreAsync(settings, progress, cancellationToken, true);

    private async Task<GeneratedBook> GenerateCoreAsync(BookGenerationSettings settings,
        IProgress<GenerationProgress>? progress, CancellationToken cancellationToken, bool cooperative)
    {
        Validate(settings);
        var required = settings.Selections.ToDictionary(selection => selection, _ => 0);
        for (var index = 0; index < settings.Count; index++)
            required[settings.Selections[index % settings.Selections.Count]]++;

        var queues = new Dictionary<ModuleSelection, Queue<GeneratedWorksheet>>();
        var completed = 0;
        foreach (var (selection, count) in required)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var module = registry.GetRequired(selection.ModuleId);
            progress?.Report(new(completed, settings.Count,
                $"Generuję {module.DisplayName}: {completed} z {settings.Count} gotowych"));
            if (cooperative) await Task.Delay(16, cancellationToken);
            var errors = module.Validate(new(selection.VariantId, count, settings.Seed));
            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
            var generated = settings.RelativeStars
                ? GenerateInRange(module, selection, count, settings, cancellationToken)
                : Calibrate(module.Generate(
                    new(selection.VariantId, count, DeriveSeed(settings.Seed, selection, 0)), null, cancellationToken));
            queues[selection] = new Queue<GeneratedWorksheet>(generated);
            completed += count;
            progress?.Report(new(completed, settings.Count, $"Wygenerowano {completed} z {settings.Count} zadań"));
            if (cooperative) await Task.Delay(16, cancellationToken);
        }

        var result = new List<GeneratedWorksheet>(settings.Count);
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < settings.Count; index++)
        {
            var item = queues[settings.Selections[index % settings.Selections.Count]].Dequeue();
            if (!fingerprints.Add(item.Fingerprint))
                throw new InvalidOperationException("Generator utworzył powtarzające się zadanie w jednej książeczce.");
            result.Add(item with { Number = index + 1 });
        }

        if (settings.RelativeStars)
        {
            result = result.OrderBy(item => item.Difficulty.Score).ToList();
            for (var index = 0; index < result.Count; index++)
                result[index] = result[index] with { Number = index + 1, DisplayStars = RelativeStars(index, result.Count) };
        }
        result = BookLayout.ArrangeForFullPages(result)
            .Select((item, index) => item with { Number = index + 1 })
            .ToList();
        progress?.Report(new(settings.Count, settings.Count, $"Gotowe: {settings.Count} zadań"));
        return new(settings, result);
    }

    private static IReadOnlyList<GeneratedWorksheet> GenerateInRange(IWorksheetModule module,
        ModuleSelection selection, int count, BookGenerationSettings settings, CancellationToken cancellationToken)
    {
        var candidates = new List<GeneratedWorksheet>();
        for (var round = 0; round < 8 && candidates.Count < count * 2; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = Calibrate(module.Generate(new(selection.VariantId, Math.Max(60, count * 3),
                DeriveSeed(settings.Seed, selection, round)), null, cancellationToken));
            candidates.AddRange(batch.Where(item => item.Difficulty.Score >= settings.ScoreMinimum &&
                item.Difficulty.Score <= settings.ScoreMaximum));
        }
        var ordered = candidates.DistinctBy(item => item.Fingerprint).OrderBy(item => item.Difficulty.Score).ToArray();
        if (ordered.Length < count)
            throw new InvalidOperationException($"Za mało unikalnych zadań {module.DisplayName} w wybranym zakresie.");
        return Enumerable.Range(0, count).Select(index => ordered[count == 1 ? ordered.Length / 2 :
            (int)Math.Round(index * (ordered.Length - 1d) / (count - 1))]).ToArray();
    }

    private static IReadOnlyList<GeneratedWorksheet> Calibrate(IReadOnlyList<GeneratedWorksheet> worksheets)
    {
        if (worksheets.Count == 0) return worksheets;
        var scoreByRawValue = new Dictionary<int, int>();
        var rank = 0;
        foreach (var group in worksheets.GroupBy(item => item.Difficulty.RawScore).OrderBy(group => group.Key))
        {
            var averageRank = rank + (group.Count() - 1) / 2d;
            scoreByRawValue[group.Key] = (int)Math.Round(5 + 90 * (averageRank + .5) / worksheets.Count);
            rank += group.Count();
        }
        return worksheets.Select(item =>
        {
            var difficulty = item.Difficulty.WithScore(scoreByRawValue[item.Difficulty.RawScore]);
            return item with { Difficulty = difficulty, DisplayStars = difficulty.Stars };
        }).ToArray();
    }

    private static void Validate(BookGenerationSettings settings)
    {
        if (settings.Count <= 0) throw new ArgumentOutOfRangeException(nameof(settings.Count));
        if (settings.Selections.Count == 0) throw new ArgumentException("Wybierz co najmniej jeden rodzaj zadania.");
        if (string.IsNullOrWhiteSpace(settings.Title) || string.IsNullOrWhiteSpace(settings.Subtitle))
            throw new ArgumentException("Tytuł i podtytuł nie mogą być puste.");
        if (settings.RelativeStars && (settings.Count < 5 || settings.ScoreMinimum is null || settings.ScoreMaximum is null ||
            settings.ScoreMinimum < 0 || settings.ScoreMaximum > 100 || settings.ScoreMinimum >= settings.ScoreMaximum))
            throw new ArgumentException("Personalizowana książeczka wymaga co najmniej 5 zadań i poprawnego zakresu 0–100.");
    }

    private static int DeriveSeed(int seed, ModuleSelection selection, int round)
    {
        unchecked
        {
            var hash = 17;
            foreach (var character in $"{selection.ModuleId}:{selection.VariantId}") hash = hash * 31 + character;
            return seed * 397 ^ hash * 7919 ^ round * 104729;
        }
    }

    private static int RelativeStars(int index, int count)
    {
        var baseGroup = count / 5;
        var largerGroups = count % 5;
        var largerSize = baseGroup + 1;
        var largerSection = largerGroups * largerSize;
        return index < largerSection ? index / largerSize + 1 : largerGroups + (index - largerSection) / baseGroup + 1;
    }
}
