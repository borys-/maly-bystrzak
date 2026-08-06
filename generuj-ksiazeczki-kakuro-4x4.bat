@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 goto :no_dotnet

echo.
echo ========================================
echo  Ksiazki Kakuro 4x4 dla dzieci
echo ========================================
echo.

call :generate "Julka" "output\kakuro4x4\Julka" 91072018
if errorlevel 1 goto :failed
call :generate "Antek" "output\kakuro4x4\Antek" 93042019
if errorlevel 1 goto :failed
call :generate "Olga" "output\kakuro4x4\Olga" 95092020
if errorlevel 1 goto :failed

echo.
echo Gotowe. Pliki sa w katalogu output\kakuro4x4.
echo Do druku wybierz kakuro-broszura-a4.pdf.
echo.
pause
exit /b 0

:generate
echo Generuje Kakuro 4x4 dla: %~1
dotnet run --project "src\MalyBystrzak.Cli\MalyBystrzak.Cli.csproj" -c Release -- kakuro --size 4 --count 60 --child-name "%~1" --output "%~2" --seed %~3 --overwrite
if errorlevel 1 exit /b 1
echo.
exit /b 0

:no_dotnet
echo BLAD: Nie znaleziono programu dotnet. Zainstaluj .NET 8 SDK.
pause
exit /b 1

:failed
echo BLAD: Nie udalo sie wygenerowac wszystkich ksiazeczek Kakuro 4x4.
pause
exit /b 1


