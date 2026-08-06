namespace MalyBystrzak.Modules.Nonograms;

public sealed class NonogramGenerator(int seed)
{
    private readonly Random random = new(seed);

    public IReadOnlyList<NonogramPuzzle> GenerateBook(int count, int size, CancellationToken cancellationToken = default)
    {
        if (size is not 5 and not 7 and not 10) throw new ArgumentOutOfRangeException(nameof(size));
        var result = new List<NonogramPuzzle>(count);
        var fingerprints = new HashSet<string>();
        var attempts = 0;
        while (result.Count < count && attempts++ < Math.Max(10_000, count * 500))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = CreateConnectedPattern(size);
            var fingerprint = string.Concat(cells.Select(value => value ? '1' : '0'));
            if (!fingerprints.Add(fingerprint)) continue;
            var rows = Enumerable.Range(0, size).Select(row => NonogramSolver.Clues(
                Enumerable.Range(0, size).Select(column => cells[row * size + column]))).ToArray();
            var columns = Enumerable.Range(0, size).Select(column => NonogramSolver.Clues(
                Enumerable.Range(0, size).Select(row => cells[row * size + column]))).ToArray();
            var solved = NonogramSolver.Solve(size, rows, columns, cancellationToken: cancellationToken);
            if (solved.Solutions != 1) continue;
            result.Add(new(result.Count + 1, size, cells, rows, columns, solved.Branches));
        }
        if (result.Count != count) throw new InvalidOperationException("Nie udało się utworzyć wymaganej liczby jednoznacznych nonogramów.");
        return result;
    }

    private bool[] CreateConnectedPattern(int size)
    {
        var cells = new bool[size * size];
        var target = random.Next((int)(cells.Length * .34), (int)(cells.Length * .58) + 1);
        var start = random.Next(size / 3, size - size / 3) * size + random.Next(size / 3, size - size / 3);
        cells[start] = true;
        var filled = new List<int> { start };
        while (filled.Count < target)
        {
            var source = filled[random.Next(filled.Count)];
            var row = source / size;
            var column = source % size;
            var neighbours = new[] { (row - 1, column), (row + 1, column), (row, column - 1), (row, column + 1) }
                .Where(point => point.Item1 >= 0 && point.Item1 < size && point.Item2 >= 0 && point.Item2 < size)
                .Select(point => point.Item1 * size + point.Item2).Where(index => !cells[index]).OrderBy(_ => random.Next()).ToArray();
            if (neighbours.Length == 0) continue;
            var next = neighbours[0];
            cells[next] = true;
            filled.Add(next);
            if (random.NextDouble() < .42 && filled.Count < target)
            {
                var mirrorColumn = size - 1 - next % size;
                var mirror = next / size * size + mirrorColumn;
                if (!cells[mirror]) { cells[mirror] = true; filled.Add(mirror); }
            }
        }
        return cells;
    }
}
