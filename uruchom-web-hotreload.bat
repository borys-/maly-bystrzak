@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo Blad: nie znaleziono .NET SDK w zmiennej PATH.
    echo Zainstaluj .NET SDK 8 ze strony https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo.
echo ===============================================
echo  Maly Bystrzak - Blazor WebAssembly Hot Reload
echo ===============================================
echo.
echo Aplikacja: http://localhost:5280
echo Zatrzymanie: Ctrl+C
echo.

start "" powershell.exe -NoProfile -WindowStyle Hidden -Command "Start-Sleep -Seconds 4; Start-Process 'http://localhost:5280'"

set "DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1"
dotnet watch --project "src\MalyBystrzak.Web\MalyBystrzak.Web.csproj" run --urls "http://localhost:5280"

echo.
echo Serwer zostal zatrzymany.
pause
endlocal
