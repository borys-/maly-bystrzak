using MalyBystrzak.Core;
using MalyBystrzak.Modules.Mazes;

namespace MalyBystrzak.Tests;

public class MazeGeneratorTests
{
    [Fact]
    public void LargeMazeRequestsLargeLayout()
    {
        var worksheet = Assert.Single(new MazeModule().Generate(new("15x15", 1, 42)));
        Assert.Equal(WorksheetLayout.Large, worksheet.Layout);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(15)]
    public void GeneratedMazesHaveValidUniquePath(int size)
    {
        var puzzles = new MazeGenerator(12345).GenerateBook(12, size);
        Assert.Equal(12, puzzles.Select(Fingerprint).Distinct().Count());
        foreach (var puzzle in puzzles)
        {
            Assert.Equal(puzzle.Entrance, puzzle.Solution[0]);
            Assert.Equal(puzzle.Exit, puzzle.Solution[^1]);
            for (var index = 1; index < puzzle.Solution.Length; index++)
                Assert.True(AreConnected(puzzle, puzzle.Solution[index - 1], puzzle.Solution[index]));
            Assert.InRange(puzzle.Difficulty.Score, 0, 100);
        }
    }

    [Fact]
    public void SameSeedProducesSameMazes()
    {
        var first = new MazeGenerator(77).GenerateBook(8, 9);
        var second = new MazeGenerator(77).GenerateBook(8, 9);
        Assert.Equal(first.Select(Fingerprint), second.Select(Fingerprint));
    }

    [Fact]
    public void ModuleCreatesTaskAndSolutionVisuals()
    {
        var worksheets = new MazeModule().Generate(new ModuleGenerationRequest("9x9", 4, 91));
        Assert.All(worksheets, worksheet =>
        {
            Assert.Contains(worksheet.Task.Elements, element => element is VisualEllipse);
            Assert.True(worksheet.Solution.Elements.Count > worksheet.Task.Elements.Count);
            Assert.Equal("Labirynt", worksheet.Instruction.Title);
        });
    }

    private static bool AreConnected(MazePuzzle puzzle, int first, int second)
    {
        var difference = second - first;
        var direction = difference switch { var value when value == -puzzle.Size => 0, 1 => 1,
            var value when value == puzzle.Size => 2, -1 => 3, _ => -1 };
        return direction >= 0 && !puzzle.HasWall(first, direction);
    }

    private static string Fingerprint(MazePuzzle puzzle) =>
        $"{puzzle.Entrance}:{puzzle.Exit}:{string.Join(',', puzzle.Walls)}";
}
