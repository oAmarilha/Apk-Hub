from application import *
from pathlib import Path 
from apps.browser import KidsBrowser
from apps.camera import KidsCamera
from apps.canvas import KidsCanvas
from apps.phone import KidsPhone
from apps.gallery import KidsGallery
from apps.musicband import KidsMusicBand
from apps.home import KidsHome
from apps.homeR import KidsHomeR
from apps.homeOld import KidsHomeOld
from apps.adventure import KidsAdventure
from apps.magicvoice import KidsMagicVoice
from apps.house import KidsHouse
from apps.studio import KidsStudio
from datetime import datetime #pegar data
import io
import logging
import os
import time
import re

class Executor:
    def __init__(self, serialno):
        self.log_stream = io.StringIO()  # Garante um novo StringIO a cada instância
        self.base_path = os.path.join(os.environ["USERPROFILE"], "Documents", "ApkHub", "Log", "Automation")
        if not Path(f"{self.base_path}").exists():
            os.mkdir(f"{self.base_path}")
            os.mkdir(f"{self.base_path}\\reports")
        self.initAirtest = InitAirtest(serialno=serialno)
        self.airtest = self.initAirtest.airtest
        self.cmdpmt = self.initAirtest.airtest.cmdpmt
        self.cmdandroid = self.initAirtest.airtest.cmdandroid
        self.buildMode = self.setBuildMode()
        self.androidVersion = self.setAndroidVersion()
        self.uiMode = self.setMode() 
        self.appList = []
        self.app_instance = []
        self.res = self.setRes()
        self.selected_settings = []
        self.model = str(self.cmdpmt.shell('getprop ro.product.model')).replace('\n', '')
        self.output = True
        self.appMappings = {"KidsHome": (KidsHome, "com.sec.android.app.kidshome", "Samsung Kids", 19),
                            "KidsHomeR": (KidsHomeR, "com.sec.android.app.kidshome", "Samsung Kids", 6),
                            "KidsHomeOld": (KidsHomeOld, "com.sec.android.app.kidshome", "Samsung Kids", 6),
                            "KidsGallery": (KidsGallery,"com.sec.kidsplat.kidsgallery", "Minha galeria", 6),
                            "KidsCamera": (KidsCamera, "com.sec.kidsplat.camera", "Minha câmera", 5),
                            "KidsBrowser": (KidsBrowser,"com.sec.kidsplat.kidsbrowser", "Meu navegador", 10),
                            "KidsPhone": (KidsPhone,"com.sec.kidsplat.phone", "Meu telefone", 3),
                            "KidsMusicBand": (KidsMusicBand,"com.sec.kidsplat.media.kidsmusic", "Banda musical da Lisa", 5),
                            "KidsAdventure": (KidsAdventure,"com.sec.kidsplat.kidsbcg", "Aventura do Crocro", 9),
                            "KidsMagicVoice": (KidsMagicVoice,"com.sec.kidsplat.kidstalk", "Minha voz mágica", 1),
                            "KidsCanvas": (KidsCanvas,"com.sec.kidsplat.drawing", "Tela do Bobby", 15),
                            "KidsHouse": (KidsHouse,"com.sec.android.app.kids3d", "Aldeia Amigos do Crocro", 16),
                            "KidsStudio": (KidsStudio,"br.org.sidi.kidsplat.artstudio","Meu estúdio de arte", 1)
                            }
        self.cmdpmt.start_shell("am force-stop com.sec.android.app.kidshome")
        self.cmdpmt.start_shell("svc power stayon true") #Keeps the screen awake when connected
        self.cmdpmt.start_shell("settings put system accelerometer_rotation 0") #Blocks the rotation via accelerometer
        self.defaultpath = Path(__file__).resolve().parents[1]

    def initialSetup(self):
        """
        Triggered by the user when pressing the INICIAR button. Makes the initial setup of the system and execute the tests.
        """
        self.new_request()
        self.clearAppList()
        self.addAppFromInput()
        self.setSettings()
        self.getPasswordOrientation()
        self.execute()
        return

    def setSettings(self):
        """
        Set test run settings based on GUI options chooses.
        """
        app_settings = {'Clear_Media': self.deleteFiles,
                        'Clear_Data': self.closeApps,
                        'Add_Contact': self.addContact,
                        'Grant_Permission': self.grantPermissions
                        }
        for selected_setting in self.selected_settings:
            if selected_setting in app_settings:
                metodo = app_settings[selected_setting]
                metodo()
        return

    #add app to the list
    def addApp(self, Application):
        """
        Add a new application class to execute.
        """
        self.appList.append(Application)
        logging.info(f"{Application.appName}({Application.appPkg}) adicionado(a) com {Application.numTest} caso(s) de teste(s)")
        return
    
    def addAppFromInput(self):
        """
        Add apps to test queue based on GUI chooses.
        """
        classNames = self.app_instance
        for className in classNames:
            if className in self.appMappings:
                appClass, appPkg, appName, numTest = self.appMappings[className]
                application = appClass(appPkg, appName, numTest, self.initAirtest)
                self.addApp(application)
            else:
                logging.debug(f"Classe '{className}' inválida. Verifique o nome da classe e tente novamente.")
                logging.debug("Classe inválida. Verifique o nome e tente novamente.")
        return

    def getPasswordOrientation(self):
        """
        Get coords from 0 button both portrait and landscape.
        """
        self.cmdpmt.unlock()
        self.cmdpmt.shell("am force-stop com.sec.android.app.kidshome")
        self.cmdpmt.shell("input keyevent KEYCODE_HOME")
        self.changeOrient(0)
        self.cmdpmt.shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.parentalcontrol.pin.ui.PinActivity")
        Application.coord.append(("Password_Portrait", self.getPassword()))
        self.changeOrient(1)
        Application.coord.append(("Password_Landscape", self.getPassword()))
        self.changeOrient(0)
        self.cmdpmt.shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
        return
    
    def cancellation_request(self):
        Application.cancellation_requested = True
        return

    def new_request(self):
        #Definindo o StringIO para capturar os logs
        logging.basicConfig(stream=self.log_stream, level=logging.DEBUG)
        self.logger = logging.getLogger()
        self.logger.setLevel(logging.DEBUG)
        for handler in self.logger.handlers[:]:
            self.logger.removeHandler(handler)
        self.stream_handler = logging.StreamHandler(self.log_stream)
        formatter = logging.Formatter('%(levelname)s:%(name)s:%(message)s')
        self.stream_handler.setFormatter(formatter)
        self.logger.addHandler(self.stream_handler)
        Application.cancellation_requested = False
        return

    #execute all tests of apps in the list
    def execute(self):
        """
        Execute all the test cases in each app added to the queue.
        """
        self.reports = []
        self.logname = None
        for i in self.appList:
            if not Application.cancellation_requested:
                self.logname = f"{self.base_path}\\reports\\log_{self.getDateTime()}_{i.appPkg}"
                os.mkdir(self.logname)

                auto_setup(__file__, logdir=self.logname) #init log 

                i.res = self.res
                    
                    
                i.executeTest(res = self.res, osVer = self.osVersion, uiMode = self.uiMode, buildMode = self.buildMode)

                simple_report(__file__, logpath = False , logfile = f"{self.logname}\\log.txt",output = f"{self.logname}/log.html") #dump log

                self.file_path = (f"{self.logname}/log.html")
                self.reports.append(self.logname)
        return
    
    def changeHtml(self, conteudo, busca, substituicao):
        """
        Substitute ``busca`` for ``substituicao`` in ``conteudo``.
        """
        return conteudo.replace(busca, substituicao)

    def fixReport(self, logname):
        """
        Substitute reference paths in log.html to able the file to be opened in S:
        """
        with open(f"{logname}/log.html", 'r', encoding='utf-8') as arquivo:
            conteudo = arquivo.read()
        pathRemoteReport = "S:/PROJECTS/KIDS/Kids Android 2015/Test/Test_VictorAmarilha/test/report"
        pathDelete = ''
        pathRemoteImages = 'S:/PROJECTS/KIDS/Kids Android 2015/Test/Test_VictorAmarilha/test/images'
        pathLocalImages = os.path.abspath(f"{self.defaultpath}\\/images").replace('\\','/')
        pathLocalImagesSlash = os.path.abspath(f"{self.defaultpath}\\/images").replace('\\', '\\\\')
        pathLocalRecording = os.path.abspath(logname) + '\\'
        pathLocalReport = os.path.abspath(logname).replace('\\', '\\\\') + '\\\\'
        pathScript = os.path.abspath(f"{self.defaultpath}\\/scripts").replace('\\', '\\\\') + '\\\\'
        pathPythonReport = str(os.getenv("LOCALAPPDATA")).replace('\\','/') + '/Programs/Python/Python311/Lib/site-packages/airtest/report'
        conteudo = self.changeHtml(conteudo, pathLocalReport, pathDelete)
        conteudo = self.changeHtml(conteudo, pathPythonReport, pathRemoteReport)
        conteudo = self.changeHtml(conteudo, fr'{pathScript}\\{self.defaultpath}\\images', pathRemoteImages)
        conteudo = self.changeHtml(conteudo, f'{self.defaultpath}\\Python/images', pathRemoteImages)
        conteudo = self.changeHtml(conteudo, pathLocalImages, pathRemoteImages)
        conteudo = self.changeHtml(conteudo, pathLocalImagesSlash, pathRemoteImages)
        conteudo = self.changeHtml(conteudo, pathLocalReport, pathDelete)
        conteudo = self.changeHtml(conteudo, pathLocalRecording, pathDelete)

        with open(f"{logname}/log_export.html", 'w', encoding='utf-8') as arquivo:
            arquivo.write(conteudo)
            return f"{logname}/log_export.html"
    
    #close all apps in the list + kids home
    def closeApps(self):
        """
        The app selected to test will be closed and cleared.
        """
        for i in self.appList:
            self.cmdpmt.start_shell(f"am force-stop {i.getAppPkg()}")
            if not isinstance(i, KidsHome):
                self.cmdpmt.start_shell(f"pm clear {i.getAppPkg()}")
        time.sleep(3)
            
    #get device resolution and set the res variable
    def setRes(self):
        """
        Get the device resolution and returns it in tuple shape.
        """
        size = str(self.cmdpmt.shell("wm size"))
        size = size.split(" ")
        size = size[-1].split("x")
        return (int(size[0]), int(size[1]))

    #set android version in androidVersion 
    def setAndroidVersion(self):
        """
        Get the device OS version and set the ``self.osVersion`` att.
        """
        osVer = int(self.cmdpmt.shell("getprop ro.build.version.release"))
        self.osVersion = osVer

        if osVer >= 12:
            return "new"
        else:
            return "old"

    #return the app list
    def getAppList(self):
        """
        Returns ``self.applist``.
        """
        return self.appList

    #return device resolution
    def getRes(self):
        """
        Returns ``self.applist``.
        """
        return self.res
    
    #Get coordinates from the number 0 in the password screen
    def getPassword(self):
        """
        Get coordinates from the number 0 in the password screen.
        """
        coordX, coordY = self.regexCoordinates(r"pin_button_0(.|\n)*?bounds=(\"(.|\n)*?\")")

        if coordX and coordY:
            return (coordX, coordY)
        else:
            logging.info("Coord not found")
            return
    
    #set uiMode according to the UI Mode of the device (Light/Dark)
    def setMode(self):
        """
        Set uiMode according to the UI Mode of the device (Light/Dark).
        """
        rep = str(self.cmdpmt.shell("cmd uimode night"))
        if rep.__contains__("no"):
            # Light Mode
            self.themeMode = 'Light Theme'
            return 0
        else:
            # Dark Mode
            self.themeMode = 'Dark Theme'
            return 1
    
    #set buildMode according to the type of binary of the device (eng or user)
    def setBuildMode(self):
        """
        Set buildMode according to the type of binary of the device (eng or user).
        """
        build = ""
        if str(self.cmdpmt.shell('cat /proc/version')).__contains__("eng"):
            build = 'eng'
            self.build = 'Engineering'
        else:
            build = 'user'
            self.build = 'User'
        return build
    
    #Get current date time
    def getDateTime(self):
        """
        Get current date time and returns it.
        """
        today = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
        return str(today).replace("-", "_").replace(":", "_")
    
    #Grant all permissions especified by permissions array from the apps from the list
    def grantPermissions(self):
        """
        This function allows permissions that it were previously set inside each class.
            Example:: 
            >>> class KidsHome(Application):
                    def __init__(self, appPkg, appName, numTest, airtestinstance):
                        super().__init__(appPkg, appName, numTest, airtestinstance)
                        self.permissions = ['POST_NOTIFICATIONS',
                                            'READ_CONTACTS',
                                            'READ_MEDIA_VIDEO',
                                            'READ_MEDIA_IMAGES',
                                            'READ_MEDIA_AUDIO']
        * ``self.permissions = ['']``    here it is defined the permissions that it will be allowed by grantPermission().
        * To get the permission that you need, you can run this adb command ``adb shell dumpsys package your.packagename``
        * It will return information about your package, find the section ``permissions`` and then copy the required permission at permission variable.
                        
        """
        for _ in self.appList:
            for permission in _.permissions:
                self.cmdpmt.start_shell(f"pm grant {_.appPkg} android.permission.{permission}")

    
    def addContact(self):
        """
        Add a test contact in the device.
        """
        time.sleep(1)
        for i in range(0, 3):
            self.cmdpmt.start_shell("am force-stop com.sec.android.app.kidshome")
            self.cmdpmt.start_shell("am start -a android.intent.action.INSERT -t vnd.android.cursor.dir/contact -e name 'Teste' -e phone 123456789")
            time.sleep(1)
            saveX, saveY = self.regexCoordinates(r'content-desc="Salvar"(.|\n)*?bounds=(\"(.|\n)*?\")')
            time.sleep(1)
            if saveX or saveY:
                self.cmdpmt.shell(f"input tap {saveX} {saveY}")
                self.cmdpmt.shell("am force-stop com.samsung.android.app.contacts")
                logging.info("Contact saved in the device")
                return

    

    def regexCoordinates(self, regex):
        """
        Perform RegEx search in dump string.
        """
        time.sleep(1)
        self.cmdpmt.shell("uiautomator dump")
        dump = str(self.cmdpmt.shell(r"cat storage/emulated/0/window_dump.xml | sed 's/&#10;/ /g'"))
        reResult = re.search(
            regex
            , dump)
        if reResult:
            coord = reResult.group(2).replace('"', '').replace(']', ' ').replace('[', '').replace(',', ' ').strip(' ').split(' ')
            coordX = int((int(coord[0])+int(coord[2]))/2)
            coordY = int((int(coord[1])+int(coord[3]))/2)
            return coordX, coordY
        else:
            return None, None
            
    def deleteFiles(self, pictures = True, movies = True, music = True, contacts = True):
        """
        :param pictures: Default is True, if do not want delete pictures folder before the begin of the test set as False.
        :param movies: Default is True, if do not want delete movies folder before the begin of the test set as False.
        :param music: Default is True, if do not want delete music folder before the begin of the test set as False
        :param contacts: Default is True, if do not want delete contacts info before the begin of the test set as False
            Example::

            >>> self.deleteFiles(pictures = False)

        The function will not delete pictures folder at the begginer of the test, but instead it will delete all the others folders and contacts.
        """
        self.cmdpmt.start_shell('rm -r sdcard/window_dump.xml')
        self.cmdpmt.start_shell('rm -r sdcard/Pictures')
        logging.info("Pasta pictures Apagada")
        self.cmdpmt.start_shell('rm -r sdcard/Movies')
        logging.info("Pasta movies Apagada")
        self.cmdpmt.start_shell('rm -r sdcard/Music')
        logging.info("Pasta music Apagada")
        self.cmdpmt.start_shell(r'am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file:///sdcard/Pictures/')
        self.cmdpmt.start_shell('pm clear com.samsung.android.providers.contacts')
        logging.info("Mídia Apagada")
        time.sleep(3)
        return    
    
    def changeOrient(self, orient):
        """
        Change device orient.
        """
        self.cmdpmt.start_shell(f'settings put system user_rotation {orient}')
        return
    
    def clearAppList(self):
        """
        Clear app list. Executed before add new apps to the queue.
        """
        self.appList = []
        return
    


                