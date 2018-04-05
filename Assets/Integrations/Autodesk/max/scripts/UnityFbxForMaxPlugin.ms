-- MacroScripts that will perform actions
macroScript UnityImport category:"Unity" tooltip:"Import an FBX file from a Unity project and auto-configure for exporting"
(
    rootDummyName = "UnityFbxExportSets"
    
    origObjects = #()
    currSetName = ""
    unityFbxFilePathAttr = ""
    unityFbxFileNameAttr = ""
    unityAnimFileNameAttr = ""
    unityAnimFilePathAttr = ""
    isAnimFile = false

    persistent global unityAskSetUnits
    
    global afterImport
    fn afterImport = (
        unityFbxExportSet = #()
        currSet = selectionSets[currSetName]
        if not isdeleted currSet and currSet != undefined then (
            unityFbxExportSet = unityFbxExportSet + currSet
        )
        for obj in objects do(
            if findItem origObjects obj == 0 then(
                -- add to selection set
                append unityFbxExportSet obj
            )
        )
        selectionSets[currSetName] = unityFbxExportSet
        
        -- check if dummy already exists in scene
        unityDummy = UnityImportHelpers.getOrCreateSettingsDummy currSetName
        
        if((not isAnimFile) or unityDummy.unityData.modelFilePath == "") do (
            unityDummy.unityData.modelFilePath = unityFbxFilePathAttr
        )
        if((not isAnimFile) or unityDummy.unityData.modelFileName == "") do (
            unityDummy.unityData.modelFileName = unityFbxFileNameAttr
        )
        unityDummy.unityData.animFilePath = unityAnimFilePathAttr
        unityDummy.unityData.animFileName = unityAnimFileNameAttr
        
        unityRootDummy = UnityImportHelpers.getOrCreateRootDummy rootDummyName
        unityDummy.parent = unityRootDummy
    );
    
    -- Make sure the FbxImporter plugin is loaded
    pluginManager.loadClass FbxImporter
    
    UnityImportHelpers.loadUnityFbxImportSettings()
    
    unityProjectPath = getINISetting (GetMAXIniFile()) "Unity" "UnityProject"
    local unityAssetsPath = (unityProjectPath + "/Assets/")
    unityAssetsPath = substituteString unityAssetsPath "/" "\\" -- dot net class requires backspaces
    local fbxFileNames = UnityImportHelpers.getMultiOpenFilenames caption:"Import FBX from Unity" filename:unityAssetsPath  types:"FBX (*.fbx)|*.fbx"
    if fbxFileNames != undefined then
    (
        -- ask to set units to cm if not already
        if (unityAskSetUnits == undefined or not unityAskSetUnits) and units.SystemType != #Centimeters then (
            result = false
            message = ("Detected system units set to "+units.SystemType+".\n\n" +
                      "Using system units other than centimeters is not recommended for the Unity FBX " +
                      "workflow and may result in unexpected scaling on export.\n\n" + 
                      "Would you like to change the system units to centimeters?")
            (
                result = queryBox message title:"Warning, system units not set to centimeters"
            )
            if result then (
                units.SystemType = #Centimeters
            )
            else (
                unityAskSetUnits = true
            )
        )
        
        unityFbxFilePathAttr = ""
        for fbxFileName in fbxFileNames do (
            -- Get all objects in scene before importAction
            origObjects = objects as array
            
            unityFbxFilePathAttr = getFilenamePath fbxFileName
            unityFbxFileNameAttr = filenameFromPath fbxFileName
            unityFbxFileName = getFilenameFile fbxFileName
            
            unityAnimFilePathAttr = unityFbxFilePathAttr
            unityAnimFileNameAttr = unityFbxFileNameAttr
            
            -- check if importing an animation file (contains @ symbol)
            local isAnimFile = false
            if(matchPattern unityFbxFileName pattern:"*@*") do (
                isAnimFile = true
                unityFbxFileName = (filterString unityFbxFileName "@")[1]
            )
            
            currSetName = unityFbxFileName + unityExportSetPostFix
            
            callbacks.addScript #postImport ("afterImport()") id:#unityPlugin
            
            importFile fbxFileName #noPrompt using:FBXIMP
            
            callbacks.removeScripts #postImport id:#unityPlugin
        )
        
        UnityImportHelpers.switchProject unityFbxFilePathAttr
    )
)
macroScript UnityExport category:"Unity" tooltip:"Export models and animation to Unity"
(   
    UnityExportHelpers.exportSelection modelOnly:false
)

