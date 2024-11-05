from airtest.core.android.adb import ADB
from airtest.core.android.android import Android
from airtest.core.api import *
from airtest.core.assertions import *
from airtest.report.report import *
import logging
import re
import os

class AirTest:
    """
    Setup current connected device.
    """
    def __init__(self, serialno):
        self.serialno = serialno
        self.cmdpmt = ADB(serialno=self.serialno, server_addr= None)
        self.cmdandroid = Android(serialno=self.serialno)
        init_device(platform="Android", uuid= self.serialno)

# airtest = AirTest()