from application import *

class KidsBrowser(Application):
    def __init__(self, appPkg, appName, numTest, airtestinstance):
        super().__init__(appPkg, appName, numTest, airtestinstance)
        self.permissions = ['POST_NOTIFICATIONS']
    
    
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
       #add tests to be executed here             
#Test Case 1 Open App    
    def tc_01_openApp(self, res, osVer, uiMode, buildMode):
        touch(self.getCoord(r'navegador')) 
        assert_exists(Template(f"{self.imgPATH}add.png"), msg='Open app')
        return

#Test Case 2 Open New Site
    def tc_02_newSite(self, res, osVer, uiMode, buildMode):
        self.url= "www.samsung.com"
        # Given the user is inside the home screen in my browser
        #  When the user clicks in "New Site"
        touch(self.getCoord(f'Novo site')) 
        # Then the PIN is required , When the user placed the pin correctly 
        self.password() 
        # Then the field to unblock the site is shown
        text(self.url, enter=True) # When the user chooses the website and clicks enter
        sleep(4)
        snapshot(filename="siteOpened.png", msg= "Screenshot of the New Site Opened")
        # Then the website is loaded 
        return

    #Test Case 3 Allow Site 
    #Given the user is inside the website field
    #When the user chooses the website and clicks enter
    #Then the website is loaded
    def tc_03_allowSite(self, res, osVer, uiMode, buildMode):
        
        touch(self.getCoord(r'Adicionar a Sites permitidos')) # When the user clicks in the left above "star" 

        touch(self.getCoord(r'Logotipo do site da web'))

        #Then the unblock page is shown
        #Swipe bellow and don't favorite the site
        self.swipeBellow()
        touch(self.getCoord(r'Definir como favorito'))
        #And the user the clicks to "save"
        touch(self.getCoord('com.sec.kidsplat.kidsbrowser:id/save_button', False))
        #touch(Template(f"{self.imgPATH}save.png"))
        sleep(2)
        self.buttonHome = self.getCoord("Ir para tela principal")
        touch(self.buttonHome)
        time.sleep(1)
        #assert_not_exists(Template(f"{self.imgPATH}icon_{self.url}.png"))
        #Then the website is allowed
        return
    
    #Test Case 4 Add site to Favorites
    def tc_04_favoriteSite(self, res, osVer, uiMode, buildMode):
        #Given the user has a site allowed
        #When the user enters this site 
        self.buttonSearch = self.getCoord("Pesquisar")
        touch(self.buttonSearch)
        text(self.url, enter=True)

        sleep(5)

        MobileData = Template(f"{self.imgPATH}mobileData.jpg")

        if (exists(MobileData)):
            touch(MobileData)
            self.password()
            sleep(2)
            touch(self.buttonSearch)
            text(self.url, enter=True)

        sleep(5)
        #Then the website is loaded
        #When the user clicks in "star" bellow
        touch(self.getCoord(r"Adicionar o site da web aos Favoritos"))
        #Then the unblock page is shown with icons available
        touch(self.getCoord(r'Logotipo do site da web'))
        #When the user clicks in the image
        touch(self.buttonHome)
        assert_exists(Template(f"{self.imgPATH}icon_{self.url}.png"), msg='TC 4 - Add sites to favorites')
        #Then the website is favorited
        return
    
    #Test Case 5 Open History
    def tc_05_checkHistory(self, res, osVer, uiMode, buildMode):
        # Given the user is inside the home screen in my browser
        sleep(1)
        # When the user clicks in the three dots right bellow
        touch(self.getCoord('Mais opções'))
        sleep(1)
        # And "Settings"
        touch(self.getCoord('Configurações'))
        sleep(1)
        # Then the PIN is required
        self.password()
        sleep(1)
        # When the user placed the pin correctly 
        # And click in "History"
        touch(self.getCoord(f"Histórico"))
        sleep(1)
        assert_is_not_none(self.getCoord('Todos'), msg= 'TC 5 - Open History')
        #checkHistory = exists(Template(f"{self.imgPATH}historypage.png", resolution=res))
        #if checkHistory:
        #    print("History page detected")
        # Then the browser history is shown
        return
    
#Test Case 6 Manage Allowed Websites
    def tc_06_manageSites(self, res, osVer, uiMode, buildMode):
        # Given the user is inside the settings screen
        self.backButton(x=1) # x = times to repeat the button back
        sleep(1)
        # When clicks in "Manage Websites screen"
        touch(self.getCoord(r"Gerenciar sites da web permitidos"))
        assert_is_not_none(self.getCoord('Samsung'), msg= 'TC 6 - Manage allowed websites')
        #checkSites = exists(Template(f"{self.imgPATH}checksites.png", resolution=res))
        #if checkSites:
        #    print("Manage Sites page detected")
        # Then the websites allowed screen is shown
        return
    