macroScript UnityExportModel category:"Unity" tooltip:"Export models to Unity"
(
    UnityExportHelpers.exportSelection modelOnly:true
)

global unityExportSetPostFix = "_UnityExportSet"

struct UnityImportHelpers (
    fn loadUnityFbxImportSettings = (
        fbxImportSettings = getINISetting (GetMAXIniFile()) "Unity" "UnityFbxImportSettings"
        if fbxImportSettings != undefined and doesFileExist fbxImportSettings then(
            filein fbxImportSettings
        )
    ),

    -- allow multiple files to be selected for import
    fn getMultiOpenFilenames caption:"Open" filename:"" types:"All Files (*.*)|*.*" default:1 =
    (
        local dialog = DotNetObject "System.Windows.Forms.OpenFileDialog"
        dialog.multiSelect = true
        dialog.title = caption

        if doesFileExist filename then
            dialog.initialDirectory = filename

        dialog.filter = types
        dialog.filterIndex = default

        local result = dialog.ShowDialog()
        if (result.Equals result.OK) then
            dialog.filenames
        else
            undefined
    ),
    
    fn switchProject filePath = (
        -- Change Unity project if fbx is from a different Unity project.
        -- Get the project based on the folder structure (i.e. folder above Assets)
        local head = filePath
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
    ),
    
    fn getOrCreateDummy name = (
        unityDummy = getNodeByName (name)
        if (unityDummy == undefined) do (
            unityDummy = Dummy()
            unityDummy.name = name
            unityDummy.boxsize = [1,1,1]
        )
        unityDummy -- return dummy
    ),
    
    fn getOrCreateRootDummy rootDummyName = (
        UnityImportHelpers.getOrCreateDummy rootDummyName
    ),
    
    fn getOrCreateSettingsDummy name = (
        unityDummy = UnityImportHelpers.getOrCreateDummy name
        
        if not (isProperty unityDummy "modelFilePath") do (
            unitySetData = attributes unityData
            (
                parameters main rollout:params
                (
                    modelFilePath type: #string ui:filep default:""
                    modelFileName type: #string ui:filen default:""
                    animFilePath type: #string ui:afilep default:""
                    animFileName type: #string ui:afilen default:""
                )
                rollout params "Unity Export Parameters"
                ( 
                    edittext filep "Model File Path:" text:modelFilePath readOnly:false labelOnTop:true
                    edittext filen "Model File Name:" text:modelFileName readOnly:false labelOnTop:true
                    edittext afilep "Animation File Path:" text:animFilePath readOnly:false labelOnTop:true
                    edittext afilen "Animation File Name:" text:animFileName readOnly:false labelOnTop:true
                 )
            )
            custAttributes.add unityDummy unitySetData
        )
        
        unityDummy -- return dummy
    )
)

