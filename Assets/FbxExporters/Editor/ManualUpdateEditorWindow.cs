using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FbxExporters;
using System.Linq;

public class ManualUpdateEditorWindow : EditorWindow
{
    int[] selectedNodesToDestroy;
    int[] selectedNodesToRename;

    FbxPrefabAutoUpdater.FbxPrefabUtility m_fbxPrefabUtility;
    FbxPrefab m_fbxPrefab;
    GUIContent[] options;
    List<string> m_nodesToCreate;
    List<string> m_nodesToDestroy;
    List<string> m_nodesToRename;

    List<string> m_nodeNameToSuggest;

    public void Init(FbxPrefabAutoUpdater.FbxPrefabUtility fbxPrefabUtility, FbxPrefab fbxPrefab)
    {
        FbxPrefabAutoUpdater.FbxPrefabUtility.UpdateList updates = new FbxPrefabAutoUpdater.FbxPrefabUtility.UpdateList(new FbxPrefabAutoUpdater.FbxPrefabUtility.FbxRepresentation(fbxPrefab.FbxHistory), fbxPrefab.FbxModel.transform, fbxPrefab);

        m_fbxPrefabUtility = fbxPrefabUtility;
        m_fbxPrefab = fbxPrefab;
        // Convert Hashset into List
        m_nodesToCreate = updates.GetNodesToCreate().ToList();
        m_nodesToDestroy = updates.GetNodesToDestroy().ToList();
        m_nodesToRename = updates.GetNodesToRename().ToList();
        // Create the dropdown list
        m_nodeNameToSuggest = new List<string>();
        m_nodeNameToSuggest.AddRange(m_nodesToCreate);
        m_nodeNameToSuggest.AddRange(m_nodesToRename);

        // Add extra 1 for the [Delete] option
        selectedNodesToDestroy = new int[m_nodeNameToSuggest.Count + 1];
        selectedNodesToRename = new int[m_nodeNameToSuggest.Count + 1];

        // Default option for nodes to rename. Shows the current name mapping
        for (int i = 0; i < m_nodesToRename.Count; i++)
        {
            for (int j = 0; j < m_nodeNameToSuggest.Count; j++)
            {
                if (m_nodeNameToSuggest[j] == m_nodesToRename[i])
                {
                    // Add extra 1 for the [Delete] option
                    selectedNodesToRename[i] = j + 1;
                }
            }
        }
    }

    void OnGUI()
    {
        // If there is nothing to map, sync prefab automatically and close the window
        if (m_nodesToDestroy.Count == 0 && m_nodesToRename.Count == 0)
        {
            m_fbxPrefabUtility.SyncPrefab();
            Close();
        }

        //Titles of the columns
        GUILayout.BeginHorizontal();
        GUILayout.Label("Unity Names", EditorStyles.boldLabel);
        GUILayout.Label("FBX Names", EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        // List of nodes that will be destroyed on the Unity object, unless the user wants to map them
        for (int i = 0; i < m_nodesToDestroy.Count; i++)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_nodesToDestroy[i]);

            List<GUIContent> listFbxNames = new List<GUIContent>();
            listFbxNames.Add(new GUIContent("[Delete]"));

            for(int j = 0; j < m_nodeNameToSuggest.Count; j++)
            {
                listFbxNames.Add(new GUIContent(m_fbxPrefabUtility.GetFBXObjectName(m_nodeNameToSuggest[j])));
            }

            options = listFbxNames.ToArray();
            selectedNodesToDestroy[i] = EditorGUILayout.Popup(selectedNodesToDestroy[i], options);

            GUILayout.EndHorizontal();
        }

        // List of nodes that will be renamed on the Unity object, unless the user wants to map them or delete them
        for (int i = 0; i < m_nodesToRename.Count; i++)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_fbxPrefabUtility.GetUnityObjectName(m_nodesToRename[i]));

            List<GUIContent> listFbxNames = new List<GUIContent>();
            listFbxNames.Add(new GUIContent("[Delete]"));

            for (int j = 0; j < m_nodeNameToSuggest.Count; j++)
            {
                listFbxNames.Add(new GUIContent(m_fbxPrefabUtility.GetFBXObjectName(m_nodeNameToSuggest[j])));
            }

            options = listFbxNames.ToArray();

            selectedNodesToRename[i] = EditorGUILayout.Popup(selectedNodesToRename[i], options);

            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Apply Changes"))
        {
            ApplyChanges();
            //Close editor window
            Close();
        }

        if (GUILayout.Button("Cancel"))
        {
            //Close editor window
            Close();
        }
        GUILayout.EndHorizontal();
    }

    void ApplyChanges()
    {
        // Nodes to Destroy have Unity names
        for (int i = 0; i < m_nodesToDestroy.Count; i++)
        {
            // != [Delete] 
            if (selectedNodesToDestroy[i] != 0)
            {
                FbxPrefab.StringPair stringpair = new FbxPrefab.StringPair();
                stringpair.FBXObjectName = options[selectedNodesToDestroy[i]].text;
                stringpair.UnityObjectName = m_nodesToDestroy[i];

                m_fbxPrefab.NameMapping.Add(stringpair);
                Debug.Log("Mapped Unity: " + stringpair.UnityObjectName + " to FBX: " + stringpair.FBXObjectName);
            }
        }

        // Nodes to Rename have FBX names
        for (int i = 0; i < m_nodesToRename.Count; i++)
        {
            string currentUnityNodeName = m_fbxPrefabUtility.GetUnityObjectName(m_nodesToRename[i]);
            // == [Delete] 
            if (selectedNodesToRename[i] == 0)
            {
                // Remove previous mapping
                m_fbxPrefabUtility.RemoveMappingUnityObjectName(currentUnityNodeName);
            }
            else
            {
                if (currentUnityNodeName != m_fbxPrefabUtility.GetUnityObjectName(options[selectedNodesToRename[i]].text))
                {
                    m_fbxPrefabUtility.RemoveMappingUnityObjectName(currentUnityNodeName);
                    FbxPrefab.StringPair stringpair = new FbxPrefab.StringPair();
                    stringpair.FBXObjectName = options[selectedNodesToRename[i]].text;
                    stringpair.UnityObjectName = currentUnityNodeName;
                    m_fbxPrefab.NameMapping.Add(stringpair);

                    Debug.Log("Mapped Unity: " + stringpair.UnityObjectName + " to FBX: " + stringpair.FBXObjectName);
                }
                else
                {
                    Debug.Log("ALREADY Mapped Unity: " + currentUnityNodeName + " to FBX: " + options[selectedNodesToRename[i]].text);
                }
            }
        }

        m_fbxPrefabUtility.SyncPrefab();
    }
}