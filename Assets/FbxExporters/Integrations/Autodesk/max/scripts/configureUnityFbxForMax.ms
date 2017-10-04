temp = pathConfig.GetDir(#userStartupScripts) + "/" + UnityPluginScript_Name;
deleteFile temp;
copyFile UnityPluginScript_Source temp;

setINISetting (GetMAXIniFile()) "Unity" "UnityProject" UnityProject

quitMax()