using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Sudoku;

public static class SudokuSolver
{
    public static int CountSolutions(int[] cells, int size, int blockRows, int blockColumns, int limit = 2)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Length != size * size)
            throw new ArgumentException("Nieprawidłowa liczba pól planszy.", nameof(cells));

        var board = (int[])cells.Clone();
        var solutions = 0;
        Solve(board, size, blockRows, blockColumns, limit, ref solutions);
        return solutions;
    }

    public static bool IsValidComplete(int[] cells, int size, int blockRows, int blockColumns)
    {
        if (cells.Length != size * size || cells.Any(value => value < 1 || value > size))
            return false;

        for (var row = 0; row < size; row++)
            if (!HasAllValues(Enumerable.Range(0, size).Select(column => cells[row * size + column]), size))
                return false;

        for (var column = 0; column < size; column++)
            if (!HasAllValues(Enumerable.Range(0, size).Select(row => cells[row * size + column]), size))
                return false;

        for (var top = 0; top < size; top += blockRows)
        for (var left = 0; left < size; left += blockColumns)
        {
            var values = new List<int>();
            for (var row = top; row < top + blockRows; row++)
            for (var column = left; column < left + blockColumns; column++)
                values.Add(cells[row * size + column]);
            if (!HasAllValues(values, size))
                return false;
        }

        return true;
    }

    internal static bool CanPlace(int[] board, int index, int value, int size, int blockRows, int blockColumns)
    {
        var row = index / size;
        var column = index % size;

        for (var i = 0; i < size; i++)
            if (board[row * size + i] == value || board[i * size + column] == value)
                return false;

        var blockTop = row / blockRows * blockRows;
        var blockLeft = column / blockColumns * blockColumns;
        for (var r = blockTop; r < blockTop + blockRows; r++)
        for (var c = blockLeft; c < blockLeft + blockColumns; c++)
            if (board[r * size + c] == value)
                return false;

        return true;
    }

    private static void Solve(int[] board, int size, int blockRows, int blockColumns, int limit, ref int solutions)
    {
        if (solutions >= limit)
            return;

        var bestIndex = -1;
        List<int>? bestCandidates = null;
        for (var index = 0; index < board.Length; index++)
        {
            if (board[index] != 0)
                continue;

            var candidates = Enumerable.Range(1, size)
                .Where(value => CanPlace(board, index, value, size, blockRows, blockColumns))
                .ToList();
            if (candidates.Count == 0)
                return;
            if (bestCandidates is null || candidates.Count < bestCandidates.Count)
            {
                bestIndex = index;
                bestCandidates = candidates;
                if (candidates.Count == 1)
                    break;
            }
        }

        if (bestIndex < 0)
        {
            solutions++;
            return;
        }

        foreach (var value in bestCandidates!)
        {
            board[bestIndex] = value;
            Solve(board, size, blockRows, blockColumns, limit, ref solutions);
            board[bestIndex] = 0;
            if (solutions >= limit)
                return;
        }
    }

    private static bool HasAllValues(IEnumerable<int> values, int size) =>
        values.OrderBy(value => value).SequenceEqual(Enumerable.Range(1, size));
}



