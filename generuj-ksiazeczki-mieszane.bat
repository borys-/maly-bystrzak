@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 goto :no_dotnet

echo.
echo ========================================
echo  Mieszane ksiazki z lamiglowkami
echo ========================================
echo.

call :generate "Julka" "output\mieszane\Julka" 81072018
if errorlevel 1 goto :failed
call :generate "Antek" "output\mieszane\Antek" 83042019
if errorlevel 1 goto :failed
call :generate "Olga" "output\mieszane\Olga" 85092020
if errorlevel 1 goto :failed

echo.
echo Gotowe. Pliki sa w katalogu output\mieszane.
echo Do druku wybierz lamiglowki-broszura-a4.pdf.
echo Ustaw druk dwustronny, skale 100%% i obrot po krotkiej krawedzi.
echo.
pause
exit /b 0

:generate
echo Generuje mieszana ksiazeczke dla: %~1
dotnet run --project "src\MalyBystrzak.Cli\MalyBystrzak.Cli.csproj" -c Release -- mixed --types "sudoku4,sudoku6,kakuro3,kakuro4" --count 60 --child-name "%~1" --output "%~2" --seed %~3 --overwrite
if errorlevel 1 exit /b 1
echo.
exit /b 0

:no_dotnet
echo BLAD: Nie znaleziono programu dotnet. Zainstaluj .NET 8 SDK.
pause
exit /b 1

:failed
echo.
echo BLAD: Nie udalo sie wygenerowac wszystkich mieszanych ksiazeczek.
pause
exit /b 1


