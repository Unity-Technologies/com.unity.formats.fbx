using UnityEngine;
using UnityEditor;
using System;

Namespace FbxExporters
{
   class Integrations
   {
        Public static void InstallMaya2017()
        {
            string[] params;
            params = Environment.GetCommandLineArgs();

            //DO SOMETHING
            Debug.Log("InstallMaya2017");

            EditorApplication.Exit(0);
        }
   }
}