-- MacroScripts that will perform actions
macroScript Import category:"Unity"
(
    origObjects = #()

    persistent global unityFbxFilePathAttr
    persistent global unityFbxFileNameAttr
    
    global afterImport
    fn afterImport = (
        unityFbxExportSet = #()
        currSet = selectionSets["UnityFbxExportSet"]
        if not isdeleted currSet and currSet != undefined then (
            unityFbxExportSet = unityFbxExportSet + currSet
        )
        for obj in objects do(
            if findItem origObjects obj == 0 then(
                -- add to selection set
                append unityFbxExportSet obj
            )
        )
        selectionSets["UnityFbxExportSet"] = unityFbxExportSet
    );

    -- Make sure the FbxImporter plugin is loaded
    pluginManager.loadClass FbxImporter
    fbxFileName = getOpenFileName caption:"Import FBX from Unity" filename:"C:/Users/Viktoria/Desktop/" types:"FBX (*.fbx)|*.fbx|"
    if fbxFileName != undefined then
    (
        -- reset persistent values, in case import fails
        unityFbxFilePathAttr = ""
        unityFbxFileNameAttr = ""
    
        -- Get all objects in scene before importAction
        origObjects = objects as array
        callbacks.addScript #postImport ("afterImport()") id:#unityPlugin

        importFile fbxFileName using:FBXIMP
        
        unityFbxFilePathAttr = getFilenamePath fbxFileName
        unityFbxFileNameAttr = filenameFromPath fbxFileName
        
        callbacks.removeScripts #postImport id:#unityPlugin
    )
)
macroScript Preview category:"Unity"
(
print "I do nothing, but I should Preview!"
)
macroScript Export category:"Unity"
(
    -- Make sure the FbxExporter plugin is loaded
    pluginManager.loadClass FbxExporter
    
    origSelection = getCurrentSelection()
    exportSet = selectionSets["UnityFbxExportSet"]
    if not isdeleted exportSet and exportSet != undefined then(
        select exportSet
    )
    
    exportFileName = undefined
    if unityFbxFilePathAttr != undefined and unityFbxFileNameAttr != undefined then(
        exportFileName = unityFbxFilePathAttr + unityFbxFileNameAttr
    )
    else(
        exportFileName = getSaveFileName caption:"Export FBX to Unity" filename:"C:/Users/Viktoria/Desktop/" types:"FBX (*.fbx)|*.fbx|"
    )
    
    if exportFileName != undefined then (
        exportFile exportFileName selectedOnly:true using:FBXEXP
    )
    
    if origSelection != undefined then (
        select origSelection
    )
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