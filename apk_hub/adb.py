from __future__ import annotations

import os
import platform
import re
import shlex
import shutil
import subprocess
import threading
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Iterable, Sequence

from .paths import WINDOWS_ADB_DIR, log_dir

OutputHandler = Callable[[str], None]


@dataclass
class CommandResult:
    exit_code: int
    output: str


class AdbManager:
    def __init__(self) -> None:
        self._processes: list[subprocess.Popen[str]] = []
        self._lock = threading.Lock()
        self.adb_path = self._resolve_tool("adb", include_android_home=True, include_windows_vendor=True)
        self.scrcpy_path = self._resolve_tool("scrcpy", include_android_home=True, include_windows_vendor=True)
        self.aapt_path = self._resolve_aapt()

    def _platform_exe(self, name: str) -> str:
        if platform.system().lower() == "windows" and not name.lower().endswith(".exe"):
            return f"{name}.exe"
        return name

    def _resolve_tool(self, name: str, *, include_android_home: bool, include_windows_vendor: bool) -> str:
        exe = self._platform_exe(name)
        candidates: list[Path] = []

        if include_android_home:
            for env in ("ANDROID_HOME", "ANDROID_SDK_ROOT"):
                root = os.environ.get(env)
                if root:
                    candidates.append(Path(root) / "platform-tools" / exe)

        found = shutil.which(exe) or shutil.which(name)
        if found:
            candidates.append(Path(found))

        if include_windows_vendor and platform.system().lower() == "windows":
            candidates.append(WINDOWS_ADB_DIR / exe)

        for candidate in candidates:
            if candidate.exists():
                return str(candidate)
        return exe

    def _resolve_aapt(self) -> str | None:
        exe = self._platform_exe("aapt")
        candidates: list[Path] = []

        for env in ("ANDROID_HOME", "ANDROID_SDK_ROOT"):
            root = os.environ.get(env)
            if not root:
                continue
            build_tools = Path(root) / "build-tools"
            if build_tools.exists():
                for child in sorted(build_tools.iterdir(), reverse=True):
                    candidates.append(child / exe)

        found = shutil.which(exe) or shutil.which("aapt")
        if found:
            candidates.append(Path(found))

        if platform.system().lower() == "windows":
            candidates.append(WINDOWS_ADB_DIR / exe)

        for candidate in candidates:
            if candidate.exists():
                return str(candidate)
        return None

    def _normalize_parts(self, parts: str | Sequence[str]) -> list[str]:
        if isinstance(parts, str):
            return shlex.split(parts, posix=platform.system().lower() != "windows")
        return [str(part) for part in parts]

    def _run_process(self, command: Sequence[str], output_handler: OutputHandler | None = None) -> CommandResult:
        startupinfo = None
        creationflags = 0
        if platform.system().lower() == "windows":
            creationflags = getattr(subprocess, "CREATE_NO_WINDOW", 0)

        try:
            process = subprocess.Popen(
                list(command),
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                stdin=subprocess.DEVNULL,
                text=True,
                encoding="utf-8",
                errors="replace",
                bufsize=1,
                startupinfo=startupinfo,
                creationflags=creationflags,
            )
        except FileNotFoundError:
            message = f"Executable not found: {command[0]}"
            if output_handler:
                output_handler(message)
            return CommandResult(exit_code=127, output=message)

        with self._lock:
            self._processes.append(process)

        output_lines: list[str] = []
        try:
            assert process.stdout is not None
            for raw_line in process.stdout:
                line = raw_line.rstrip("\r\n")
                output_lines.append(line)
                if output_handler and line:
                    output_handler(line)
            exit_code = process.wait()
            return CommandResult(exit_code=exit_code, output="\n".join(output_lines))
        finally:
            with self._lock:
                if process in self._processes:
                    self._processes.remove(process)

    def stop_all(self) -> None:
        with self._lock:
            processes = list(self._processes)

        for process in processes:
            if process.poll() is not None:
                continue
            try:
                process.terminate()
            except Exception:
                pass

        deadline = time.monotonic() + 1.5
        for process in processes:
            while process.poll() is None and time.monotonic() < deadline:
                time.sleep(0.05)
            if process.poll() is None:
                try:
                    process.kill()
                except Exception:
                    pass

    def run_adb_parts(
        self,
        parts: str | Sequence[str],
        selected_device: str | None = None,
        *,
        shell: bool = False,
        output_handler: OutputHandler | None = None,
    ) -> CommandResult:
        command = [self.adb_path]
        if selected_device:
            command.extend(["-s", selected_device])
        if shell:
            command.append("shell")
            if isinstance(parts, str):
                command.append(parts)
            else:
                command.extend(self._normalize_parts(parts))
        else:
            command.extend(self._normalize_parts(parts))
        return self._run_process(command, output_handler)

    def run_external(self, executable: str | None, parts: Sequence[str], output_handler: OutputHandler | None = None) -> CommandResult:
        if not executable:
            message = "Required executable was not found. Install it or add it to PATH."
            if output_handler:
                output_handler(message)
            return CommandResult(exit_code=127, output=message)
        return self._run_process([executable, *map(str, parts)], output_handler)

    def devices(self) -> list[tuple[str, str]]:
        result = self.run_adb_parts(["devices"])
        devices: list[tuple[str, str]] = []
        for line in result.output.splitlines():
            line = line.strip()
            if not line or line.startswith("List of devices"):
                continue
            parts = line.split()
            if len(parts) >= 2 and parts[1] == "device":
                serial = parts[0]
                devices.append((serial, self.get_device_name(serial)))
        return devices

    def get_device_name(self, serial: str) -> str:
        result = self.run_adb_parts(["getprop", "ro.product.model"], serial, shell=True)
        name = result.output.splitlines()[0].strip() if result.output.strip() else serial
        return name or serial

    def get_build_characteristics(self, serial: str) -> str:
        result = self.run_adb_parts(["getprop", "ro.build.characteristics"], serial, shell=True)
        return result.output.strip().lower()

    def clear_logcat(self, serial: str) -> CommandResult:
        return self.run_adb_parts(["logcat", "-c"], serial)

    def get_app_name(self, app_pkg: str, serial: str) -> str:
        temp_dir = log_dir() / "apk"
        shutil.rmtree(temp_dir, ignore_errors=True)
        temp_dir.mkdir(parents=True, exist_ok=True)
        local_apk = temp_dir / "base.apk"
        try:
            result = self.run_adb_parts(["pm", "list", "package", "-f", app_pkg], serial, shell=True)
            match = re.search(r"package:(.*?base\.apk)=" + re.escape(app_pkg), result.output)
            if not match or not self.aapt_path:
                return app_pkg

            pull = self.run_adb_parts(["pull", match.group(1), str(local_apk)], serial)
            if pull.exit_code != 0 or not local_apk.exists():
                return app_pkg

            badging = self.run_external(self.aapt_path, ["d", "badging", str(local_apk)])
            label_match = re.search(r"application-label(?:-[a-zA-Z]{2})*:'(.*?)'", badging.output)
            return label_match.group(1) if label_match else app_pkg
        finally:
            shutil.rmtree(temp_dir, ignore_errors=True)

    def uninstall_package(self, serial: str, app_pkg: str, output_handler: OutputHandler | None = None) -> bool:
        app_name = self.get_app_name(app_pkg, serial)
        result = self.run_adb_parts(["uninstall", app_pkg], serial, output_handler=output_handler)
        if "Success" in result.output:
            if output_handler:
                output_handler(f"The app {app_name} was successfully uninstalled")
            return True
        if output_handler:
            output_handler(f"The app {app_pkg} was not uninstalled, check the package name and try again")
        return False

    def clear_package(self, serial: str, app_pkg: str, output_handler: OutputHandler | None = None) -> bool:
        app_name = self.get_app_name(app_pkg, serial)
        result = self.run_adb_parts(["pm", "clear", app_pkg], serial, shell=True, output_handler=output_handler)
        if "Success" in result.output:
            if output_handler:
                output_handler(f"The app {app_name} was successfully cleared")
            return True
        if output_handler:
            output_handler(f"The app {app_pkg} was not cleared, check the package name and try again")
        return False
