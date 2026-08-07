using System.Text.Json.Serialization;

namespace MalyBystrzak.Core;

public sealed record WorksheetVariant(string Id, string DisplayName, string Description);
public sealed record ModuleSelection(string ModuleId, string VariantId);
public sealed record ModuleGenerationRequest(string VariantId, int Count, int Seed);

public sealed record GenerationProgress(int Completed, int Total, string Message)
{
    public int Percentage => Total == 0 ? 0 : (int)Math.Round(Completed * 100d / Total);
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(VisualLine), "line")]
[JsonDerivedType(typeof(VisualRectangle), "rectangle")]
[JsonDerivedType(typeof(VisualEllipse), "ellipse")]
[JsonDerivedType(typeof(VisualPolygon), "polygon")]
[JsonDerivedType(typeof(VisualText), "text")]
public abstract record VisualElement;

public sealed record VisualLine(double X1, double Y1, double X2, double Y2, double Width, string Color) : VisualElement;
public sealed record VisualRectangle(double X, double Y, double Width, double Height, string Fill,
    string Stroke, double StrokeWidth = 0) : VisualElement;
public sealed record VisualEllipse(double CenterX, double CenterY, double RadiusX, double RadiusY,
    string Fill, string Stroke, double StrokeWidth = 0) : VisualElement;
public sealed record VisualPoint(double X, double Y);
public sealed record VisualPolygon(IReadOnlyList<VisualPoint> Points, string Fill,
    string Stroke, double StrokeWidth = 0) : VisualElement;
public sealed record VisualText(double X, double Y, string Text, double Size, string Color,
    bool Bold = false, string Anchor = "middle") : VisualElement;
public sealed record WorksheetVisual(double Width, double Height, IReadOnlyList<VisualElement> Elements);
public sealed record WorksheetInstruction(string Title, string FirstLine, string SecondLine, string Accent);
public enum WorksheetLayout { Standard, Large }

public sealed record GeneratedWorksheet(
    int Number, string ModuleId, string VariantId, string TypeName, string Fingerprint,
    CognitiveDifficulty Difficulty, int DisplayStars, WorksheetVisual Task, WorksheetVisual Solution,
    WorksheetInstruction Instruction, WorksheetLayout Layout = WorksheetLayout.Standard);

public interface IWorksheetModule
{
    string Id { get; }
    string DisplayName { get; }
    string Symbol { get; }
    WorksheetInstruction Instruction { get; }
    IReadOnlyList<WorksheetVariant> Variants { get; }
    IReadOnlyList<string> Validate(ModuleGenerationRequest request);
    IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default);
}

public sealed class WorksheetModuleRegistry
{
    private readonly IReadOnlyDictionary<string, IWorksheetModule> modules;

    public WorksheetModuleRegistry(IEnumerable<IWorksheetModule> modules) =>
        this.modules = modules.ToDictionary(module => module.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<IWorksheetModule> All => modules.Values.ToArray();
    public IWorksheetModule GetRequired(string id) => modules.TryGetValue(id, out var module) ? module :
        throw new KeyNotFoundException($"Nie znaleziono modułu zadań: {id}.");
}
