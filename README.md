# Mały Bystrzak

„Mały Bystrzak” to działający bez backendu generator drukowanych książeczek z zadaniami dla dzieci w wieku 7–10 lat. Aplikacja obsługuje Sudoku 4×4 i 6×6, Kakuro 3×3 i 4×4, labirynty 9×9 i 15×15 oraz nonogramy 5×5, 7×7 i 10×10. Pozwala tworzyć książeczki mieszane z sześcioma poziomami trudności, wynikiem poznawczym 0–100 i odtwarzalnym ziarnem.

Docelowy adres aplikacji: <https://borys-.github.io/maly-bystrzak/>

## Możliwości

- podgląd PDF A5 w naturalnej kolejności;
- broszura PDF A4 z impozycją do druku dwustronnego;
- sześcioslotowy układ strony; Nonogram 7×7 i 10×10 oraz Labirynt 15×15 zajmują po cztery sloty;
- raport trudności dostępny bezpośrednio w aplikacji;
- personalizacja tytułu, podtytułu i imienia dziecka;
- zakres wyniku trudności i względne gwiazdki;
- lokalny zapis projektów w IndexedDB;
- instalowalna aplikacja PWA działająca offline po pierwszym pełnym otwarciu;
- responsywny interfejs na komputer, tablet i telefon.

Treść książeczek i zapisane projekty nie są wysyłane poza urządzenie użytkownika.

Wynik trudności 0–100 jest normalizowany percentylowo osobno dla każdego wariantu, dzięki czemu można sensownie mieszać różne rodzaje zadań. Raport na stronie pokazuje wynik i składowe trudności każdego zadania bez pól do ręcznego uzupełniania.

## Architektura

- `MalyBystrzak.Core` — modele książeczek, kontrakty modułów i orkiestracja generowania;
- `MalyBystrzak.Modules.Sudoku` — generator i solver Sudoku;
- `MalyBystrzak.Modules.Kakuro` — generator i solver Kakuro;
- `MalyBystrzak.Modules.Mazes` — generator doskonałych labiryntów z jednym rozwiązaniem;
- `MalyBystrzak.Modules.Nonograms` — generator i solver jednoznacznych nonogramów;
- `MalyBystrzak.Pdf` — wspólny renderer PDFsharp Core dla CLI i Web;
- `MalyBystrzak.Cli` — interfejs konsolowy korzystający z tych samych modułów i PDF;
- `MalyBystrzak.Web` — samodzielna aplikacja Blazor WebAssembly PWA;
- `MalyBystrzak.Tests` — testy domenowe i integracyjne;
- `MalyBystrzak.Web.E2E` — scenariusze przeglądarkowe Playwright.

Moduły zadań implementują `IWorksheetModule` i zwracają neutralny model wizualny. Renderer PDF i aplikacja Web nie zawierają centralnych instrukcji `switch` zależnych od rodzaju zadania.

## Uruchomienie lokalne

Wymagany jest .NET SDK 10.

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

Labirynt 15×15:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- maze --count 24 --size 15 --output ./output/labirynty
```

Nonogram 10×10:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- nonogram --count 18 --size 10 --output ./output/nonogramy
```

Książeczka mieszana:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- mixed --types sudoku4,kakuro4,maze9,nonogram7 --count 108 --score-min 20 --score-max 80 --relative-stars --seed 12345 --output ./output/mieszane
```

Pełna lista argumentów:

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
pwsh tests/MalyBystrzak.Web.E2E/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test tests/MalyBystrzak.Web.E2E/MalyBystrzak.Web.E2E.csproj -c Release --no-build
```

Lokalny test wydajności mierzy generowanie 108 zadań ze wszystkich wariantów oraz eksport podglądu PDF A5:

```powershell
./testuj-wydajnosc.bat
```

Test ma kategorię `Performance` i jest celowo wyłączony z workflow GitHub Actions, aby wynik nie zależał od współdzielonego runnera CI.

## GitHub Pages

Workflow „Weryfikacja i publikacja GitHub Pages” jest uruchamiany wyłącznie ręcznie przez `workflow_dispatch`. Wykonuje kompilację, testy domenowe, testy Playwright oraz publikację aplikacji pod ścieżką `/maly-bystrzak/`. Sam push do gałęzi `main` nie uruchamia buildu ani wdrożenia.

## Licencja

Projekt jest publiczny. Warunki ponownego użycia zostaną określone przed pierwszym wydaniem.
