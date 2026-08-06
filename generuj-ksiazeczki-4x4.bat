@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 goto :no_dotnet

echo.
echo ========================================
echo  Ksiazki Sudoku 4x4
echo ========================================
echo.

call :generate "Julka" "output\4x4\Julka" 11072018
if errorlevel 1 goto :failed
call :generate "Antek" "output\4x4\Antek" 23042019
if errorlevel 1 goto :failed
call :generate "Olga" "output\4x4\Olga" 15092020
if errorlevel 1 goto :failed

echo.
echo Gotowe. Pliki sa w katalogu output\4x4.
echo Do druku wybierz sudoku-broszura-a4.pdf.
echo Ustaw druk dwustronny, skale 100%% i obrot po krotkiej krawedzi.
echo.
pause
exit /b 0

:generate
echo Generuje 4x4 dla: %~1
dotnet run --project "src\MalyBystrzak.Cli\MalyBystrzak.Cli.csproj" -c Release -- generate --count 60 --size 4 --child-name "%~1" --output "%~2" --seed %~3 --overwrite
if errorlevel 1 exit /b 1
echo.
exit /b 0

:no_dotnet
echo BLAD: Nie znaleziono programu dotnet. Zainstaluj .NET 8 SDK.
pause
exit /b 1

:failed
echo.
echo BLAD: Nie udalo sie wygenerowac wszystkich ksiazeczek 4x4.
pause
exit /b 1


