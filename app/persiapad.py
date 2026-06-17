#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PersiaPad — یک Notepad سبک با پشتیبانی کامل فارسی
=================================================
ویرایشگر متن تک‌فایلی مبتنی بر Tkinter (بدون وابستگی خارجی) با:
  - پشتیبانی کامل فارسی/انگلیسی (دوجهته)
  - چند تب
  - ذخیرهٔ خودکار لحظه‌ای + بازیابی پس از بستن/کرش
  - ۵ تم (سفید، مشکی، کد، هکری، سایبرپانک)
  - تغییر فونت/سایز/رنگ
  - Ctrl+اسکرول = تغییر سایز فونت
  - Ctrl+D = تکرار خط
  - رنگ‌بندی کد (syntax highlighting) + تشخیص زبان
  - نوار وضعیت با نوع محتوا (text / html / json / ...)
  - تایپ سریع بدون گیر

اجرا: python persiapad.py   (یا دابل‌کلیک روی PersiaPad.pyw روی ویندوز)
"""

import os
import sys
import json
import re
import time
import uuid
import threading
import tkinter as tk
from tkinter import ttk, filedialog, messagebox, font as tkfont

APP_NAME = "PersiaPad"


# ---------------------------------------------------------------------------
#  مسیرهای داده (auto-save / تنظیمات)
# ---------------------------------------------------------------------------
def data_root():
    if sys.platform.startswith("win"):
        base = os.environ.get("LOCALAPPDATA", os.path.expanduser("~"))
    else:
        base = os.path.join(os.path.expanduser("~"), ".local", "share")
    root = os.path.join(base, APP_NAME)
    os.makedirs(os.path.join(root, "session"), exist_ok=True)
    return root


ROOT_DIR = data_root()
SESSION_DIR = os.path.join(ROOT_DIR, "session")
SETTINGS_FILE = os.path.join(ROOT_DIR, "settings.json")


# ---------------------------------------------------------------------------
#  تم‌ها
# ---------------------------------------------------------------------------
THEMES = {
    "light": {
        "name": "تم سفید",
        "bg": "#ffffff", "fg": "#1f1f1f", "chrome": "#f3f3f3", "chrome_fg": "#1f1f1f",
        "accent": "#0066cc", "select": "#add6ff", "caret": "#000000",
        "linenum_bg": "#f3f3f3", "linenum_fg": "#a0a0a0", "current_line": "#f0f4ff",
        "tab_active": "#ffffff", "tab_inactive": "#e8e8e8", "border": "#d0d0d0",
        "syn_keyword": "#0000ff", "syn_string": "#a31515", "syn_comment": "#008000",
        "syn_number": "#098658", "syn_func": "#795e26", "syn_tag": "#800000",
    },
    "dark": {
        "name": "تم مشکی",
        "bg": "#1e1e1e", "fg": "#d4d4d4", "chrome": "#252526", "chrome_fg": "#d4d4d4",
        "accent": "#0e639c", "select": "#264f78", "caret": "#ffffff",
        "linenum_bg": "#252526", "linenum_fg": "#858585", "current_line": "#2a2a2d",
        "tab_active": "#1e1e1e", "tab_inactive": "#2d2d2d", "border": "#3f3f46",
        "syn_keyword": "#569cd6", "syn_string": "#ce9178", "syn_comment": "#6a9955",
        "syn_number": "#b5cea8", "syn_func": "#dcdcaa", "syn_tag": "#569cd6",
    },
    "code": {
        "name": "تم کد",
        "bg": "#0d1117", "fg": "#c9d1d9", "chrome": "#161b22", "chrome_fg": "#c9d1d9",
        "accent": "#1f6feb", "select": "#1f3a5f", "caret": "#58a6ff",
        "linenum_bg": "#161b22", "linenum_fg": "#6e7681", "current_line": "#161b22",
        "tab_active": "#0d1117", "tab_inactive": "#161b22", "border": "#30363d",
        "syn_keyword": "#ff7b72", "syn_string": "#a5d6ff", "syn_comment": "#8b949e",
        "syn_number": "#79c0ff", "syn_func": "#d2a8ff", "syn_tag": "#7ee787",
    },
    "hacker": {
        "name": "تم هکری",
        "bg": "#000000", "fg": "#00ff66", "chrome": "#0a0a0a", "chrome_fg": "#00ff66",
        "accent": "#00ff66", "select": "#0b5c2a", "caret": "#00ff66",
        "linenum_bg": "#0a0a0a", "linenum_fg": "#117711", "current_line": "#0a1a0a",
        "tab_active": "#000000", "tab_inactive": "#0a0a0a", "border": "#0f3d0f",
        "syn_keyword": "#39ff14", "syn_string": "#7CFC00", "syn_comment": "#117711",
        "syn_number": "#00ffcc", "syn_func": "#adff2f", "syn_tag": "#00ff66",
    },
    "cyberpunk": {
        "name": "تم سایبرپانک",
        "bg": "#0b0221", "fg": "#f2f0ff", "chrome": "#160a33", "chrome_fg": "#00f0ff",
        "accent": "#ff2a9d", "select": "#5a1e7a", "caret": "#ff2a9d",
        "linenum_bg": "#160a33", "linenum_fg": "#8a5cff", "current_line": "#1a0a40",
        "tab_active": "#0b0221", "tab_inactive": "#160a33", "border": "#3a1b6b",
        "syn_keyword": "#ff2a9d", "syn_string": "#00f0ff", "syn_comment": "#8a5cff",
        "syn_number": "#ffd400", "syn_func": "#00ffa3", "syn_tag": "#ff6ec7",
    },
}
THEME_ORDER = ["light", "dark", "code", "hacker", "cyberpunk"]


# ---------------------------------------------------------------------------
#  تنظیمات
# ---------------------------------------------------------------------------
DEFAULT_SETTINGS = {
    "theme": "dark",
    "font_family": "Consolas",
    "font_size": 15,
    "text_color": "",          # خالی = رنگ تم
    "word_wrap": True,
    "show_line_numbers": True,
    "rtl": False,
    "restore_session": True,
}


def load_settings():
    try:
        with open(SETTINGS_FILE, "r", encoding="utf-8") as f:
            s = json.load(f)
        merged = dict(DEFAULT_SETTINGS)
        merged.update({k: v for k, v in s.items() if k in DEFAULT_SETTINGS})
        return merged
    except Exception:
        return dict(DEFAULT_SETTINGS)


def save_settings(settings):
    try:
        tmp = SETTINGS_FILE + ".tmp"
        with open(tmp, "w", encoding="utf-8") as f:
            json.dump(settings, f, ensure_ascii=False, indent=2)
        os.replace(tmp, SETTINGS_FILE)
    except Exception:
        pass


# ---------------------------------------------------------------------------
#  تشخیص زبان/نوع محتوا
# ---------------------------------------------------------------------------
EXT_MAP = {
    ".json": ("json", "json"),
    ".html": ("html", "html"), ".htm": ("html", "html"),
    ".xml": ("xml", "xml"), ".xaml": ("xml", "xaml"),
    ".css": ("css", "css"),
    ".php": ("php", "php"),
    ".cs": ("csharp", "c#"),
    ".cpp": ("cpp", "c++"), ".cc": ("cpp", "c++"), ".cxx": ("cpp", "c++"),
    ".hpp": ("cpp", "c++"), ".h": ("cpp", "c++"), ".c": ("cpp", "c"),
    ".py": ("python", "python"), ".pyw": ("python", "python"),
    ".js": ("javascript", "javascript"), ".ts": ("javascript", "typescript"),
    ".java": ("java", "java"),
    ".sql": ("sql", "sql"),
    ".md": ("text", "markdown"),
    ".txt": ("text", "text"), ".log": ("text", "text"),
}


def detect_language(filepath, content):
    """برمی‌گرداند (نوع_رنگ‌بندی, برچسب_نوار_وضعیت)"""
    if filepath:
        ext = os.path.splitext(filepath)[1].lower()
        if ext in EXT_MAP and ext not in (".txt", ".log"):
            return EXT_MAP[ext]
    return sniff_content(content)


def sniff_content(content):
    if not content or not content.strip():
        return ("text", "text")
    sample = content[:4000]
    t = sample.lstrip()

    # JSON
    if t[:1] in "{[":
        try:
            json.loads(t)
            return ("json", "json")
        except Exception:
            if re.search(r'"[^"]+"\s*:', t):
                return ("json", "json")

    has_html = re.search(
        r"<\s*(!doctype|html|head|body|div|span|p|a|table|script|style|h[1-6]|ul|ol|li|img|br|meta|link)\b",
        sample, re.IGNORECASE)
    if has_html:
        no_tags = re.sub(r"<[^>]+>", " ", sample)
        no_tags = re.sub(r"\s+", " ", no_tags).strip()
        if len(no_tags) >= 3 and re.search(r"\w{2,}", no_tags):
            return ("html", "text, html")
        return ("html", "html")

    if re.search(r"<\?php", sample):
        return ("php", "php")
    if re.search(r"[.#]?[\w\-]+\s*\{[^}]*:[^}]*;", sample, re.DOTALL):
        return ("css", "css")
    if re.search(r"^\s*(def |class |import |from \w+ import)", sample, re.MULTILINE):
        return ("python", "python")
    if re.search(r"\b(using\s+System|namespace\s+\w+|public\s+class|Console\.WriteLine)\b", sample):
        return ("csharp", "c#")
    if re.search(r"#include\s*<|std::|int\s+main\s*\(", sample):
        return ("cpp", "c++")
    return ("text", "text")


# ---------------------------------------------------------------------------
#  الگوهای رنگ‌بندی نحوی (syntax) برای زبان‌های مختلف
# ---------------------------------------------------------------------------
KEYWORDS = {
    "python": r"\b(def|class|import|from|as|return|if|elif|else|for|while|try|except|finally|with|lambda|yield|global|nonlocal|pass|break|continue|raise|in|is|not|and|or|None|True|False|self)\b",
    "javascript": r"\b(var|let|const|function|return|if|else|for|while|do|switch|case|break|continue|new|this|typeof|instanceof|class|extends|super|import|export|from|async|await|try|catch|finally|throw|null|undefined|true|false)\b",
    "csharp": r"\b(using|namespace|class|public|private|protected|internal|static|void|int|string|bool|double|float|var|new|return|if|else|for|foreach|while|switch|case|break|continue|try|catch|finally|throw|null|true|false|this|base|override|virtual|abstract|interface|enum|struct)\b",
    "cpp": r"\b(int|float|double|char|bool|void|long|short|unsigned|signed|const|static|struct|class|public|private|protected|return|if|else|for|while|do|switch|case|break|continue|new|delete|namespace|using|template|typename|virtual|override|nullptr|true|false|include|define)\b",
    "php": r"\b(function|class|public|private|protected|static|return|if|else|elseif|foreach|for|while|do|switch|case|break|continue|new|echo|print|require|include|namespace|use|try|catch|finally|throw|null|true|false|array|var|const)\b",
    "java": r"\b(public|private|protected|class|interface|extends|implements|static|final|void|int|long|double|float|boolean|char|String|new|return|if|else|for|while|do|switch|case|break|continue|try|catch|finally|throw|throws|import|package|null|true|false|this|super)\b",
    "css": r"([.#]?[\w\-]+)(?=\s*\{)|\b(color|background|margin|padding|border|font|width|height|display|position|top|left|right|bottom|flex|grid)\b",
    "sql": r"\b(SELECT|FROM|WHERE|INSERT|INTO|VALUES|UPDATE|SET|DELETE|CREATE|TABLE|DROP|ALTER|JOIN|LEFT|RIGHT|INNER|OUTER|ON|AND|OR|NOT|NULL|ORDER|BY|GROUP|HAVING|LIMIT|AS|DISTINCT)\b",
}


def get_syntax_rules(lang):
    """فهرستی از (tag, regex) برای رنگ‌بندی برمی‌گرداند، به ترتیب اولویت."""
    rules = []
    if lang in ("html", "xml"):
        rules.append(("syn_comment", r"<!--[\s\S]*?-->"))
        rules.append(("syn_string", r'"[^"]*"|\'[^\']*\''))
        rules.append(("syn_tag", r"</?[\w:-]+|/?>"))
        return rules
    if lang == "json":
        rules.append(("syn_tag", r'"(?:[^"\\]|\\.)*"\s*:'))
        rules.append(("syn_string", r'"(?:[^"\\]|\\.)*"'))
        rules.append(("syn_number", r"\b-?\d+\.?\d*\b"))
        rules.append(("syn_keyword", r"\b(true|false|null)\b"))
        return rules
    if lang == "css":
        rules.append(("syn_comment", r"/\*[\s\S]*?\*/"))
        rules.append(("syn_string", r'"[^"]*"|\'[^\']*\''))
        rules.append(("syn_number", r"\b-?\d+\.?\d*(px|em|rem|%|pt|vh|vw)?\b"))
        rules.append(("syn_keyword", KEYWORDS["css"]))
        return rules
    if lang in KEYWORDS:
        # کامنت‌ها
        if lang == "python":
            rules.append(("syn_comment", r"#.*$"))
            rules.append(("syn_string", r'"""[\s\S]*?"""|\'\'\'[\s\S]*?\'\'\'|"(?:[^"\\]|\\.)*"|\'(?:[^\'\\]|\\.)*\''))
        elif lang == "sql":
            rules.append(("syn_comment", r"--.*$"))
            rules.append(("syn_string", r"'(?:[^'\\]|\\.)*'"))
        else:
            rules.append(("syn_comment", r"//.*$|/\*[\s\S]*?\*/"))
            rules.append(("syn_string", r'"(?:[^"\\]|\\.)*"|\'(?:[^\'\\]|\\.)*\''))
        rules.append(("syn_number", r"\b-?\d+\.?\d*\b"))
        rules.append(("syn_func", r"\b([A-Za-z_]\w*)\s*(?=\()"))
        rules.append(("syn_keyword", KEYWORDS[lang]))
        return rules
    return rules


# ---------------------------------------------------------------------------
#  یک تب ویرایشگر (متن + شماره خط + auto-save + رنگ‌بندی)
# ---------------------------------------------------------------------------
class EditorTab:
    def __init__(self, app, tab_id=None, filepath=None, restored_text=None):
        self.app = app
        self.id = tab_id or uuid.uuid4().hex
        self.filepath = filepath
        self.dirty = False
        self.encoding = "utf-8"
        self._hl_after = None
        self._save_after = None
        self.lang = "text"
        self.status_label = "text"

        # کانتینر
        self.frame = tk.Frame(app.notebook)

        # شماره خط (Text widget کم‌حجم)
        self.linenumbers = tk.Text(
            self.frame, width=5, padx=6, takefocus=0, border=0,
            state="disabled", wrap="none", cursor="arrow",
        )
        # ویرایشگر اصلی
        self.text = tk.Text(
            self.frame, wrap="word", undo=True, maxundo=-1,
            border=0, padx=8, pady=4, insertwidth=2,
            autoseparators=True, spacing1=1, spacing3=1,
        )
        # اسکرول‌بار مشترک
        self.scroll = ttk.Scrollbar(self.frame, orient="vertical",
                                    command=self._on_scrollbar)
        self.text.configure(yscrollcommand=self._on_textscroll)

        self.linenumbers.pack(side="left", fill="y")
        self.scroll.pack(side="right", fill="y")
        self.text.pack(side="right", fill="both", expand=True)

        if restored_text is not None:
            self.text.insert("1.0", restored_text)
            self.dirty = True
        elif filepath and os.path.exists(filepath):
            self._load_file(filepath)

        self._bind_events()
        self.app.apply_theme_to_tab(self)
        self.text.edit_modified(False)
        self.refresh_linenumbers()
        self.schedule_highlight(immediate=True)

    # ---- بارگذاری/ذخیره ------------------------------------------------
    def _load_file(self, path):
        data = None
        for enc in ("utf-8-sig", "utf-8", "utf-16", "cp1256", "latin-1"):
            try:
                with open(path, "r", encoding=enc) as f:
                    data = f.read()
                self.encoding = "utf-8" if enc.startswith("utf-8") else enc
                break
            except (UnicodeError, UnicodeDecodeError):
                continue
        if data is None:
            with open(path, "rb") as f:
                data = f.read().decode("utf-8", errors="replace")
        self.text.insert("1.0", data)
        self.filepath = path
        self.dirty = False

    def save(self, path=None):
        path = path or self.filepath
        if not path:
            return False
        content = self.text.get("1.0", "end-1c")
        tmp = path + ".pptmp"
        with open(tmp, "w", encoding="utf-8", newline="") as f:
            f.write(content)
        os.replace(tmp, path)
        self.filepath = path
        self.dirty = False
        self.text.edit_modified(False)
        self.clear_snapshot()
        self.app.update_tab_title(self)
        self.app.update_status()
        return True

    # ---- auto-save (snapshot) -----------------------------------------
    def snapshot_path(self):
        return os.path.join(SESSION_DIR, self.id + ".snapshot")

    def write_snapshot(self):
        try:
            snap = {
                "filepath": self.filepath,
                "text": self.text.get("1.0", "end-1c"),
                "caret": self.text.index("insert"),
            }
            tmp = self.snapshot_path() + ".tmp"
            with open(tmp, "w", encoding="utf-8") as f:
                json.dump(snap, f, ensure_ascii=False)
            os.replace(tmp, self.snapshot_path())
        except Exception:
            pass

    def clear_snapshot(self):
        try:
            p = self.snapshot_path()
            if os.path.exists(p):
                os.remove(p)
        except Exception:
            pass

    # ---- رویدادها ------------------------------------------------------
    def _bind_events(self):
        self.text.bind("<<Modified>>", self._on_modified)
        self.text.bind("<KeyRelease>", self._on_keyrelease)
        self.text.bind("<ButtonRelease-1>", lambda e: self.app.update_status())
        self.text.bind("<Double-Button-1>", self._on_double_click)
        # Ctrl+D = تکرار خط
        self.text.bind("<Control-d>", self._duplicate_line)
        self.text.bind("<Control-D>", self._duplicate_line)
        # Ctrl + اسکرول = تغییر سایز فونت
        self.text.bind("<Control-MouseWheel>", self._ctrl_wheel)        # ویندوز/مک
        self.text.bind("<Control-Button-4>", lambda e: self.app.adjust_font(1))   # لینوکس
        self.text.bind("<Control-Button-5>", lambda e: self.app.adjust_font(-1))  # لینوکس
        # هماهنگی اسکرول شماره خط
        self.text.bind("<MouseWheel>", self._sync_wheel)
        self.text.bind("<Configure>", lambda e: self.refresh_linenumbers())

    def _on_modified(self, event=None):
        if self.text.edit_modified():
            self.dirty = True
            self.text.edit_modified(False)
            self.app.update_tab_title(self)

    def _on_keyrelease(self, event=None):
        self.refresh_linenumbers()
        self.app.update_status()
        self.schedule_highlight()
        self.schedule_save()

    def _on_double_click(self, event):
        # انتخاب دقیق کلمه شامل حروف فارسی و نیم‌فاصله
        idx = self.text.index("@%d,%d" % (event.x, event.y))
        line, col = map(int, idx.split("."))
        line_text = self.text.get("%d.0" % line, "%d.end" % line)
        if not line_text:
            return
        col = min(col, len(line_text) - 1) if line_text else 0

        def is_word(ch):
            return ch.isalnum() or ch in ("_", "\u200c", "\u200d")

        if col >= len(line_text) or not is_word(line_text[col]):
            if col > 0 and is_word(line_text[col - 1]):
                col -= 1
            else:
                return
        start = col
        while start > 0 and is_word(line_text[start - 1]):
            start -= 1
        end = col
        while end < len(line_text) and is_word(line_text[end]):
            end += 1
        self.text.tag_remove("sel", "1.0", "end")
        self.text.tag_add("sel", "%d.%d" % (line, start), "%d.%d" % (line, end))
        self.text.mark_set("insert", "%d.%d" % (line, end))
        self.app.update_status()
        return "break"

    def _duplicate_line(self, event=None):
        try:
            if self.text.tag_ranges("sel"):
                sel = self.text.get("sel.first", "sel.last")
                self.text.insert("sel.last", sel)
            else:
                line = self.text.index("insert").split(".")[0]
                line_text = self.text.get("%s.0" % line, "%s.end" % line)
                self.text.insert("%s.end" % line, "\n" + line_text)
        finally:
            self.refresh_linenumbers()
            self.schedule_highlight()
            self.schedule_save()
        return "break"

    def _ctrl_wheel(self, event):
        self.app.adjust_font(1 if event.delta > 0 else -1)
        return "break"

    def _sync_wheel(self, event=None):
        self.frame.after_idle(self.refresh_linenumbers)

    # ---- اسکرول هماهنگ -------------------------------------------------
    def _on_scrollbar(self, *args):
        self.text.yview(*args)
        self.linenumbers.yview(*args)

    def _on_textscroll(self, first, last):
        self.scroll.set(first, last)
        self.linenumbers.yview_moveto(first)

    # ---- شماره خط ------------------------------------------------------
    def refresh_linenumbers(self):
        if not self.app.settings["show_line_numbers"]:
            if self.linenumbers.winfo_ismapped():
                self.linenumbers.pack_forget()
            return
        if not self.linenumbers.winfo_ismapped():
            self.linenumbers.pack(side="left", fill="y", before=self.text)
        total = int(self.text.index("end-1c").split(".")[0])
        nums = "\n".join(str(i) for i in range(1, total + 1))
        self.linenumbers.config(state="normal")
        self.linenumbers.delete("1.0", "end")
        self.linenumbers.insert("1.0", nums)
        self.linenumbers.tag_configure("right", justify="right")
        self.linenumbers.tag_add("right", "1.0", "end")
        self.linenumbers.config(state="disabled")
        self.linenumbers.yview_moveto(self.text.yview()[0])

    # ---- رنگ‌بندی نحوی --------------------------------------------------
    def schedule_save(self):
        if self._save_after:
            self.frame.after_cancel(self._save_after)
        self._save_after = self.frame.after(400, self.write_snapshot)

    def schedule_highlight(self, immediate=False):
        if self._hl_after:
            self.frame.after_cancel(self._hl_after)
        delay = 0 if immediate else 350
        self._hl_after = self.frame.after(delay, self.highlight)

    def highlight(self):
        content = self.text.get("1.0", "end-1c")
        lang, label = detect_language(self.filepath, content)
        self.lang = lang
        self.status_label = label
        # پاک‌کردن تگ‌های قبلی
        for tag in ("syn_keyword", "syn_string", "syn_comment",
                    "syn_number", "syn_func", "syn_tag"):
            self.text.tag_remove(tag, "1.0", "end")
        if lang == "text":
            self.app.update_status()
            return
        rules = get_syntax_rules(lang)
        # فقط بخش قابل‌مشاهده + کمی حاشیه را رنگ می‌کنیم تا سریع بماند
        try:
            first = self.text.index("@0,0")
            last = self.text.index("@0,%d" % self.text.winfo_height())
            start_line = max(1, int(first.split(".")[0]) - 50)
            end_line = int(last.split(".")[0]) + 50
        except Exception:
            start_line, end_line = 1, 400
        region = self.text.get("%d.0" % start_line, "%d.end" % end_line)
        base = start_line
        for tag, pattern in rules:
            try:
                for m in re.finditer(pattern, region, re.MULTILINE):
                    s, e = m.span()
                    si = "%d.0+%dc" % (base, s)
                    ei = "%d.0+%dc" % (base, e)
                    self.text.tag_add(tag, si, ei)
            except re.error:
                continue
        self.app.update_status()


# ---------------------------------------------------------------------------
#  برنامهٔ اصلی
# ---------------------------------------------------------------------------
class PersiaPadApp:
    def __init__(self, root, open_files=None):
        self.root = root
        self.settings = load_settings()
        self.tabs = []  # list of EditorTab

        root.title(APP_NAME)
        root.geometry("1040x680")
        try:
            root.iconphoto(True, _make_icon())
        except Exception:
            pass

        self._build_menu()
        self._build_notebook()
        self._build_statusbar()

        self.apply_theme()

        # باز کردن فایل‌های ورودی / بازیابی نشست / تب خالی
        opened = False
        for fp in (open_files or []):
            if os.path.exists(fp):
                self.add_tab(filepath=fp); opened = True
        if self.settings["restore_session"]:
            opened = self._restore_session() or opened
        if not opened:
            self.add_tab()

        root.protocol("WM_DELETE_WINDOW", self.on_close)
        self._bind_global_keys()
        self.update_status()

    # ---- ساخت رابط ------------------------------------------------------
    def _build_menu(self):
        self.menubar = tk.Menu(self.root)

        m_file = tk.Menu(self.menubar, tearoff=0)
        m_file.add_command(label="جدید", accelerator="Ctrl+N", command=self.new_tab)
        m_file.add_command(label="باز کردن...", accelerator="Ctrl+O", command=self.open_file)
        m_file.add_separator()
        m_file.add_command(label="ذخیره", accelerator="Ctrl+S", command=self.save_file)
        m_file.add_command(label="ذخیره به‌نام...", accelerator="Ctrl+Shift+S", command=self.save_as)
        m_file.add_separator()
        m_file.add_command(label="بستن تب", accelerator="Ctrl+W", command=self.close_current_tab)
        m_file.add_command(label="خروج", command=self.on_close)
        self.menubar.add_cascade(label="فایل", menu=m_file)

        m_edit = tk.Menu(self.menubar, tearoff=0)
        m_edit.add_command(label="واگرد", accelerator="Ctrl+Z", command=lambda: self._edit("undo"))
        m_edit.add_command(label="ازنو", accelerator="Ctrl+Y", command=lambda: self._edit("redo"))
        m_edit.add_separator()
        m_edit.add_command(label="برش", accelerator="Ctrl+X", command=lambda: self._event("<<Cut>>"))
        m_edit.add_command(label="کپی", accelerator="Ctrl+C", command=lambda: self._event("<<Copy>>"))
        m_edit.add_command(label="چسباندن", accelerator="Ctrl+V", command=lambda: self._event("<<Paste>>"))
        m_edit.add_separator()
        m_edit.add_command(label="تکرار خط", accelerator="Ctrl+D",
                           command=lambda: self.current() and self.current()._duplicate_line())
        m_edit.add_command(label="انتخاب همه", accelerator="Ctrl+A", command=self.select_all)
        m_edit.add_separator()
        m_edit.add_command(label="یافتن/جایگزینی...", accelerator="Ctrl+F", command=self.open_find)
        self.menubar.add_cascade(label="ویرایش", menu=m_edit)

        m_view = tk.Menu(self.menubar, tearoff=0)
        self.var_wrap = tk.BooleanVar(value=self.settings["word_wrap"])
        self.var_lines = tk.BooleanVar(value=self.settings["show_line_numbers"])
        self.var_rtl = tk.BooleanVar(value=self.settings["rtl"])
        m_view.add_checkbutton(label="شکستن خطوط", variable=self.var_wrap, command=self.toggle_wrap)
        m_view.add_checkbutton(label="شماره خطوط", variable=self.var_lines, command=self.toggle_lines)
        m_view.add_checkbutton(label="جهت راست‌به‌چپ (فارسی)", variable=self.var_rtl, command=self.toggle_rtl)
        m_view.add_separator()
        m_view.add_command(label="بزرگ‌نمایی فونت", accelerator="Ctrl++", command=lambda: self.adjust_font(1))
        m_view.add_command(label="کوچک‌نمایی فونت", accelerator="Ctrl+-", command=lambda: self.adjust_font(-1))
        self.menubar.add_cascade(label="نما", menu=m_view)

        m_theme = tk.Menu(self.menubar, tearoff=0)
        self.var_theme = tk.StringVar(value=self.settings["theme"])
        for key in THEME_ORDER:
            m_theme.add_radiobutton(label=THEMES[key]["name"], value=key,
                                    variable=self.var_theme,
                                    command=lambda k=key: self.set_theme(k))
        self.menubar.add_cascade(label="تم", menu=m_theme)

        m_tools = tk.Menu(self.menubar, tearoff=0)
        m_tools.add_command(label="تنظیمات...", command=self.open_settings)
        self.menubar.add_cascade(label="ابزار", menu=m_tools)

        self.root.config(menu=self.menubar)

    def _build_notebook(self):
        self.notebook = ttk.Notebook(self.root)
        self.notebook.pack(fill="both", expand=True)
        self.notebook.bind("<<NotebookTabChanged>>", lambda e: self.update_status())

    def _build_statusbar(self):
        self.status = tk.Frame(self.root, height=24)
        self.status.pack(fill="x", side="bottom")
        self.lbl_msg = tk.Label(self.status, text="آماده", anchor="w")
        self.lbl_caret = tk.Label(self.status, text="خط 1، ستون 1")
        self.lbl_len = tk.Label(self.status, text="0 نویسه")
        self.lbl_lang = tk.Label(self.status, text="text", padx=10)
        self.lbl_msg.pack(side="left", padx=8)
        self.lbl_lang.pack(side="right", padx=4)
        self.lbl_len.pack(side="right", padx=8)
        self.lbl_caret.pack(side="right", padx=8)

    def _bind_global_keys(self):
        self.root.bind("<Control-n>", lambda e: (self.new_tab(), "break")[1])
        self.root.bind("<Control-o>", lambda e: (self.open_file(), "break")[1])
        self.root.bind("<Control-s>", lambda e: (self.save_file(), "break")[1])
        self.root.bind("<Control-S>", lambda e: (self.save_as(), "break")[1])
        self.root.bind("<Control-w>", lambda e: (self.close_current_tab(), "break")[1])
        self.root.bind("<Control-f>", lambda e: (self.open_find(), "break")[1])
        self.root.bind("<Control-a>", lambda e: (self.select_all(), "break")[1])
        self.root.bind("<Control-equal>", lambda e: self.adjust_font(1))
        self.root.bind("<Control-plus>", lambda e: self.adjust_font(1))
        self.root.bind("<Control-minus>", lambda e: self.adjust_font(-1))

    # ---- کمکی -----------------------------------------------------------
    def current(self):
        if not self.tabs:
            return None
        try:
            idx = self.notebook.index(self.notebook.select())
            return self.tabs[idx]
        except Exception:
            return self.tabs[-1] if self.tabs else None

    def _edit(self, op):
        t = self.current()
        if not t:
            return
        try:
            getattr(t.text, "edit_" + op)()
        except Exception:
            pass

    def _event(self, ev):
        t = self.current()
        if t:
            t.text.event_generate(ev)

    def select_all(self):
        t = self.current()
        if t:
            t.text.tag_add("sel", "1.0", "end-1c")
        return "break"

    # ---- مدیریت تب ------------------------------------------------------
    def add_tab(self, tab_id=None, filepath=None, restored_text=None):
        tab = EditorTab(self, tab_id=tab_id, filepath=filepath, restored_text=restored_text)
        self.tabs.append(tab)
        self.notebook.add(tab.frame, text=self._title_for(tab))
        self.notebook.select(tab.frame)
        tab.text.focus_set()
        self.update_tab_title(tab)
        return tab

    def new_tab(self):
        self.add_tab()

    def _title_for(self, tab):
        name = os.path.basename(tab.filepath) if tab.filepath else "بدون عنوان"
        return ("● " if tab.dirty else "") + name

    def update_tab_title(self, tab):
        try:
            self.notebook.tab(tab.frame, text=self._title_for(tab))
            t = self.current()
            if t is tab:
                self.root.title("%s — %s" % (
                    os.path.basename(tab.filepath) if tab.filepath else "بدون عنوان", APP_NAME))
        except Exception:
            pass

    def close_current_tab(self):
        t = self.current()
        if not t:
            return
        if t.dirty and t.text.get("1.0", "end-1c").strip():
            # متن ذخیره‌نشده در snapshot می‌ماند؛ فقط هشدار ملایم
            t.write_snapshot()
        else:
            t.clear_snapshot()
        idx = self.tabs.index(t)
        self.notebook.forget(t.frame)
        self.tabs.pop(idx)
        if not self.tabs:
            self.add_tab()

    # ---- فایل -----------------------------------------------------------
    def open_file(self):
        paths = filedialog.askopenfilenames(
            title="باز کردن فایل",
            filetypes=[("همه فایل‌ها", "*.*"), ("متنی", "*.txt"),
                       ("کد", "*.py *.js *.json *.html *.css *.php *.cs *.cpp *.c *.xml")])
        for p in paths:
            # اگر باز است فقط فعالش کن
            existing = next((t for t in self.tabs if t.filepath == p), None)
            if existing:
                self.notebook.select(existing.frame)
            else:
                self.add_tab(filepath=p)

    def save_file(self):
        t = self.current()
        if not t:
            return
        if not t.filepath:
            return self.save_as()
        try:
            t.save()
            self.lbl_msg.config(text="ذخیره شد ✔")
        except Exception as ex:
            messagebox.showerror(APP_NAME, "خطا در ذخیره:\n" + str(ex))

    def save_as(self):
        t = self.current()
        if not t:
            return
        p = filedialog.asksaveasfilename(
            title="ذخیره به‌نام", defaultextension=".txt",
            filetypes=[("متنی", "*.txt"), ("همه فایل‌ها", "*.*")],
            initialfile=os.path.basename(t.filepath) if t.filepath else "بدون عنوان.txt")
        if p:
            try:
                t.save(p)
                t.schedule_highlight(immediate=True)
                self.lbl_msg.config(text="ذخیره شد ✔")
            except Exception as ex:
                messagebox.showerror(APP_NAME, "خطا در ذخیره:\n" + str(ex))

    # ---- نشست (auto-restore) -------------------------------------------
    def _restore_session(self):
        restored = False
        try:
            for fn in os.listdir(SESSION_DIR):
                if not fn.endswith(".snapshot"):
                    continue
                path = os.path.join(SESSION_DIR, fn)
                try:
                    with open(path, "r", encoding="utf-8") as f:
                        snap = json.load(f)
                except Exception:
                    continue
                tab_id = fn[:-len(".snapshot")]
                self.add_tab(tab_id=tab_id, filepath=snap.get("filepath"),
                             restored_text=snap.get("text", ""))
                restored = True
        except Exception:
            pass
        return restored

    # ---- تم و فونت ------------------------------------------------------
    def theme(self):
        return THEMES[self.settings["theme"]]

    def apply_theme_to_tab(self, tab):
        th = self.theme()
        fg = self.settings["text_color"] or th["fg"]
        fnt = (self.settings["font_family"], self.settings["font_size"])
        tab.text.configure(
            bg=th["bg"], fg=fg, insertbackground=th["caret"],
            selectbackground=th["select"], selectforeground=th["fg"],
            font=fnt, wrap=("word" if self.settings["word_wrap"] else "none"),
        )
        tab.linenumbers.configure(
            bg=th["linenum_bg"], fg=th["linenum_fg"], font=fnt,
            selectbackground=th["linenum_bg"],
        )
        # رنگ تگ‌های syntax
        for tag in ("syn_keyword", "syn_string", "syn_comment",
                    "syn_number", "syn_func", "syn_tag"):
            tab.text.tag_configure(tag, foreground=th[tag])
        # جهت متن
        try:
            tab.text.configure(state="normal")
        except Exception:
            pass

    def apply_theme(self):
        th = self.theme()
        self.root.configure(bg=th["bg"])
        # نوار وضعیت
        self.status.configure(bg=th["accent"])
        for w in (self.lbl_msg, self.lbl_caret, self.lbl_len, self.lbl_lang):
            w.configure(bg=th["accent"], fg="white")
        # ttk استایل
        style = ttk.Style()
        try:
            style.theme_use("clam")
        except Exception:
            pass
        style.configure("TNotebook", background=th["chrome"], borderwidth=0)
        style.configure("TNotebook.Tab", background=th["tab_inactive"],
                        foreground=th["chrome_fg"], padding=(14, 6), borderwidth=0)
        style.map("TNotebook.Tab",
                  background=[("selected", th["tab_active"])],
                  foreground=[("selected", th["chrome_fg"])])
        for tab in self.tabs:
            self.apply_theme_to_tab(tab)
            tab.schedule_highlight(immediate=True)

    def set_theme(self, key):
        self.settings["theme"] = key
        self.var_theme.set(key)
        save_settings(self.settings)
        self.apply_theme()

    def adjust_font(self, direction):
        size = max(6, min(96, self.settings["font_size"] + direction))
        self.settings["font_size"] = size
        save_settings(self.settings)
        for tab in self.tabs:
            self.apply_theme_to_tab(tab)
            tab.refresh_linenumbers()
        self.lbl_msg.config(text="اندازه فونت: %d" % size)
        return "break"

    def toggle_wrap(self):
        self.settings["word_wrap"] = self.var_wrap.get()
        save_settings(self.settings)
        for tab in self.tabs:
            tab.text.configure(wrap=("word" if self.settings["word_wrap"] else "none"))

    def toggle_lines(self):
        self.settings["show_line_numbers"] = self.var_lines.get()
        save_settings(self.settings)
        for tab in self.tabs:
            tab.refresh_linenumbers()

    def toggle_rtl(self):
        self.settings["rtl"] = self.var_rtl.get()
        save_settings(self.settings)
        # Tkinter Text جهت پاراگراف ندارد؛ اما تراز راست را شبیه‌سازی می‌کنیم
        for tab in self.tabs:
            try:
                tab.text.tag_configure("rtl", justify=("right" if self.settings["rtl"] else "left"))
                tab.text.tag_add("rtl", "1.0", "end")
            except Exception:
                pass

    # ---- نوار وضعیت ----------------------------------------------------
    def update_status(self):
        t = self.current()
        if not t:
            return
        try:
            caret = t.text.index("insert")
            line, col = caret.split(".")
            self.lbl_caret.config(text="خط %s، ستون %d" % (line, int(col) + 1))
            content = t.text.get("1.0", "end-1c")
            n = len(content)
            if t.text.tag_ranges("sel"):
                sel = len(t.text.get("sel.first", "sel.last"))
                self.lbl_len.config(text="%d نویسه (%d انتخاب)" % (n, sel))
            else:
                self.lbl_len.config(text="%d نویسه" % n)
            self.lbl_lang.config(text=t.status_label)
        except Exception:
            pass

    # ---- تنظیمات --------------------------------------------------------
    def open_settings(self):
        win = tk.Toplevel(self.root)
        win.title("تنظیمات " + APP_NAME)
        win.geometry("420x360")
        win.transient(self.root)
        th = self.theme()
        win.configure(bg=th["chrome"])

        def row(label):
            f = tk.Frame(win, bg=th["chrome"]); f.pack(fill="x", padx=16, pady=6)
            tk.Label(f, text=label, bg=th["chrome"], fg=th["chrome_fg"]).pack(anchor="e")
            return f

        # فونت
        f1 = row("فونت")
        fonts = sorted(set(["Consolas", "Courier New", "Tahoma", "Segoe UI",
                            "Vazirmatn", "Sahel"] + list(tkfont.families())))
        cb_font = ttk.Combobox(f1, values=fonts)
        cb_font.set(self.settings["font_family"]); cb_font.pack(fill="x")

        # سایز
        f2 = row("اندازهٔ فونت")
        sz = tk.IntVar(value=self.settings["font_size"])
        tk.Scale(f2, from_=6, to=48, orient="horizontal", variable=sz,
                 bg=th["chrome"], fg=th["chrome_fg"], highlightthickness=0).pack(fill="x")

        # رنگ متن
        f3 = row("رنگ متن (اختیاری، مثل #FF8800 — خالی=رنگ تم)")
        ent_color = tk.Entry(f3); ent_color.insert(0, self.settings["text_color"]); ent_color.pack(fill="x")

        def apply():
            self.settings["font_family"] = cb_font.get() or "Consolas"
            self.settings["font_size"] = int(sz.get())
            self.settings["text_color"] = ent_color.get().strip()
            save_settings(self.settings)
            for tab in self.tabs:
                self.apply_theme_to_tab(tab); tab.refresh_linenumbers()
            win.destroy()

        bar = tk.Frame(win, bg=th["chrome"]); bar.pack(fill="x", padx=16, pady=16)
        tk.Button(bar, text="ذخیره", command=apply, width=10).pack(side="left", padx=4)
        tk.Button(bar, text="انصراف", command=win.destroy, width=10).pack(side="left")

    # ---- یافتن/جایگزینی -------------------------------------------------
    def open_find(self):
        t = self.current()
        if not t:
            return
        win = tk.Toplevel(self.root)
        win.title("یافتن و جایگزینی")
        win.geometry("380x180")
        win.transient(self.root)
        th = self.theme(); win.configure(bg=th["chrome"])

        seed = ""
        if t.text.tag_ranges("sel"):
            seed = t.text.get("sel.first", "sel.last")

        tk.Label(win, text="متن جستجو:", bg=th["chrome"], fg=th["chrome_fg"]).pack(anchor="e", padx=12, pady=(12, 0))
        e_find = tk.Entry(win); e_find.insert(0, seed); e_find.pack(fill="x", padx=12)
        tk.Label(win, text="جایگزین با:", bg=th["chrome"], fg=th["chrome_fg"]).pack(anchor="e", padx=12, pady=(8, 0))
        e_repl = tk.Entry(win); e_repl.pack(fill="x", padx=12)

        def do_find():
            needle = e_find.get()
            if not needle:
                return
            start = t.text.index("insert")
            pos = t.text.search(needle, start, stopindex="end")
            if not pos:
                pos = t.text.search(needle, "1.0", stopindex="end")
            if pos:
                end = "%s+%dc" % (pos, len(needle))
                t.text.tag_remove("sel", "1.0", "end")
                t.text.tag_add("sel", pos, end)
                t.text.mark_set("insert", end)
                t.text.see(pos)
                self.update_status()
            else:
                self.lbl_msg.config(text="یافت نشد")

        def do_replace_all():
            needle = e_find.get(); repl = e_repl.get()
            if not needle:
                return
            content = t.text.get("1.0", "end-1c")
            count = content.count(needle)
            if count:
                t.text.delete("1.0", "end")
                t.text.insert("1.0", content.replace(needle, repl))
                t.refresh_linenumbers(); t.schedule_highlight(immediate=True)
                t.schedule_save()
                self.lbl_msg.config(text="%d مورد جایگزین شد ✔" % count)
            else:
                self.lbl_msg.config(text="یافت نشد")

        bar = tk.Frame(win, bg=th["chrome"]); bar.pack(fill="x", padx=12, pady=12)
        tk.Button(bar, text="یافتن بعدی", command=do_find, width=10).pack(side="left", padx=4)
        tk.Button(bar, text="جایگزینی همه", command=do_replace_all, width=12).pack(side="left", padx=4)
        e_find.focus_set()

    # ---- بستن برنامه ----------------------------------------------------
    def on_close(self):
        for tab in self.tabs:
            try:
                if tab.dirty and tab.text.get("1.0", "end-1c").strip():
                    tab.write_snapshot()
                else:
                    tab.clear_snapshot()
            except Exception:
                pass
        save_settings(self.settings)
        self.root.destroy()


# ---------------------------------------------------------------------------
#  آیکون ساده (تولید در حافظه)
# ---------------------------------------------------------------------------
def _make_icon():
    # یک آیکون ۳۲x۳۲ ساده با PhotoImage (بدون نیاز به فایل)
    img = tk.PhotoImage(width=32, height=32)
    img.put("#0e639c", to=(0, 0, 32, 32))
    img.put("#f5f5f5", to=(6, 5, 26, 27))
    for y in (9, 14, 19):
        img.put("#0e639c", to=(9, y, 23, y + 2))
    img.put("#0e639c", to=(9, 24, 18, 26))
    return img


# ---------------------------------------------------------------------------
#  main
# ---------------------------------------------------------------------------
def main():
    open_files = [a for a in sys.argv[1:] if os.path.exists(a)]
    root = tk.Tk()
    PersiaPadApp(root, open_files=open_files)
    root.mainloop()


if __name__ == "__main__":
    main()
