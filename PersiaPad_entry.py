#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
نقطه‌ی ورود برای ساختن exe با PyInstaller.

این فایل کد اصلی را به‌صورت یک ماژول معمولی import می‌کند تا PyInstaller
بتواند تمام وابستگی‌ها (uuid, threading, tkinter و ...) را خودکار شناسایی کند.
"""
import os
import sys

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
APP_DIR = os.path.join(BASE_DIR, "app")
for p in (APP_DIR, BASE_DIR):
    if os.path.isdir(p) and p not in sys.path:
        sys.path.insert(0, p)

import persiapad  # noqa: E402  (app/persiapad.py)

if __name__ == "__main__":
    persiapad.main()
