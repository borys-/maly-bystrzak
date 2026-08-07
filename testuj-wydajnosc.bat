@echo off
setlocal
cd /d "%~dp0"

dotnet build MalyBystrzak.sln -c Release --no-restore
if errorlevel 1 exit /b %errorlevel%

dotnet publish src\MalyBystrzak.Web\MalyBystrzak.Web.csproj -c Release --no-build -o tmp\performance-publish
if errorlevel 1 exit /b %errorlevel%

set "MALY_BYSTRZAK_PUBLISHED_DIR=%CD%\tmp\performance-publish\wwwroot"
dotnet test tests\MalyBystrzak.Web.E2E\MalyBystrzak.Web.E2E.csproj -c Release --no-build --filter "Category=Performance" --logger "console;verbosity=detailed"
exit /b %errorlevel%
