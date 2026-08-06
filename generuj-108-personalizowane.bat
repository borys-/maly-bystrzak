@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 goto :no_dotnet

echo.
echo ========================================
echo  108 personalizowanych lamiglowek
echo ========================================
echo.

call :generate "Olga" "output\personalizowane\Olga" 0 80 10882020
if errorlevel 1 goto :failed
call :generate "Antek" "output\personalizowane\Antek" 20 80 10842019
if errorlevel 1 goto :failed
call :generate "Julka" "output\personalizowane\Julka" 20 100 10872018
if errorlevel 1 goto :failed

echo.
echo Gotowe. Pliki sa w katalogu output\personalizowane.
echo Kazda ksiazeczka ma 108 zadan i nie zawiera pustej strony.
echo Podzial gwiazdek: 22, 22, 22, 21, 21.
echo.
pause
exit /b 0

:generate
echo Generuje dla %~1, zakres %~3-%~4...
dotnet run --project "src\MalyBystrzak.Cli\MalyBystrzak.Cli.csproj" -c Release -- mixed --types "sudoku4,sudoku6,kakuro3,kakuro4" --count 108 --score-min %~3 --score-max %~4 --relative-stars --child-name "%~1" --output "%~2" --seed %~5 --overwrite
if errorlevel 1 exit /b 1
echo.
exit /b 0

:no_dotnet
echo BLAD: Nie znaleziono programu dotnet. Zainstaluj .NET 8 SDK.
pause
exit /b 1

:failed
echo BLAD: Nie udalo sie wygenerowac wszystkich ksiazeczek.
pause
exit /b 1


