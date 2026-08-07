using MalyBystrzak.Core;
using MalyBystrzak.Modules.Educational;

namespace MalyBystrzak.Tests;

public class EducationalPuzzleTests
{
    public static TheoryData<IWorksheetModule, string, WorksheetLayout> Modules => new()
    {
        { new PictureEquationsModule(), "animals", WorksheetLayout.HalfPage },
        { new ArithmeticCodeModule(), "six-letter", WorksheetLayout.HalfPage },
        { new MathCrosswordModule(), "chain", WorksheetLayout.FullPage },
        { new ProductGridModule(), "3x3", WorksheetLayout.FullPage },
        { new WordPathModule(), "5x4", WorksheetLayout.HalfPage }
    };

    [Theory, MemberData(nameof(Modules))]
    public void ModulesAreDeterministicAndCreateDistinctWorksheets(IWorksheetModule module, string variant,
        WorksheetLayout layout)
    {
        var first = module.Generate(new(variant, 12, 20260807));
        var second = module.Generate(new(variant, 12, 20260807));

        Assert.Equal(first.Select(item => item.Fingerprint), second.Select(item => item.Fingerprint));
        Assert.Equal(12, first.Select(item => item.Fingerprint).Distinct().Count());
        Assert.All(first, item =>
        {
            Assert.Equal(layout, item.Layout);
            Assert.NotEmpty(item.Task.Elements);
            Assert.NotEmpty(item.Solution.Elements);
            Assert.InRange(item.Difficulty.Score, 0, 100);
        });
    }

    [Fact]
    public void PublicPuzzleValidatorsConfirmGeneratedData()
    {
        var picture = new PictureEquationsPuzzle(1, 1, [2, 3, 4], [0, 1, 2], 6,
            CognitiveDifficulty.Create(20, 20, 20, 20, 20, 20));
        Assert.Equal(1, picture.CountSolutions());

        var crossword = new MathCrosswordPuzzle(1, 1, [new(2, 3, "+", 5), new(5, 2, "×", 10)],
            CognitiveDifficulty.Create(20, 20, 20, 20, 20, 20));
        Assert.Equal(1, crossword.CountSolutions());

        var product = new ProductGridPuzzle(1, 1, [1, 2, 3, 4, 5], [0, 1, 2, 3, 4],
            [0, 0, 0, 1, 1, 1, 2, 3, 4], [1, 8, 60], [6, 8, 10],
            CognitiveDifficulty.Create(20, 20, 20, 20, 20, 20));
        Assert.Equal(1, product.CountSolutions());

        var grid = "PLANEXABCTKLMNOPQRST".ToCharArray();
        grid[9] = 'T';
        var path = new WordPathPuzzle(1, 1, "PLANET", grid,
            [new(0, 0), new(0, 1), new(0, 2), new(0, 3), new(0, 4), new(1, 4)],
            CognitiveDifficulty.Create(20, 20, 20, 20, 20, 20));
        Assert.True(path.IsValid);
    }

    [Fact]
    public void ArithmeticCodeUsesValidOperationsAndDistinctResults()
    {
        var worksheets = new ArithmeticCodeModule().Generate(new("six-letter", 36, 13));
        Assert.Equal(36, worksheets.Count);
        Assert.All(worksheets, item => Assert.Equal(WorksheetLayout.HalfPage, item.Layout));
    }

    [Fact]
    public void WordPathAvoidsRepeatedWordsUntilDictionaryIsExhausted()
    {
        const int dictionarySize = 48;
        var worksheets = new WordPathModule().Generate(new("5x4", dictionarySize, 20260808));
        var answers = worksheets.Select(item => item.Solution.Elements.OfType<VisualText>()
            .Single(text => text.Size >= 7 && text.Text.Length is >= 6 and <= 9 && text.Text.All(char.IsUpper)).Text).ToArray();

        Assert.Equal(dictionarySize, answers.Length);
        Assert.Equal(answers.Length, answers.Distinct().Count());
    }
}
