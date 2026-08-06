using System.Text;

namespace MalyBystrzak.Core;

public static class DifficultyReport
{
    public const string FileName = "raport-trudnosci.csv";

    public static byte[] Create(IReadOnlyList<GeneratedWorksheet> worksheets)
    {
        var content = new StringBuilder();
        content.AppendLine("Numer;Typ;Wynik_0_100;Wynik_techniczny;Gwiazdki;Luka_informacyjna;Obciazenie_wyboru;Obciazenie_wiezow;Pamiec_robocza;Arytmetyka;Opis;Czas_sekundy;Bledy;Podpowiedzi;Wysilek_1_5;Ukonczone_TAK_NIE");
        foreach (var worksheet in worksheets)
        {
            var value = worksheet.Difficulty;
            content.AppendLine(string.Join(';', worksheet.Number, worksheet.TypeName, value.Score, value.RawScore,
                worksheet.DisplayStars, value.InformationGap, value.ChoiceLoad, value.ConstraintLoad,
                value.WorkingMemoryLoad, value.ArithmeticLoad, value.Label, "", "", "", "", ""));
        }
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(content.ToString());
    }
}
