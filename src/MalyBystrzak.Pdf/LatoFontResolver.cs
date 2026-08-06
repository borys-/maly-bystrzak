using System.Reflection;
using PdfSharp.Fonts;

namespace MalyBystrzak.Pdf;

internal sealed class LatoFontResolver : IFontResolver
{
    private const string Regular = "LatoRegular";
    private const string Bold = "LatoBold";

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? Bold : Regular, mustSimulateBold: false, mustSimulateItalic: isItalic);

    public byte[] GetFont(string faceName)
    {
        var resource = faceName == Bold
            ? "MalyBystrzak.Pdf.Fonts.Lato-Bold.ttf"
            : "MalyBystrzak.Pdf.Fonts.Lato-Regular.ttf";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Brak osadzonego fontu: {resource}.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
