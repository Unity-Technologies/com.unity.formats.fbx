-- MacroScripts that will perform actions
macroScript UnityImport category:"File-Import"
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
    
    unityProjectPath = getINISetting (GetMAXIniFile()) "Unity" "UnityProject"
    fbxFileName = getOpenFileName caption:"Import FBX from Unity" filename:(unityProjectPath + "/Assets/")  types:"FBX (*.fbx)|*.fbx|"
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
macroScript UnityExport category:"File-Export"
(
    fn loadUnityFbxExportSettings = (
        fbxExportSettings = getINISetting (GetMAXIniFile()) "Unity" "UnityFbxExportSettings"
        if fbxExportSettings != undefined and doesFileExist fbxExportSettings then(
            filein fbxExportSettings
        )
    )

    -- Make sure the FbxExporter plugin is loaded
    pluginManager.loadClass FbxExporter
    
    loadUnityFbxExportSettings()
    
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
        unityProjectPath = getINISetting (GetMAXIniFile()) "Unity" "UnityProject"
        exportFileName = getSaveFileName caption:"Export FBX to Unity" filename:unityProjectPath types:"FBX (*.fbx)|*.fbx|"
        exportFileName = getSaveFileName caption:"Export FBX to Unity" filename:(unityProjectPath + "/Assets/") types:"FBX (*.fbx)|*.fbx|"
    )
    
    if exportFileName != undefined then (
        exportFile exportFileName #noPrompt selectedOnly:true using:FBXEXP
    )
    
    if origSelection != undefined then (
        select origSelection
    )
)

-- Setup UI
importMenuName = "File-Import"
exportMenuName = "File-Export"
fn setupUnityPluginUI = (
    importMenu = menuMan.findMenu importMenuName
    exportMenu = menuMan.findMenu exportMenuName
    if importMenu != undefined and exportMenu != undefined do
    (
        unityImportTitle = "Import from Unity"
        unityExportTitle = "Export to Unity"
        
        -- check if menu items already exists
        foundUnityImport = false
        for i=1 to importMenu.numItems() do(
            mi = importMenu.getItem i
            if mi.getTitle() == unityImportTitle then(
                foundUnityImport = true
                global unityImportAction = mi
                break
            )
        )
        foundUnityExport = false
        for i=1 to exportMenu.numItems() do(
            mi = exportMenu.getItem i
            if mi.getTitle() == unityExportTitle then(
                foundUnityExport = true
                global unityExportAction = mi
                break
            )
        )
        
        if foundUnityImport == false or foundUnityExport == false then(
            id = genClassID returnValue:true
            if menuMan.registerMenuContext id[1] then
            (
                if foundUnityImport == false do (
                    global unityImportAction = menuMan.createActionItem "UnityImport" importMenuName; --create an ActionItem from the MacroScript
                    unityImportAction.setTitle unityImportTitle;
                    unityImportAction.setUseCustomTitle true;
                    importMenu.addItem unityImportAction -1; --add the ActionItem to the menu
                )
                if foundUnityExport == false do (
                    global unityExportAction = menuMan.createActionItem "UnityExport" exportMenuName;
                    unityExportAction.setTitle unityExportTitle;
                    unityExportAction.setUseCustomTitle true;
                    exportMenu.addItem unityExportAction -1;
                )
                menuMan.updateMenuBar() --update the menu bar
            )
        )
    )
);
setupUnityPluginUI()

-- Make sure that Menu gets removed at shutdown, force menu to reload each time Max is opened
callbacks.addScript #preSavingMenus (
    "importMenu = menuMan.findMenu \""+importMenuName+"\"; \
     exportMenu = menuMan.findMenu \""+exportMenuName+"\"; \
     if importMenu != undefined and unityImportAction != undefined then( \
        importMenu.removeItem unityImportAction; \
     ) \
     if exportMenu != undefined and unityExportAction != undefined then( \
        exportMenu.removeItem unityExportAction; \
     )"
)