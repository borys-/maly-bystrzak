namespace MalyBystrzak.Core;

public enum Difficulty
{
    Level1 = 1, Level2 = 2, Level3 = 3, Level4 = 4, Level5 = 5, Level6 = 6
}

public sealed record CognitiveDifficulty(
    int Score, int Stars, int InformationGap, int ChoiceLoad, int ConstraintLoad,
    int WorkingMemoryLoad, int ArithmeticLoad, string Label)
{
    public static CognitiveDifficulty Create(int score, int informationGap, int choiceLoad,
        int constraintLoad, int workingMemoryLoad, int arithmeticLoad)
    {
        score = Math.Clamp(score, 0, 100);
        var label = score switch
        {
            <= 16 => "bardzo lekkie", <= 33 => "lekkie", <= 49 => "umiarkowane",
            <= 66 => "wymagające", <= 83 => "trudne", _ => "bardzo trudne"
        };
        return new(score, Math.Clamp(score / 17 + 1, 1, 6), Math.Clamp(informationGap, 0, 100),
            Math.Clamp(choiceLoad, 0, 100), Math.Clamp(constraintLoad, 0, 100),
            Math.Clamp(workingMemoryLoad, 0, 100), Math.Clamp(arithmeticLoad, 0, 100), label);
    }
}
