from __future__ import annotations

import traceback
from typing import Callable, Any

from PySide6.QtCore import QThread, Signal


class TaskThread(QThread):
    message = Signal(str)
    result = Signal(object)
    failed = Signal(str)

    def __init__(self, task: Callable[[Callable[[str], None]], Any], parent=None) -> None:
        super().__init__(parent)
        self._task = task

    def run(self) -> None:
        try:
            value = self._task(self.message.emit)
            self.result.emit(value)
        except Exception:
            self.failed.emit(traceback.format_exc())
