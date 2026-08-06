# Sudoku dla dzieci - generator książeczek

Konsolowy generator kolorowych książeczek Sudoku 4×4 i 6×6. Tworzy dwa pliki PDF:

- `sudoku-podglad-a5.pdf` - strony A5 w naturalnej kolejności,
- `sudoku-broszura-a4.pdf` - arkusze A4 z impozycją do druku dwustronnego i złożenia na pół.

Każda pełna strona A5 mieści sześć zadań w układzie 2×3. Zadania przechodzą przez sześć kolorowych poziomów oznaczonych od 1 do 6 gwiazdek i zawsze mają dokładnie jedno rozwiązanie.

Przy każdym zadaniu drukowany jest heurystyczny wskaźnik obciążenia `0–100`. W katalogu wynikowym powstaje także `raport-trudnosci.csv` z częściowymi metrykami oraz pustymi kolumnami do zapisania czasu, błędów, podpowiedzi, wysiłku dziecka i ukończenia zadania. Wskaźnik służy do stopniowania ćwiczeń, a nie do diagnozy psychologicznej.

Generator obsługuje także dziecięce Kakuro 3×3 i 4×4 z sumami poziomymi i pionowymi:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- kakuro --count 60 --child-name "Julka" --output ./output/kakuro/Julka
dotnet run --project src/MalyBystrzak.Cli -- kakuro --size 4 --count 60 --child-name "Julka" --output ./output/kakuro4x4/Julka
```

Plik `generuj-ksiazeczki-kakuro.bat` tworzy książeczki 3×3, a `generuj-ksiazeczki-kakuro-4x4.bat` książeczki 4×4.

## Książeczki mieszane

Polecenie `mixed` przeplata wybrane rodzaje zadań. Domyślnie używa Sudoku 4×4, Sudoku 6×6 i różnych plansz Kakuro:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- mixed --types sudoku4,sudoku6,kakuro3,kakuro4 --count 60 --child-name "Julka" --output ./output/mieszane/Julka
```

Można podać dowolny podzbiór, np. `--types sudoku4,kakuro`. Plik `generuj-ksiazeczki-mieszane.bat` tworzy gotowe mieszane książeczki dla całej trójki dzieci.

### Personalizowane zakresy

Plik `generuj-108-personalizowane.bat` tworzy po 108 zadań dla każdego dziecka: Olga 0–80, Antek 20–80, Julka 20–100. Daje to 18 pełnych stron z zadaniami oraz okładki, bez pustych stron. Zadania są sortowane według wskaźnika i dzielone możliwie równo na grupy gwiazdkowe: 22, 22, 22, 21 i 21. Gwiazdki oznaczają względną trudność w danej książeczce, nie stałe progi wskaźnika.

```powershell
dotnet run --project src/MalyBystrzak.Cli -- mixed --types sudoku4,sudoku6,kakuro3,kakuro4 --count 108 --score-min 20 --score-max 80 --relative-stars --child-name "Antek" --output ./output/personalizowane/Antek
```

## Wymagania

- .NET SDK 8.0 lub nowszy.

## Uruchomienie

### Gotowe książeczki dla Julki, Antka i Olgi

Na Windows dostępne są dwa skrypty uruchamiane dwukrotnym kliknięciem:

- `generuj-ksiazeczki-4x4.bat` tworzy zestawy w katalogu `output\4x4`,
- `generuj-ksiazeczki-6x6.bat` tworzy zestawy w katalogu `output\6x6`.

Każdy skrypt generuje po jednej książeczce dla Julki, Antka i Olgi. Ponowne uruchomienie odtwarza te same zestawy i zastępuje wcześniejsze pliki.

### Ręczne uruchomienie

```powershell
dotnet run --project src/MalyBystrzak.Cli -- generate --output ./output --count 60 --size 4
```

Personalizowana okładka:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- generate --child-name "Zosia" --title "Moja książeczka Sudoku"
```

Książeczka 6×6 z odtwarzalnym zestawem:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- generate --size 6 --count 72 --seed 12345
```

Pełna lista opcji:

```powershell
dotnet run --project src/MalyBystrzak.Cli -- --help
```

## Drukowanie broszury

Otwórz `sudoku-broszura-a4.pdf` i wybierz:

1. papier A4, orientacja pozioma,
2. skala 100% / rzeczywisty rozmiar,
3. druk dwustronny,
4. obrót po krótkiej krawędzi.

Zachowaj kolejność wydrukowanych arkuszy, złóż cały stos na pół i zszyj na grzbiecie.

## Testy i publikacja

```powershell
dotnet test MalyBystrzak.sln -c Release
dotnet publish src/MalyBystrzak.Cli -c Release -r win-x64 --self-contained false
```

Opcja `--seed` jest wypisywana w konsoli i na tylnej okładce, dzięki czemu ten sam zestaw można wygenerować ponownie.


