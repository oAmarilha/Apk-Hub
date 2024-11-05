from application import *

class KidsPhone(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
        self.permissions = ['READ_CONTACTS',
                            'CALL_PHONE',
                            'POST_NOTIFICATIONS'
                            ]

    def executeTest(self, res, osVer, uiMode, buildMode):
        self.imgPATH = self.setImgPATH(os.path.basename(__file__)) #set image path
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
        self.recordScreen() #start recording
        
        for i in dir(self):
           if self.cancellation_requested:
                LOGGING.info("Cancellation requested, stopping test case execution.")
                self.stopRecording()  # Para a gravação de tela
                return
           if i.startswith('tc_'):
                result = getattr(self, i) #takes method name starting with self.tc_
                try:
                    result(res, osVer, uiMode, buildMode)
                except:
                    print(f"error executing {str(result)}")
                    self.stopRecording() #end recording
                    return
                
        self.stopRecording() #end recording
        return
    
    def tc_01_openApp(self, res, osVer, uiMode, buildMode):
        #Given the user is in Kids Home
        #When it opens the app
        #And access the permissions
        #Then the app must open in the Main screen.

        touch(self.getCoord(self.appName)) 
        if uiMode == 0 :
             wait(Template(f"{self.imgPATH}opened.png"), timeout=2)
        else:
            wait(Template(f"{self.imgPATH}opened_dark.png"), timeout=2)
        return
        
    
    def tc_02_addContact(self, res, osVer, uiMode, buildMode):
        #Given the user is in Main Menu
        #When the user press the add option
        #And select a contact
        #Then the contact must appear as added in the main screen.
        touch(self.getCoord("Adicion"))
        time.sleep(1)
        self.password()
        time.sleep(1)
        touch(self.getCoord("Adicionar"))
        time.sleep(1)
        try:
            touch(self.getCoord("Teste")) 
        except:
            if uiMode == 0:
                testContact = Template(f'{self.imgPATH}testContact.png')
            else:
                testContact = Template(f"{self.imgPATH}testContact_dark.png")
            if not testContact:
                swipeBelow = f'input touchscreen swipe {res[0]/2} {res[1]*3/4} {res[0]/2} {res[1]/4}'
                shell(swipeBelow)
                time.sleep(1)
                touch(testContact)
        addContact = self.getCoord("Concluir")
        if addContact is None :
            addContact = self.getCoord("OK")
        touch(addContact)
        time.sleep(2)
        self.backButton(x=1)
        time.sleep(3)
        screen = self.hierarchyDump()
        assert_equal(screen.__contains__("Teste"), True)
        return
    
    def tc_03_deleteContact(self, res, osVer, uiMode, buildMode):
        #Given the user is in Main Menu
        #When the user press the add option
        #And select an added contact
        #And delete the selected contact
        #Then the contact must disappear in the main screen.
        touch(self.getCoord("Adicion"))
        time.sleep(1)
        self.password()
        time.sleep(3)
        touch(self.getCoord("Mais opções"))
        touch(self.getCoord("Editar"))
        touch(self.getCoord("Apagar tudo"))
        self.cmdpmt.keyevent("KEYCODE_BACK") 
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return
    