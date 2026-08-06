using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace MalyBystrzak.Web.E2E;

public sealed class GeneratorFlowTests(WebServerFixture server) : PageTest, IClassFixture<WebServerFixture>
{
    [Fact]
    public async Task HomeShowsCompleteGenerator()
    {
        await Page.GotoAsync(server.BaseUrl);
        await Expect(Page).ToHaveTitleAsync("Mały Bystrzak — generator książeczek dla dzieci");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Ułóż wyjątkową książeczkę dla małego bystrzaka." })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("generate")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Nie masz jeszcze zapisanych projektów.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task GeneratesMixedBookAndDownloadsPdf()
    {
        await Page.GotoAsync(server.BaseUrl);
        await Page.GetByTestId("variant-kakuro-3x3").ClickAsync();
        await Page.GetByLabel("Liczba zadań").FillAsync("8");
        await Page.GetByTestId("generate").ClickAsync();
        await Expect(Page.GetByTestId("result")).ToContainTextAsync("8 zadań", new() { Timeout = 60_000 });
        var download = await Page.RunAndWaitForDownloadAsync(() => Page.GetByTestId("download-booklet").ClickAsync());
        Assert.Equal("maly-bystrzak-broszura-a4.pdf", download.SuggestedFilename);
    }

    [Fact]
    public async Task SavesProjectInIndexedDb()
    {
        await Page.GotoAsync(server.BaseUrl);
        await Page.GetByLabel("Liczba zadań").FillAsync("4");
        await Page.GetByTestId("generate").ClickAsync();
        await Expect(Page.GetByTestId("result")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Page.GetByLabel("Nazwa projektu").FillAsync("Test Playwright");
        await Page.GetByTestId("save-project").ClickAsync();
        await Expect(Page.GetByText("Projekt został zapisany na tym urządzeniu.")).ToBeVisibleAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Test Playwright")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task IgnoresCorruptedAndUnsupportedProjects()
    {
        await Page.GotoAsync(server.BaseUrl);
        await Expect(Page.GetByText("Nie masz jeszcze zapisanych projektów.")).ToBeVisibleAsync();
        await Page.EvaluateAsync("""
            async () => {
              const db = await new Promise((resolve, reject) => {
                const request = indexedDB.open('maly-bystrzak', 1);
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
              });
              const store = db.transaction('projects', 'readwrite').objectStore('projects');
              store.put({ id: 'broken', document: '{', summary: '{' });
              store.put({
                id: 'future',
                document: JSON.stringify({ schemaVersion: 999 }),
                summary: JSON.stringify({ id: 'future', name: 'Nieobsługiwany projekt', updatedAt: '2026-01-01T00:00:00Z', worksheetCount: 0 })
              });
              await new Promise((resolve, reject) => {
                store.transaction.oncomplete = resolve;
                store.transaction.onerror = () => reject(store.transaction.error);
              });
            }
            """);
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Nie masz jeszcze zapisanych projektów.")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Nieobsługiwany projekt")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task MobileLayoutHasNoHorizontalOverflow()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(server.BaseUrl);
        var dimensions = await Page.EvaluateAsync<int[]>("() => [document.documentElement.clientWidth, document.documentElement.scrollWidth]");
        Assert.Equal(dimensions[0], dimensions[1]);
        await Expect(Page.GetByTestId("generate")).ToBeVisibleAsync();
    }
}
