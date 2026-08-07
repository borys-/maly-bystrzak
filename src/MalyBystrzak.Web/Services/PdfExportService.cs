using Microsoft.AspNetCore.Components.WebAssembly.Services;
using MalyBystrzak.Core;

namespace MalyBystrzak.Web.Services;

public sealed class PdfExportService(LazyAssemblyLoader assemblyLoader)
{
    private Task<IBookPdfRenderer>? rendererTask;

    public async ValueTask<byte[]> RenderAsync(BookDocument document, bool booklet)
    {
        var renderer = await GetRendererAsync();
        return booklet ? renderer.RenderBooklet(document) : renderer.RenderPreview(document);
    }

    public async Task WarmUpAsync()
    {
        try
        {
            await Task.Delay(1200);
            await GetRendererAsync();
        }
        catch
        {
            rendererTask = null;
        }
    }

    private Task<IBookPdfRenderer> GetRendererAsync() => rendererTask ??= CreateRendererAsync();

    private async Task<IBookPdfRenderer> CreateRendererAsync()
    {
        var assemblies = await assemblyLoader.LoadAssembliesAsync([
            "PdfSharp.wasm", "PdfSharp.BarCodes.wasm", "PdfSharp.Charting.wasm", "PdfSharp.Cryptography.wasm",
            "PdfSharp.Quality.wasm", "PdfSharp.Shared.wasm", "PdfSharp.Snippets.wasm", "PdfSharp.System.wasm",
            "PdfSharp.WPFonts.wasm", "MalyBystrzak.Pdf.wasm"
        ]);
        var rendererType = assemblies.Select(assembly => assembly.GetType("MalyBystrzak.Pdf.BookPdfRenderer"))
            .FirstOrDefault(type => type is not null)
            ?? throw new InvalidOperationException("Nie udało się załadować generatora PDF.");
        return (IBookPdfRenderer)(Activator.CreateInstance(rendererType)
            ?? throw new InvalidOperationException("Nie udało się uruchomić generatora PDF."));
    }
}
