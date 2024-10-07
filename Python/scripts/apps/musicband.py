from application import *

class KidsMusicBand(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
        self.permissions = ["READ_EXTERNAL_STORAGE",
                            "POST_NOTIFICATIONS",
                            "READ_MEDIA_AUDIO",
                            "READ_MEDIA_VIDEO",
                            "READ_MEDIA_IMAGES",
                            ]
        self.orient = 1
    
    def executeTest(self, res, osVer, uiMode, buildMode):
        self.imgPATH = self.setImgPATH(os.path.basename(__file__)) #set image path
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")

        self.recordScreen(orientation= 2) #start recording
        
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

        #Given that the application is allowed on Kids Home 
        #When the user taps the icon “Lisa's Music Band” 
        #Then the application will be open in landscape mode 

        self.cmdpmt.cmd(f'push {self.defaultpath}\\rep/Musics /sdcard/Music')
        self.cmdpmt.start_shell(r'am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file:///sdcard/Music/')
        touch(self.getCoord("Banda musical da Lisa"))
        return
    
    def tc_02_addMusic(self, res, osVer, uiMode, buildMode):
        #Given that the user is on the main screen
        #When the user taps the "music list" icon and  "+" button and "+"
        #And select a music
        #Then the music will be added to Lisa´s Music Band Application


        def add(name):
            self.password()
            sleep(1)
            touch(self.getCoord("Adicionar"))
            sleep(1)
            touch(self.getCoord(name))
            sleep(1)
            if osVer >= 12:
                touch(self.getCoord("OK"))
            else:
                touch(self.getCoord("Adicionar"))
            self.backButton(x=1)
            return
        
        
        touch(Template(f"{self.imgPATH}music_list.png"))

        touch(self.getCoord("mídia permitidos"))
        add("Leaving California")
        
        assert_not_equal((None, None), self.getCoord("Leaving California"))
        
        touch(self.getCoord("mídia permitidos"))
        add("Sugar")
        

        return
    
    def tc_03_viewMusic(self, res, osVer, uiMode, buildMode):
        #Given there is at least a music allowed
        #When the user taps the "music list" icon 
        #Then the musics list is shown

        touch(Template(f"{self.imgPATH}play_screen_button.png"))
        touch(Template(f"{self.imgPATH}music_list.png"))

        assert_not_equal(self.getCoord("Leaving"), (None, None))
        assert_not_equal(self.getCoord("Sugar"), (None, None))

        return
    
    def tc_04_deleteMusic(self, res, osVer, uiMode, buildMode):
        #Given there is at least a music allowed
        #When the user taps the "music list" 
        #And removes a music
        #Then the music will be deleted from Lisa´s Music Band Application

        touch(self.getCoord("mídia permitidos"))
        self.password()
        sleep(1)
        if osVer >= 12:
            touch(self.getCoord("Excluir"))
        else:
            touch(self.getCoord("Remover"))
        touch(self.getCoord("Todos"))

        if osVer >= 12:
            touch(self.getCoord(r'content-desc="Apagar tudo"'))
        else:
            touch(self.getCoord(r'Excluir'))

        assert_not_equal(self.getCoord("Adicione algumas"), (None, None))

        self.backButton(x=1)

        assert_exists(Template(f"{self.imgPATH}no_tracks.png"))


        return
    
    def tc_05_changeInstruments(self, res, osVer, uiMode, buildMode):
        #Given that the user is on the main screen
        #When the user taps on Lisa
        #Then the instruments type will be changed

        touch(Template(f"{self.imgPATH}play_screen_button.png"))
        lisa = exists(Template(f"{self.imgPATH}lisa.png"))
        #1 animals
        touch(lisa)
        assert_exists(Template(f"{self.imgPATH}dog.png"))
        #2 transport
        touch(lisa)
        assert_exists(Template(f"{self.imgPATH}ship.png"))
        #3 body
        touch(lisa)
        assert_exists(Template(f"{self.imgPATH}heart.png"))
        #4 instruments
        touch(lisa)
        assert_exists(Template(f"{self.imgPATH}triangle.png"))
        
        return