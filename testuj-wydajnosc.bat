@echo off
setlocal
cd /d "%~dp0"

dotnet build MalyBystrzak.sln -c Release --no-restore
if errorlevel 1 exit /b %errorlevel%

dotnet test tests\MalyBystrzak.Web.E2E\MalyBystrzak.Web.E2E.csproj -c Release --no-build --filter "Category=Performance" --logger "console;verbosity=detailed"
exit /b %errorlevel%
