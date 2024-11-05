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
import PySimpleGUI as sg
from PySimpleGUI import Window
from io import StringIO
import sys
import logging
import os
import time
import re

class StreamToLogger:
    """Classe que redireciona prints para um logger."""

    def __init__(self, logger, level=logging.INFO):
        self.logger = logger
        self.level = level

    def write(self, message):
        # Verifica se a mensagem não está vazia
        if message.strip():
            self.logger.log(self.level, message.strip())

class Executor:
    def __init__(self, serialno):
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
        self.log_stream = StringIO()
        self.defaultpath = Path(__file__).resolve().parents[1]
        logging.basicConfig(stream=self.log_stream, level=logging.DEBUG)
        self.logger = logging.getLogger(__name__)
        sys.stdout = StreamToLogger(self.logger, level=logging.INFO)
        sys.stderr = StreamToLogger(self.logger, level=logging.ERROR)
        # self.interface()

    def initialSetup(self):
        """
        Triggered by the user when pressing the INICIAR button. Makes the initial setup of the system and execute the tests.
        """
        self.clearAppList()
        self.addAppFromInput()
        #self.setSettings()
        self.getPasswordOrientation()
        self.execute()   
        return
    
    def outputUpdate(self):
        """
        Update the GUI output with the stdout data.
        """
        self.updateLog = True
        while self.updateLog == True:
            self.window['-OUTPUT-'].update(value = self.log_stream.getvalue())
            time.sleep(1)
        return
    
    def screenShare(self):
        """
        This function calls the screen share while the test is executed.
        """
        os.system('scrcpy')
        return
        
    def interface(self):
        """
        Init the GUI.
        """
        sg.theme('BlueMono')
        app_options = list(self.appMappings.keys())
        self.app_settings = {'Apagar mídia do dispositivo': self.deleteFiles,
                        'Limpar os dados': self.closeApps,
                        'Adicionar contato de teste': self.addContact,
                        'Instalar o Apk': self.installApk,
                        'Garantir as permissões': self.grantPermissions
                        }

        layout =[
            [sg.Text('Escolha o app:', size=(15,1), justification='center'),  sg.Text('Ajustes: ', size=(30,1), justification='center'), 
             sg.Text('Informações do dispositivo:',  justification='right')], 
            [sg.Column([[sg.Checkbox(app, key=f'-{app}-', size=(15,1))] for app in app_options], vertical_alignment='top'), 
            sg.Column([[sg.Checkbox(settings, key=f'-{settings}-',size=(20,1))] for settings in self.app_settings],vertical_alignment='top'),
            sg.Column([[sg.Text(f'Device Model: {self.model}', justification='left')],[sg.Text(f'Android Version: {self.osVersion}', justification='left')],
                       [sg.Text(f'Build Mode: {self.build}', justification='left')],[sg.Text(f'Ui Mode: {self.themeMode}', justification='left')], 
                       [sg.Text(f'Resolution: {self.res[0]} x {self.res[1]}', justification='left')]],vertical_alignment='top')],
            [sg.Button('INICIAR', button_color=('green')), sg.Button('VISUALIZAR', button_color='blue'),
             sg.Button('LIMPAR', disabled=True), sg.Button('ABRIR REPORT', disabled=True), 
             sg.Button('EXPORTAR REPORT', disabled=True), sg.Button('ABORTAR', disabled=True, button_color=('red'))],
            [sg.Text('Log de execução:')],
            [sg.Multiline(size=(78,10), key='-OUTPUT-', disabled= True, autoscroll=True, )]
        ]

        self.window = sg.Window('Kids Automation', layout ,
                        icon=f'{self.defaultpath}\\Python/images/ico/icon.ico', grab_anywhere=True)
        selected_app = []
        selected_settings = []
        while True:
            self.event, self.values =  self.window.read() # type: ignore
            if self.event == sg.WIN_CLOSED or self.event == 'ABORTAR':
                self.cmdpmt.cmd('kill-server')
                self.updateLog = False
                break
            
            if self.event == 'INICIAR': #When pressing INICIAR button
                self.window['EXPORTAR REPORT'].update(disabled=True)
                selected_app = [app for app in app_options if self.values[f'-{app}-']]
                selected_settings = [settings for settings in self.app_settings if self.values[f'-{settings}-']]
                self.app_instance = selected_app
                self.selected_settings = selected_settings
                if not selected_app:
                    sg.popup('Nenhum app escolhido, escolha um aplicativo para teste', 
                             icon=f'{self.defaultpath}\\Python/images/ico/icon.ico', location= self.window.current_location())
                else:
                    for app in app_options:
                        self.window[f'-{app}-'].update(disabled = True)
                    for setting in self.app_settings:
                        self.window[f'-{setting}-'].update(disabled = True)
                    self.window['LIMPAR'].update(disabled = False)
                    self.window['ABORTAR'].update(disabled=False)
                    self.window['INICIAR'].update(disabled=True) 
                    self.window.start_thread(self.outputUpdate, '--')
                    self.window.start_thread(self.initialSetup, '-FUNCTION COMPLETED-')

            elif self.event== 'VISUALIZAR':
                self.window.start_thread(self.screenShare, '-CLOSED SCREENMIRROR-')
                self.window['VISUALIZAR'].update(disabled = True)

            elif self.event== '-CLOSED SCREENMIRROR-':
                self.window['VISUALIZAR'].update(disabled = False)
                                                                            
            elif self.event== 'LIMPAR': #When pressing LIMPAR button
                    self.log_stream.seek(0)
                    self.log_stream.truncate(0)
                    self.window['-OUTPUT-'].update(value = '')
                    self.window['EXPORTAR REPORT'].update(disabled=True)
                    self.window['ABRIR REPORT'].update(disabled = True)
                    if self.updateLog == False:
                        self.window['LIMPAR'].update(disabled = True)

            elif self.event == '-FUNCTION COMPLETED-': #When tests end
                for app in app_options:
                    self.window[f'-{app}-'].update(disabled = False)
                for setting in self.app_settings:
                    self.window[f'-{setting}-'].update(disabled = False)
                self.updateLog = False
                self.window['ABORTAR'].update(disabled=True)
                self.window['INICIAR'].update(disabled=False)
                self.window['EXPORTAR REPORT'].update(disabled=False)
                self.window['ABRIR REPORT'].update(disabled = False)
                sg.popup(f'Aplicativo(s) testado(s): {", ".join(selected_app)}', title='Teste finalizado', 
                        icon= f'{self.defaultpath}\\Python/images/ico/icon.ico')
                selected_app = []
                selected_settings = []             

            elif self.event == 'EXPORTAR REPORT': #When pressing EXPORTAR REPORT button
                for reports in self.reports:
                    self.fixReport(reports)
                sg.popup(f'Report(s) exportado(s) em {", ".join(self.reports)}', title='Report exportado', 
                        icon= f'{self.defaultpath}\\Python/images/ico/icon.ico')
                
            elif self.event == 'ABRIR REPORT':
                for reports in self.reports:
                    os.startfile(f"{reports}/log.html")
        return
                
    def setSettings(self):
        """
        Set test run settings based on GUI options chooses.
        """
        for selected_setting in self.selected_settings:
            if selected_setting in self.app_settings:
                metodo = self.app_settings[selected_setting]
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
        self.cmdpmt.start_shell("am force-stop com.sec.android.app.kidshome")
        self.cmdpmt.start_shell("input keyevent KEYCODE_HOME")
        self.changeOrient(0)
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.parentalcontrol.pin.ui.PinActivity")
        Application.coord.append(("Password_Portrait", self.getPassword()))
        self.changeOrient(1)
        Application.coord.append(("Password_Landscape", self.getPassword()))
        self.changeOrient(0)
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
        return
    
    def cancellation_request(self):
        Application.cancellation_requested = True
        return
    
    def new_request(self):
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
    
    #Install all apks if eng mode
    def installEngApk(self):
        """
        Install selected apks if eng mode.
        """
        for apps in self.appList:
            for apkFile in os.listdir(f"{self.defaultpath}\\/rep/apks/{apps.appPkg}"):
                    if apkFile.endswith(".apk"):
                        logging.info(f'Instalando {apkFile} ...')
                        self.cmdpmt.cmd(f"adb install -r -d {self.defaultpath}\\rep\\apks\\{apps.appPkg}\\{apkFile}") 
        return

    def installUserApk(self):
        """
        Install selected apks if user mode.
        """
        for app in self.appList:
            if not isinstance(app, KidsHome):
                self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
                name = app.appName
                coordX, coordY = self.regexCoordinates(f"Não instalado, {name}(.|\n)*?bounds=(\"(.|\n)*?\")")
                if coordX and coordY:
                    logging.info("achou coord")
                    self.cmdpmt.shell(f"input tap {coordX} {coordY}")
                    time.sleep(3)
                else:
                    continue
                
                coordX, coordY = self.regexCoordinates(r'\"Instalar(.|\n)*?bounds=(\"(.|\n)*?\")')

                if coordX and coordY:
                    self.cmdpmt.shell(f"input tap {coordX} {coordY}")
                    for checkDownload in range(10):
                        time.sleep(15)
                        self.cmdpmt.shell("uiautomator dump")
                        dump = str(self.cmdpmt.shell(r"cat storage/emulated/0/window_dump.xml | sed 's/&#10;/ /g'"))
                        if re.search('com.sec.android.app.samsungapps:id/tv_detail_install_reduce_price', dump):
                            logging.info("======================\n\tDownload APK OK\n======================")
                            break
                        else:
                            logging.info("======================\n\tDownload APK NOT OK\n\tTRYING TO INSTALL\n======================")
                            if checkDownload == 9:
                                logging.info("Maximum tries to check if app is installed, test was aborted")
                                exit() 

                    self.cmdpmt.shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
                else:
                    continue
            else:
                self.cmdpmt.shell("am start -n com.samsung.android.kidsinstaller/com.samsung.android.kidsinstaller.install.ui.KidsHomeInstallActivity")
                coordX, coordY = self.regexCoordinates(r"com.samsung.android.kidsinstaller:id/button_start(.|\n)*?bounds=(\"(.|\n)*?\")")
                self.cmdpmt.shell(f"input tap {coordX} {coordY}")
                for checkDownload in range(15):
                    self.cmdpmt.shell("uiautomator dump")
                    dump = str(self.cmdpmt.shell(r"cat storage/emulated/0/window_dump.xml | sed 's/&#10;/ /g'"))
                    if re.search('com.sec.android.app.kidshome', dump):
                        logging.info("======================\nDownload HOME OK\n======================")
                        coordX, coordY = self.regexCoordinates(r"com.sec.android.app.kidshome:id/setup_wizard_intro_agreement(.|\n)*?bounds=(\"(.|\n)*?\")")   
                        self.cmdpmt.shell(f"input tap {coordX} {coordY}")
                        for _ in range(2):
                            coordX, coordY = self.regexCoordinates(r"Continuar(.|\n)*?bounds=(\"(.|\n)*?\")")   
                            self.cmdpmt.shell(f"input tap {coordX} {coordY}")   
                        coordX, coordY = self.regexCoordinates(r"pin_button_0(.|\n)*?bounds=(\"(.|\n)*?\")")
                        for _ in range(8):
                            self.cmdpmt.shell(f"input tap {coordX} {coordY}")
                        break
                    else:
                        logging.info("======================\nDownload HOME NOT OK\n======================")
                        if checkDownload == 9:
                            logging.info("Maximum tries to check if home is installed, test was aborted")
                            exit()
                        time.sleep(2)
        return
            
    #Uninstall all apks
    def installApk(self):
        """
        The app selected to test can be uninstalled before the execution and then installed with the last version in user binary or with the apk selected in rep folder in eng binary
        """    
        logging.info("O app será desinstalado")
        for i in self.appList:
            try:
                self.cmdpmt.shell(f"pm clear {i.appPkg}")
                self.cmdpmt.cmd(f"uninstall {i.appPkg}")
            except:
                logging.info("O aplicativo não foi encontrado, seguindo para a instalação...")
            if self.buildMode == "eng":
                self.installEngApk()
            elif self.buildMode == "user":
                self.installUserApk()
        return
    
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
                    def __init__(self, appPkg, appName, numTest):
                        super().__init__(appPkg, appName, numTest)
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
    


                