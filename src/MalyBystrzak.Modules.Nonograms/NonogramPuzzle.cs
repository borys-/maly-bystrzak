using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Nonograms;

public sealed record NonogramPuzzle(int Number, int Size, bool[] Cells, int[][] RowClues, int[][] ColumnClues, int SolverBranches)
{
    public CognitiveDifficulty Difficulty
    {
        get
        {
            var groups = RowClues.Concat(ColumnClues).Sum(clues => clues.Length);
            var density = Cells.Count(value => value) * 100 / Cells.Length;
            var multiGroups = RowClues.Concat(ColumnClues).Count(clues => clues.Length > 1) * 100 / (Size * 2);
            var score = (Size - 5) * 9 + groups * 55 / (Size * 2) + multiGroups / 4 +
                Math.Min(20, SolverBranches / 4);
            return CognitiveDifficulty.Create(score, 65, multiGroups, groups * 50 / Size,
                Math.Abs(50 - density) * 2, 0);
        }
    }
}
