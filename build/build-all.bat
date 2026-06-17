@echo off
REM ===========================================================================
REM  PersiaPad - ONE-CLICK build: publish app + compile installer
REM  Requirements on this Windows machine:
REM    - .NET 8 SDK   : https://dotnet.microsoft.com/download/dotnet/8.0
REM    - Inno Setup 6 : https://jrsoftware.org/isdl.php  (ISCC on PATH or default loc)
REM ===========================================================================
setlocal
cd /d "%~dp0\.."

echo ########################################################
echo #  Step 1/2 : publishing the self-contained app
echo ########################################################
call build\publish.bat
if %ERRORLEVEL% NEQ 0 exit /b 1

echo.
echo ########################################################
echo #  Step 2/2 : compiling the installer with Inno Setup
echo ########################################################

set "ISCC=ISCC.exe"
where %ISCC% >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
)

"%ISCC%" installer\PersiaPad.iss
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Installer compile failed. Is Inno Setup 6 installed?
    echo You can still run the app directly: build\publish\PersiaPad.exe
    exit /b 1
)

echo.
echo ########################################################
echo #  ALL DONE
echo #  App      : build\publish\PersiaPad.exe
echo #  Installer: installer\Output\PersiaPad-Setup.exe
echo ########################################################
endlocal
