namespace MalyBystrzak.Modules.Mazes;

public sealed class MazeGenerator(int seed)
{
    private readonly Random random = new(seed);
    private static readonly (int Row, int Column)[] Directions = [(-1, 0), (0, 1), (1, 0), (0, -1)];
    private static readonly int[] Opposite = [2, 3, 0, 1];

    public IReadOnlyList<MazePuzzle> GenerateBook(int count, int size, CancellationToken cancellationToken = default)
    {
        if (size is not 9 and not 15) throw new ArgumentOutOfRangeException(nameof(size));
        var result = new List<MazePuzzle>(count);
        var fingerprints = new HashSet<string>();
        while (result.Count < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var puzzle = Generate(result.Count + 1, size);
            if (fingerprints.Add(string.Join(',', puzzle.Walls.Select(value => value ? '1' : '0')))) result.Add(puzzle);
        }
        return result;
    }

    private MazePuzzle Generate(int number, int size)
    {
        var walls = Enumerable.Repeat(true, size * size * 4).ToArray();
        var visited = new bool[size * size];
        var entrance = random.Next(size) * size;
        var stack = new Stack<int>();
        stack.Push(entrance);
        visited[entrance] = true;
        while (stack.Count > 0)
        {
            var cell = stack.Peek();
            var row = cell / size;
            var column = cell % size;
            var available = Enumerable.Range(0, 4).Where(direction =>
            {
                var nextRow = row + Directions[direction].Row;
                var nextColumn = column + Directions[direction].Column;
                return nextRow >= 0 && nextRow < size && nextColumn >= 0 && nextColumn < size && !visited[nextRow * size + nextColumn];
            }).OrderBy(_ => random.Next()).ToArray();
            if (available.Length == 0) { stack.Pop(); continue; }
            var selected = available[0];
            var next = (row + Directions[selected].Row) * size + column + Directions[selected].Column;
            walls[cell * 4 + selected] = false;
            walls[next * 4 + Opposite[selected]] = false;
            visited[next] = true;
            stack.Push(next);
        }

        var distances = Distances(entrance, size, walls, out _);
        var exit = Enumerable.Range(0, size).Select(row => row * size + size - 1).MaxBy(cell => distances[cell]);
        Distances(entrance, size, walls, out var parents);
        var path = new List<int>();
        for (var cell = exit; cell >= 0; cell = parents[cell])
        {
            path.Add(cell);
            if (cell == entrance) break;
        }
        path.Reverse();
        walls[entrance * 4 + 3] = false;
        walls[exit * 4 + 1] = false;
        return new(number, size, walls, entrance, exit, path.ToArray());
    }

    private static int[] Distances(int start, int size, bool[] walls, out int[] parents)
    {
        var distances = Enumerable.Repeat(-1, size * size).ToArray();
        parents = Enumerable.Repeat(-1, size * size).ToArray();
        var queue = new Queue<int>();
        distances[start] = 0;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            var row = cell / size;
            var column = cell % size;
            for (var direction = 0; direction < 4; direction++)
            {
                if (walls[cell * 4 + direction]) continue;
                var nextRow = row + Directions[direction].Row;
                var nextColumn = column + Directions[direction].Column;
                if (nextRow < 0 || nextRow >= size || nextColumn < 0 || nextColumn >= size) continue;
                var next = nextRow * size + nextColumn;
                if (distances[next] >= 0) continue;
                distances[next] = distances[cell] + 1;
                parents[next] = cell;
                queue.Enqueue(next);
            }
        }
        return distances;
    }
}