struct UnityExportHelpers (
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
    ),

    fn loadUnityFbxExportSettings = (
        fbxExportSettings = getINISetting (GetMAXIniFile()) "Unity" "UnityFbxExportSettings"
        if fbxExportSettings != undefined and doesFileExist fbxExportSettings then(
            filein fbxExportSettings
        )
        FbxExporterSetParam "ScaleFactor" (UnityExportHelpers.unitToScaleFactor units.SystemType)
    ),

    fn getUnityExportSets = (
        local unityExportSets = #()
        local exportSetPattern = ("*" + unityExportSetPostFix)
        for expSet in selectionSets do (
            if(matchPattern expSet.name pattern:exportSetPattern and 
                (not isdeleted expSet) and (expSet != undefined) and 
                getNodeByName (expSet.name) != undefined) do 
            (
                append unityExportSets expSet
            )
        )
        unityExportSets
    ),

    fn exportUnitySet expSet = (
        select expSet
        
        -- get export set's dummy
        local expSetDummy = getNodeByName (expSet.name)
        
        exportFileName = undefined
        if expSetDummy != undefined and (isProperty expSetDummy "modelFilePath") and (isProperty expSetDummy "modelFileName") and
            expSetDummy.unityData.modelFilePath != "" and expSetDummy.unityData.modelFileName != "" then
        (
            exportFileName = expSetDummy.unityData.modelFilePath + "/" + expSetDummy.unityData.modelFileName
        )
        else(
            unityProjectPath = getINISetting (GetMAXIniFile()) "Unity" "UnityProject"
            exportFileName = getSaveFileName caption:"Export FBX to Unity" filename:(unityProjectPath + "/Assets/") types:"FBX (*.fbx)|*.fbx|"
        )
        
        if exportFileName != undefined then (
            exportFile exportFileName #noPrompt selectedOnly:true using:FBXEXP
        )
    ),
    
    fn exportSelection modelOnly:false = (
        -- Make sure the FbxExporter plugin is loaded
        pluginManager.loadClass FbxExporter
        
        UnityExportHelpers.loadUnityFbxExportSettings()
        
        if(modelOnly) do (
            FbxExporterSetParam "Animation" false
        )
        
        origSelection = getCurrentSelection()
        
        local origUnits = units.SystemType
        units.SystemType = #Centimeters
        
        -- get all the unity export sets
        local unityExportSets = UnityExportHelpers.getUnityExportSets()
        
        -- find all sets that contain at least one object from the current selection
        local setsToExport = #()
        for expSet in unityExportSets do (
            for obj in origSelection do (
                if (findItem expSet obj > 0) do (
                    appendIfUnique setsToExport expSet
                    break
                )
            )
        )
        
        -- if no sets are selected, then export selection, otherwise export each set to its corresponding file
        if (setsToExport.count <= 0) then (
            local unityProjectPath = getINISetting (GetMAXIniFile()) "Unity" "UnityProject"
            local exportFileName = getSaveFileName caption:"Export FBX to Unity" filename:(unityProjectPath + "/Assets/") types:"FBX (*.fbx)|*.fbx|"
            if exportFileName != undefined do (
                exportFile exportFileName #noPrompt selectedOnly:true using:FBXEXP
            )
        )
        else (
            for expSet in setsToExport do (
                UnityExportHelpers.exportUnitySet expSet
            )
        )
        
        units.SystemType = origUnits
        
        if origSelection != undefined then (
            select origSelection
        )
    )
)

struct UnityHelpers (
    fn createUnityActionItem macroName category title parentMenu = (
        local unityAction = menuMan.createActionItem macroName category; --create an ActionItem from the MacroScript
        unityAction.setTitle title;
        unityAction.setUseCustomTitle true;
        parentMenu.addItem unityAction -1;
        unityAction
    ),

    fn createUnityImportAction title parentMenu = (
        UnityHelpers.createUnityActionItem "UnityImport" "Unity" title parentMenu;
    ),

    fn createUnityExportAction title parentMenu = (
        UnityHelpers.createUnityActionItem "UnityExport" "Unity" title parentMenu;
    ),
    
    fn createUnityExportModelAction title parentMenu = (
        UnityHelpers.createUnityActionItem "UnityExportModel" "Unity" title parentMenu;
    ),

    fn unityResetExportOptions = (
        unityAskSetUnits=undefined;
    )
)

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
            local unityExportModelTitle = "Export Model Only to Unity"
			
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
                global unityImportAction = UnityHelpers.createUnityImportAction unityImportTitle importMenu
                global unityExportAction = UnityHelpers.createUnityExportAction unityExportTitle exportMenu
                global unityExportModelAction = UnityHelpers.createUnityExportModelAction unityExportModelTitle exportMenu
                
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
		 ) \
		 if exportMenu != undefined and unityExportModelAction != undefined then( \
			exportMenu.removeItem unityExportModelAction; \
		 )"
	)
    
    -- when opening a new scene, reset the export options to make
    -- sure we don't accidentally overwrite anything
    callbacks.addScript #postSceneReset ("UnityHelpers.unityResetExportOptions()")
    callbacks.addScript #systemPreNew ("UnityHelpers.unityResetExportOptions()")
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
        local unityExportModelTitle = "Export Model Only"
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
			mainMenuBar.addItem unityMenuItem (mainMenuBar.numItems())
			
			UnityHelpers.createUnityImportAction unityImportTitle unityMenu
			UnityHelpers.createUnityExportAction unityExportTitle unityMenu
            UnityHelpers.createUnityExportModelAction unityExportModelTitle unityMenu
			
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
    
    -- when opening a new scene, reset the export options to make
    -- sure we don't accidentally overwrite anything
    callbacks.addScript #postSceneReset ("UnityHelpers.unityResetExportOptions()")
    callbacks.addScript #systemPreNew ("UnityHelpers.unityResetExportOptions()")
)
else(
    print "Error: Unity Integration only supports 3ds Max 2017 or later"
)