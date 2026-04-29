$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

py -3 -m venv .venv
& .\.venv\Scripts\python.exe -m pip install --upgrade pip
& .\.venv\Scripts\python.exe -m pip install -e ".[build]"
& .\.venv\Scripts\python.exe -m PyInstaller --clean --noconfirm ApkHub.spec

Write-Host "Created dist\ApkHub.exe"
