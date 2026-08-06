using System.Text.Json;
using MalyBystrzak.Core;
using Microsoft.JSInterop;

namespace MalyBystrzak.Web.Services;

public sealed class BrowserProjectStore(IJSRuntime js) : IProjectStore
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
            var project = JsonSerializer.Deserialize<GeneratorProject>(json, JsonOptions);
            return project?.SchemaVersion == GeneratorProject.CurrentSchemaVersion ? project : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public ValueTask SaveAsync(GeneratorProject project) => js.InvokeVoidAsync("malyBystrzakStore.save",
        project.Id.ToString(), JsonSerializer.Serialize(project, JsonOptions), JsonSerializer.Serialize(
            new ProjectSummary(project.Id, project.Name, project.UpdatedAt, project.Book.Worksheets.Count), JsonOptions));

    public ValueTask DeleteAsync(Guid id) => js.InvokeVoidAsync("malyBystrzakStore.remove", id.ToString());
}
