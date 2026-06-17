#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PersiaPad launcher (no console window on Windows).

اگر پایتون روی ویندوز نصب باشد، کافی است روی این فایل دابل‌کلیک کنید
تا برنامه بدون پنجره‌ی مشکی کنسول باز شود.
"""
import os
import sys

# اجازه بده هم در حالت اجرای مستقیم و هم بعد از بسته‌بندی PyInstaller کار کند.
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
APP_DIR = os.path.join(BASE_DIR, "app")
if APP_DIR not in sys.path:
    sys.path.insert(0, APP_DIR)
if BASE_DIR not in sys.path:
    sys.path.insert(0, BASE_DIR)

try:
    import persiapad  # وقتی app/ در sys.path است
except Exception:
    # حالت پشتیبان: بارگذاری مستقیم از مسیر فایل
    import importlib.util
    cand = os.path.join(APP_DIR, "persiapad.py")
    if not os.path.exists(cand):
        cand = os.path.join(BASE_DIR, "persiapad.py")
    spec = importlib.util.spec_from_file_location("persiapad", cand)
    persiapad = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(persiapad)

if __name__ == "__main__":
    persiapad.main()
