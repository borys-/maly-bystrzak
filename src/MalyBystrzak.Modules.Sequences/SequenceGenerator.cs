namespace MalyBystrzak.Modules.Sequences;

public sealed class SequenceGenerator(int seed)
{
    private readonly Random random = new(seed);
    private static readonly IReadOnlyList<(string Rule, int[] Values)> NumberCandidates = BuildNumberCandidates();

    public IReadOnlyList<SequencePuzzle> GenerateBook(int count, string variant, CancellationToken cancellationToken = default)
    {
        if (variant is not "pictures" and not "numbers") throw new ArgumentException("Nieobsługiwany wariant sekwencji.");
        var result = new List<SequencePuzzle>(count);
        var fingerprints = new HashSet<string>();
        while (result.Count < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = Enumerable.Range(0, 3).Select(_ => variant == "numbers" ? CreateNumberRow() : CreatePictureRow()).ToArray();
            var fingerprint = string.Join('|', rows.Select(Fingerprint));
            if (fingerprints.Add(fingerprint)) result.Add(new(result.Count + 1, variant, rows));
        }
        return result;
    }

    public static int CountNumberSolutions(SequenceRow row) => NumberCandidates.Count(candidate =>
        Enumerable.Range(0, candidate.Values.Length).All(index => row.Missing.Contains(index) || candidate.Values[index] == row.Numbers![index]));

    public static int CountPicturePeriods(SequenceRow row) => Enumerable.Range(2, 3).Count(period =>
        Enumerable.Range(0, row.Pictures!.Length).Where(index => !row.Missing.Contains(index)).GroupBy(index => index % period)
            .All(group => group.Select(index => row.Pictures[index]).Distinct().Count() == 1) &&
        row.Missing.All(index => Enumerable.Range(0, row.Pictures.Length).Any(other => other != index &&
            !row.Missing.Contains(other) && other % period == index % period)));

    private SequenceRow CreateNumberRow()
    {
        while (true)
        {
            var candidate = NumberCandidates[random.Next(NumberCandidates.Count)];
            var missing = random.NextDouble() < .72 ? [random.Next(1, 5)] : TwoDistinct(1, 5);
            var row = new SequenceRow(candidate.Rule, candidate.Values, null, missing);
            if (CountNumberSolutions(row) == 1) return row;
        }
    }

    private SequenceRow CreatePictureRow()
    {
        while (true)
        {
            var period = random.Next(2, 5);
            var motif = Enumerable.Range(0, period).Select(_ => new PictureToken((PictureShape)random.Next(4), random.Next(4), random.Next(2))).ToArray();
            if (motif.Distinct().Count() != motif.Length) continue;
            var values = Enumerable.Range(0, 8).Select(index => motif[index % period]).ToArray();
            var missing = random.NextDouble() < .72 ? [random.Next(2, 7)] : TwoDistinct(2, 7);
            var row = new SequenceRow($"repeat{period}", null, values, missing);
            if (CountPicturePeriods(row) == 1) return row;
        }
    }

    private int[] TwoDistinct(int minimum, int maximumExclusive)
    {
        var first = random.Next(minimum, maximumExclusive);
        int second;
        do second = random.Next(minimum, maximumExclusive); while (second == first);
        return [first, second];
    }

    private static IReadOnlyList<(string Rule, int[] Values)> BuildNumberCandidates()
    {
        var candidates = new List<(string, int[])>();
        for (var start = 1; start <= 45; start++)
        for (var step = 1; step <= 9; step++)
            candidates.Add(("arithmetic", Enumerable.Range(0, 6).Select(index => start + index * step).ToArray()));
        for (var start = 1; start <= 24; start++)
        for (var first = 1; first <= 6; first++)
        for (var second = 1; second <= 6; second++)
            if (first != second) candidates.Add(("alternating", BuildAlternating(start, first, second)));
        for (var start = 1; start <= 24; start++)
        for (var step = 1; step <= 4; step++)
            candidates.Add(("growing", BuildGrowing(start, step)));
        for (var oddStart = 1; oddStart <= 15; oddStart++)
        for (var evenStart = 1; evenStart <= 15; evenStart++)
        for (var oddStep = 1; oddStep <= 4; oddStep++)
        for (var evenStep = 1; evenStep <= 4; evenStep++)
            candidates.Add(("interleaved", [oddStart, evenStart, oddStart + oddStep, evenStart + evenStep,
                oddStart + oddStep * 2, evenStart + evenStep * 2]));
        return candidates.DistinctBy(candidate => string.Join(',', candidate.Item2)).ToArray();
    }

    private static int[] BuildAlternating(int start, int first, int second)
    {
        var values = new int[6];
        values[0] = start;
        for (var index = 1; index < values.Length; index++) values[index] = values[index - 1] + (index % 2 == 1 ? first : second);
        return values;
    }

    private static int[] BuildGrowing(int start, int step)
    {
        var values = new int[6];
        values[0] = start;
        for (var index = 1; index < values.Length; index++) values[index] = values[index - 1] + step + index - 1;
        return values;
    }

    private static string Fingerprint(SequenceRow row) => row.Numbers is not null
        ? $"{row.Rule}:{string.Join(',', row.Numbers)}:{string.Join(',', row.Missing)}"
        : $"{row.Rule}:{string.Join(';', row.Pictures!.Select(token => $"{token.Shape}-{token.Color}-{token.Size}"))}:{string.Join(',', row.Missing)}";
}
