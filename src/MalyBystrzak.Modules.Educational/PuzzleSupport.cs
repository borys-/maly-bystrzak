using System.Security.Cryptography;
using System.Text;
using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Educational;

internal static class PuzzleSupport
{
    public static int Tier(int index, int count) => Math.Min(6, index * 6 / Math.Max(1, count) + 1);

    public static CognitiveDifficulty Difficulty(int tier, int arithmetic, int constraints, int memory = 35) =>
        CognitiveDifficulty.Create(8 + tier * 14, 25 + tier * 10, 20 + tier * 9,
            constraints, memory, arithmetic);

    public static string Fingerprint(params object[] values) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(string.Join('|', values.Select(value => value?.ToString())))));

    public static VisualText Text(double x, double y, string value, double size = 8, bool bold = true,
        string color = "#25316d", string anchor = "middle") => new(x, y, value, size, color, bold, anchor);

    public static void AnswerBox(ICollection<VisualElement> elements, double x, double y, double width = 17,
        double height = 14, string? answer = null)
    {
        elements.Add(new VisualRectangle(x, y, width, height, "#ffffff", "#19a88e", 1));
        if (answer is not null) elements.Add(Text(x + width / 2, y + height * .68, answer, 8));
    }

    public static void Icon(ICollection<VisualElement> elements, int kind, double cx, double cy, double scale = 1)
    {
        var colors = new[] { "#f15a8a", "#19a88e", "#f39a3c", "#7058b3", "#55a9df" };
        var color = colors[Math.Abs(kind) % colors.Length];
        switch (Math.Abs(kind) % 5)
        {
            case 0: // kwiat
                for (var i = 0; i < 5; i++)
                {
                    var angle = i * Math.PI * 2 / 5 - Math.PI / 2;
                    elements.Add(new VisualEllipse(cx + Math.Cos(angle) * 5 * scale, cy + Math.Sin(angle) * 5 * scale,
                        3.5 * scale, 3.5 * scale, color, "#25316d", .35));
                }
                elements.Add(new VisualEllipse(cx, cy, 3 * scale, 3 * scale, "#ffd35a", "#25316d", .35));
                break;
            case 1: // jabłko
                elements.Add(new VisualEllipse(cx, cy + scale, 7 * scale, 6 * scale, color, "#25316d", .45));
                elements.Add(new VisualLine(cx, cy - 5 * scale, cx + scale, cy - 9 * scale, 1 * scale, "#25316d"));
                elements.Add(new VisualEllipse(cx + 4 * scale, cy - 7 * scale, 3 * scale, 1.5 * scale, "#8ac75a", "#25316d", .3));
                break;
            case 2: // rybka
                elements.Add(new VisualEllipse(cx, cy, 8 * scale, 5 * scale, color, "#25316d", .45));
                elements.Add(new VisualPolygon([new(cx - 7 * scale, cy), new(cx - 13 * scale, cy - 6 * scale),
                    new(cx - 13 * scale, cy + 6 * scale)], color, "#25316d", .45));
                elements.Add(new VisualEllipse(cx + 4 * scale, cy - scale, .8 * scale, .8 * scale, "#25316d", "none"));
                break;
            case 3: // motyl
                elements.Add(new VisualEllipse(cx - 4 * scale, cy - 2 * scale, 5 * scale, 6 * scale, color, "#25316d", .35));
                elements.Add(new VisualEllipse(cx + 4 * scale, cy - 2 * scale, 5 * scale, 6 * scale, color, "#25316d", .35));
                elements.Add(new VisualEllipse(cx, cy + scale, 1.5 * scale, 6 * scale, "#25316d", "none"));
                break;
            default: // domek
                elements.Add(new VisualRectangle(cx - 7 * scale, cy - 2 * scale, 14 * scale, 11 * scale, color, "#25316d", .45));
                elements.Add(new VisualPolygon([new(cx - 9 * scale, cy - 2 * scale), new(cx, cy - 11 * scale),
                    new(cx + 9 * scale, cy - 2 * scale)], "#ffd35a", "#25316d", .45));
                elements.Add(new VisualRectangle(cx - 2 * scale, cy + 3 * scale, 4 * scale, 6 * scale, "#ffffff", "#25316d", .3));
                break;
        }
    }

    public static Equation CreateEquation(Random random, int result, int tier)
    {
        var operations = tier switch { <= 2 => "+", <= 3 => random.Next(2) == 0 ? "+" : "−", <= 4 => "×", _ => random.Next(2) == 0 ? "×" : "÷" };
        return operations switch
        {
            "+" => MakeAddition(random, result),
            "−" => new Equation(result + random.Next(1, 10), random.Next(1, 10), "−", 0) is var draft
                ? draft with { Right = draft.Left - result, Result = result } : throw new InvalidOperationException(),
            "×" => MakeMultiplication(random, result),
            _ => new Equation(result * random.Next(2, 10), random.Next(2, 10), "÷", result) is var division
                ? division with { Right = division.Left / result } : throw new InvalidOperationException()
        };
    }

    private static Equation MakeAddition(Random random, int result)
    {
        var left = random.Next(1, result);
        return new(left, result - left, "+", result);
    }

    private static Equation MakeMultiplication(Random random, int preferred)
    {
        var factors = Enumerable.Range(2, 8).Where(value => preferred % value == 0 && preferred / value <= 12).ToArray();
        if (factors.Length == 0) return new(1, preferred, "×", preferred);
        var left = factors[random.Next(factors.Length)];
        return new(left, preferred / left, "×", preferred);
    }
}

public sealed record Equation(int Left, int Right, string Operator, int Result)
{
    public bool IsValid => Operator switch
    {
        "+" => Left + Right == Result,
        "−" => Left - Right == Result && Result >= 0,
        "×" => Left * Right == Result,
        "÷" => Right != 0 && Left % Right == 0 && Left / Right == Result,
        _ => false
    };
}
