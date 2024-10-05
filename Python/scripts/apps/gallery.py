from application import *

class KidsGallery(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
        self.permissions = ["READ_EXTERNAL_STORAGE",
                            "POST_NOTIFICATIONS",
                            "CAMERA",
                            "RECORD_AUDIO",
                            "WRITE_EXTERNAL_STORAGE",
                            "READ_MEDIA_AUDIO",
                            "READ_MEDIA_VIDEO",
                            "READ_MEDIA_IMAGES",
                            "INTENT_PERMISSION",
                            "WRITE_MEDIA_STORAGE",
                            "MEDIA_PROVIDER_PERMISSION",
                            ]
    
    def executeTest(self, res, osVer, uiMode, buildMode):
        self.imgPATH = self.setImgPATH(os.path.basename(__file__)) #set image path
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
        self.recordScreen() #start recording
        
        for i in dir(self):
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
        #When the user tap the icon “My Gallery” 
        #Then the application will be open 
        self.cmdpmt.cmd(f'push {self.defaultpath}\\rep\\Gallery\\ /sdcard/Pictures')
        self.cmdpmt.start_shell(r'am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file:///sdcard/Pictures/')
        touch(self.getCoord('Minha galeria')) 
        return
    
    def tc_02_addMedia(self, res, osVer, uiMode, buildMode):
        #FOR THE AUTOMATED TEST BE CORRECTLY EXECUTED, IT'S REQUIRED THAT NO MEDIA IS ALLOWED INSIDE THE GALLERY
        #Given that the user is on the main screen of "My Gallery"
        #When the user tap the "+" button
        #And select a <media>
        #Then the <media> will be added to My Gallery Application
        touch(self.getCoord(textRequired='Adicionar'))
        self.password()
        touch(self.getCoord(textRequired='Adicionar'))
        touch(self.getCoord(textRequired='Gallery'))
        touch(self.getCoord(textRequired='Todos'))
        touch(self.getCoord(textRequired='Adicionar'))
        self.backButton(x=1)
        return
    
    def tc_03_viewMedia(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord(textRequired=r'index=\"1\" text=\"\" resource-id=\"\" class=\"android\.view\.ViewGroup\"'))
        currentApp = self.hierarchyDump()
        assert_equal(currentApp.__contains__("com.sec.kidsplat.kidsgallery:id/imageViewer"), True)
        self.backButton(x=1)
        return
    
    def tc_04_viewAlbums(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord(textRequired='Álbuns'))
        checkAlbum = self.hierarchyDump()
        assert_equal(checkAlbum.__contains__("NOVO"), True)
        touch(self.getCoord(textRequired='Galeria'))
        checkOpenedAlbum = self.hierarchyDump()
        assert_equal(checkOpenedAlbum.__contains__("com.sec.kidsplat.kidsgallery:id/album_item_name"), True)
        return
    
    def tc_05_openCamera(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord(textRequired='Câmera'))
        checkCamera = self.hierarchyDump()
        assert_equal(checkCamera.__contains__("com.sec.kidsplat.camera"), True)
        self.backButton(x=1)
        return
    
    def tc_06_disallowMedia(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord(textRequired='Adicionar'))
        self.password()
        touch(self.getCoord(textRequired="Excluir"))
        touch(self.getCoord(textRequired="Apagar"))
        self.backButton(x=1)
        checkMedia = self.hierarchyDump()
        assert_equal(checkMedia.__contains__("Adicione fotos ou tire uma foto"), True)
        return
    
    def tc_07_closeApp(self, res, osVer, uiMode, buildMode):
        self.backButton(x=1)
        self.checkAppStatus(False)
        return