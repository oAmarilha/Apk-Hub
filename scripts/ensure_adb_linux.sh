#!/usr/bin/env bash
set -euo pipefail

if command -v adb >/dev/null 2>&1; then
  echo "adb found: $(command -v adb)"
  exit 0
fi

echo "adb not found. attempting installation..."

if command -v apt-get >/dev/null 2>&1; then
  sudo apt-get update && sudo apt-get install -y android-sdk-platform-tools
elif command -v dnf >/dev/null 2>&1; then
  sudo dnf install -y android-tools
elif command -v pacman >/dev/null 2>&1; then
  sudo pacman -Sy --noconfirm android-tools
elif command -v zypper >/dev/null 2>&1; then
  sudo zypper --non-interactive install android-tools
else
  echo "No supported package manager found. Install adb manually and re-run."
  exit 1
fi

if command -v adb >/dev/null 2>&1; then
  echo "adb installed: $(command -v adb)"
else
  echo "adb installation command completed but adb is still unavailable."
  exit 1
fi
