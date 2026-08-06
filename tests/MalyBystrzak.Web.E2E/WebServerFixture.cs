using System.Diagnostics;
using System.Net;
using Xunit;

namespace MalyBystrzak.Web.E2E;

public sealed class WebServerFixture : IAsyncLifetime
{
    private Process? process;
    private HttpListener? listener;
    private CancellationTokenSource? serverCancellation;
    private Task? serverTask;
    public string BaseUrl { get; } = "http://127.0.0.1:5280";

    public async Task InitializeAsync()
    {
        var root = FindRepositoryRoot();
        var publishedDirectory = Environment.GetEnvironmentVariable("MALY_BYSTRZAK_PUBLISHED_DIR");
        if (!string.IsNullOrWhiteSpace(publishedDirectory))
        {
            StartStaticServer(publishedDirectory);
            return;
        }

        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        info.ArgumentList.Add("run");
        info.ArgumentList.Add("--project");
        info.ArgumentList.Add("src/MalyBystrzak.Web");
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add("Release");
        info.ArgumentList.Add("--no-build");
        info.ArgumentList.Add("--urls");
        info.ArgumentList.Add(BaseUrl);
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

    public async Task DisposeAsync()
    {
        if (process is { HasExited: false })
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        process?.Dispose();
        if (serverCancellation is not null) await serverCancellation.CancelAsync();
        listener?.Close();
        if (serverTask is not null)
            try { await serverTask; }
            catch (Exception) when (serverCancellation?.IsCancellationRequested == true) { }
        serverCancellation?.Dispose();
    }

    private void StartStaticServer(string directory)
    {
        var root = Path.GetFullPath(directory);
        listener = new HttpListener();
        listener.Prefixes.Add($"{BaseUrl}/");
        listener.Start();
        serverCancellation = new CancellationTokenSource();
        serverTask = ServeAsync(listener, root, serverCancellation.Token);
    }

    private static async Task ServeAsync(HttpListener server, string root, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var context = await server.GetContextAsync().WaitAsync(cancellationToken);
            _ = SendFileAsync(context, root, cancellationToken);
        }
    }

    private static async Task SendFileAsync(HttpListenerContext context, string root, CancellationToken cancellationToken)
    {
        try
        {
            var relativePath = Uri.UnescapeDataString(context.Request.Url?.AbsolutePath.TrimStart('/') ?? "");
            var path = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.Ordinal) || !File.Exists(path)) path = Path.Combine(root, "index.html");
            context.Response.ContentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".html" => "text/html; charset=utf-8",
                ".js" => "text/javascript; charset=utf-8",
                ".json" => "application/json",
                ".css" => "text/css; charset=utf-8",
                ".wasm" => "application/wasm",
                ".woff2" => "font/woff2",
                ".png" => "image/png",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, cancellationToken);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MalyBystrzak.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Nie znaleziono katalogu solution.");
    }
}
