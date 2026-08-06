using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Sudoku;

public sealed record SudokuPuzzle(int Number, int Size, int BlockRows, int BlockColumns,
    Difficulty Difficulty, int[] Cells, int[] Solution)
{
    public int ClueCount => Cells.Count(value => value != 0);
    public CognitiveDifficulty CognitiveDifficulty => SudokuDifficultyEstimator.Estimate(this);
    public int DifficultyStars => CognitiveDifficulty.Stars;
    public int this[int row, int column] => Cells[row * Size + column];
}
