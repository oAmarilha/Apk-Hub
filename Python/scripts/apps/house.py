from application import *

class KidsHouse(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
        self.permissions = ["CAMERA"]
    
    def executeTest(self, res, osVer, uiMode, buildMode):

        self.imgPATH = self.setImgPATH(os.path.basename(__file__)) #set image path
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")

        self.recordScreen() #start recording

        for i in dir(self):
            if i.startswith('tc_'):
                result = getattr(self, i) #takes method name starting with self.tc_
                try:
                    result(res)
                except:
                    print(f"error executing {str(result)}")
                    self.stopRecording() #end recording
                    return
                
        self.stopRecording() #end recording
        return
    
    #tc01
    #Given the user is in Kids Home
    #When it opens the app
    #Then the app must open in the Main screen.
    
    
    def tc_01_openApp(self,res):
        touch(self.getCoord(self.appName))
        assert_is_not_none(self.getCoord('com.sec.android.app.kids3d'), 'TC 01 - Open application')
        return
    
    #tc02~tc03
    #Given that the user is in the Main screen
    #When the user taps Crocro
    #Then it enters the Crocro level
    #And must see animations and listen sounds from the character.
    
    def tc_02_enterCrocroLevel(self, res):
        touch(wait(Template(f"{self.imgPATH}/crocrolevel.png")))
        assert_is_not_none(self.waitElement(target='Cabeça e corpo, Botão'), 'TC 02 - Enter the Crocro floor')
        return
    
    def tc_03_colorAndClothes(self, res):
        touch(wait(Template(f"{self.imgPATH}/head.png")))
        assert_is_not_none(self.getCoord("GirafaPadrão"), msg="TC 03 Validated 1 - Checked if the head and body customization was opened")
        touch(self.getCoord("ZebraPadrão", False))
        assert_exists(Template(f"{self.imgPATH}/crocroZebra.png", threshold=0.85), msg= "TC 03 Validated 2 - Checked if the pattern was applied in Crocro head and body")
        return
    
    #tc04
    #Given that the user is in the Main screen
    #When the user taps Lisa
    #Then it enters the Lisa's level
    #And must see animations and listen sounds from the character.
    
    def tc_04_enterLisaLevel(self, res):
        self.backButton(x=1)
        touch(Template(f"{self.imgPATH}lisalevel.png"))          
        assert_is_not_none(self.waitElement(target='Lisa'), 'TC 04 - Enter the Lisa floor')
        return
    
    #tc05
    #Given that the user is in Lisa's level
    #When the user taps dance scenarios
    #Then the characters must appear dancing according to that scenario
    #And music must play according to that scenario


    def tc_05_changeTheCharacter(self,res):   
        self.bobbyButton = self.getCoord(textRequired="Bobby") 
        touch(self.bobbyButton)
        assert_is_not_none(self.waitElement(target='Selecionado, Bobby, Botão'), 'TC 05 - Change the character in the floor of Lisa')
        return
    
    #tc06
    #Given that the user is in Lisa's level
    #When the user takes a photo
    #Then that photo must appear in the character
    
    def tc_06_takeandCheckPhoto(self, res):
        touch(Template(f"{self.imgPATH}cameraButton.png"))
        time.sleep(3)
        self.triggerButton = self.getCoord(textRequired="Disparador") 
        touch(self.triggerButton)
        time.sleep(4)
        self.okButton = self.getCoord(textRequired="OK") 
        assert_is_not_none(self.okButton, 'TC 06 - Take a photo in Lisa floor')
        touch(self.okButton)
        return
    
    #tc07~tc11
    #Given that the user is in the Main screen
    #When the user taps Cooki
    #Then it enters the Cooki level
    #And must see animations and listen sounds from the character.
    
    def tc_07_enterCookiLevel(self, res):
        self.backButton(x=1)
        touch(Template(f"{self.imgPATH}cookilevel.png"))     
        assert_is_not_none(self.waitElement(target='Biscoito'), 'TC 07 - Enter the Cooki level')     
        
    def tc_08_selectCookieOption(self, res):  
        self.biscoitoButton = self.getCoord(textRequired="Biscoito", getDump= False) 
        touch(self.biscoitoButton)
        assert_is_not_none(self.getCoord('Selecionado, Biscoito, Botão'), 'TC 08 - Select cookie option')
        self.random_touch()
        
    def tc_09_selectFruitOption(self, res): 
        self.frutaButton = self.getCoord(textRequired="Fruta, Botão") 
        touch(self.frutaButton)
        assert_is_not_none(self.getCoord('Selecionado, Fruta, Botão'), 'TC 09 - Select fruit option')
        self.random_touch()
        
    def tc_10_selectFishOption(self, res):  
        self.peixeButton = self.getCoord(textRequired="Peixe") 
        touch(self.peixeButton)
        assert_is_not_none(self.getCoord('Selecionado, Peixe, Botão'), 'TC 10 - Select fish option')
        self.random_touch()
        
    def tc_11_closeApplication(self,res):    
        self.backButton(x=1)
        assert_is_not_none(self.getCoord('Decore a casa de brinquedo, Botão'), 'TC 11 - Return to app home screen')
        return
    
    #tc12~tc16
    #Given that the user is in the Main screen
    #When the user taps Bobby
    #Then it enters the Bobby level
    #And must see animations and listen sounds from the character.
    
    def tc_12_enterBobbyLevel(self, res):
        touch(Template(f"{self.imgPATH}bobbylevel.png")) 
        assert_is_not_none(self.waitElement(target='Escova de dentes'), 'TC 12 - Enter Bobby floor')
        
    def tc_13_selectShampooOption(self,res):
        self.xampuButton = self.getCoord(textRequired="Xampu") 
        touch(self.xampuButton)
        self.random_touch()
        time.sleep(3)
        self.swipeBellow()
        time.sleep(3)
        self.swipeUp()
        time.sleep(3)
        touch(Template(f"{self.imgPATH}elephant.png"))
        assert_is_not_none(self.getCoord('Selecionado, Xampu, Botão'), 'TC 13 - Select shampoo option')
        return
    
    def tc_14_hairDryerOption(self,res):
        self.secadorDeCabelosButton = self.getCoord(textRequired="Secador de cabelos") 
        touch(self.secadorDeCabelosButton)
        self.random_touch()
        time.sleep(3)
        self.swipeRight()
        time.sleep(3)
        assert_is_not_none(self.getCoord('Selecionado, Secador de cabelos, Botão'), 'TC 14 - Select hair dryer option')
        return
    
    def tc_15_ToothBrushOption(self,res):
        self.escovaDeDentesButton = self.getCoord(textRequired="Escova de dentes") 
        touch(self.escovaDeDentesButton)
        time.sleep(3)
        self.swipeUp()
        time.sleep(3)
        self.swipeBellow()
        time.sleep(3)
        self.swipeRight()
        time.sleep(3)
        self.swipeLeft()
        assert_is_not_none(self.getCoord('Selecionado, Escova de dentes, Botão'), 'TC 15 - Select toothbrush option')
        self.backButton(1)
        time.sleep(3)
        return
    
    def tc_16_closeApp(self, res):
        self.backButton(x=2)
        time.sleep(3)
        assert_is_not_none(self.getCoord('com.sec.android.app.kidshome'), 'TC 16 - Return to Samsung Kids home')
        return

      
    

