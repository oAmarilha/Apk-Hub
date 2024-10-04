from application import *

class KidsCanvas(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
        self.permissions=["READ_EXTERNAL_STORAGE",
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
        self.recordScreen(orientation=2) #start recording 123
        
        for i in dir(self):
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
    
    #tc01-04 em 27 de junho
    def tc_01_openApp(self, res):  
        self.telaDoBobby = self.getCoord(textRequired="Tela do Bobby") 
        touch(self.telaDoBobby)
        time.sleep(4)
        self.res = self.cmdandroid.get_current_resolution()
        print("Resolução atual: ", self.res)
        return
    
    def tc_02_sand_MakeDrawing(self, res): 
        touch(Template(f"{self.imgPATH}sandMenu.png"))          
        time.sleep(3)
        self.stoneButton = self.getCoord(textRequired="Stone") 
        touch(self.stoneButton)
        self.swipeLeft()
        time.sleep(4)
        self.swipeUp()
        time.sleep(4) 
        return
              
    def tc_03_sand_createNewDrawing(self, res): 
        self.newButton = self.getCoord(textRequired="Criar") 
        touch(self.newButton)
        self.woodenButton = self.getCoord(textRequired="Wooden") 
        touch(self.woodenButton)
        self.swipeUp()
        time.sleep(4)
        self.swipeLeft()
        time.sleep(4)
        return

    def tc_04_sand_returnToMainScreen(self, res):
        touch(self.getCoord("Navegar"))
        time.sleep(1)
        return
    
    #tc 05-07 29/06
    def tc_05_scratch_makeDrawing(self, res):
        touch(Template(f"{self.imgPATH}scratchMenu.png"))
        time.sleep(3)
        self.forkButton = self.getCoord(textRequired="Fork") 
        touch(self.forkButton)
        self.swipeUp()
        time.sleep(2)
        self.swipeLeft()
        time.sleep(2) 
        return
    
    def tc_06_scratch_NewDrawing(self, res):
        self.newButton = self.getCoord(textRequired="Criar") 
        touch(self.newButton)
        time.sleep(4)
        self.chiselButton = self.getCoord(textRequired="Chisel") 
        touch(self.chiselButton)
        self.swipeLeft()
        time.sleep(4)
        self.swipeUp()
        time.sleep(4)
        stencilButton = self.getCoord(textRequired="Stencils")
        if stencilButton is not None:
            touch(stencilButton)
        self.emblemaLisa = self.getCoord(textRequired="Lisa")
        touch(self.emblemaLisa)    
        self.random_touch()
        time.sleep(4)
        if stencilButton is not None:
            touch(stencilButton)
        self.emblemaCooki = self.getCoord(textRequired="Cooki")
        touch(self.emblemaCooki)
        self.random_touch()
        time.sleep(4)
        return
#
    def tc_07_scratch_returnToMainScreen(self, res):
        touch(self.getCoord("Navegar"))
        time.sleep(2)
        return
    
    def tc_08_drawing_MakeDrawing(self, res):
        touch(Template(f"{self.imgPATH}drawingMenu.png"))
        time.sleep(3)
        self.pincelButton = self.getCoord(textRequired="Pincel")
        touch(self.pincelButton)
        self.swipeLeft()
        time.sleep(2)
        self.swipeUp()
        time.sleep(4)
        return
    
    def tc_09_drawing_createNewDrawing(self, res):
        self.newButton = self.getCoord(textRequired="Criar")
        touch(self.newButton)
        self.canetaButton = self.getCoord(textRequired="Caneta")
        touch(self.canetaButton)
        self.swipeUp()
        time.sleep(4)
        self.swipeLeft()
        time.sleep(4)
        return       

    def tc_10_drawing_returnToMainScreen(self, res):
        touch(self.getCoord("Navegar")) 
        time.sleep(4)
        return


    def tc_11_coloring_MakeDrawing(self, res):
        touch(Template(f"{self.imgPATH}coloringMenu.png"))
        time.sleep(4)
        self.beeButton = self.getCoord(textRequired="Bee,")
        time.sleep(4)
        touch(self.beeButton)
        self.lataButton = self.getCoord(textRequired="Lata")
        touch(self.lataButton)
        self.random_touch()
        time.sleep(4)
        return
    
    def tc_12_coloring_createNewDrawing(self, res):
        self.newButton = self.getCoord(textRequired="Criar")
        touch(self.newButton)
        self.forestButton = self.getCoord(textRequired="Forest,")
        time.sleep(4)
        touch(self.forestButton)
        self.lataButton = self.getCoord(textRequired="Lata")
        touch(self.lataButton)
        self.random_touch()
        time.sleep(4)
        return       

    def tc_13_coloring_returnToMainScreen(self, res):
        touch(self.getCoord("Navegar")) 
        time.sleep(4)
        return
    
    def tc_14_access_creations_MainScreen(self, res):
        touch(self.getCoord(textRequired='Galeria de desenho'))
        time.sleep(5)
        return

    def tc_15_closeApplication(self, res):
        self.cmdpmt.keyevent("KEYCODE_HOME") 
        return