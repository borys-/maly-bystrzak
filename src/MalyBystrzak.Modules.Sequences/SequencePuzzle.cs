using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Sequences;

public enum PictureShape { Circle, Square, Triangle, Diamond }
public sealed record PictureToken(PictureShape Shape, int Color, int Size);
public sealed record SequenceRow(string Rule, int[]? Numbers, PictureToken[]? Pictures, int[] Missing);
public sealed record SequencePuzzle(int Number, string Variant, IReadOnlyList<SequenceRow> Rows)
{
    public CognitiveDifficulty Difficulty
    {
        get
        {
            var ruleLoad = Rows.Sum(row => row.Rule switch
            {
                "arithmetic" or "repeat2" => 15, "repeat3" => 28, "alternating" => 45,
                "growing" => 58, "interleaved" or "repeat4" => 72, _ => 35
            }) / Rows.Count;
            var missing = Rows.Sum(row => row.Missing.Length) * 100 / (Rows.Count * 2);
            var arithmetic = Variant == "numbers" ? ruleLoad : 0;
            return CognitiveDifficulty.Create(Math.Clamp(12 + ruleLoad * 3 / 4 + missing / 5, 0, 100),
                missing, ruleLoad, 35, ruleLoad, arithmetic);
        }
    }
}
