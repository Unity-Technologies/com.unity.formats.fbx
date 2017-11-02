-- MacroScripts that will perform actions
macroScript UnityImport category:"Unity"
(
    origObjects = #()

    persistent global unityFbxFilePathAttr
    persistent global unityFbxFileNameAttr
    persistent global unityAskSetUnits
    
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
        -- ask to set units to cm if not already
        if (unityAskSetUnits == undefined or not unityAskSetUnits) and units.SystemType != #Centimeters then (
            result = false
            (
                result = queryBox "Using system units other than centimeters is not recommended and may result in unexpected scaling on export.\nWould you like to set system units to centimeters?" title:"Warning"
            )
            if result then (
                units.SystemType = #Centimeters
            )
            else (
                unityAskSetUnits = true
            )
        )
    
        -- reset persistent values, in case import fails
        unityFbxFilePathAttr = ""
        unityFbxFileNameAttr = ""
    
        -- Get all objects in scene before importAction
        origObjects = objects as array
        callbacks.addScript #postImport ("afterImport()") id:#unityPlugin
        
        importFile fbxFileName using:FBXIMP
        
        unityFbxFilePathAttr = getFilenamePath fbxFileName
        unityFbxFileNameAttr = filenameFromPath fbxFileName
		
		-- Change Unity project if fbx is from a different Unity project.
		-- Get the project based on the folder structure (i.e. folder above Assets)
		local head = unityFbxFilePathAttr
		head = trimRight head "\\/"
		-- Check that we are not at the root directory.
		while head != "" and not (pathConfig.isRootPath head) do(
			if (pathConfig.stripPathToLeaf head) == "Assets" do (
				-- this is a valid Unity project, so set it
				unityProject = pathConfig.removePathLeaf head
				maxIniFile = (GetMAXIniFile())
				setINISetting maxIniFile "Unity" "UnityProject" unityProject
					
			    -- in order to break out of loop without calling break (because "break" is slow)
			    head = ""
			)
			head = pathConfig.removePathLeaf head
		)
		
        callbacks.removeScripts #postImport id:#unityPlugin
    )
)
macroScript UnityExport category:"Unity"
(
    fn unitToScaleFactor unit = (
        case unit of (
            #Inches: 2.54
            #Feet: 30.48
            #Miles: 160934
            #Millimeters: 0.1
            #Kilometers: 100000
            #Meters: 100
            default: 1
        )
    );

    fn loadUnityFbxExportSettings = (
        fbxExportSettings = getINISetting (GetMAXIniFile()) "Unity" "UnityFbxExportSettings"
        if fbxExportSettings != undefined and doesFileExist fbxExportSettings then(
            filein fbxExportSettings
        )
        FbxExporterSetParam "ScaleFactor" (unitToScaleFactor units.SystemType)
    );

    -- Make sure the FbxExporter plugin is loaded
    pluginManager.loadClass FbxExporter
    
    loadUnityFbxExportSettings()
    
    origSelection = getCurrentSelection()
    exportSet = selectionSets["UnityFbxExportSet"]
    if not isdeleted exportSet and exportSet != undefined then(
        select exportSet
    )
    
    local origUnits = units.SystemType
    units.SystemType = #Centimeters
    
    exportFileName = undefined
    if unityFbxFilePathAttr != undefined and unityFbxFileNameAttr != undefined then(
        exportFileName = unityFbxFilePathAttr + unityFbxFileNameAttr
    )
    else(
        unityProjectPath = getINISetting (GetMAXIniFile()) "Unity" "UnityProject"
        exportFileName = getSaveFileName caption:"Export FBX to Unity" filename:(unityProjectPath + "/Assets/") types:"FBX (*.fbx)|*.fbx|"
    )
    
    if exportFileName != undefined then (
        exportFile exportFileName #noPrompt selectedOnly:true using:FBXEXP
    )
    
    units.SystemType = origUnits
    
    if origSelection != undefined then (
        select origSelection
    )
)

fn createUnityActionItem macroName category title parentMenu = (
	local unityAction = menuMan.createActionItem macroName category; --create an ActionItem from the MacroScript
	unityAction.setTitle title;
	unityAction.setUseCustomTitle true;
	parentMenu.addItem unityAction -1;
	unityAction
);

