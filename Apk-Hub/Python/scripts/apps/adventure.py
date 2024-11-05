from application import *

class KidsAdventure(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
        self.permissions = []
    
    def executeTest(self, res, osVer, uiMode, buildMode):

        self.imgPATH = self.setImgPATH(os.path.basename(__file__)) #set image path
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
               
        self.recordScreen(orientation=2) #start recording
        
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
    
    def tc_01_openApp(self, res): #not tracked

        touch(self.getCoord("Aventura"))

        return
    
    def tc_02_skipVideo(self, res): #5688
        sleep(5)
        touch((res[0]/4, res[1]/4))
        sleep(2)
        assert_exists(Template(f"{self.imgPATH}play.jpg"))

        return
    
    def tc_03_checkPlayButton(self, res=None):

        touch(Template(f"{self.imgPATH}play.jpg"))

        wait(Template(f"{self.imgPATH}exit.jpg"), timeout=4)

        sleep(2)
        return
    
    def tc_04_clickBoat(self, res=None): #5691

        touch(Template(f"{self.imgPATH}boat.jpg"))

        wait(Template(f"{self.imgPATH}map.jpg"), timeout=10)
        return
    
    def tc_05_checkRefrigerator(self, res=None): #5693

        fridge = wait(Template(f"{self.imgPATH}fridge.jpg"), timeout=2)
        touch(fridge)
        sleep(2)
        exit = wait(Template(f"{self.imgPATH}exit_asset.jpg", threshold= 0.5), timeout=10)
        touch(exit)
        return
    
    def tc_06_exitBoat(self, res=None): #5695

        touch(Template(f"{self.imgPATH}map.jpg"))
        wait(Template(f"{self.imgPATH}exit.jpg"), timeout=4)
        return
    
    def tc_07_clickLevel(self, res=None): #5696

        touch(Template(f"{self.imgPATH}target.jpg"))
        sleep(10)
        wait(Template(f"{self.imgPATH}map.jpg"), timeout=4)
        return
    
    def tc_08_exitLevel(self, res=None): #5697

        touch(Template(f"{self.imgPATH}map.jpg"))
        sleep(5)
        wait(Template(f"{self.imgPATH}exit.jpg"), timeout=4)
        return
    
    def tc_09_exitGame(self, res=None): #

        touch(Template(f"{self.imgPATH}exit.jpg"))
        for _ in range(5):
            coord = self.getCoord("Aventura")
            if coord != (None, None):
                assert_not_equal(coord, (None, None))
                break
            else:
                continue
        return
