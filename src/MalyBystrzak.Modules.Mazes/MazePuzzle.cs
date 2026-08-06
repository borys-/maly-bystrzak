using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Mazes;

public sealed record MazePuzzle(int Number, int Size, bool[] Walls, int Entrance, int Exit, int[] Solution)
{
    public bool HasWall(int cell, int direction) => Walls[cell * 4 + direction];

    public CognitiveDifficulty Difficulty
    {
        get
        {
            var deadEnds = Enumerable.Range(0, Size * Size).Count(cell => Openings(cell) == 1);
            var junctions = Enumerable.Range(0, Size * Size).Count(cell => Openings(cell) >= 3);
            var pathLoad = Solution.Length * 100 / (Size * Size);
            var score = Math.Clamp((Size == 15 ? 28 : 8) + pathLoad / 2 + deadEnds * 80 / (Size * Size) +
                junctions * 60 / (Size * Size), 0, 100);
            return CognitiveDifficulty.Create(score, 75, junctions * 100 / (Size * Size),
                deadEnds * 100 / (Size * Size), pathLoad, 0);
        }
    }

    private int Openings(int cell) => Enumerable.Range(0, 4).Count(direction => !HasWall(cell, direction));
}
