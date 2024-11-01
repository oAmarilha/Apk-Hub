from airtestInstance import *
import random
from pathlib import Path 

class InitAirtest:
    def __init__(self, serialno):
        self.serialno = serialno
        self.airtest = AirTest(serialno=self.serialno)
        self.cmdpmt = self.airtest.cmdpmt
        self.cmdandroid = self.airtest.cmdandroid

class Application:
    coord = []
    cancellation_requested = False
    def __init__(self, appPkg, appName, numTest, airtestinstance):
        self.appPkg = appPkg
        self.appName = appName
        self.numTest = numTest
        self.numPass = 0
        self.numFail = 0
        self.imgPATH = ""
        self.orient = 0
        self.res = (int(0),int(0))
        self.airtest = airtestinstance.airtest
        self.cmdpmt = airtestinstance.cmdpmt
        self.cmdandroid = airtestinstance.cmdandroid
        self.defaultpath = Path(__file__).resolve().parents[1]
        
    #Getters and setters
    def getAppPkg(self):
        return self.appPkg

    def setAppPkg(self, newPkg):
        self.appPkg = newPkg
        return

    def getNumTest(self):
        return self.numTest

    def setNumTest(self, newNumTest):
        self.numTest = newNumTest
        return

    def getNumPass(self):
        return self.numPass
    
    def incrementNumPass(self):
        self.numPass += 1
        return

    def getNumFail(self):
        return self.numFail
    
    def incrementNumFail(self):
        self.numFail += 1
        return
    
    #Function back button from device    
    def backButton(self, x=0):
        """
        :param x: (int) times to perform the back button action
            Example::

            >>> self.backButton(x=2)
        It will be performed a back action twice.
        """
        for _ in range(x):
            self.cmdpmt.keyevent("KEYCODE_BACK")
        return    
    
    #click in password 0-0-0-0
    def password(self):
        """
        :return: Touch four times at ``password`` coordinates defined as ``(0000)``
        """
        time.sleep(1)
        if self.orient == 1:
            for _ in Application.coord:
                if _[0] == "Password_Landscape":
                    for i in range(0, 4):
                        touch(_[1])  
                    time.sleep(1) 
                    return 
        else:
            for _ in Application.coord:
                if _[0] == "Password_Portrait":
                    for i in range(0, 4):
                        touch(_[1])
                    time.sleep(1) 
                    return
        return
    
    def setImgPATH(self, filename):
        """
        Set the path to image repository with the actual python filename.
        """
        return f"{self.defaultpath}\\images/{str(filename)[:-3]}/"

    
    def hierarchyDump(self):
        """
        This function is used to get the dump from uiautomator, it will be return data (it contains dump content decoded).
        In the repository located at src/misc it'll be saved a new file with the name of the application where this function was previosly called.
        """
        self.cmdpmt.shell('uiautomator dump')
        dump = str(self.cmdpmt.shell("cat storage/emulated/0/window_dump.xml"))
        return dump
        
    def getCoord(self, textRequired= "", getDump= True):   
        """
        :param textRequired: String to find in dump
        :param getDump: Default is set as True if you want to get an element in a new UI Hierarchy dump, False if you want to get an element that it already exists in the last UI dump.
        :return: The coordinates are returned as a tuple
            Example::

            >>> x = self.getCoord(textRequired= "Botão 1")

        The ``Botão 1`` will be searched in a new UI Hierarchy dump and then the variable x gets the coordinates related as a tuple::

            >>> touch(self.getCoord(textRequired= "Botão 2", getDump= False))

        The ``Botão 2`` is in the same UI dump than the last one, so you can set dump parameter as False, it will be searched in the same dump and then it will be touched in these coordinates::

            >>> touch(self.getCoord(textRequired= "Salvar", getDump= True))

        The ``Salvar`` element is in another screen than last dump, so dump needs to be True to correctly find the element and then a Touch action is performed (Default is True, it is not necessary set dump as True)::
        """
        time.sleep(2)
        if getDump == True:
            self.cmdpmt.shell("uiautomator dump")
        self.dump = str(self.cmdpmt.shell(r"cat storage/emulated/0/window_dump.xml | sed 's/&#10;/ /g'"))
        reResult = re.search(fr"{textRequired}(.|\n)*?bounds=(\"(.|\n)*?\")", self.dump)
        if reResult:
            coord = reResult.group(2).replace('"', '').replace(']', ' ').replace('[', '').replace(',', ' ').strip(' ').split(' ')
            coord = int((int(coord[0])+int(coord[2]))/2) , int((int(coord[1])+int(coord[3]))/2)
            time.sleep(2)
            return coord
        else:
            return None
            
    def swipeRight(self):
        """
        This function could be used to swipe according each screen resolution from left side to right side.
            Example:: 
            >>> def tc_03_openHomeMinus1(self, res):
                    self.cmdpmt.keyevent("KEYCODE_BACK")
                    self.swipeRight()
                    wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}home-1.png"))
                    return
        ``In this case after the previous command, the function will be called and make the swipe action``
        """
        orientation = self.getOrientation()
        if orientation == "portrait" or "foldable":
            rightSwipe = [(self.res[0]/5 , self.res[1]/2), (self.res[0]*9/10 , self.res[1]/2)]
        else:
            rightSwipe = [(self.res[0]*3/8, self.res[1]/3) , (self.res[0]*5/8 , self.res[1]/3)]
        swipe(rightSwipe[0], rightSwipe[1])
        return
    
    def swipeLeft(self):
        """
        This function could be used to swipe according each screen resolution from right side to left side.
            Example:: 
            >>> def tc_03_openHomeMinus1(self, res):
                    self.cmdpmt.keyevent("KEYCODE_BACK")
                    self.swipeLeft()
                    wait(Template(f"{self.defaultpath}\\images\\{self.imgPATH}home-1.png"))
                    return
        ``In this case after the previous command, the function will be called and make the swipe action``
        """
        orientation = self.getOrientation()
        if orientation == "portrait":
            leftSwipe = [(self.res[0]*9/10 , self.res[1]/2) , (self.res[0]/5 , self.res[1]/2)]
        else:
            leftSwipe = [(self.res[0]*5/8  , self.res[1]/2) , (self.res[0]*3/8 , self.res[1]/2)]
        swipe(leftSwipe[0], leftSwipe[1])
        return
    
    def swipeBellow(self):
        """
        This function could be used to swipe according each screen resolution from down direction to up.
            Example:: 
            >>> def tc_03_openHomeMinus1(self, res):
                    self.cmdpmt.keyevent("KEYCODE_BACK")
                    self.swipeBellow()
                    wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}home-1.png"))
                    return
        ``In this case after the previous command, the function will be called and make the swipe action``
        """
        orientation = self.getOrientation()
        if orientation == "portrait":
            downSwipe = [(self.res[0]/2 , self.res[1]*3/4), (self.res[0]/2 , self.res[1]/4)]
        else:
            downSwipe = [(self.res[0]/2 , self.res[1]*3/4) , (self.res[0]/2 , self.res[1]*1/3)]
        swipe(downSwipe[0], downSwipe[1])
        return
    
    def swipeUp(self):
        """
        This function could be used to swipe according each screen resolution from up direction to down.
            Example:: 
            >>> def tc_03_openHomeMinus1(self, res):
                    self.cmdpmt.keyevent("KEYCODE_BACK")
                    self.swipeUp()
                    wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}home-1.png"))
                    return
        ``In this case after the previous command, the function will be called and make the swipe action``
        """
        orientation = self.getOrientation()
        if orientation == "portrait":
            upSwipe = [(self.res[0]/2 , self.res[1]/4), (self.res[0]/2 , self.res[1]*3/4)]
        else:
            upSwipe = [(self.res[0]/2 , self.res[1]*1/4), (self.res[0]/2 , self.res[1]*3/4)]
        swipe(upSwipe[0], upSwipe[1])
        return
    
    def recordScreen(self, orientation= 1):
        """
        :param orientation: Define if the video must be recorded as landscape mode, portrait mode or in rotation mode (if any app need to rotate during the execution)\n
        Default is portrait.
        ``PORTRAIT : 1``
        ``LANDSCAPE : 2``
        ``ROTATION: 0``\n
        Initialize the recording of screen with the correct adjust of resolution.
        """
        self.cmdandroid.adjust_all_screen(self.cmdandroid.start_recording(orientation=orientation, mode = "ffmpeg", bit_rate_level=5))
        return
    
    def stopRecording(self):
        """
        It stops the active recording and save it in reports folder.
        """
        self.cmdandroid.stop_recording()
        return    
    
    def checkAppStatus(self, opened = True):
        """
        Verify if the app tested is opened or not.\n
        :param opened: Set True or False to verify if the app that it's being tested is currently opened or not.
        ``False: Verify if the app set isn't opened``
        ``True: Verify if the app set is opened``\n
        This function verify the currently app opened in the device, via package, and then compare if it is or not opened according the parameter ``opened``. 
            Example::
            >>> def tc_01_testAppNotOpened:
                    checkAppStatus(False)
                return
        In this case the function will be executed to verify if the app is not opened, in the report will be added the result
            >>> def tc_02_testAppOpened:
                    checkAppStatus(True)
                return
        In this case the function will verify if the package is opened while the script is executed.\n
        ``NOTE: The parameter "opened" is set by default as True, it is not necessary add it as True``
        """
        time.sleep(2)
        currentApp = str(self.cmdpmt.shell("dumpsys window"))
        compare = re.search(fr"mCurrentFocus.*?{self.appPkg}/", currentApp)
        if opened == True:    
            assert_is_not_none(compare, msg=f"Checked if the app {self.appName}({self.appPkg}) is opened")
        else:
            assert_is_none(compare, msg=f"Checked if the app {self.appName}({self.appPkg}) is not opened")
        return
        
    def findElement(self, res, elementName = "", findIt = True):
        """
        It can be used to verify if the element requested is in the screen, useful when the Template is too small or has differences between versions, to check if some screen was correctly open or to verify if a specific element in the screen appear.\n
        :param elementName: Give a string with the name of the element (text, resource-id, any other that refers to the requested element) to find the element in the ``CURRENT`` screen.
        :param findIt: ``Default is True``, change it to not find the element in the screen.
        """
        self.cmdpmt.shell("uiautomator dump")
        currentApp = str(self.cmdpmt.shell(r"cat storage/emulated/0/window_dump.xml | sed 's/&#10;/ /g'"))
        compare = re.search(f"{elementName}", currentApp)
        if findIt == True:    
            assert_is_not_none(compare, msg=f"Checked if the element ({elementName}) exists in the current screen")
        else:
            assert_is_none(compare, msg=f"Checked if the element ({elementName}) does not exists in the current screen")
        return

    def random_touch(self):
        """
        To randomly touch the screen 
        """

        inferior_bound_x = self.res[0]/4
        superior_bound_x = 3*self.res[0]/4
        inferior_bound_y = self.res[1]/4
        superior_bound_y = 3*self.res[1]/4

        touch_x = int(random.uniform(inferior_bound_x, superior_bound_x))
        touch_y = int(random.uniform(inferior_bound_y, superior_bound_y))

        touch((touch_x, touch_y))
        time.sleep(2)
        return
    
    def getOrientation(self):
        """
        This function is used to get the orientation of the device, "Portrait" or "Landscape"
        return: The orientation of device as string "portrait" or "landscape"
        """
        resolution = self.cmdandroid.get_current_resolution()
        width = resolution[0]
        height = resolution[1]
        if width > height:
            return "landscape"
        elif (height/width) <= 1.33:
            return "foldable"
        else:
            return "portrait"
        
    def oldPassword(self):
        "to press the 0 button 4 times"    
        self.passold = self.getCoord("pin_button_0")
        self.airtest.touch(self.passold, times=4)
        time.sleep(3)
        return
    
    def waitElement(self, interval = 3, qtd = 5, target=''):
        """
        This function is used to wait an element appears in the screen
        :param interval: This parameter defines the interval time to try to find again the element in the screen (default is 3 seconds).
        :param qtd: This parameter defines how many times the function will try to find the element in the screen (default is 5 times).
        :param target: This paramater defines the element that the function will try to find in the screen (It can be the id name, a package app name, resource-id, any element that exists in the dump hierarchy).
        return: This function returns the value of element, it could be None (if the element is not found in the screen) or the coordinates of the element as a tuple (x, y), it is possible to add this function in AirTest Asserts functions or any function that receives coordinates as parameter.
        """
        element = None
        for _ in range(qtd):
            element = self.getCoord(target)
            if element is not None:
                break
            else:
                time.sleep(interval)
        return element

