from application import *

class KidsCamera(Application):
    def __init__(self, appPkg, appName, numTest, airtestinstance):
        super().__init__(appPkg, appName, numTest, airtestinstance)
        self.permissions = ["CAMERA",
                            "RECORD_AUDIO",
                            "WRITE_EXTERNAL_STORAGE",
                            "READ_MEDIA_AUDIO",
                            "READ_MEDIA_VIDEO",
                            "READ_MEDIA_IMAGES",
                            "INTENT_PERMISSION",
                            "WRITE_MEDIA_STORAGE",
                            "MEDIA_PROVIDER_PERMISSION"
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
    #PRE CONDITION - MYCAMERA APK MUST BE INSTALLED BEFORE THE TEST
    #Given that the application is allowed on Kids Home 
    #When the user tap the icon “My Camera” 
    #Then the application will be open
        self.cameraApp = self.getCoord("Minha câmera")
        touch(self.cameraApp)    
        self.checkAppStatus()
        return
    
    def tc_02_takePicture(self, res, osVer, uiMode, buildMode):
    #Given that the user is on the main screen of "My Camera"
    #When the user tap the "Take a Photo" button
    #And the memory is not full
    #Then the photo will be saved 
        touch(self.getCoord("Tirar uma foto"))
        time.sleep(2)
        touch(self.getCoord("Exibição rápida"))
        time.sleep(2)
        assert_not_exists(Template(f"{self.imgPATH}stickers.jpg"), msg= 'TC 02 - Take picture')
        return
    
    def tc_03_recordVideo(self, res, osVer, uiMode, buildMode):
    #Given that the user is on the main screen of "My Camera"
    #When the user tap the "Video" option
    #And press the "Record" button
    #Then a new video will be recorded
        self.backButton(x=1)
        touch(self.cameraApp)
        time.sleep(1)
        touch(self.getCoord("Pré-visualização de vídeo", False))
        assert_is_not_none(self.getCoord("Gravar"), msg= 'TC 03 - Record video')
        touch(self.getCoord("Gravar", False))
        time.sleep(3)
        #assert_is_not_none(self.getCoord("com.sec.kidsplat.camera:id/timer"))
        #assert_exists(Template(f"{self.imgPATH}recorder_in_progress.png"))
        touch(self.getCoord("Gravar", False))
        
        return
    
    def tc_04_changeCamera(self, res, osVer, uiMode, buildMode): # TC Change to camera mode and front mode
        getMode = self.hierarchyDump()
        cameraMode = re.search('frontal', getMode)
        if cameraMode:
            print("Câmera no modo traseira")
            touch(self.getCoord("Alternar", False))
            assert_is_not_none(self.getCoord("Alternar para câmera traseira"), msg= 'TC 04 - Change to back camera')
            print("Câmera alterada corretamente para o modo frontal")
        else:
            print("Câmera no modo frontal")
            touch(self.getCoord("Alternar", False))
            assert_is_not_none(self.getCoord("Alternar para câmera frontal"), msg= 'TC 04 - Change to front camera')
            print("Câmera alterada corretamente para o modo traseiro")               
        return   
    
    def tc_05_accessGallery(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord("Exibição rápida", False))
        assert_is_not_none(self.getCoord("com.sec.kidsplat.kidsgallery"), msg= 'TC 05 - Access my gallery')
        self.backButton(x=2)
        self.checkAppStatus(False)
        return










