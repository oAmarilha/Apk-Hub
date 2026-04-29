from __future__ import annotations

import os
import platform
import subprocess
import sys
from datetime import datetime
from pathlib import Path

APP_NAME = "Apk Hub"
PACKAGE_ROOT = Path(__file__).resolve().parent
ASSETS_DIR = PACKAGE_ROOT / "assets"
IMAGES_DIR = ASSETS_DIR / "images"
VENDOR_DIR = PACKAGE_ROOT / "vendor"
WINDOWS_ADB_DIR = VENDOR_DIR / "adb" / "windows"
ICON_PATH = ASSETS_DIR / "bobby.ico"
PNG_ICON_PATH = ASSETS_DIR / "icon.png"


def documents_dir() -> Path:
    docs = Path.home() / "Documents"
    return docs if docs.exists() else Path.home()


def log_dir() -> Path:
    path = documents_dir() / "ApkHub" / "Log"
    path.mkdir(parents=True, exist_ok=True)
    return path


def timestamp() -> str:
    return datetime.now().strftime("%Y-%m-%d_%H-%M-%S")


def open_path(path: Path) -> None:
    target = str(path)
    system = platform.system().lower()
    if system == "windows":
        os.startfile(target)  # type: ignore[attr-defined]
    elif system == "darwin":
        subprocess.Popen(["open", target])
    else:
        subprocess.Popen(["xdg-open", target])
