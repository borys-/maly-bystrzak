using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Kakuro;

public sealed class KakuroGenerator
{
    private readonly Random random;

    public KakuroGenerator(int seed) => random = new Random(seed);

    public IReadOnlyList<KakuroPuzzle> GenerateBook(int count, int size = 3)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (size is not 3 and not 4)
            throw new ArgumentOutOfRangeException(nameof(size), "Kakuro obsługuje rozmiar 3 albo 4.");
        return Enumerable.Range(0, count)
            .Select(index => Generate(index + 1, size, DifficultyForIndex(index, count)))
            .ToArray();
    }

    private static Difficulty DifficultyForIndex(int index, int total) => (Difficulty)(index * 6 / total + 1);

    private KakuroPuzzle Generate(int number, int size, Difficulty difficulty)
    {
        var targetGivens = size * size - (int)difficulty;
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var solution = GenerateSolution(size);
            var rowSums = Enumerable.Range(0, size).Select(row => solution.Skip(row * size).Take(size).Sum()).ToArray();
            var columnSums = Enumerable.Range(0, size).Select(column => Enumerable.Range(0, size).Sum(row => solution[row * size + column])).ToArray();
            var givens = (int[])solution.Clone();

            foreach (var position in Enumerable.Range(0, solution.Length).OrderBy(_ => random.Next()))
            {
                if (givens.Count(value => value != 0) <= targetGivens)
                    break;
                var previous = givens[position];
                givens[position] = 0;
                var candidate = new KakuroPuzzle(number, size, difficulty, rowSums, columnSums, givens, solution);
                if (KakuroSolver.CountSolutions(candidate) != 1)
                    givens[position] = previous;
            }

            if (givens.Count(value => value != 0) == targetGivens)
                return new KakuroPuzzle(number, size, difficulty, rowSums, columnSums, givens, solution);
        }
        throw new InvalidOperationException($"Nie udało się wygenerować Kakuro nr {number}.");
    }

    private int[] GenerateSolution(int size)
    {
        var board = new int[size * size];
        if (!Fill(board, size))
            throw new InvalidOperationException("Nie udało się utworzyć planszy Kakuro.");
        return board;
    }

    private bool Fill(int[] board, int size)
    {
        var index = Array.IndexOf(board, 0);
        if (index < 0)
            return true;
        var row = index / size;
        var column = index % size;
        foreach (var value in Enumerable.Range(1, 9).OrderBy(_ => random.Next()))
        {
            if (Enumerable.Range(0, size).Any(i => board[row * size + i] == value || board[i * size + column] == value))
                continue;
            board[index] = value;
            if (Fill(board, size))
                return true;
            board[index] = 0;
        }
        return false;
    }
}



