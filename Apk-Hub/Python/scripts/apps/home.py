from application import *

class KidsHome(Application):
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

    def tc_01_parentalControl(self, res):
        self.moreOption = self.getCoord("Mais opções")
        touch(self.moreOption)
        self.parentalControl = self.getCoord("Controles parentais")
        touch(self.parentalControl)
        self.password()
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}parental_control.png"))
        return

    def tc_02_openSettings(self, res):
        self.cmdpmt.keyevent("KEYCODE_BACK")
        touch(self.moreOption)
        self.settings = self.getCoord("Configurações", False)
        touch(self.settings)
        self.password()
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}settings.png"))
        return
    
    def tc_03_openHomeMinus1(self, res):
        self.cmdpmt.keyevent("KEYCODE_BACK")
        self.swipeRight()
        wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}home-1.png"))
        self.testMinus1 = str(input("Testar o download dos apps da Home -1? (s/n): "))
        return
    
    def tc_04_downloadTravelBuddies(self, res):
        if self.testMinus1 == 's':    
            try:
                print("Verificando se o Travel Buddies está instalado")
                self.cmdandroid.uninstall_app("br.org.sidi.kidsplat.travel")
                print("Travel Buddies encontrado e desinstalado")
            except:
                print("Travel Buddies não está instalado")    
            self.swipeBellow()
            touch(self.getCoord("Todas as escolhas"))
            wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}download_apps.png"))
            if self.getCoord("Travel Buddies") is None:            
                self.swipeBellow()
            touch(self.getCoord("Travel Buddies"))
            touch(self.getCoord("com.sec.android.app.samsungapps:id/btn_detail_install"))
            wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}/abrir.png"), timeout= 300, interval= 2)
        return
    
    def tc_05_downloadCookisCollection(self, res):
        if self.testMinus1 == 's':
            try:
                print("Verificando se o Cooki's Collection está instalado")
                self.cmdandroid.uninstall_app("br.org.sidi.kidsplat.collection")
                print("Cooki's Collection encontrado e desinstalado")
            except:
                print("Cooki's Collection não está instalado")
            self.cmdpmt.keyevent("KEYCODE_BACK")
            touch(self.getCoord("Cooki's Collection"))
            time.sleep(1)
            touch(self.getCoord("com.sec.android.app.samsungapps:id/btn_detail_install"))
            wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}/abrir.png"), timeout= 300, interval= 2)
        return
    
    def tc_06_downloadMyArtStudio(self, res):
        if self.testMinus1 == 's':
            try:
                print("Verificando se o My Art Studio está instalado")
                self.cmdandroid.uninstall_app("br.org.sidi.kidsplat.artstudio")
                print("My Art Studio encontrado e desinstalado")
            except:
                print("My Art Studio não está instalado")
            self.cmdpmt.keyevent("KEYCODE_BACK")
            touch(self.getCoord("My Art Studio"))
            time.sleep(1)
            touch(self.getCoord("com.sec.android.app.samsungapps:id/btn_detail_install"))
            wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}/abrir.png"), timeout= 300, interval= 2)
        return
    
    def tc_07_downloadSweetJump(self, res):
        if self.testMinus1 == 's':
            try:
                print("Verificando se o Sweet Jump está instalado")
                self.cmdandroid.uninstall_app("com.SRUKRRnDInstituteUkraine.SweetJump")
                print("Sweet Jump encontrado e desinstalado")
            except:
                print("Sweet Jump não está instalado")
            self.cmdpmt.keyevent("KEYCODE_BACK")
            if self.getCoord("Sweet Jump") is None:
                self.swipeBellow()
            touch(self.getCoord("Sweet Jump"))
            time.sleep(1)
            touch(self.getCoord("com.sec.android.app.samsungapps:id/btn_detail_install"))
            wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}/abrir.png"), timeout= 300, interval= 2)
        return
    
    def tc_08_downloadMessengerKids(self, res):
        if self.testMinus1 == 's':
            try:
                print("Verificando se o Messenger Kids está instalado")
                self.cmdandroid.uninstall_app("com.facebook.talk")
                print("Messenger Kids encontrado e desinstalado")
            except:
                print("Messenger Kids não está instalado")
            self.cmdpmt.keyevent("KEYCODE_BACK")
            touch(self.getCoord("Messenger Kids"))
            time.sleep(1)
            touch(self.getCoord("com.sec.android.app.samsungapps:id/btn_detail_install"))
            wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}/abrir.png"), timeout= 300, interval= 2)
        return
    
    def tc_09_defineFriend(self, res):
        if self.testMinus1 == 's':
            self.backButton(x=2)
        touch(self.moreOption)
        touch(self.getCoord("Definir amigo"))
        checkScreen = self.getCoord("com.sec.android.app.kidshome:id/random_image")
        assert_is_not_none(checkScreen, msg= "Checked if friends page was opened or not")
        touch(self.getCoord("Crocro"))
        self.cmdpmt.keyevent("KEYCODE_BACK")
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}/crocro_friend.png"))
        return
    
    def tc_10_createProfile(self, res):
        self.swipeLeft()
        touch(self.moreOption)
        touch(self.parentalControl)
        self.password()
        touch(self.getCoord("com.sec.android.app.kidshome:id/profile_guide"))
        text("Criança 1")
        touch(self.getCoord("Crocro"))
        touch(self.getCoord("Data de nascimento", False))
        touch(self.getCoord("com.sec.android.app.kidshome:id/sesl_date_picker_calendar_header_text"))
        touch(self.getCoord("Dia"))
        text("01", enter=False)
        touch(self.getCoord("Mês", False))
        text("JAN.", enter= False)
        touch(self.getCoord("Ano", False))
        text("2013", enter=False)
        touch(self.getCoord("OK", False))
        touch(self.getCoord("Salvar"))
        profileCheck = self.getCoord("Criança 1")
        assert_is_not_none(profileCheck, msg= "Profile name was checked if it was correctly added")
        birthCheck = self.getCoord("01/01/2013", False)
        assert_is_not_none(birthCheck, msg= "Birth date was checked if it was correctly added")
        return
    
    def tc_11_createSecondProfile(self, res):
        touch(self.getCoord("Ver tudo", False))
        touch(self.getCoord("com.sec.android.app.kidshome:id/action_add"))
        text("Criança 2")
        touch(self.getCoord("Lisa"))
        touch(self.getCoord("Data de nascimento", False))
        touch(self.getCoord("com.sec.android.app.kidshome:id/sesl_date_picker_calendar_header_text"))
        touch(self.getCoord("Dia"))
        text("02", False)
        touch(self.getCoord("Mês", False))
        text("FEV.",  False)
        touch(self.getCoord("Ano", False))
        text("2015", False)
        touch(self.getCoord("OK", False))
        touch(self.getCoord("Salvar"))
        self.backButton(x=1)
        assert_is_not_none(self.getCoord("Criança 2"), msg= "Profile name 2 was checked if it was correctly added")
        assert_is_not_none(self.getCoord("02/02/2015", False), msg= "Birth date was checked if it was correctly added")
        touch(self.getCoord("Ver tudo", False))
        touch(self.getCoord("Criança 1"))
        self.backButton(x=1)
        return

    def tc_12_checkDailyUsage(self, res):
        touch(self.getCoord("com.sec.android.app.kidshome:id/time_gragh_summary_container"))
        validate = self.getCoord("com.sec.android.app.kidshome:id/app_usage_today_time")
        assert_is_not_none(validate, msg= "Checked if Daily Usage page it was opened or not")
        return
    
    def tc_13_screenTime(self, res):
        self.cmdpmt.keyevent("KEYCODE_BACK")
        if self.getCoord("Tempo de tela") is None:
            self.swipeBellow()    
        touch(self.getCoord("Tempo de tela"))
        touch(self.getCoord("com.sec.android.app.kidshome:id/sesl_switchbar_switch"))
        touch(self.getCoord("Igual a todos os dias", False))
        touch(self.getCoord("Personalizado", False))
        touch(self.getCoord("Minuto"))
        text("05", enter=False)
        touch(self.getCoord("OK", False))
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}screentime.png"))
        return
    
    def tc_14_checkScreenTime(self, res):
        self.backButton(x=2)
        x = self.getCoord( "O tempo acabou!")
        assert_is_not_none(x, msg= "Checked if the time screen is correctly over or not")
        touch(self.getCoord("com.sec.android.app.kidshome:id/action_extend"))
        self.password()
        touch(self.getCoord("Definir"))
        return
    
    def tc_15_deleteProfile(self,res):
        self.swipeRight()
        wait(Template(f"{self.defaultpath}\\images/{self.imgPATH}home-1.png"))
        touch(self.moreOption)
        touch(self.getCoord("Perfil"))
        self.password()
        touch(self.getCoord("com.sec.android.app.kidshome:id/action_delete"))
        touch(self.getCoord("Criança 1"))
        touch(self.getCoord("Excluir"))
        assert_is_none(self.getCoord("Criança 1"))
        self.backButton(x=1)
        assert_is_not_none(self.getCoord("Criança 2"), msg="Checked if the profile is correctly deleted and then it was changed to a second one")
        return
    
    def tc_16_createProfileMinus1(self, res):
        touch(self.moreOption)
        touch(self.getCoord("Perfil"))
        self.password()
        touch(self.getCoord("com.sec.android.app.kidshome:id/action_add"))
        text("Criança 3")
        touch(self.getCoord("Cooki"))
        touch(self.getCoord("Data de nascimento", False))
        touch(self.getCoord("com.sec.android.app.kidshome:id/sesl_date_picker_calendar_header_text"))
        touch(self.getCoord("Dia"))
        text("10", False)
        touch(self.getCoord("Mês", False))
        text("DEZ.",  False)
        touch(self.getCoord("Ano", False))
        text("2017", False)
        touch(self.getCoord("OK", False))
        touch(self.getCoord("Salvar"))
        self.backButton(x=1)
        assert_is_not_none(self.getCoord("Criança 3"), msg= "Profile name 3 was checked if it was correctly added")
        return
    
    def tc_17_screenTimeMinus1(self, res):
        touch(self.moreOption)
        touch(self.getCoord("Controles parentais"))
        self.password()
        touch(self.getCoord("Tempo de tela"))
        touch(self.getCoord("com.sec.android.app.kidshome:id/sesl_switchbar_switch"))
        touch(self.getCoord("Igual a todos os dias", False))
        touch(self.getCoord("Personalizado", False))
        touch(self.getCoord("Minuto"))
        text("05", enter=False)
        touch(self.getCoord("OK", False))
        assert_exists(Template(f"{self.defaultpath}\\images/{self.imgPATH}screentime.png"))
        self.backButton(x=1)
        assert_is_not_none(self.getCoord("Meta: 5 min"))
        touch(self.getCoord("Tempo de tela"))
        touch(self.getCoord("com.sec.android.app.kidshome:id/sesl_switchbar_switch"))
        self.backButton(x=2)
        return
    
    def tc_18_closeHome(self, res):
        touch(self.moreOption)
        touch(self.getCoord("Fechar"))
        self.password()
        self.checkAppStatus(False)
        return
    
    def tc_19_restoreHome(self, res):
        self.cmdpmt.shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
        touch(self.moreOption)
        touch(self.settings)
        self.password()
        touch(self.getCoord("Restaurar"))
        touch(self.getCoord("Redefinir"))
        self.cmdpmt.shell("self.cmd statusbar expand-settings")
        self.swipeLeft()
        touch(self.getCoord("Kids,Desativado,Botão"))
        time.sleep(2)
        checkResetHome = str(self.cmdpmt.shell("dumpsys window"))
        checkResetHome = re.search("com.sec.android.app.kidshome/com.sec.android.app.kidshome.parentalcontrol.setupwizard.ui.SetupWizardIntroActivity", checkResetHome)
        assert_is_not_none(checkResetHome, msg="Checked if Samsung Kids Home was reseted")
        return