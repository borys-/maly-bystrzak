using System.Diagnostics;
using Xunit;

namespace MalyBystrzak.Web.E2E;

public sealed class WebServerFixture : IAsyncLifetime
{
    private Process? process;
    public string BaseUrl { get; } = "http://127.0.0.1:5280";

    public async Task InitializeAsync()
    {
        var root = FindRepositoryRoot();
        var publishedDirectory = Environment.GetEnvironmentVariable("MALY_BYSTRZAK_PUBLISHED_DIR");
        var info = new ProcessStartInfo(string.IsNullOrWhiteSpace(publishedDirectory) ? "dotnet" : "python3")
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (string.IsNullOrWhiteSpace(publishedDirectory))
        {
            info.ArgumentList.Add("run");
            info.ArgumentList.Add("--project");
            info.ArgumentList.Add("src/MalyBystrzak.Web");
            info.ArgumentList.Add("-c");
            info.ArgumentList.Add("Release");
            info.ArgumentList.Add("--no-build");
            info.ArgumentList.Add("--urls");
            info.ArgumentList.Add(BaseUrl);
        }
        else
        {
            info.ArgumentList.Add("-m");
            info.ArgumentList.Add("http.server");
            info.ArgumentList.Add("5280");
            info.ArgumentList.Add("--bind");
            info.ArgumentList.Add("127.0.0.1");
            info.ArgumentList.Add("--directory");
            info.ArgumentList.Add(publishedDirectory);
        }
        process = Process.Start(info) ?? throw new InvalidOperationException("Nie udało się uruchomić aplikacji testowej.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var client = new HttpClient();
        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (process.HasExited) throw new InvalidOperationException("Aplikacja testowa zakończyła działanie przed uruchomieniem testów.");
            try
            {
                if ((await client.GetAsync(BaseUrl)).IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(250);
        }
        throw new TimeoutException("Aplikacja testowa nie uruchomiła się w wymaganym czasie.");
    }

    public Task DisposeAsync()
    {
        if (process is { HasExited: false }) process.Kill(entireProcessTree: true);
        process?.Dispose();
        return Task.CompletedTask;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MalyBystrzak.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Nie znaleziono katalogu solution.");
    }
}
