from application import *

class KidsStudio(Application):
    def __init__(self, appPkg, appName, numTest, airtestinstance):
        super().__init__(appPkg, appName, numTest, airtestinstance)
        self.permissions = ["CAMERA",
                            "RECORD_AUDIO",
                            "WRITE_EXTERNAL_STORAGE",
                            "READ_MEDIA_AUDIO",
                            "READ_MEDIA_VIDEO",
                            "READ_MEDIA_IMAGES",
                            "INTENT_PERMISSION",
                            "READ_EXTERNAL_STORAGE",
                            "WRITE_MEDIA_STORAGE",
                            "MEDIA_PROVIDER_PERMISSION"
                            ]

    def executeTest(self, res, osVer, uiMode, buildMode):
        for permissions in self.permissions:
            self.cmdpmt.start_shell(f"pm grant com.sec.kidsplat.kidsgallery android.permission.{permissions}")
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
        touch(self.getCoord(self.appName))
        assert_is_not_none(self.getCoord('license_icon'), msg='TC 01 - Open the Application')    
        time.sleep(2)
        return

#Cassiano 

    def tc_02_enterDrawing(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord(textRequired="drawing_maker_ic"))
        onBoardingCheck = self.getCoord(textRequired="ok_button")
        if onBoardingCheck != None: 
            assert_is_not_none(onBoardingCheck, msg='TC 02 - Enter drawing maker') 
            touch(onBoardingCheck)
        else:
            assert_is_not_none(self.getCoord('new_drawing'), msg='TC 02 - Enter drawing maker')
            time.sleep(2) 
        return 

    def tc_03_changeBrush(self, res, osVer, uiMode, buildMode):
        if "landscape" in self.getOrientation() or "foldable" in self.getOrientation():
            touch(self.getCoord(textRequired="crayon"))
            touch(self.getCoord(textRequired="watercolor", getDump=False))
            touch(self.getCoord(textRequired="oil_paint", getDump=False))
            assert_is_not_none(self.getCoord('oil_paint'), msg = 'TC 03 - Change Brush')
        else:
            self.openBrushes = self.getCoord(textRequired="br.org.sidi.kidsplat.artstudio:id/open_brushes")
            touch(self.openBrushes)
            touch(self.getCoord(textRequired="crayon"))
            touch(self.getCoord(textRequired="btn_brush_size_largest")) 
            self.backButton(x=1)
            time.sleep(2)
            touch(self.openBrushes)
            touch(self.getCoord(textRequired="watercolor"))
            self.backButton(x=1)
            time.sleep(2)
            touch(self.openBrushes)
            touch(self.getCoord(textRequired="oil_paint"))
            assert_exists(v= Template(f'{self.imgPATH}oilBrush.jpg'), msg = 'TC 03 - Change Brush')
            self.backButton(x=1)
            time.sleep(2)
        return
    
    def tc_04_drawOverTheScreen(self, res, osVer, uiMode, buildMode):
        if "landscape" in self.getOrientation() or "foldable" in self.getOrientation():
            self.swipeRight()
            touch(self.getCoord(textRequired="crayon"))
            touch(self.getCoord(textRequired="Vermelho"))
            self.swipeUp()
            touch(self.getCoord(textRequired="undo"))
            touch(self.getCoord(textRequired="redo"))
            touch(self.getCoord(textRequired="watercolor"))
            touch(self.getCoord(textRequired="Azul"))
            self.swipeLeft()
            touch(self.getCoord(textRequired="air_brush"))
            touch(self.getCoord(textRequired="Magenta"))
            self.random_touch()
            touch(self.getCoord(textRequired="Verde"))
            self.random_touch()
            self.swipeUp()
            assert_is_not_none(self.getCoord('Desfazer'), msg = 'TC 04 - Draw over the screen') 
            time.sleep(2)        

        else:
            self.swipeRight()
            touch(self.openBrushes)
            touch(self.getCoord(textRequired="crayon"))
            touch(self.getCoord(textRequired="btn_brush_size_largest")) 
            self.backButton(x=1)
            time.sleep(2)
            touch(self.getCoord(textRequired="Vermelho"))
            self.swipeUp()
            touch(self.getCoord(textRequired="undo"))
            touch(self.getCoord(textRequired="redo"))
            touch(self.openBrushes)
            touch(self.getCoord(textRequired="watercolor"))
            self.backButton(x=1)
            time.sleep(1)
            touch(self.getCoord(textRequired="Azul"))
            self.swipeLeft()
            touch(self.openBrushes)
            touch(self.getCoord(textRequired="air_brush"))
            self.backButton(x=1)
            touch(self.getCoord(textRequired="Magenta"))
            self.random_touch()
            touch(self.getCoord(textRequired="Verde"))
            self.random_touch()
            self.swipeUp()
            assert_is_not_none(self.getCoord('Desfazer'), msg = 'TC 04 - Draw over the screen') 
            time.sleep(2)
        return
    
    def tc_05_saveDrawing(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord(textRequired="save"))
        time.sleep(2)
        self.swipeBellow()
        self.swipeRight()
        touch(self.getCoord(textRequired="Salvar"))  
        assert_exists(v= Template(f'{self.imgPATH}blankScreen.jpg'), msg = 'TC 05 - Save the drawing')
        time.sleep(2)      
        return
    
    def tc_06_drawingReturnScreen(self, res, osVer, uiMode, buildMode):
        self.backButton(x=1)
        assert_is_not_none(self.getCoord('license_icon'), msg='TC 06 - Return to main screen')   
        return

