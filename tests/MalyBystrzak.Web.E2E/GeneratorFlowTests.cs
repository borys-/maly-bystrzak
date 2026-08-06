using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System.Text.RegularExpressions;
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
        var focusedHeadingOutline = await Page.EvaluateAsync<string>(
            "() => document.activeElement?.tagName === 'H1' ? getComputedStyle(document.activeElement).outlineStyle : 'unexpected-focus'");
        Assert.Equal("none", focusedHeadingOutline);
        await Expect(Page.GetByTestId("generate")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("variant-maze-9x9")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Nie masz jeszcze zapisanych projektów.")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Checkbox, new() { Name = "Dołącz rozwiązania Odpowiedzi znajdą się w osobnej sekcji na końcu.", Exact = true })).Not.ToBeCheckedAsync();
    }

    [Fact]
    public async Task GeneratesMixedBookAndDownloadsPdf()
    {
        await Page.GotoAsync(server.BaseUrl);
        await Page.GetByTestId("variant-kakuro-3x3").ClickAsync();
        await Page.GetByLabel("Liczba zadań").FillAsync("8");
        await Page.GetByTestId("generate").ClickAsync();
        await Expect(Page.GetByTestId("result")).ToContainTextAsync("8 zadań", new() { Timeout = 60_000 });
        await Page.GetByTestId("preview-solutions").ClickAsync();
        await Expect(Page.Locator(".preview-card svg").First).ToHaveAttributeAsync("aria-label", new Regex("Rozwiązanie 1"));
        await Page.GetByTestId("preview-tasks").ClickAsync();
        await Expect(Page.GetByTestId("preview-page-label")).ToHaveTextAsync("Strona 1 z 2");
        await Expect(Page.GetByText("Nr 1", new() { Exact = true })).ToBeVisibleAsync();
        await Page.GetByTestId("preview-next").ClickAsync();
        await Expect(Page.GetByTestId("preview-page-label")).ToHaveTextAsync("Strona 2 z 2");
        await Expect(Page.GetByText("Nr 7", new() { Exact = true })).ToBeVisibleAsync();
        await Page.GetByTestId("preview-previous").ClickAsync();
        await Expect(Page.GetByTestId("preview-page-label")).ToHaveTextAsync("Strona 1 z 2");
        var visibleGlyphs = await Page.Locator(".preview-card svg text").EvaluateAllAsync<int>(
            "elements => elements.filter(element => element.getBoundingClientRect().width > 0).length");
        Assert.True(visibleGlyphs > 0);
        var download = await Page.RunAndWaitForDownloadAsync(() => Page.GetByTestId("download-booklet").ClickAsync());
        Assert.Equal("maly-bystrzak-broszura-a4.pdf", download.SuggestedFilename);
        var solutions = await Page.RunAndWaitForDownloadAsync(() => Page.GetByTestId("download-solutions").ClickAsync());
        Assert.Equal("maly-bystrzak-rozwiazania-a5.pdf", solutions.SuggestedFilename);
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
        var storedDocumentLength = await Page.EvaluateAsync<int>("""
            async () => {
              const db = await new Promise((resolve, reject) => {
                const request = indexedDB.open('maly-bystrzak', 1);
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
              });
              const request = db.transaction('projects', 'readonly').objectStore('projects').getAll();
              const rows = await new Promise((resolve, reject) => {
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
              });
              return rows[0].document.length;
            }
            """);
        Assert.InRange(storedDocumentLength, 1, 10_000);
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Test Playwright")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task OpensOverwritesAndDeletesProject()
    {
        await Page.GotoAsync(server.BaseUrl);
        await Page.GetByLabel("Liczba zadań").FillAsync("4");
        await Page.GetByTestId("generate").ClickAsync();
        await Expect(Page.GetByTestId("result")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Page.GetByLabel("Nazwa projektu").FillAsync("Pierwsza nazwa");
        await Page.GetByTestId("save-project").ClickAsync();

        await Page.GetByTestId("open-project").ClickAsync();
        await Expect(Page.GetByText("Projekt został otwarty.")).ToBeVisibleAsync();
        await Page.GetByLabel("Nazwa projektu").FillAsync("Nazwa po zmianie");
        await Page.GetByTestId("save-project").ClickAsync();

        await Expect(Page.GetByText("Projekt został zaktualizowany.")).ToBeVisibleAsync();
        await Expect(Page.Locator(".project-list article")).ToHaveCountAsync(1);
        await Expect(Page.GetByText("Nazwa po zmianie")).ToBeVisibleAsync();
        await Page.GetByTestId("delete-project").ClickAsync();
        await Expect(Page.GetByText("Nie masz jeszcze zapisanych projektów.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RestoresLastGeneratorPreferencesAfterReload()
    {
        await Page.GotoAsync(server.BaseUrl);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Tytuł", Exact = true }).FillAsync("Zapamiętana książeczka");
        await Page.GetByLabel("Liczba zadań").FillAsync("5");
        await Page.GetByTestId("variant-kakuro-3x3").ClickAsync();
        await Page.GetByTestId("generate").ClickAsync();
        await Expect(Page.GetByTestId("result")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.ReloadAsync();

        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Tytuł", Exact = true })).ToHaveValueAsync("Zapamiętana książeczka");
        await Expect(Page.GetByLabel("Liczba zadań")).ToHaveValueAsync("5");
        await Expect(Page.GetByTestId("variant-kakuro-3x3").Locator("input")).ToBeCheckedAsync();
    }

    [Fact]
    public async Task DrawsNewSeedOnEveryFreshPageLoad()
    {
        await Page.GotoAsync(server.BaseUrl);
        var seedInput = Page.GetByRole(AriaRole.Spinbutton, new() { Name = "Ziarno zestawu ↻", Exact = true });
        var firstSeed = await seedInput.InputValueAsync();

        await Page.ReloadAsync();

        var secondSeed = await Page.GetByRole(AriaRole.Spinbutton, new() { Name = "Ziarno zestawu ↻", Exact = true }).InputValueAsync();
        Assert.NotEqual(firstSeed, secondSeed);
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

    [Fact]
    public async Task PublishedPwaWorksOfflineAfterFirstLoad()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MALY_BYSTRZAK_PUBLISHED_DIR")))
            return;

        await Page.GotoAsync(server.BaseUrl);
        await Page.EvaluateAsync("() => navigator.serviceWorker.ready");
        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null");

        await Context.SetOfflineAsync(true);
        try
        {
            await Page.ReloadAsync();
            await Expect(Page.GetByTestId("generate")).ToBeVisibleAsync();
            await Expect(Page).ToHaveTitleAsync("Mały Bystrzak — generator książeczek dla dzieci");
        }
        finally
        {
            await Context.SetOfflineAsync(false);
        }
    }
}
