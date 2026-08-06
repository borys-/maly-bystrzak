# Mały Bystrzak

„Mały Bystrzak” to działający bez backendu generator drukowanych książeczek z zadaniami dla dzieci. Aplikacja obsługuje Sudoku 4×4 i 6×6 oraz Kakuro 3×3 i 4×4, książeczki mieszane, sześć poziomów trudności, wynik poznawczy 0–100 i odtwarzalne ziarno.

Docelowy adres aplikacji: <https://borys-.github.io/maly-bystrzak/>

> Aplikacja zostanie udostępniona pod tym adresem po pierwszym, ręcznie zatwierdzonym wdrożeniu GitHub Pages.

## Możliwości

- podgląd PDF A5 w naturalnej kolejności;
- broszura PDF A4 z impozycją do druku dwustronnego;
- sześć zadań na stronie i opcjonalna sekcja rozwiązań;
- raport trudności CSV;
- personalizacja tytułu, podtytułu i imienia dziecka;
- zakres wyniku trudności i względne gwiazdki;
- lokalny zapis projektów w IndexedDB;
- instalowalna aplikacja PWA działająca offline po pierwszym pełnym otwarciu;
- responsywny interfejs na komputer, tablet i telefon.

Treść książeczek i zapisane projekty nie są wysyłane poza urządzenie użytkownika.

## Architektura

- `MalyBystrzak.Core` — modele książeczek, kontrakty modułów i orkiestracja generowania;
- `MalyBystrzak.Modules.Sudoku` — generator i solver Sudoku;
- `MalyBystrzak.Modules.Kakuro` — generator i solver Kakuro;
- `MalyBystrzak.Pdf` — wspólny renderer PDFsharp Core dla CLI i Web;
- `MalyBystrzak.Cli` — zgodny wstecznie interfejs konsolowy;
- `MalyBystrzak.Web` — samodzielna aplikacja Blazor WebAssembly PWA;
- `MalyBystrzak.Tests` — testy domenowe i integracyjne;
- `MalyBystrzak.Web.E2E` — scenariusze przeglądarkowe Playwright.

Moduły zadań implementują `IWorksheetModule` i zwracają neutralny model wizualny. Renderer PDF i aplikacja Web nie zawierają centralnych instrukcji `switch` zależnych od rodzaju zadania.

## Uruchomienie lokalne

Wymagany jest .NET SDK 8.

```powershell
dotnet restore MalyBystrzak.sln
dotnet run --project src/MalyBystrzak.Web
```

Po uruchomieniu należy użyć adresu wyświetlonego przez `dotnet run`.

## CLI

Sudoku 4×4:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- generate --count 60 --size 4 --output ./output
```

Kakuro 4×4:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- kakuro --count 60 --size 4 --child-name "Julka" --output ./output/kakuro
```

Książeczka mieszana:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- mixed --types sudoku4,sudoku6,kakuro3,kakuro4 --count 108 --score-min 20 --score-max 80 --relative-stars --seed 12345 --output ./output/mieszane
```

Pełna lista zgodnych wstecznie argumentów:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- --help
```

## Drukowanie broszury

W oknie drukowania pliku `*-broszura-a4.pdf` wybierz papier A4, orientację poziomą, skalę 100%, druk dwustronny i obrót po krótkiej krawędzi. Zachowaj kolejność arkuszy, złóż stos na pół i zszyj na grzbiecie.

## Testy

Testy domenowe i integracyjne:

```powershell
dotnet test tests/MalyBystrzak.Tests/MalyBystrzak.Tests.csproj -c Release
```

Testy przeglądarkowe samodzielnie uruchamiają lokalny serwer aplikacji:

```powershell
dotnet build MalyBystrzak.sln -c Release
pwsh tests/MalyBystrzak.Web.E2E/bin/Release/net8.0/playwright.ps1 install chromium
dotnet test tests/MalyBystrzak.Web.E2E/MalyBystrzak.Web.E2E.csproj -c Release --no-build
```

## GitHub Pages

Workflow „Weryfikacja i publikacja GitHub Pages” jest uruchamiany wyłącznie ręcznie przez `workflow_dispatch`. Wykonuje kompilację, testy domenowe, testy Playwright oraz publikację aplikacji pod ścieżką `/maly-bystrzak/`. Sam push do gałęzi `main` nie uruchamia buildu ani wdrożenia.

## Licencja

Projekt jest publiczny. Warunki ponownego użycia zostaną określone przed pierwszym wydaniem.
