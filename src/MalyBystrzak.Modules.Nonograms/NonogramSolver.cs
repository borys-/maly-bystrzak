namespace MalyBystrzak.Modules.Nonograms;

public static class NonogramSolver
{
    public static int CountSolutions(int size, int[][] rowClues, int[][] columnClues, int limit = 2,
        CancellationToken cancellationToken = default) => Solve(size, rowClues, columnClues, limit, cancellationToken).Solutions;

    public static (int Solutions, int Branches) Solve(int size, int[][] rowClues, int[][] columnClues, int limit = 2,
        CancellationToken cancellationToken = default)
    {
        var rowOptions = rowClues.Select(clues => LineOptions(size, clues)).ToArray();
        var columnOptions = columnClues.Select(clues => LineOptions(size, clues)).ToArray();
        var selectedRows = new int[size];
        var solutions = 0;
        var branches = 0;

        Search(0);
        return (solutions, branches);

        void Search(int row)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (solutions >= limit) return;
            if (row == size) { solutions++; return; }
            foreach (var option in rowOptions[row])
            {
                branches++;
                selectedRows[row] = option;
                if (ColumnsCanMatch(row + 1)) Search(row + 1);
                if (solutions >= limit) return;
            }
        }

        bool ColumnsCanMatch(int completedRows)
        {
            var prefixMask = (1 << completedRows) - 1;
            for (var column = 0; column < size; column++)
            {
                var prefix = 0;
                for (var row = 0; row < completedRows; row++)
                    if ((selectedRows[row] & (1 << column)) != 0) prefix |= 1 << row;
                if (!columnOptions[column].Any(option => (option & prefixMask) == prefix)) return false;
            }
            return true;
        }
    }

    public static int[] Clues(IEnumerable<bool> values)
    {
        var result = new List<int>();
        var run = 0;
        foreach (var value in values)
        {
            if (value) run++;
            else if (run > 0) { result.Add(run); run = 0; }
        }
        if (run > 0) result.Add(run);
        return result.Count == 0 ? [0] : result.ToArray();
    }

    private static int[] LineOptions(int size, int[] clues)
    {
        if (clues.Length == 1 && clues[0] == 0) return [0];
        return Enumerable.Range(0, 1 << size).Where(mask =>
            Clues(Enumerable.Range(0, size).Select(index => (mask & (1 << index)) != 0)).SequenceEqual(clues)).ToArray();
    }
}
