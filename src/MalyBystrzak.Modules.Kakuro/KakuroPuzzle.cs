using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Kakuro;

public sealed record KakuroPuzzle(int Number, int Size, Difficulty Difficulty, int[] RowSums,
    int[] ColumnSums, int[] Givens, int[] Solution)
{
    public CognitiveDifficulty CognitiveDifficulty => KakuroDifficultyEstimator.Estimate(this);
    public int DifficultyStars => CognitiveDifficulty.Stars;
    public int GivenCount => Givens.Count(value => value != 0);
}
