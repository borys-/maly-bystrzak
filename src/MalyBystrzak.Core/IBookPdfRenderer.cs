namespace MalyBystrzak.Core;

public interface IBookPdfRenderer
{
    byte[] RenderPreview(BookDocument document);
    byte[] RenderBooklet(BookDocument document);
}