#Test Case 7 Search for allowed site
    def tc_07_searchAllowedSites(self, res, osVer, uiMode, buildMode):
        # Given the user is inside the home screen in browser
        self.backButton(x=2) # x = time to repeat the button back
        # When the user clicks in search button
        touch(self.buttonSearch)
        # Then the website field is shown
        sleep(1)
        # And the user searches for a website allowed
        text(self.url, enter=True)
        sleep(1)
        self.buttonUnblock = self.getCoord('Desbloq.')
        if self.buttonUnblock is None:
            self.buttonUnblock = self.getCoord('DESBLOQ.')
        assert_is_none(self.buttonUnblock, msg='TC 7 - Search for allowed site')
        #Then the website is opened without error
        return

#Test Case 8 Search: Search for not allowed site
    def tc_08_searchBlockedSite(self, res, osVer, uiMode, buildMode):
        url="www.youtube.com"
        # Given the user is inside the home screen in my browser
        touch(self.buttonHome)
        # When the user clicks in search button
        touch(self.buttonSearch)
        # Then the website field is shown
        sleep(1)
        # And user searches for a website not allowed
        text(url, enter=True)
        sleep(1)
        self.buttonUnblock = self.getCoord('Desbloq.')
        if self.buttonUnblock is None:
            self.buttonUnblock = self.getCoord('DESBLOQ.')
        assert_is_not_none(self.buttonUnblock, msg='TC 8 - Search for not allowed site')
        # Then the website is not opened and a message is shown informing that this website is not allowed
        return
    
#Test Case 9 Access favorite site: Block site
    def tc_09_blockSite(self, res, osVer, uiMode, buildMode):
        url="www.samsung.com"
        # Given the user is on a website allowed
        touch(self.buttonHome)
        touch(self.buttonSearch)
        sleep(1)
        text(url, enter=True)
        self.buttonUnblock = self.getCoord('Desbloq.')
        if self.buttonUnblock is None:
            self.buttonUnblock = self.getCoord('DESBLOQ.')
        assert_is_none(self.buttonUnblock)
        sleep(1)
        # When the user clicks in the three dots right bellow
        touch(self.getCoord('Mais opções'))
        sleep(1)
        # And "Block"
        touch(self.getCoord(f"Bloquear o site da web"))
        sleep(1)
        # Then the PIN is required
        self.password()
        sleep(1)
        #assert_exists(Template(f"{self.imgPATH}blockingSite.png"))
        #sleep(1)
        touch(self.buttonHome)
        touch(self.buttonSearch)
        text(url, enter=True)
        self.buttonUnblock = self.getCoord('Desbloq.')
        if self.buttonUnblock is None:
            self.buttonUnblock = self.getCoord('DESBLOQ.')
        assert_is_not_none(self.buttonUnblock, msg='TC 9 - Access favorite site: Block site')
        # And the website is blocked    
        return
    
    #Test Case 10 Return to home page
    def tc_10_returnHome(self, res, osVer, uiMode, buildMode):
        # Given the user is any screen that it is possible click in home screen
        touch(self.buttonHome)
        # When the user clicks in the "home"
        sleep(1)
        assert_exists(Template(f"{self.imgPATH}add.png"), msg='TC 10 - Return to home page')
        # Then the main screen of application is shown
        return
    
    #Test case 11 Allow site after searching on search icon
    def tc_11_allowBlockedSite(self, res, osVer, uiMode, buildMode):
        url="www.netflix.com"
        # Given that the user is on "My Browser" main screen
        assert_exists(Template(f"{self.imgPATH}add.png"))
        # When user clicks on search icon
        touch(self.buttonSearch)
        # And search for a blocked website
        sleep(1)
        text(url, enter=True)
        # And taps Unblock website
        touch(self.getCoord(r'common_error_button'))
        sleep(3)
        # Insert PIN
        self.password()
        # Choose an icon to website
        touch(self.getCoord(r'favorite_icon_beachball'))
        # swipe screen
        self.swipeBellow()
        touch(self.getCoord(r'Definir como favorito'))
        # save
        touch(self.getCoord(r'save_button'))
        sleep(2)
        touch(self.getCoord(r'menu_bar_home_button'))
        assert_is_not_none(self.getCoord(r'netflix'), msg='TC 11 - Allow site after searching on search icon')
        return
    
    #Test case 12 Remove sites from favorites list on main screen
    def tc_12_removeFromMainScreen(self, res, osVer, uiMode, buildMode):
        #Given that the user is on main screen
        assert_exists(Template(f"{self.imgPATH}add.png"))
        #When user select an icon from screen
        touch(self.getCoord(r'netflix'), duration=2)
        #And tap on Delete button
        touch(self.getCoord(r'favorite_screen_delete_button'))
        #Then the website is removed from favorites list
        assert_is_none(self.getCoord(r'netflix'), msg='TC 12 - Remove sites from favorites list on main screen')
        return
    
    #new tests (methods) below here