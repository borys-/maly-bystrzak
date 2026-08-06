using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Kakuro;

public static class KakuroDifficultyEstimator
{
    public static CognitiveDifficulty Estimate(KakuroPuzzle puzzle)
    {
        var empty = puzzle.Givens.Count(value => value == 0);
        var informationGap = Percent(empty, puzzle.Givens.Length);
        var candidateCounts = Enumerable.Range(0, puzzle.Givens.Length).Where(index => puzzle.Givens[index] == 0)
            .Select(index => Enumerable.Range(1, 9).Count(value => KakuroSolver.CanPlace(
                puzzle.Givens, puzzle.Size, index, value, puzzle.RowSums, puzzle.ColumnSums))).ToArray();
        var choiceLoad = candidateCounts.Length == 0 ? 0 : Clamp((candidateCounts.Average() - 1) / 8d * 100);
        var allSums = puzzle.RowSums.Concat(puzzle.ColumnSums).ToArray();
        var combinations = allSums.Select(sum => CountCombinations(sum, puzzle.Size)).ToArray();
        var constraintLoad = Clamp(combinations.Average() / Math.Max(1, MaxCombinations(puzzle.Size)) * 100);
        var maximumSum = Enumerable.Range(10 - puzzle.Size, puzzle.Size).Sum();
        var arithmetic = Clamp(allSums.Average() / maximumSum * 100);
        var normalizedGap = Math.Clamp((empty - 1) / 5d, 0, 1);
        var score = Clamp(5 + normalizedGap * 72 + choiceLoad * .12 + constraintLoad * .06 +
            arithmetic * .05 + (puzzle.Size == 4 ? 4 : 0));
        return CognitiveDifficulty.Create(score, informationGap, choiceLoad, constraintLoad,
            puzzle.Size == 4 ? 65 : 45, arithmetic);
    }

    private static int CountCombinations(int target, int size)
    {
        var count = 0;
        Count(1, size, target);
        return count;
        void Count(int next, int remaining, int sum)
        {
            if (remaining == 0) { if (sum == 0) count++; return; }
            for (var value = next; value <= 9; value++) Count(value + 1, remaining - 1, sum - value);
        }
    }

    private static int MaxCombinations(int size) => Enumerable.Range(size * (size + 1) / 2,
        Enumerable.Range(10 - size, size).Sum() - size * (size + 1) / 2 + 1).Max(sum => CountCombinations(sum, size));
    private static int Percent(double value, double total) => Clamp(value * 100 / total);
    private static int Clamp(double value) => Math.Clamp((int)Math.Round(value), 0, 100);
}
