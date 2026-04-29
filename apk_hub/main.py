from __future__ import annotations

import sys
import time
import traceback
from pathlib import Path
from typing import Callable

from PySide6.QtCore import Qt, QTimer, Signal, QSize
from PySide6.QtGui import QAction, QCloseEvent, QIcon, QPixmap, QTextCursor
from PySide6.QtWidgets import (
    QApplication,
    QComboBox,
    QDialog,
    QFileDialog,
    QFrame,
    QGridLayout,
    QHBoxLayout,
    QLabel,
    QLineEdit,
    QListWidget,
    QListWidgetItem,
    QMainWindow,
    QMessageBox,
    QPushButton,
    QScrollArea,
    QSplitter,
    QTextEdit,
    QToolButton,
    QVBoxLayout,
    QWidget,
)

from .adb import AdbManager, CommandResult
from .constants import CARE_SAMPLE_PACKAGE, KIDS_APPS, PARENTAL_PACKAGE, AppProfile
from .paths import ICON_PATH, IMAGES_DIR, PNG_ICON_PATH, log_dir, open_path, timestamp
from .workers import TaskThread


STYLE = """
QWidget {
    background: #111216;
    color: #f5f7fb;
    font-family: "Segoe UI", "Noto Sans", "DejaVu Sans", sans-serif;
    font-size: 13px;
}
QMainWindow, QDialog {
    background: #111216;
}
QFrame#Card {
    background: #191a20;
    border: 1px solid #353844;
    border-radius: 22px;
}
QFrame#HeroCard {
    background: qlineargradient(x1:0, y1:0, x2:1, y2:0, stop:0 #0e2f1e, stop:0.55 #162620, stop:1 #111d18);
    border: 1px solid #2f6f49;
    border-radius: 24px;
}
QLabel#Eyebrow {
    color: #aeb6c7;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 2px;
}
QLabel#Title {
    color: #ffffff;
    font-size: 24px;
    font-weight: 800;
}
QLabel#SectionTitle {
    color: #ffffff;
    font-size: 20px;
    font-weight: 800;
}
QLabel#Muted {
    color: #a9b0c0;
    font-size: 12px;
}
QLabel#StatusChip {
    background: #242631;
    border: 1px solid #3b3f4c;
    border-radius: 18px;
    padding: 8px 14px;
    color: #dce2ef;
}
QLineEdit, QComboBox, QTextEdit, QListWidget {
    background: #131419;
    border: 1px solid #3a3d49;
    border-radius: 14px;
    color: #f7f9ff;
    selection-background-color: #3ddc84;
    padding: 8px;
}
QLineEdit:focus, QComboBox:focus, QTextEdit:focus, QListWidget:focus {
    border: 1px solid #3ddc84;
}
QTextEdit#Output {
    background: #101116;
    font-family: "Cascadia Mono", "Consolas", "DejaVu Sans Mono", monospace;
    font-size: 12px;
    border-radius: 18px;
}
QPushButton, QToolButton {
    background: #20222a;
    border: 1px solid #3b3e4a;
    border-radius: 13px;
    color: #ffffff;
    padding: 9px 14px;
    font-weight: 700;
}
QPushButton:hover, QToolButton:hover {
    background: #292c35;
    border-color: #545966;
}
QPushButton:disabled, QToolButton:disabled {
    color: #737989;
    background: #1a1b21;
    border-color: #2c2f39;
}
QPushButton[accent="true"] {
    background: #2d7d46;
    border-color: #3aa95f;
}
QPushButton[accent="true"]:hover {
    background: #3ddc84;
}
QPushButton[danger="true"] {
    background: #1a3f2a;
    border-color: #2f8f50;
    color: #d6ffe6;
}
QPushButton[ghost="true"] {
    background: #131419;
}
QToolButton:checked {
    background: #1d4a31;
    border-color: #3ddc84;
}
QSplitter::handle {
    background: #333743;
    border-radius: 2px;
}
QScrollBar:vertical, QScrollBar:horizontal {
    background: #111216;
    width: 10px;
    height: 10px;
}
QScrollBar::handle:vertical, QScrollBar::handle:horizontal {
    background: #565b68;
    border-radius: 5px;
}
"""


def make_button(text: str, *, accent: bool = False, danger: bool = False, ghost: bool = False) -> QPushButton:
    button = QPushButton(text)
    button.setCursor(Qt.PointingHandCursor)
    button.setProperty("accent", accent)
    button.setProperty("danger", danger)
    button.setProperty("ghost", ghost)
    return button


def make_card(title: str, subtitle: str | None = None, object_name: str = "Card") -> tuple[QFrame, QVBoxLayout]:
    frame = QFrame()
    frame.setObjectName(object_name)
    layout = QVBoxLayout(frame)
    layout.setContentsMargins(22, 20, 22, 20)
    layout.setSpacing(12)

    title_label = QLabel(title)
    title_label.setObjectName("SectionTitle")
    layout.addWidget(title_label)

    if subtitle:
        subtitle_label = QLabel(subtitle)
        subtitle_label.setObjectName("Muted")
        subtitle_label.setWordWrap(True)
        layout.addWidget(subtitle_label)
    return frame, layout


def app_icon() -> QIcon:
    if ICON_PATH.exists():
        return QIcon(str(ICON_PATH))
    if PNG_ICON_PATH.exists():
        return QIcon(str(PNG_ICON_PATH))
    return QIcon()


def sanitize_path_part(value: str | None) -> str:
    text = value or "full_device"
    safe = "".join(ch if ch.isalnum() or ch in ".-_" else "_" for ch in text)
    return safe.strip("_") or "full_device"


