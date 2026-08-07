using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MalyBystrzak.Web;
using MalyBystrzak.Core;
using MalyBystrzak.Modules.Kakuro;
using MalyBystrzak.Modules.Mazes;
using MalyBystrzak.Modules.Nonograms;
using MalyBystrzak.Modules.Sudoku;
using MalyBystrzak.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IWorksheetModule, SudokuModule>();
builder.Services.AddSingleton<IWorksheetModule, KakuroModule>();
builder.Services.AddSingleton<IWorksheetModule, MazeModule>();
builder.Services.AddSingleton<IWorksheetModule, NonogramModule>();
builder.Services.AddSingleton(sp => new WorksheetModuleRegistry(sp.GetServices<IWorksheetModule>()));
builder.Services.AddSingleton<BookGenerator>();
builder.Services.AddSingleton<PdfExportService>();
builder.Services.AddScoped<IProjectStore, BrowserProjectStore>();

await builder.Build().RunAsync();
