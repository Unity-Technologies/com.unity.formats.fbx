temp = pathConfig.GetDir(#userStartupScripts) + "/" + UnityPluginScript_Name;
deleteFile temp;
copyFile UnityPluginScript_Source temp;

maxIniFile = (GetMAXIniFile())
unityCategory = "Unity"
setINISetting maxIniFile unityCategory "UnityProject" UnityProject
setINISetting maxIniFile unityCategory "UnityFbxExportSettings" UnityFbxExportSettings
setINISetting maxIniFile unityCategory "UnityFbxImportSettings" UnityFbxImportSettings

quitMax()