#Cassiano FIM

#Luís 
    
    def tc_07_enterStickerCreator(self, res, osVer, uiMode, buildMode):
    #Given that the user is on the main screen
    #When the user tap the "Sticker Creator" feature
    #Then the feature will be opened
        touch(Template(f"{self.imgPATH}CriadorDeSticker.png"))
        self.skipButton = self.getCoord(textRequired="br.org.sidi.kidsplat.artstudio:id/skip_button")
        touch(self.skipButton)
        time.sleep(2) 
        assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/confirm_template_button'), msg='TC 07 - Enter sticker creator')    
        time.sleep(2)
        return
    
    def tc_08_selectTemplateDrawing(self, res, osVer, uiMode, buildMode):
    #Given that the user selected the "Sticker Creator" option                                              
    #When the user choose a template
    #And select brush button 
    #Then the template will be opened 
        touch(Template(f"{self.imgPATH}template.png")) 
        time.sleep(2)
        self.brushButton = self.getCoord(textRequired="Preencher o modelo com cores")
        touch(self.brushButton)
        assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/balloon_text'), msg='TC 08 - Select template drawing') 
        time.sleep(2)
        return
    
    def tc_09_paintTemplate(self, res, osVer, uiMode, buildMode):
    #Given that the user selected the "Sticker Creator" option
    #When the user choose a template
    #And tap "Paint Can" button                                                                      
    #Then the user will fill to the colors in white spaces
        self.color = self.getCoord(textRequired="Cor")
        touch(self.color)
        time.sleep(2)
        self.area = self.getCoord(textRequired="Area")
        touch(self.area)
        time.sleep(2)
        assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/balloon_text'), msg='TC 09 Validate 1 - Paint template') 
        time.sleep(2)
        self.cameraButton = self.getCoord(textRequired="Minha câmera")
        touch(self.cameraButton)
        assert_is_not_none(self.waitElement(interval=5, qtd=4, target='com.sec.kidsplat.camera:id/shutter'), 'TC 09 Validate 2 - Enter camera')
        self.backButton(x=1)
        time.sleep(2)
        return


    def tc_10_saveTemplate(self, res, osVer, uiMode, buildMode):
    #Given that the template is already painted
    #When the user select 'save' button
    #Then the template will be saved   
        touch(Template(f"{self.imgPATH}Salvar.png"))
        assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/sticker_template_toggle_button'), msg='TC 10 - Save template')         
        time.sleep(2)
        return

    def tc_11_stickerReturnScreen(self, res, osVer, uiMode, buildMode):
    #Given the user selected the "Sticker Creator" option
    #When the user tap the "Back" button
    #Then the user will return to the main screen
        self.backButton(x=2)
        assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/drawing_maker_ic'), msg='TC 11 - Return screen') 
        time.sleep(2)
        return
    