class ApkListWidget(QListWidget):
    files_dropped = Signal(list)

    def __init__(self) -> None:
        super().__init__()
        self.setAcceptDrops(True)
        self.setSelectionMode(QListWidget.ExtendedSelection)
        self.setAlternatingRowColors(False)

    def dragEnterEvent(self, event) -> None:  # noqa: N802 - Qt API
        if event.mimeData().hasUrls():
            paths = [url.toLocalFile() for url in event.mimeData().urls()]
            if any(path.lower().endswith(".apk") for path in paths):
                event.acceptProposedAction()
                return
        event.ignore()

    def dragMoveEvent(self, event) -> None:  # noqa: N802 - Qt API
        self.dragEnterEvent(event)

    def dropEvent(self, event) -> None:  # noqa: N802 - Qt API
        paths = [url.toLocalFile() for url in event.mimeData().urls()]
        apk_paths = [path for path in paths if path.lower().endswith(".apk")]
        if apk_paths:
            self.files_dropped.emit(apk_paths)
            event.acceptProposedAction()
        else:
            event.ignore()


class BaseDialog(QDialog):
    def __init__(self, main_window: "MainWindow", title: str) -> None:
        super().__init__(main_window)
        self.main_window = main_window
        self.setWindowTitle(title)
        self.setWindowIcon(app_icon())
        self.setAttribute(Qt.WA_DeleteOnClose, True)
        self.setMinimumWidth(420)


class LogcatDialog(BaseDialog):
    def __init__(self, main_window: "MainWindow", serial: str, package_filter: str | None = None) -> None:
        title = f"Logcat - {package_filter}" if package_filter else "Logcat - full device"
        super().__init__(main_window, title)
        self.serial = serial
        self.package_filter = package_filter.strip() if package_filter else None
        self.thread: TaskThread | None = None

        layout = QVBoxLayout(self)
        layout.setContentsMargins(18, 18, 18, 18)
        layout.setSpacing(12)

        header = QLabel(title)
        header.setObjectName("SectionTitle")
        layout.addWidget(header)

        self.log_text = QTextEdit()
        self.log_text.setObjectName("Output")
        self.log_text.setReadOnly(True)
        self.log_text.setMinimumSize(980, 430)
        layout.addWidget(self.log_text)

        buttons = QHBoxLayout()
        self.start_stop_button = make_button("Stop", danger=True)
        self.save_button = make_button("Save", ghost=True)
        self.clear_button = make_button("Clear", ghost=True)
        buttons.addWidget(self.start_stop_button)
        buttons.addWidget(self.save_button)
        buttons.addWidget(self.clear_button)
        buttons.addStretch(1)
        layout.addLayout(buttons)

        self.start_stop_button.clicked.connect(self.toggle_logcat)
        self.save_button.clicked.connect(self.save_log)
        self.clear_button.clicked.connect(self.log_text.clear)
        QTimer.singleShot(0, self.start_logcat)

    def append_log(self, text: str) -> None:
        if not text:
            return
        self.log_text.append(text)
        self.log_text.moveCursor(QTextCursor.End)

    def start_logcat(self) -> None:
        if self.thread and self.thread.isRunning():
            return
        self.start_stop_button.setText("Stop")
        self.start_stop_button.setProperty("danger", True)
        self.start_stop_button.style().unpolish(self.start_stop_button)
        self.start_stop_button.style().polish(self.start_stop_button)
        self.main_window.adb.clear_logcat(self.serial)

        def task(emit: Callable[[str], None]) -> CommandResult:
            def filtered_emit(line: str) -> None:
                if not self.package_filter or self.package_filter in line:
                    emit(line)

            return self.main_window.adb.run_adb_parts(
                ["logcat", "*:I", "*:D", "*:W", "*:E", "*:V"],
                self.serial,
                output_handler=filtered_emit,
            )

        self.thread = TaskThread(task, self)
        self.thread.message.connect(self.append_log)
        self.thread.failed.connect(self.append_log)
        self.thread.result.connect(lambda _result: self.start_stop_button.setText("Start"))
        self.thread.start()

    def stop_logcat(self) -> None:
        self.main_window.adb.stop_all()
        self.start_stop_button.setText("Start")

    def toggle_logcat(self) -> None:
        if self.start_stop_button.text() == "Stop":
            self.stop_logcat()
        else:
            self.log_text.clear()
            self.start_logcat()

    def save_log(self) -> None:
        contents = self.log_text.toPlainText()
        if not contents.strip():
            QMessageBox.information(self, "Output empty", "There is no logcat output to save.")
            return

        stamp = timestamp()
        name = sanitize_path_part(self.package_filter)
        directory = log_dir() / "Logcat" / name / stamp
        directory.mkdir(parents=True, exist_ok=True)
        file_path = directory / f"{name}_{stamp}.txt"
        file_path.write_text(contents, encoding="utf-8")
        if QMessageBox.question(self, "File Saved", f"File saved at:\n{file_path}\n\nOpen the folder?") == QMessageBox.Yes:
            open_path(directory)

    def closeEvent(self, event: QCloseEvent) -> None:  # noqa: N802 - Qt API
        self.stop_logcat()
        super().closeEvent(event)


