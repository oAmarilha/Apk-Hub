from application import *

class KidsHomeOld(Application):
    def __init__(self, appPkg, appName, numTest, airtestinstance):
        super().__init__(appPkg, appName, numTest, airtestinstance)
        self.permissions = ['POST_NOTIFICATIONS',
                            'READ_CONTACTS',
                            'READ_MEDIA_VIDEO',
                            'READ_MEDIA_IMAGES',
                            'READ_MEDIA_AUDIO']

    def executeTest(self, res, osVer, uiMode, buildMode):
        self.imgPATH = self.setImgPATH(os.path.basename(__file__))
        self.recordScreen() #start recording
        
        for i in dir(self):
           if self.cancellation_requested:
                LOGGING.info("Cancellation requested, stopping test case execution.")
                self.stopRecording()  # Para a gravação de tela
                return
           if i.startswith('tc_'):
                result = getattr(self, i) #takes method name starting with self.tc_
                try:
                    result(res = res)
                except:
                    print(f"error executing {str(result)}")
                    self.stopRecording() #end recording
                    return
                
        self.stopRecording() #end recording
        return
    
    def tc_01_enterHome(self, res): 
        time.sleep(3)
        self.swipeRight() 
        time.sleep(4) 
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}home_one.png"))
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return
    
    def tc_02_createProfile(self, res):
        self.moreOption = self.getCoord("Mais opções")
        touch(self.moreOption)
        self.parentalControl = self.getCoord("Controle parental")
        touch(self.parentalControl)
        self.oldPassword()
        self.defineProfile = self.getCoord("Nome da criança")
        touch(self.defineProfile)
        time.sleep(2)
        text("Kids One", enter= False)
        self.profileBirthday = self.getCoord("DD/MM/AAAA")
        touch(self.profileBirthday)
        self.selectDate = self.getCoord("1 de")
        touch(self.selectDate)
        self.okButton = self.getCoord("OK")
        touch(self.okButton)
        self.saveButton = self.getCoord("Salvar")
        touch(self.saveButton)
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return
    
    def tc_03_createSecondProfile(self, res):
        self.moreOption = self.getCoord("Mais opções")
        touch(self.moreOption)
        self.parentalControl = self.getCoord("Controle parental")
        touch(self.parentalControl)
        self.oldPassword()
        self.profileList = self.getCoord("Lista de perfil da criança")
        touch(self.profileList)
        touch(Template(f"{self.imgPATH}addButton.png"))    
        time.sleep(2)
        text("Kids Two", enter= False)
        self.profileBirthday = self.getCoord("DD/MM/AAAA")
        touch(self.profileBirthday)
        self.selectDate = self.getCoord("1 de")
        touch(self.selectDate)
        self.okButton = self.getCoord("OK")
        touch(self.okButton)
        self.saveButton = self.getCoord("Salvar")
        touch(self.saveButton)
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        time.sleep(5)
        return

    def tc_04_setDailyPlaytime(self, res):
        self.moreOption = self.getCoord("Mais opções")
        touch(self.moreOption)
        self.parentalControl = self.getCoord("Controle parental")
        touch(self.parentalControl)
        self.oldPassword()
        touch(Template(f"{self.imgPATH}playTime.png")) 
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return
    
    def tc_05_deleteProfile(self, res):
        self.moreOption = self.getCoord("Mais opções")
        touch(self.moreOption)
        self.parentalControl = self.getCoord("Controle parental")
        touch(self.parentalControl)
        self.oldPassword()
        self.touchProfile = self.getCoord("Kids Two")
        touch(self.touchProfile)
        self.removeButton = self.getCoord("button2")
        touch(self.removeButton)
        self.removeConfirm = self.getCoord("buttonPanel")
        touch(self.removeButton)
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return
    
    def tc_06_closeKidsHome(self, res):
        self.moreOption = self.getCoord("Mais opções")
        touch(self.moreOption)
        self.closeKidsHome = self.getCoord("Fechar")
        touch(self.closeKidsHome)
        return




    

    