#Luís FIM

#Victor
    
    def tc_12_enterColoringBook(self, res, osVer, uiMode, buildMode):
        if osVer >= 14:
            touch(self.getCoord('Livro de colorir'))
            assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/coloring_templates_list'), 'TC 12 Enter Coloring Book')
        return
    
    def tc_13_selectTemplateColoring(self, res, osVer, uiMode, buildMode):
        if osVer >= 14:
            touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/confirm_coloring_template_button'))
            time.sleep(5)
            assert_is_not_none(self.getCoord('Vermelho'), 'TC 13 Select a template')
        return
    
    def tc_14_drawOverTemplate(self, res, osVer, uiMode, buildMode):            
        if osVer >= 14:
            touch(self.getCoord('Vermelho', False))
            self.swipeBellow()
            if "portrait" in self.getOrientation():
                touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/open_brushes', False))
                assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/drawing_tool_name'), 'TC 14 Validate 1 - Checking tools')
            touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/crayon', False))
            if "landscape" in self.getOrientation() or "foldable" in self.getOrientation():
                touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/crayon', False))
                touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/btn_brush_size_largest'))
            else:
                touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/btn_brush_size_largest', False))
            self.backButton(1)
            touch(self.getCoord('Azul'))
            self.swipeLeft()
            touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/undo', False))
            time.sleep(1)
            touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/redo', False))
            time.sleep(1)
            if "portrait" in self.getOrientation():
                assert_exists(v= Template(f'{self.imgPATH}redline.png'), msg = 'TC 14 Validate 2 - Drawing in canvas')
            else:
                assert_exists(v= Template(f'{self.imgPATH}redline.png'), msg = 'TC 14 - Drawing in canvas')
        return
    
    def tc_15_saveDrawing(self, res, osVer, uiMode, buildMode):
        if osVer >= 14:
            touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/save', False))
            time.sleep(2)
            self.backButton(2)
            assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/gallery_icon'), 'TC 15 - Saving the image')
        return
    
    def tc_16_selectImageGallery(self, res, osVer, uiMode, buildMode):
        if osVer >= 14:
            touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/coloring_book_ic', False))
            touch(Template(f'{self.imgPATH}gallery.jpg'))
            touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/coloring_pick_image_button'))
            assert_is_not_none(self.getCoord('com.sec.kidsplat.kidsgallery:id/collapsed_view_title_txt_view'), 'TC 16 - Selecting an image option')
        return
    
    def tc_17_coloringReturnMainScreen(self, res, osVer, uiMode, buildMode):
        if osVer >= 14:
            self.backButton(2)
            assert_is_not_none(self.getCoord('br.org.sidi.kidsplat.artstudio:id/drawing_maker_ic'), 'TC 17 - Return to main screen')
        return
    
    def tc_18_accessCreations(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord('br.org.sidi.kidsplat.artstudio:id/gallery_icon'))
        if osVer >= 14:
            TcNumber = 'TC 18 - Access gallery creation window'
        else:
            TcNumber = 'TC 12 - Access gallery creation window'
        assert_is_not_none(self.getCoord('com.sec.kidsplat.kidsgallery:id/coordinator_layout'), msg= TcNumber)
        return
    
    def tc_19_closeApplication(self, res, osVer, uiMode, buildMode):
        self.backButton(2)
        if osVer >= 14:
            TcNumber = 'TC 19 - Close My Art Studio app'
        else:
            TcNumber = 'TC 13 - Close My Art Studio app'
        assert_is_not_none(self.getCoord('com.sec.android.app.kidshome'), msg= TcNumber)
        return
    
#Victor FIM