class PackageActionDialog(BaseDialog):
    def __init__(self, main_window: "MainWindow", serial: str, mode: str) -> None:
        titles = {
            "run": "Run ADB Command",
            "clear": "Clear Package",
            "uninstall": "Uninstall Package",
            "logcat": "Package Logcat",
        }
        super().__init__(main_window, titles[mode])
        self.serial = serial
        self.mode = mode

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(12)

        label = QLabel(titles[mode])
        label.setObjectName("SectionTitle")
        layout.addWidget(label)

        self.input = QLineEdit()
        if mode == "run":
            self.input.setPlaceholderText("adb subcommand, for example: shell pm list packages")
        else:
            self.input.setPlaceholderText("package name, for example: com.example.app")
        layout.addWidget(self.input)

        buttons = QHBoxLayout()
        self.action_button = make_button(titles[mode].split()[0], accent=mode in {"run", "logcat"}, danger=mode == "uninstall")
        cancel = make_button("Close", ghost=True)
        buttons.addWidget(self.action_button)
        buttons.addWidget(cancel)
        layout.addLayout(buttons)

        self.action_button.clicked.connect(self.execute)
        cancel.clicked.connect(self.close)
        self.input.returnPressed.connect(self.execute)
        self.input.setFocus()

    def execute(self) -> None:
        value = self.input.text().strip()
        if not value:
            QMessageBox.information(self, "Input missing", "Inform a command or package before executing.")
            return

        if self.mode == "logcat":
            self.main_window.open_logcat(self.serial, value.lower())
            return

        self.action_button.setEnabled(False)
        self.input.setEnabled(False)

        def finish(_result: object) -> None:
            self.action_button.setEnabled(True)
            self.input.setEnabled(True)
            self.input.setFocus()

        if self.mode == "clear":
            package = value.lower()
            self.main_window.append_output(f"Clearing package {package}", clear=True)
            self.main_window.run_task(
                lambda emit: self.main_window.adb.clear_package(self.serial, package, emit),
                on_result=finish,
            )
            return

        if self.mode == "uninstall":
            package = value.lower()
            self.main_window.append_output(f"Uninstalling package {package}", clear=True)
            self.main_window.run_task(
                lambda emit: self.main_window.adb.uninstall_package(self.serial, package, emit),
                on_result=finish,
            )
            return

        command = value
        self.main_window.append_output(f"Executing '{command}' command", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            lowered = command.lower().strip()
            if lowered == "shell" or "logcat" in lowered:
                emit("Invalid command. Use the Logcat tool for logcat streaming.")
                return False
            if lowered.startswith("shell "):
                result = self.main_window.adb.run_adb_parts(command[6:].strip(), self.serial, shell=True, output_handler=emit)
            else:
                result = self.main_window.adb.run_adb_parts(command, self.serial, output_handler=emit)
            output = result.output.lower()
            if "unknown command" in output or "inaccessible or not found" in output or "error:" in output:
                emit("Invalid command")
                return False
            return result.exit_code == 0

        self.main_window.run_task(task, on_result=finish)


class AppSelectionDialog(BaseDialog):
    def __init__(self, main_window: "MainWindow", serial: str, action: str) -> None:
        titles = {"clear": "Clear Kids Packages", "uninstall": "Uninstall Kids Apps", "logcat": "Kids App Logcat"}
        super().__init__(main_window, titles[action])
        self.serial = serial
        self.action = action
        self.buttons: dict[QToolButton, AppProfile] = {}

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(12)
        title = QLabel(titles[action])
        title.setObjectName("SectionTitle")
        layout.addWidget(title)

        subtitle = QLabel("Choose one app for logcat or one or more apps for package actions.")
        subtitle.setObjectName("Muted")
        subtitle.setWordWrap(True)
        layout.addWidget(subtitle)

        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        content = QWidget()
        grid = QGridLayout(content)
        grid.setSpacing(10)

        for index, profile in enumerate(KIDS_APPS):
            tool = QToolButton()
            tool.setCheckable(True)
            tool.setText(profile.label)
            tool.setToolButtonStyle(Qt.ToolButtonTextUnderIcon)
            tool.setIconSize(QSize(44, 44))
            icon_path = IMAGES_DIR / profile.icon
            if icon_path.exists():
                tool.setIcon(QIcon(str(icon_path)))
            tool.setMinimumSize(112, 88)
            tool.setCursor(Qt.PointingHandCursor)
            if action == "logcat":
                tool.toggled.connect(lambda checked, current=tool: self._single_select(current, checked))
            self.buttons[tool] = profile
            grid.addWidget(tool, index // 3, index % 3)

        scroll.setWidget(content)
        layout.addWidget(scroll)

        action_text = {"clear": "Clear", "uninstall": "Uninstall", "logcat": "Open Logcat"}[action]
        actions = QHBoxLayout()
        submit = make_button(action_text, accent=action != "uninstall", danger=action == "uninstall")
        close = make_button("Close", ghost=True)
        actions.addWidget(submit)
        actions.addWidget(close)
        layout.addLayout(actions)

        submit.clicked.connect(self.execute)
        close.clicked.connect(self.close)

    def _single_select(self, current: QToolButton, checked: bool) -> None:
        if not checked:
            return
        for button in self.buttons:
            if button is not current:
                button.setChecked(False)

    def selected_profiles(self) -> list[AppProfile]:
        return [profile for button, profile in self.buttons.items() if button.isChecked()]

    def execute(self) -> None:
        profiles = self.selected_profiles()
        if not profiles:
            QMessageBox.information(self, "No app selected", "Select at least one app first.")
            return

        if self.action == "logcat":
            self.main_window.open_logcat(self.serial, profiles[0].package)
            return

        action_name = "Clear pkg action" if self.action == "clear" else "Uninstall app action"
        self.main_window.append_output(f"Trying to start {action_name} on the apps selected", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            success = True
            for profile in profiles:
                if self.action == "clear":
                    result = self.main_window.adb.run_adb_parts(
                        ["pm", "clear", profile.package],
                        self.serial,
                        shell=True,
                        output_handler=emit,
                    )
                    if "Failed" in result.output:
                        emit(f"{action_name} not executed on package {profile.package}. Check if the app is installed.")
                        success = False
                        break
                    if "Success" in result.output:
                        emit(f"{action_name} executed on package {profile.package}")
                else:
                    if not self.main_window.adb.uninstall_package(self.serial, profile.package, emit):
                        success = False
                        break
            return success

        def finished(result: object) -> None:
            if result:
                names = ", ".join(profile.display_name for profile in profiles)
                QMessageBox.information(self, "Action Executed", f"{action_name} successfully executed on apps: {names}")
            else:
                QMessageBox.warning(self, "Action failed", "An error occurred. Check the output and try again.")

        self.main_window.run_task(task, on_result=finished)


class KidsToolsDialog(BaseDialog):
    def __init__(self, main_window: "MainWindow", serial: str) -> None:
        super().__init__(main_window, "Kids Tools")
        self.serial = serial
        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(14)

        title = QLabel("Kids Tools")
        title.setObjectName("SectionTitle")
        layout.addWidget(title)
        hint = QLabel("Manual Samsung Kids helpers. Automation was removed from this Qt port.")
        hint.setObjectName("Muted")
        hint.setWordWrap(True)
        layout.addWidget(hint)

        grid = QGridLayout()
        actions = [
            ("Clear Pkg", lambda: self.open_selector("clear"), False),
            ("Logcat Clear", self.clear_logcat, False),
            ("Logcat View", lambda: self.open_selector("logcat"), False),
            ("Uninstall", lambda: self.open_selector("uninstall"), True),
        ]
        for index, (text, callback, danger) in enumerate(actions):
            button = make_button(text, danger=danger)
            button.clicked.connect(callback)
            grid.addWidget(button, index // 2, index % 2)
        layout.addLayout(grid)

    def open_selector(self, action: str) -> None:
        dialog = AppSelectionDialog(self.main_window, self.serial, action)
        dialog.show()

    def clear_logcat(self) -> None:
        self.main_window.append_output("Clearing logcat", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            result = self.main_window.adb.clear_logcat(self.serial)
            if result.output.strip():
                emit("Logcat not cleared")
                emit(result.output)
                return False
            emit("Logcat cleared successfully")
            return True

        self.main_window.run_task(task)


class MoreToolsDialog(BaseDialog):
    def __init__(self, main_window: "MainWindow", serial: str) -> None:
        super().__init__(main_window, "More Tools")
        self.serial = serial
        self.screen_running = False
        self.recording = False
        self.record_remote_file: str | None = None
        self.record_local_dir: Path | None = None

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(14)
        title = QLabel("More Tools")
        title.setObjectName("SectionTitle")
        layout.addWidget(title)
        hint = QLabel("ADB command, logcat, screen share, screen record, clear and uninstall tools.")
        hint.setObjectName("Muted")
        hint.setWordWrap(True)
        layout.addWidget(hint)

        grid = QGridLayout()
        self.screen_button = make_button("Screen")
        self.record_button = make_button("Record", accent=True)
        actions = [
            ("ADB", lambda: self.open_package_dialog("run")),
            ("Clear APK", lambda: self.open_package_dialog("clear")),
            ("Logcat", self.open_logcat_choice),
            ("Uninstall Pkg", lambda: self.open_package_dialog("uninstall")),
        ]
        for index, (text, callback) in enumerate(actions):
            button = make_button(text)
            button.clicked.connect(callback)
            grid.addWidget(button, index // 2, index % 2)
        self.screen_button.clicked.connect(self.toggle_screen)
        self.record_button.clicked.connect(self.toggle_record)
        grid.addWidget(self.screen_button, 2, 0)
        grid.addWidget(self.record_button, 2, 1)
        layout.addLayout(grid)

    def open_package_dialog(self, mode: str) -> None:
        dialog = PackageActionDialog(self.main_window, self.serial, mode)
        dialog.show()

    def open_logcat_choice(self) -> None:
        box = QMessageBox(self)
        box.setWindowTitle("Logcat")
        box.setText("Do you want to get the full device logcat?")
        full = box.addButton("Full device", QMessageBox.YesRole)
        package = box.addButton("Choose package", QMessageBox.NoRole)
        box.addButton(QMessageBox.Cancel)
        box.exec()
        if box.clickedButton() == full:
            self.main_window.open_logcat(self.serial, None)
        elif box.clickedButton() == package:
            self.open_package_dialog("logcat")

    def toggle_screen(self) -> None:
        if self.screen_running:
            self.main_window.adb.stop_all()
            self.screen_running = False
            self.screen_button.setText("Screen")
            self.record_button.setEnabled(True)
            return

        self.screen_running = True
        self.screen_button.setText("Stop")
        self.record_button.setEnabled(False)
        self.main_window.append_output("Starting scrcpy screen share", clear=True)

        def task(emit: Callable[[str], None]) -> CommandResult:
            return self.main_window.adb.run_external(self.main_window.adb.scrcpy_path, ["-s", self.serial], emit)

        def finished(_result: object) -> None:
            self.screen_running = False
            self.screen_button.setText("Screen")
            self.record_button.setEnabled(True)

        self.main_window.run_task(task, on_result=finished)

    def toggle_record(self) -> None:
        if self.recording:
            self.stop_recording()
        else:
            self.start_recording()

    def start_recording(self) -> None:
        device_name = self.main_window.adb.get_device_name(self.serial)
        stamp = timestamp()
        remote_name = f"{sanitize_path_part(device_name)}_{stamp}"
        self.record_remote_file = f"/sdcard/{remote_name}.mp4"
        self.record_local_dir = log_dir() / "ScreenRecords" / sanitize_path_part(device_name) / remote_name
        self.recording = True
        self.record_button.setText("Stop")
        self.screen_button.setEnabled(False)
        self.main_window.append_output("Starting screen recording and scrcpy", clear=True)

        self.main_window.run_task(lambda emit: self.main_window.adb.run_external(self.main_window.adb.scrcpy_path, ["-s", self.serial], emit))
        self.main_window.run_task(
            lambda emit: self.main_window.adb.run_adb_parts(
                ["screenrecord", self.record_remote_file or "/sdcard/apkhub_record.mp4"],
                self.serial,
                shell=True,
                output_handler=emit,
            )
        )

    def stop_recording(self) -> None:
        self.main_window.append_output("Stopping screen recording")
        self.main_window.adb.stop_all()
        self.recording = False
        self.record_button.setText("Record")
        self.screen_button.setEnabled(True)
        remote = self.record_remote_file
        local_dir = self.record_local_dir
        if not remote or not local_dir:
            return

        def task(emit: Callable[[str], None]) -> Path:
            time.sleep(1.5)
            local_dir.mkdir(parents=True, exist_ok=True)
            local_file = local_dir / Path(remote).name
            self.main_window.adb.run_adb_parts(["pull", remote, str(local_file)], self.serial, output_handler=emit)
            return local_dir

        def finished(path: object) -> None:
            if isinstance(path, Path) and QMessageBox.question(self, "Success", f"Screen recording saved to:\n{path}\n\nOpen the folder?") == QMessageBox.Yes:
                open_path(path)

        self.main_window.run_task(task, on_result=finished)

    def closeEvent(self, event: QCloseEvent) -> None:  # noqa: N802 - Qt API
        if self.screen_running or self.recording:
            self.main_window.adb.stop_all()
        super().closeEvent(event)


class ParentalCareDialog(BaseDialog):
    def __init__(self, main_window: "MainWindow", serial: str) -> None:
        super().__init__(main_window, "Parental Care")
        self.serial = serial
        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 20, 20, 20)
        layout.setSpacing(14)

        title = QLabel("Parental Care")
        title.setObjectName("SectionTitle")
        layout.addWidget(title)
        hint = QLabel("Uses APKs currently loaded in the main APK Library. Files must start with Parental and CareSample for full install.")
        hint.setObjectName("Muted")
        hint.setWordWrap(True)
        layout.addWidget(hint)

        grid = QGridLayout()
        actions = [
            ("Install-PC", lambda: self.push_parental(install=True), False),
            ("Uninstall", self.uninstall_parental, True),
            ("Push PC", lambda: self.push_parental(install=False), False),
            ("Remount", self.remount, False),
            ("Log PC", lambda: self.main_window.open_logcat(self.serial, PARENTAL_PACKAGE), False),
            ("Clear APK", self.clear_parental, False),
        ]
        for index, (text, callback, danger) in enumerate(actions):
            button = make_button(text, danger=danger, accent=text == "Install-PC")
            button.clicked.connect(callback)
            grid.addWidget(button, index // 2, index % 2)
        layout.addLayout(grid)

    def _find_loaded_apk(self, prefix: str) -> Path | None:
        for path in self.main_window.apk_files:
            if path.name.startswith(prefix):
                return path
        return None

    def remount(self) -> None:
        self.main_window.append_output("Running remount", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            self.main_window.adb.run_adb_parts(["remount"], self.serial, output_handler=emit)
            result = self.main_window.adb.run_adb_parts(["remount"], self.serial, output_handler=emit)
            return "Remount succeeded" in result.output

        self.main_window.run_task(task)

    def push_parental(self, *, install: bool) -> None:
        parental_apk = self._find_loaded_apk("Parental")
        care_sample_apk = self._find_loaded_apk("CareSample")
        if not parental_apk:
            QMessageBox.warning(self, "APK missing", "No APK file starting with 'Parental' found. Add it to the APK Library first.")
            return
        if install and not care_sample_apk:
            QMessageBox.warning(self, "APK missing", "No APK file starting with 'CareSample' found. Add it to the APK Library first.")
            return

        self.main_window.append_output("Initializing Parental Care full installation" if install else "Pushing Parental Care APK", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            characteristics = self.main_window.adb.get_build_characteristics(self.serial)
            target_dir = "/system/priv-app/ParentalCareWatch" if "watch" in characteristics else "/system/priv-app/ParentalCare"
            self.main_window.adb.run_adb_parts(["root"], self.serial, output_handler=emit)
            self.main_window.adb.run_adb_parts(["remount"], self.serial, output_handler=emit)
            self.main_window.adb.run_adb_parts(["remount"], self.serial, output_handler=emit)
            self.main_window.adb.run_adb_parts(f"rm -rf {target_dir} && mkdir -p {target_dir}", self.serial, shell=True, output_handler=emit)
            push = self.main_window.adb.run_adb_parts(
                ["push", str(parental_apk), f"{target_dir}/{parental_apk.name}"],
                self.serial,
                output_handler=emit,
            )
            if push.exit_code != 0:
                emit("Failed, check the device and try again")
                return False
            emit("Parental pushed to system folder")

            if install:
                first = self.main_window.adb.run_adb_parts(["install", "-r", "-d", str(parental_apk)], self.serial, output_handler=emit)
                second = self.main_window.adb.run_adb_parts(["install", "-r", "-d", str(care_sample_apk)], self.serial, output_handler=emit) if care_sample_apk else CommandResult(1, "")
                return "Success" in first.output and "Success" in second.output
            return True

        self.main_window.run_task(task)

    def uninstall_parental(self) -> None:
        self.main_window.append_output("Uninstalling Parental Care", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            emit("Uninstalling Parental Care...")
            first = self.main_window.adb.run_adb_parts(["uninstall", PARENTAL_PACKAGE], self.serial, output_handler=emit)
            emit("Uninstalling Care Sample...")
            second = self.main_window.adb.run_adb_parts(["uninstall", CARE_SAMPLE_PACKAGE], self.serial, output_handler=emit)
            return "Success" in first.output or "Success" in second.output

        self.main_window.run_task(task)

    def clear_parental(self) -> None:
        self.main_window.append_output("Initializing package data clear", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            emit("Clearing Parental Care app...")
            first = self.main_window.adb.run_adb_parts(["pm", "clear", PARENTAL_PACKAGE], self.serial, shell=True, output_handler=emit)
            emit("Clearing Care Sample app...")
            second = self.main_window.adb.run_adb_parts(["pm", "clear", CARE_SAMPLE_PACKAGE], self.serial, shell=True, output_handler=emit)
            return "Success" in first.output or "Success" in second.output

        self.main_window.run_task(task)


class MainWindow(QMainWindow):
    def __init__(self) -> None:
        super().__init__()
        self.adb = AdbManager()
        self.apk_files: list[Path] = []
        self.threads: list[TaskThread] = []
        self.installing = False
        self.debug_simulated_device = False
        self.setWindowTitle("APK Hub")
        self.setWindowIcon(app_icon())
        self.resize(1240, 780)
        self.setMinimumSize(980, 640)

        self._build_menu()
        self._build_ui()
        self.refresh_devices(clear_output=True)

    def _build_menu(self) -> None:
        file_menu = self.menuBar().addMenu("File")
        add_apks = QAction("Add APKs", self)
        add_apks.triggered.connect(self.browse_apks)
        save_output = QAction("Save Output", self)
        save_output.triggered.connect(self.save_output)
        exit_action = QAction("Exit", self)
        exit_action.triggered.connect(self.close)
        file_menu.addAction(add_apks)
        file_menu.addAction(save_output)
        file_menu.addSeparator()
        file_menu.addAction(exit_action)

        view_menu = self.menuBar().addMenu("View")
        refresh = QAction("Refresh Devices", self)
        refresh.triggered.connect(lambda: self.refresh_devices(clear_output=True))
        view_menu.addAction(refresh)

        debug_menu = self.menuBar().addMenu("Debug")
        self.simulate_device_action = QAction("Simulate Android Connection", self)
        self.simulate_device_action.setCheckable(True)
        self.simulate_device_action.toggled.connect(self.toggle_debug_device)
        debug_menu.addAction(self.simulate_device_action)

    def _build_ui(self) -> None:
        central = QWidget()
        root = QVBoxLayout(central)
        root.setContentsMargins(16, 16, 16, 16)
        root.setSpacing(16)
        self.setCentralWidget(central)

        hero = QFrame()
        hero.setObjectName("HeroCard")
        hero_layout = QHBoxLayout(hero)
        hero_layout.setContentsMargins(24, 20, 24, 20)
        hero_layout.setSpacing(18)

        icon_label = QLabel()
        icon_pixmap = QPixmap(str(PNG_ICON_PATH if PNG_ICON_PATH.exists() else ICON_PATH))
        if not icon_pixmap.isNull():
            icon_label.setPixmap(icon_pixmap.scaled(54, 54, Qt.KeepAspectRatio, Qt.SmoothTransformation))
        hero_layout.addWidget(icon_label)

        brand = QVBoxLayout()
        eyebrow = QLabel("ANDROID APK WORKBENCH")
        eyebrow.setObjectName("Eyebrow")
        title = QLabel("APK Hub")
        title.setObjectName("Title")
        subtitle = QLabel("Desktop shell for APK installs, ADB utilities, Samsung Kids and Parental Care operations.")
        subtitle.setObjectName("Muted")
        subtitle.setWordWrap(True)
        brand.addWidget(eyebrow)
        brand.addWidget(title)
        brand.addWidget(subtitle)
        hero_layout.addLayout(brand, 1)

        device_panel = QVBoxLayout()
        device_label = QLabel("Device")
        device_label.setObjectName("Muted")
        device_row = QHBoxLayout()
        self.device_combo = QComboBox()
        self.device_combo.setMinimumWidth(300)
        self.refresh_button = make_button("Refresh", ghost=True)
        self.refresh_button.clicked.connect(lambda: self.refresh_devices(clear_output=True))
        device_row.addWidget(self.device_combo, 1)
        device_row.addWidget(self.refresh_button)
        self.status_chip = QLabel("Disconnected")
        self.status_chip.setObjectName("StatusChip")
        device_panel.addWidget(device_label)
        device_panel.addLayout(device_row)
        device_panel.addWidget(self.status_chip)
        hero_layout.addLayout(device_panel, 1)
        root.addWidget(hero)

        splitter = QSplitter(Qt.Horizontal)
        splitter.setChildrenCollapsible(False)
        root.addWidget(splitter, 1)

        left = QWidget()
        left_layout = QVBoxLayout(left)
        left_layout.setContentsMargins(0, 0, 0, 0)
        left_layout.setSpacing(16)

        apk_card, apk_layout = make_card("APK Library", "Select or drag and drop APK files. Duplicates are checked by file name, matching the old app behavior.")
        self.apk_list = ApkListWidget()
        self.apk_list.files_dropped.connect(self.add_apk_paths)
        self.apk_list.setMinimumHeight(190)
        apk_layout.addWidget(self.apk_list)
        apk_buttons = QHBoxLayout()
        browse = make_button("Add", accent=True)
        remove = make_button("Remove", danger=True)
        clear = make_button("Clear", ghost=True)
        browse.clicked.connect(self.browse_apks)
        remove.clicked.connect(self.remove_selected_apks)
        clear.clicked.connect(self.clear_apks)
        apk_buttons.addWidget(browse)
        apk_buttons.addWidget(remove)
        apk_buttons.addWidget(clear)
        apk_layout.addLayout(apk_buttons)
        left_layout.addWidget(apk_card)

        functions_card, functions_layout = make_card("Functions", "Manual tools kept from the WPF version with modernized Qt workflows.")
        self.install_button = make_button("Install APKs", accent=True)
        self.install_button.clicked.connect(self.toggle_install)
        functions_layout.addWidget(self.install_button)
        more = make_button("More Tools")
        kids = make_button("Kids Tools")
        parental = make_button("Parental Care")
        save = make_button("Save Output", ghost=True)
        clear_output = make_button("Clear Output", ghost=True)
        more.clicked.connect(self.open_more_tools)
        kids.clicked.connect(self.open_kids_tools)
        parental.clicked.connect(self.open_parental_care)
        save.clicked.connect(self.save_output)
        clear_output.clicked.connect(lambda: self.output.clear())
        for button in (more, kids, parental, save, clear_output):
            functions_layout.addWidget(button)
        left_layout.addWidget(functions_card)
        left_layout.addStretch(1)
        splitter.addWidget(left)

        right = QWidget()
        right_layout = QVBoxLayout(right)
        right_layout.setContentsMargins(0, 0, 0, 0)
        right_layout.setSpacing(16)

        command_card, command_layout = make_card("Command Studio", "Run a direct ADB command or apply package actions to the selected device.")
        self.command_input = QLineEdit()
        self.command_input.setPlaceholderText("adb subcommand, for example: shell pm list packages")
        command_layout.addWidget(self.command_input)
        cmd_buttons = QHBoxLayout()
        run = make_button("Run Command", accent=True)
        clear_pkg = make_button("Clear Pkg")
        uninstall_pkg = make_button("Uninstall", danger=True)
        logcat_pkg = make_button("Logcat")
        run.clicked.connect(self.run_command_from_main)
        clear_pkg.clicked.connect(lambda: self.run_package_from_main("clear"))
        uninstall_pkg.clicked.connect(lambda: self.run_package_from_main("uninstall"))
        logcat_pkg.clicked.connect(lambda: self.run_package_from_main("logcat"))
        for button in (run, clear_pkg, uninstall_pkg, logcat_pkg):
            cmd_buttons.addWidget(button)
        command_layout.addLayout(cmd_buttons)
        right_layout.addWidget(command_card)

        response_card, response_layout = make_card("Latest Response", "Full transcript")
        self.output = QTextEdit()
        self.output.setObjectName("Output")
        self.output.setReadOnly(True)
        self.output.setMinimumHeight(360)
        response_layout.addWidget(self.output, 1)
        right_layout.addWidget(response_card, 1)
        splitter.addWidget(right)
        splitter.setSizes([360, 860])
        self.update_install_state()

    def current_serial(self) -> str | None:
        index = self.device_combo.currentIndex()
        if index < 0:
            return None
        value = self.device_combo.itemData(index)
        return str(value) if value else None

    def require_device(self) -> str | None:
        serial = self.current_serial()
        if not serial:
            QMessageBox.warning(self, "No Device Selected", "Please select a device before opening this tool.")
            return None
        return serial

    def append_output(self, message: str | None = None, *, clear: bool = False) -> None:
        if clear:
            self.output.clear()
        if message:
            self.output.append(message)
            self.output.moveCursor(QTextCursor.End)

    def run_task(self, task: Callable[[Callable[[str], None]], object], on_result: Callable[[object], None] | None = None) -> TaskThread:
        thread = TaskThread(task, self)
        self.threads.append(thread)
        thread.message.connect(self.append_output)
        thread.failed.connect(self._task_failed)
        if on_result:
            thread.result.connect(on_result)
        thread.finished.connect(lambda: self._remove_thread(thread))
        thread.start()
        return thread

    def _remove_thread(self, thread: TaskThread) -> None:
        if thread in self.threads:
            self.threads.remove(thread)

    def _task_failed(self, tb: str) -> None:
        self.append_output(tb)
        crash_dir = log_dir() / "Crashes"
        crash_dir.mkdir(parents=True, exist_ok=True)
        with (crash_dir / "crash_log.txt").open("a", encoding="utf-8") as file:
            file.write(f"[{timestamp()}] Task exception\n{tb}\n{'-' * 80}\n")
        QMessageBox.critical(self, "Error", f"An unexpected error occurred. A log was saved at:\n{crash_dir / 'crash_log.txt'}")

    def toggle_debug_device(self, checked: bool) -> None:
        self.debug_simulated_device = checked
        state = "enabled" if checked else "disabled"
        self.append_output(f"Debug simulated Android connection {state}")
        self.refresh_devices(clear_output=False)

    def refresh_devices(self, *, clear_output: bool = False) -> None:
        if clear_output:
            self.append_output("Checking devices connected...", clear=True)
        self.refresh_button.setEnabled(False)
        previous = self.current_serial()

        def task(_emit: Callable[[str], None]) -> list[tuple[str, str]]:
            return self.adb.devices()

        def finished(devices: object) -> None:
            self.refresh_button.setEnabled(True)
            device_list = devices if isinstance(devices, list) else []
            if self.debug_simulated_device:
                device_list = [("debug-android-5554", "Android Debug Device")] + [d for d in device_list if d[0] != "debug-android-5554"]
            self.device_combo.blockSignals(True)
            self.device_combo.clear()
            for serial, name in device_list:
                self.device_combo.addItem(f"{name} ({serial})", serial)
            if previous:
                for index in range(self.device_combo.count()):
                    if self.device_combo.itemData(index) == previous:
                        self.device_combo.setCurrentIndex(index)
                        break
            if self.device_combo.count() == 1:
                self.device_combo.setCurrentIndex(0)
            self.device_combo.blockSignals(False)
            count = self.device_combo.count()
            self.status_chip.setText(f"{count} Device(s) Connected" if count else "No Device Connected")
            self.append_output(f"{count} Device(s) Connected" if count else "No Device Connected")
            self.update_install_state()

        self.run_task(task, on_result=finished)

    def browse_apks(self) -> None:
        files, _ = QFileDialog.getOpenFileNames(self, "Select APK files", str(Path.home()), "APK files (*.apk)")
        if files:
            self.add_apk_paths(files)

    def add_apk_paths(self, paths: list[str]) -> None:
        duplicates: list[str] = []
        added: list[str] = []
        existing_names = {path.name for path in self.apk_files}
        for raw_path in paths:
            path = Path(raw_path)
            if path.suffix.lower() != ".apk":
                continue
            if path.name in existing_names:
                duplicates.append(path.name)
                continue
            self.apk_files.append(path)
            existing_names.add(path.name)
            added.append(path.name)
        self.reload_apk_list()
        if duplicates:
            msg = "The following APK file(s) are already selected:\n" + "\n".join(duplicates)
            if added:
                msg += "\n\nAdded:\n" + "\n".join(added)
            QMessageBox.warning(self, "File Selection Warning", msg)
            self.append_output("Duplicated file(s)", clear=True)
        elif added:
            self.append_output("Files loaded", clear=True)
        self.update_install_state()

    def reload_apk_list(self) -> None:
        self.apk_list.clear()
        for path in self.apk_files:
            item = QListWidgetItem(path.name)
            item.setToolTip(str(path))
            item.setData(Qt.UserRole, str(path))
            self.apk_list.addItem(item)

    def remove_selected_apks(self) -> None:
        selected = {Path(item.data(Qt.UserRole)) for item in self.apk_list.selectedItems()}
        if selected:
            self.apk_files = [path for path in self.apk_files if path not in selected]
            self.reload_apk_list()
            self.update_install_state()

    def clear_apks(self) -> None:
        self.apk_files.clear()
        self.reload_apk_list()
        self.update_install_state()

    def update_install_state(self) -> None:
        enabled = bool(self.current_serial()) and bool(self.apk_files) and not self.installing
        self.install_button.setEnabled(enabled or self.installing)

    def toggle_install(self) -> None:
        if self.installing:
            self.adb.stop_all()
            self.installing = False
            self.install_button.setText("Install APKs")
            self.apk_list.setEnabled(True)
            self.append_output("Installation canceled")
            self.update_install_state()
            return

        serial = self.require_device()
        if not serial:
            return
        if not self.apk_files:
            QMessageBox.information(self, "No APK selected", "Add at least one APK before installing.")
            return

        files = list(self.apk_files)
        self.installing = True
        self.install_button.setText("Stop")
        self.apk_list.setEnabled(False)
        self.append_output("Initializing the installation", clear=True)

        def task(emit: Callable[[str], None]) -> bool:
            success = True
            for apk_file in files:
                emit(f'Installing "{apk_file}"')
                result = self.adb.run_adb_parts(["install", "-r", "-d", str(apk_file)], serial, output_handler=emit)
                if "Success" not in result.output:
                    success = False
                    break
            emit("Installation complete" if success else "Installation not complete.")
            return success

        def finished(_result: object) -> None:
            self.installing = False
            self.install_button.setText("Install APKs")
            self.apk_list.setEnabled(True)
            self.update_install_state()

        self.run_task(task, on_result=finished)

    def run_command_from_main(self) -> None:
        serial = self.require_device()
        command = self.command_input.text().strip()
        if not serial or not command:
            return
        dialog = PackageActionDialog(self, serial, "run")
        dialog.input.setText(command)
        dialog.execute()

    def run_package_from_main(self, mode: str) -> None:
        serial = self.require_device()
        value = self.command_input.text().strip()
        if not serial:
            return
        if mode == "logcat" and value:
            self.open_logcat(serial, value.lower())
            return
        dialog = PackageActionDialog(self, serial, mode)
        if value:
            dialog.input.setText(value)
            dialog.execute()
        else:
            dialog.show()

    def open_logcat(self, serial: str, package_filter: str | None) -> None:
        dialog = LogcatDialog(self, serial, package_filter)
        dialog.show()

    def open_more_tools(self) -> None:
        serial = self.require_device()
        if serial:
            dialog = MoreToolsDialog(self, serial)
            dialog.show()

    def open_kids_tools(self) -> None:
        serial = self.require_device()
        if serial:
            dialog = KidsToolsDialog(self, serial)
            dialog.show()

    def open_parental_care(self) -> None:
        serial = self.require_device()
        if serial:
            dialog = ParentalCareDialog(self, serial)
            dialog.show()

    def save_output(self) -> None:
        text = self.output.toPlainText()
        if not text.strip():
            QMessageBox.information(self, "Output empty", "There is no output available.")
            return
        directory = log_dir() / "Output"
        directory.mkdir(parents=True, exist_ok=True)
        file_path = directory / "StatusOutputText.txt"
        file_path.write_text(text, encoding="utf-8")
        if QMessageBox.question(self, "File Saved", f"File saved at:\n{file_path}\n\nOpen the folder?") == QMessageBox.Yes:
            open_path(directory)

    def closeEvent(self, event: QCloseEvent) -> None:  # noqa: N802 - Qt API
        self.adb.stop_all()
        self.adb.run_adb_parts(["kill-server"])
        super().closeEvent(event)


def install_exception_hook() -> None:
    def hook(exc_type, exc_value, exc_tb) -> None:
        tb = "".join(traceback.format_exception(exc_type, exc_value, exc_tb))
        crash_dir = log_dir() / "Crashes"
        crash_dir.mkdir(parents=True, exist_ok=True)
        file_path = crash_dir / "crash_log.txt"
        with file_path.open("a", encoding="utf-8") as file:
            file.write(f"[{timestamp()}] Unhandled exception\n{tb}\n{'-' * 80}\n")
        app = QApplication.instance()
        if app:
            QMessageBox.critical(None, "Error", f"An unexpected error occurred. A log was saved at:\n{file_path}")
        else:
            print(tb, file=sys.stderr)

    sys.excepthook = hook


def main() -> int:
    install_exception_hook()
    app = QApplication(sys.argv)
    app.setApplicationName("APK Hub")
    app.setWindowIcon(app_icon())
    app.setStyleSheet(STYLE)
    window = MainWindow()
    window.show()
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
