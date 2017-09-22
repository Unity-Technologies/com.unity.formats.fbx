-- MacroScripts that will perform actions
macroScript Import category:"Unity"
(
print "I do nothing, but I should Import!"
)
macroScript Preview category:"Unity"
(
print "I do nothing, but I should Preview!"
)
macroScript Export category:"Unity"
(
print "I do nothing, but I should Export!"
)

-- Setup UI
unityMenuName = "Unity"
unityMenu = menuMan.findMenu unityMenuName
if unityMenu == undefined do
(
    id = genClassID returnValue:true
    if menuMan.registerMenuContext id[1] then
    (
        mainMenu = menuMan.getMainMenuBar() --get the main menu bar
        newUnityMenu = menuMan.createMenu unityMenuName
        newUnitySubMenu = menuMan.createSubMenuItem unityMenuName newUnityMenu --create a SubMenuItem
        mainMenu.addItem newUnitySubMenu -1 --add the SubMenu to the end of the Main Menu
        
        importAction = menuMan.createActionItem "Import" unityMenuName --create an ActionItem from the MacroScript
		importAction.setTitle "Import"
        importAction.setUseCustomTitle true
        
		previewAction = menuMan.createActionItem "Preview" unityMenuName
		previewAction.setTitle "Preview"
        previewAction.setUseCustomTitle true
        
		exportAction = menuMan.createActionItem "Export" unityMenuName
		exportAction.setTitle "Export"
        exportAction.setUseCustomTitle true
        
        newUnityMenu.addItem importAction -1 --add the ActionItem to the menu
		newUnityMenu.addItem previewAction -1
		newUnityMenu.addItem exportAction -1
        menuMan.updateMenuBar() --update the menu bar
    )
)
-- Make sure that Menu gets removed at shutdown, force menu to reload each time Max is opened
callbacks.addScript #preSavingMenus ("menu = menuMan.findMenu \"Unity\" if menu != undefined then menuMan.unregisterMenu menu")