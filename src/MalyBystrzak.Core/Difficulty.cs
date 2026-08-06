namespace MalyBystrzak.Core;

public enum Difficulty
{
    Level1 = 1, Level2 = 2, Level3 = 3, Level4 = 4, Level5 = 5, Level6 = 6
}

public sealed record CognitiveDifficulty(
    int RawScore, int Score, int Stars, int InformationGap, int ChoiceLoad, int ConstraintLoad,
    int WorkingMemoryLoad, int ArithmeticLoad, string Label)
{
    public static CognitiveDifficulty Create(int score, int informationGap, int choiceLoad,
        int constraintLoad, int workingMemoryLoad, int arithmeticLoad)
    {
        var rawScore = score;
        score = Math.Clamp(rawScore, 0, 100);
        var label = LabelFor(score);
        return new(rawScore, score, Math.Clamp(score / 17 + 1, 1, 6), Math.Clamp(informationGap, 0, 100),
            Math.Clamp(choiceLoad, 0, 100), Math.Clamp(constraintLoad, 0, 100),
            Math.Clamp(workingMemoryLoad, 0, 100), Math.Clamp(arithmeticLoad, 0, 100), label);
    }

    public CognitiveDifficulty WithScore(int score)
    {
        score = Math.Clamp(score, 0, 100);
        return this with { Score = score, Stars = Math.Clamp(score / 17 + 1, 1, 6), Label = LabelFor(score) };
    }

    private static string LabelFor(int score) => score switch
        {
            <= 16 => "bardzo lekkie", <= 33 => "lekkie", <= 49 => "umiarkowane",
            <= 66 => "wymagające", <= 83 => "trudne", _ => "bardzo trudne"
        };
}
