using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Sudoku;

public static class SudokuDifficultyEstimator
{
    public static CognitiveDifficulty Estimate(SudokuPuzzle puzzle)
    {
        var empty = puzzle.Cells.Count(value => value == 0);
        var informationGap = Percent(empty, puzzle.Cells.Length);
        var candidateCounts = Enumerable.Range(0, puzzle.Cells.Length).Where(index => puzzle.Cells[index] == 0)
            .Select(index => Enumerable.Range(1, puzzle.Size).Count(value => SudokuSolver.CanPlace(
                puzzle.Cells, index, value, puzzle.Size, puzzle.BlockRows, puzzle.BlockColumns))).ToArray();
        var averageCandidates = candidateCounts.Length == 0 ? 1 : candidateCounts.Average();
        var choiceLoad = Percent(averageCandidates - 1, Math.Max(1, puzzle.Size - 1));
        var constraintLoad = ConstraintLoad(puzzle);
        var normalizedGap = Normalize(empty, puzzle.Size == 6 ? 10 : 5, puzzle.Size == 6 ? 20 : 10);
        var score = Clamp(5 + normalizedGap * 78 + choiceLoad * .10 + constraintLoad * .07 + (puzzle.Size == 6 ? 3 : 0));
        return CognitiveDifficulty.Create(score, informationGap, choiceLoad, constraintLoad, puzzle.Size == 6 ? 70 : 35, 0);
    }

    private static int ConstraintLoad(SudokuPuzzle puzzle)
    {
        var board = (int[])puzzle.Cells.Clone();
        var initialEmpty = board.Count(value => value == 0);
        if (initialEmpty == 0) return 0;
        var progress = true;
        while (progress)
        {
            progress = false;
            for (var index = 0; index < board.Length; index++)
            {
                if (board[index] != 0) continue;
                var candidates = Enumerable.Range(1, puzzle.Size).Where(value => SudokuSolver.CanPlace(
                    board, index, value, puzzle.Size, puzzle.BlockRows, puzzle.BlockColumns)).ToArray();
                if (candidates.Length != 1) continue;
                board[index] = candidates[0];
                progress = true;
            }
        }
        return Percent(board.Count(value => value == 0), initialEmpty);
    }

    private static double Normalize(int value, int min, int max) => Math.Clamp((value - min) / (double)(max - min), 0, 1);
    private static int Percent(double value, double total) => Clamp(value * 100 / total);
    private static int Clamp(double value) => Math.Clamp((int)Math.Round(value), 0, 100);
}
