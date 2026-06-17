@echo off
chcp 65001 >nul
title PersiaPad
cd /d "%~dp0"

REM اجرای مستقیم برنامه با پایتون (بدون ساخت exe)
set "PYW="
where pyw >nul 2>nul && set "PYW=pyw"
if not defined PYW where pythonw >nul 2>nul && set "PYW=pythonw"

if defined PYW (
    start "" %PYW% "%~dp0PersiaPad.pyw"
    exit /b 0
)

REM اگر pythonw نبود، با python معمولی اجرا کن
set "PY="
where py >nul 2>nul && set "PY=py -3"
if not defined PY where python >nul 2>nul && set "PY=python"

if not defined PY (
    echo پایتون پیدا نشد. لطفا از https://www.python.org نصب کنید
    echo و موقع نصب گزینه Add to PATH را تیک بزنید.
    pause
    exit /b 1
)

%PY% "%~dp0PersiaPad.pyw"
