from application import *

class KidsHomeR(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
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
    
    def tc_01_enterHome(self, res): #Home -1
        self.swipeRight() #Enter Home -1
        time.sleep(4) 
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}home_one.png"))
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return
    
    def tc_02_createProfile(self, res):
        self.moreOption = self.getCoord("Mais opções")
        touch(self.moreOption)
        self.parentalControl = self.getCoord("Controles parentais")
        touch(self.parentalControl)
        self.password()
        time.sleep(5)
        self.defineProfile = self.getCoord("Definir perfil")
        touch(self.defineProfile)
        self.selectImageProfile = self.getCoord("médica")
        touch(self.selectImageProfile)
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}medica.png"))
        self.removeImage = self.getCoord("Excluir")
        touch(self.removeImage)
        self.selectImageProfileAgain = self.getCoord("índia")
        touch(self.selectImageProfileAgain)
        text("Kids One", enter= False)
        self.birthDate = self.getCoord("nascimento")
        touch(self.birthDate)
        self.selectDate = self.getCoord("1 de")
        touch(self.selectDate)
        self.okButton = self.getCoord("OK")
        touch(self.okButton)
        self.saveButton = self.getCoord("Salvar")
        touch(self.saveButton)
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}kids_one.png"))
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return

    def tc_03_createSecondProfile(self, res):
        touch(self.moreOption)
        touch(self.parentalControl)
        self.password() 
        time.sleep(5)
        self.seeAll = self.getCoord("Ver tudo")
        touch(self.seeAll)
        self.addButton = self.getCoord("Adicionar")
        touch(self.addButton)
        self.selectImageProfile = self.getCoord("médica")
        touch(self.selectImageProfile)
        text("Kids Two", enter= False)
        self.birthDate = self.getCoord("nascimento")
        touch(self.birthDate)
        self.selectDate = self.getCoord("1 de")
        touch(self.selectDate)
        self.okButton = self.getCoord("OK")
        touch(self.okButton)
        self.saveButton = self.getCoord("Salvar")
        touch(self.saveButton)
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}kids_one.png"))
        self.cmdpmt.keyevent("KEYCODE_BACK") 
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return

    def tc_04_deleteProfile(self, res):
        touch(self.moreOption)
        touch(self.parentalControl)
        self.password() 
        time.sleep(5)
        touch(self.seeAll)
        self.deleteButton = self.getCoord("Excluir")
        touch(self.deleteButton)
        self.selectProfile = self.getCoord("Two")
        touch(self.selectProfile)
        time.sleep(3)
        self.deleteProfile = self.getCoord("com.sec.android.app.kidshome:id/icon")
        touch(self.deleteProfile)
        time.sleep(3)
        self.cmdpmt.keyevent("KEYCODE_BACK") 
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return
    
    def tc_05_closeKidsHome(self, res):
        touch(self.moreOption)
        self.closeKidsHome = self.getCoord("Fechar")
        touch(self.closeKidsHome)
        self.password()
        time.sleep(2)
        return
    
    def tc_06_restoreSamsungKids(self, res):
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
        time.sleep(5)
        touch(self.moreOption)
        self.settings = self.getCoord("Configurações")
        touch(self.settings)
        self.password() 
        time.sleep(2)
        self.restore = self.getCoord("Restaurar")
        touch(self.restore)
        self.redefine = self.getCoord("Redefinir")
        touch(self.redefine)
        return
    


