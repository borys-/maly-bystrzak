using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Sudoku;

public sealed class SudokuGenerator
{
    private readonly Random random;

    public SudokuGenerator(int seed) => random = new Random(seed);

    public IReadOnlyList<SudokuPuzzle> GenerateBook(int count, int size)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Liczba zadań musi być większa od zera.");

        var (blockRows, blockColumns) = GetBlockShape(size);
        var puzzles = new List<SudokuPuzzle>(count);
        for (var index = 0; index < count; index++)
        {
            var difficulty = GetDifficulty(index, count);
            puzzles.Add(Generate(index + 1, size, blockRows, blockColumns, difficulty));
        }
        return puzzles;
    }

    public static Difficulty GetDifficulty(int zeroBasedIndex, int total)
    {
        if (zeroBasedIndex < 0 || zeroBasedIndex >= total || total <= 0)
            throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));

        var tier = zeroBasedIndex * 6 / total;
        return (Difficulty)(tier + 1);
    }

    public static (int BlockRows, int BlockColumns) GetBlockShape(int size) => size switch
    {
        4 => (2, 2),
        6 => (2, 3),
        _ => throw new ArgumentOutOfRangeException(nameof(size), "Obsługiwane rozmiary to 4 i 6.")
    };

    private SudokuPuzzle Generate(int number, int size, int blockRows, int blockColumns, Difficulty difficulty)
    {
        var target = GetTargetClues(size, difficulty);
        for (var attempt = 0; attempt < 250; attempt++)
        {
            var solution = GenerateSolution(size, blockRows, blockColumns);
            var puzzle = (int[])solution.Clone();
            var positions = Enumerable.Range(0, puzzle.Length).OrderBy(_ => random.Next()).ToArray();

            foreach (var position in positions)
            {
                if (puzzle.Count(value => value != 0) <= target)
                    break;
                var previous = puzzle[position];
                puzzle[position] = 0;
                if (SudokuSolver.CountSolutions(puzzle, size, blockRows, blockColumns) != 1)
                    puzzle[position] = previous;
            }

            if (puzzle.Count(value => value != 0) == target)
                return new SudokuPuzzle(number, size, blockRows, blockColumns, difficulty, puzzle, solution);
        }

        throw new InvalidOperationException($"Nie udało się wygenerować planszy {size}x{size} na poziomie {difficulty}.");
    }

    private int[] GenerateSolution(int size, int blockRows, int blockColumns)
    {
        var board = new int[size * size];
        if (!Fill(board, size, blockRows, blockColumns))
            throw new InvalidOperationException("Nie udało się utworzyć pełnej planszy.");
        return board;
    }

    private bool Fill(int[] board, int size, int blockRows, int blockColumns)
    {
        var index = Array.IndexOf(board, 0);
        if (index < 0)
            return true;

        foreach (var value in Enumerable.Range(1, size).OrderBy(_ => random.Next()))
        {
            if (!SudokuSolver.CanPlace(board, index, value, size, blockRows, blockColumns))
                continue;
            board[index] = value;
            if (Fill(board, size, blockRows, blockColumns))
                return true;
            board[index] = 0;
        }
        return false;
    }

    private static int GetTargetClues(int size, Difficulty difficulty) => (size, difficulty) switch
    {
        (4, Difficulty.Level1) => 11,
        (4, Difficulty.Level2) => 10,
        (4, Difficulty.Level3) => 9,
        (4, Difficulty.Level4) => 8,
        (4, Difficulty.Level5) => 7,
        (4, Difficulty.Level6) => 6,
        (6, Difficulty.Level1) => 26,
        (6, Difficulty.Level2) => 24,
        (6, Difficulty.Level3) => 22,
        (6, Difficulty.Level4) => 20,
        (6, Difficulty.Level5) => 18,
        (6, Difficulty.Level6) => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
}



