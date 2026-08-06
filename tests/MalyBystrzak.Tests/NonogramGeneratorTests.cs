using MalyBystrzak.Core;
using MalyBystrzak.Modules.Nonograms;

namespace MalyBystrzak.Tests;

public class NonogramGeneratorTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(10)]
    public void CluesMatchCellsAndHaveUniqueSolution(int size)
    {
        var puzzles = new NonogramGenerator(9876).GenerateBook(8, size);
        Assert.Equal(8, puzzles.Select(Fingerprint).Distinct().Count());
        foreach (var puzzle in puzzles)
        {
            for (var row = 0; row < size; row++)
                Assert.Equal(puzzle.RowClues[row], NonogramSolver.Clues(
                    Enumerable.Range(0, size).Select(column => puzzle.Cells[row * size + column])));
            for (var column = 0; column < size; column++)
                Assert.Equal(puzzle.ColumnClues[column], NonogramSolver.Clues(
                    Enumerable.Range(0, size).Select(row => puzzle.Cells[row * size + column])));
            Assert.Equal(1, NonogramSolver.CountSolutions(size, puzzle.RowClues, puzzle.ColumnClues));
        }
    }

    [Fact]
    public void SameSeedProducesSameNonograms()
    {
        var first = new NonogramGenerator(55).GenerateBook(10, 7);
        var second = new NonogramGenerator(55).GenerateBook(10, 7);
        Assert.Equal(first.Select(Fingerprint), second.Select(Fingerprint));
    }

    [Fact]
    public void ModuleSupportsMaximumBookSize()
    {
        var worksheets = new NonogramModule().Generate(new ModuleGenerationRequest("5x5", 180, 20260806));
        Assert.Equal(180, worksheets.Count);
        Assert.Equal(180, worksheets.Select(item => item.Fingerprint).Distinct().Count());
    }

    [Fact]
    public void ModuleCreatesReadableTaskAndFilledSolution()
    {
        var worksheets = new NonogramModule().Generate(new ModuleGenerationRequest("10x10", 4, 31));
        Assert.All(worksheets, worksheet =>
        {
            Assert.Contains(worksheet.Task.Elements, element => element is VisualText);
            Assert.Contains(worksheet.Task.Elements.OfType<VisualRectangle>(), rectangle => rectangle.Fill == "#f4f7ff");
            Assert.Contains(worksheet.Task.Elements.OfType<VisualLine>(), line => line.Width >= 1);
            Assert.Contains(worksheet.Solution.Elements.OfType<VisualRectangle>(), rectangle => rectangle.Fill == "#25316d");
            Assert.Equal("Nonogram", worksheet.Instruction.Title);
        });
    }

    private static string Fingerprint(NonogramPuzzle puzzle) => string.Concat(puzzle.Cells.Select(value => value ? '1' : '0'));
}
