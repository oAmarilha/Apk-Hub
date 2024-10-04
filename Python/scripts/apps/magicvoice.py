from application import *

class KidsMagicVoice(Application):
    def __init__(self, appPkg, appName, numTest):
        super().__init__(appPkg, appName, numTest)
        self.permissions = ['RECORD_AUDIO']
    
    def executeTest(self, res, osVer, uiMode, buildMode):
        self.cmdpmt.start_shell("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity")
        self.recordScreen(orientation = 2) #start recording

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
    
    def tc_01_openApp(self, res):
        touch(self.getCoord("Minha voz mágica"))
        checkAppOpened = str(self.cmdpmt.shell("dumpsys window"))
        checkAppOpened = re.search("com.sec.kidsplat.kidstalk", checkAppOpened)
        assert_is_not_none(checkAppOpened)
        return