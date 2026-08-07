namespace MalyBystrzak.Core;

public interface IBookPdfRenderer
{
    byte[] RenderPreview(BookDocument document);
    byte[] RenderBooklet(BookDocument document);
    Task<byte[]> RenderPreviewAsync(BookDocument document, IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);
    Task<byte[]> RenderBookletAsync(BookDocument document, IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
