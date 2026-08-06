using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Kakuro;

public static class KakuroSolver
{
    public static int CountSolutions(KakuroPuzzle puzzle, int limit = 2)
    {
        var board = (int[])puzzle.Givens.Clone();
        var count = 0;
        Solve(board, puzzle.Size, puzzle.RowSums, puzzle.ColumnSums, limit, ref count);
        return count;
    }

    public static bool IsValidSolution(KakuroPuzzle puzzle)
    {
        var board = puzzle.Solution;
        if (board.Length != puzzle.Size * puzzle.Size || board.Any(value => value is < 1 or > 9))
            return false;

        for (var index = 0; index < puzzle.Size; index++)
        {
            var row = Enumerable.Range(0, puzzle.Size).Select(column => board[index * puzzle.Size + column]).ToArray();
            var column = Enumerable.Range(0, puzzle.Size).Select(rowIndex => board[rowIndex * puzzle.Size + index]).ToArray();
            if (row.Distinct().Count() != puzzle.Size || row.Sum() != puzzle.RowSums[index] ||
                column.Distinct().Count() != puzzle.Size || column.Sum() != puzzle.ColumnSums[index])
                return false;
        }
        return true;
    }

    private static void Solve(int[] board, int size, int[] rowSums, int[] columnSums, int limit, ref int count)
    {
        if (count >= limit)
            return;

        var bestIndex = -1;
        List<int>? best = null;
        for (var index = 0; index < board.Length; index++)
        {
            if (board[index] != 0)
                continue;
            var candidates = Enumerable.Range(1, 9)
                .Where(value => CanPlace(board, size, index, value, rowSums, columnSums))
                .ToList();
            if (candidates.Count == 0)
                return;
            if (best is null || candidates.Count < best.Count)
            {
                bestIndex = index;
                best = candidates;
            }
        }

        if (bestIndex < 0)
        {
            count++;
            return;
        }

        foreach (var value in best!)
        {
            board[bestIndex] = value;
            Solve(board, size, rowSums, columnSums, limit, ref count);
            board[bestIndex] = 0;
            if (count >= limit)
                return;
        }
    }

    internal static bool CanPlace(int[] board, int size, int index, int value, int[] rowSums, int[] columnSums)
    {
        var row = index / size;
        var column = index % size;
        for (var i = 0; i < size; i++)
            if (board[row * size + i] == value || board[i * size + column] == value)
                return false;

        board[index] = value;
        var rowOk = RunCanReach(board, row * size, 1, size, rowSums[row]);
        var columnOk = RunCanReach(board, column, size, size, columnSums[column]);
        board[index] = 0;
        return rowOk && columnOk;
    }

    private static bool RunCanReach(int[] board, int start, int step, int size, int target)
    {
        var values = Enumerable.Range(0, size).Select(i => board[start + i * step]).ToArray();
        var sum = values.Sum();
        var empty = values.Count(value => value == 0);
        if (empty == 0)
            return sum == target;
        if (sum >= target)
            return false;

        var available = Enumerable.Range(1, 9).Except(values.Where(value => value != 0)).ToArray();
        return sum + available.Order().Take(empty).Sum() <= target &&
               sum + available.OrderDescending().Take(empty).Sum() >= target;
    }
}



