@echo off
chcp 65001 >nul
title ساخت PersiaPad.exe
setlocal enabledelayedexpansion

echo ============================================================
echo   ساخت برنامه PersiaPad به صورت یک فایل exe
echo ============================================================
echo.

cd /d "%~dp0"

REM --- پیدا کردن پایتون -------------------------------------------------
set "PY="
where py >nul 2>nul && set "PY=py -3"
if not defined PY (
    where python >nul 2>nul && set "PY=python"
)
if not defined PY (
    echo [خطا] پایتون روی این سیستم پیدا نشد.
    echo.
    echo لطفا پایتون را از این آدرس نصب کنید و گزينه "Add to PATH" را تیک بزنید:
    echo     https://www.python.org/downloads/
    echo.
    echo بعد از نصب، دوباره روی همين فايل build.bat دابل‌کليک کنيد.
    pause
    exit /b 1
)

echo [1/3] استفاده از: %PY%
%PY% --version

echo.
echo [2/3] نصب/به‌روزرسانی PyInstaller ...
%PY% -m pip install --upgrade pip >nul 2>nul
%PY% -m pip install --upgrade pyinstaller
if errorlevel 1 (
    echo [خطا] نصب PyInstaller شکست خورد. اینترنت را بررسی کنید.
    pause
    exit /b 1
)

echo.
echo [3/3] در حال ساخت PersiaPad.exe ... (ممکن است ۱ تا ۲ دقیقه طول بکشد)
echo.

set "ICON_ARG="
if exist "src\app.ico" set "ICON_ARG=--icon=src\app.ico"

%PY% -m PyInstaller ^
  --noconfirm ^
  --onefile ^
  --windowed ^
  --name PersiaPad ^
  %ICON_ARG% ^
  --paths app ^
  --hidden-import persiapad ^
  --hidden-import uuid ^
  PersiaPad_entry.py

if errorlevel 1 (
    echo.
    echo [خطا] ساخت exe شکست خورد. متن خطا را بالا ببينيد.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   تمام شد!  فايل آماده است:
echo.
echo        %CD%\dist\PersiaPad.exe
echo.
echo   همين فايل را هر جا بخواهيد کپي کنيد و دابل‌کليک کنيد.
echo ============================================================
echo.

REM یک کپی هم کنار همين پوشه بگذاريم تا راحت پيدا شود
if exist "dist\PersiaPad.exe" copy /Y "dist\PersiaPad.exe" "PersiaPad.exe" >nul

REM پوشه‌ي حاوي exe را باز کن
start "" explorer "%CD%\dist"

pause
endlocal
