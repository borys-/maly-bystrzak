using System.Text.Json;
using MalyBystrzak.Core;
using Microsoft.JSInterop;

namespace MalyBystrzak.Web.Services;

public sealed class BrowserProjectStore(IJSRuntime js, BookGenerator generator) : IProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<IReadOnlyList<ProjectSummary>> ListAsync()
    {
        var json = await js.InvokeAsync<string>("malyBystrzakStore.list");
        return JsonSerializer.Deserialize<ProjectSummary[]>(json, JsonOptions) ?? [];
    }

    public async ValueTask<GeneratorProject?> GetAsync(Guid id)
    {
        var json = await js.InvokeAsync<string?>("malyBystrzakStore.get", id.ToString());
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("schemaVersion", out var versionElement)) return null;
            return versionElement.GetInt32() == GeneratorProject.CurrentSchemaVersion
                ? RestoreCompactProject(json)
                : null;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    public ValueTask SaveAsync(GeneratorProject project)
    {
        var stored = new StoredGeneratorProject(GeneratorProject.CurrentSchemaVersion, project.Id, project.Name,
            project.UpdatedAt, project.Book.Settings);
        return js.InvokeVoidAsync("malyBystrzakStore.save", project.Id.ToString(),
            JsonSerializer.Serialize(stored, JsonOptions), JsonSerializer.Serialize(
                new ProjectSummary(project.Id, project.Name, project.UpdatedAt, project.Book.Worksheets.Count), JsonOptions));
    }

    public ValueTask DeleteAsync(Guid id) => js.InvokeVoidAsync("malyBystrzakStore.remove", id.ToString());

    private GeneratorProject? RestoreCompactProject(string json)
    {
        var stored = JsonSerializer.Deserialize<StoredGeneratorProject>(json, JsonOptions);
        return stored is null ? null : new(GeneratorProject.CurrentSchemaVersion, stored.Id, stored.Name,
            stored.UpdatedAt, generator.Generate(stored.Settings));
    }

    private sealed record StoredGeneratorProject(int SchemaVersion, Guid Id, string Name, DateTimeOffset UpdatedAt,
        BookGenerationSettings Settings);
}
