@echo off
REM ===========================================================================
REM  PersiaPad - publish script (run on Windows)
REM  Produces a self-contained, single-folder build under build\publish that
REM  runs even on machines WITHOUT .NET installed (best for weak/old PCs).
REM ===========================================================================
setlocal
cd /d "%~dp0\.."

echo.
echo === Restoring and publishing PersiaPad (self-contained x64) ===
echo.

REM Self-contained = no .NET required on target. ReadyToRun = faster startup.
dotnet publish src\PersiaPad.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishReadyToRun=true ^
    -p:PublishSingleFile=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o build\publish

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED. Make sure the .NET 8 SDK is installed:
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)

echo.
echo === Done. Output: build\publish\PersiaPad.exe ===
echo You can run it directly, or build the installer with installer\PersiaPad.iss
echo.
endlocal