fn createUnityImportAction title parentMenu = (
	createUnityActionItem "UnityImport" "Unity" title parentMenu;
);

fn createUnityExportAction title parentMenu = (
	createUnityActionItem "UnityExport" "Unity" title parentMenu;
);

-- Setup UI in existing Import/Export menus if using 3ds Max 2018+
if (maxVersion())[1] >= 20000 then(
	global importMenuName = "File-Import"
	global exportMenuName = "File-Export"
	
	-- get the import menu
	global getImportMenu
	fn getImportMenu = (
		menuMan.findMenu importMenuName
	);

	-- get the export menu
	global getExportMenu
	fn getExportMenu = (
		menuMan.findMenu exportMenuName
	);
	
	-- Setup UI
	fn setupUnityPluginUI = (
		local importMenu = getImportMenu()
		local exportMenu = getExportMenu()
		if importMenu != undefined and exportMenu != undefined do
		(
			local unityImportTitle = "Import from Unity"
			local unityExportTitle = "Export to Unity"
			
			-- check if menu items already exist, delete if they do
			local foundUnityImport = false
			for i=1 to importMenu.numItems() while not foundUnityImport do(
				local mi = importMenu.getItem i
				if mi.getTitle() == unityImportTitle then(
					importMenu.removeItem mi
					foundUnityImport = true
				)
			)
			local foundUnityExport = false
			for i=1 to exportMenu.numItems() while not foundUnityExport do(
				local mi = exportMenu.getItem i
				if mi.getTitle() == unityExportTitle then(
					exportMenu.removeItem mi
					foundUnityExport = true
				)
			)
			
            id = genClassID returnValue:true
            if menuMan.registerMenuContext id[1] then
            (
                global unityImportAction = createUnityImportAction unityImportTitle importMenu
                global unityExportAction = createUnityExportAction unityExportTitle exportMenu
                
                menuMan.updateMenuBar() --update the menu bar
            )
		)
	);
	setupUnityPluginUI()

	-- Make sure that Menu gets removed at shutdown, force menu to reload each time Max is opened
	callbacks.addScript #preSavingMenus (
		"importMenu = getImportMenu(); \
		 exportMenu = getExportMenu(); \
		 if importMenu != undefined and unityImportAction != undefined then( \
			importMenu.removeItem unityImportAction; \
		 ) \
		 if exportMenu != undefined and unityExportAction != undefined then( \
			exportMenu.removeItem unityExportAction; \
		 )"
	)
)
else if (maxVersion())[1] == 19000 then (
	-- for 3ds Max 2017
	
	global unityMenuName = "Unity"
	
	global getUnityMenu
	fn getUnityMenu = (
		menuMan.findMenu unityMenuName
	);
	
	-- Setup UI
	fn setupUnityPluginUI = (
		local unityMenu = getUnityMenu()
		local unityImportTitle = "Import"
		local unityExportTitle = "Export"
		if unityMenu != undefined do
		(
			-- remove the menu if it still exists
			menuMan.unRegisterMenu unityMenu
		)
		
		id = genClassID returnValue:true
		if menuMan.registerMenuContext id[1] then
		(
			local mainMenuBar = menuMan.getMainMenuBar()
			local unityMenu = menuMan.createMenu unityMenuName
			local unityMenuItem = menuMan.createSubMenuItem unityMenuName unityMenu
			mainMenuBar.addItem unityMenuItem -1
			
			createUnityImportAction unityImportTitle unityMenu
			createUnityExportAction unityExportTitle unityMenu
			
			menuMan.updateMenuBar() --update the menu bar
		)
	);
	setupUnityPluginUI()

	-- Make sure that Menu gets removed at shutdown, force menu to reload each time Max is opened
	callbacks.addScript #preSavingMenus (
		"unityMenu = getUnityMenu(); \
		 if unityMenu != undefined do ( \
			menuMan.unRegisterMenu unityMenu; \
		 )"
	)
)
else(
    print "Error: Unity Integration only supports 3ds Max 2017 or later"
)