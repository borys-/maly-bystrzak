using MalyBystrzak.Core;

namespace MalyBystrzak.Pdf;

public interface IBookPdfRenderer
{
    byte[] RenderPreview(BookDocument document);
    byte[] RenderBooklet(BookDocument document);
}
