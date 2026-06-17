@echo off
REM ===========================================================================
REM  PersiaPad - LIGHT publish (framework-dependent)
REM  Smallest possible download (~a few MB). Requires .NET 8 Desktop Runtime
REM  on the target PC. Use publish.bat instead if the target has no .NET.
REM ===========================================================================
setlocal
cd /d "%~dp0\.."

echo.
echo === Publishing PersiaPad (framework-dependent, lightweight) ===
echo.

dotnet publish src\PersiaPad.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -p:PublishReadyToRun=true ^
    -o build\publish

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED. Install the .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)

echo.
echo === Done. Output: build\publish\PersiaPad.exe ===
echo (Target PC needs the .NET 8 Desktop Runtime.)
echo.
endlocal
