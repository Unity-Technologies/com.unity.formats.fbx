using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Formats.Fbx.Exporter;
using System.Linq;

namespace UnityEditor.Formats.Fbx.Exporter
{

    internal class ManualUpdateEditorWindow : EditorWindow
    {
        int[] selectedNodesToDestroy;
        int[] selectedNodesToRename;

        FbxPrefabUtility m_fbxPrefabUtility;
        FbxPrefab m_fbxPrefab;
        GUIContent[] options;
        List<string> m_nodesToCreate;
        List<string> m_nodesToDestroy;
        List<string> m_nodesToRename;

        List<string> m_nodeNameToSuggest;

        public bool Verbose { get { return UnityEditor.Formats.Fbx.Exporter.ExportSettings.instance.VerboseProperty; } }

        public void Init(FbxPrefabUtility fbxPrefabUtility, FbxPrefab fbxPrefab)
        {
            if(fbxPrefab == null)
            {
                return;
            }

            FbxPrefabUtility.UpdateList updates = new FbxPrefabUtility.UpdateList(new FbxRepresentation(fbxPrefab.FbxHistory), fbxPrefab.FbxModel.transform, fbxPrefab);

            m_fbxPrefabUtility = fbxPrefabUtility;
            m_fbxPrefab = fbxPrefab;
            // Convert Hashset into List
            m_nodesToCreate = updates.NodesToCreate.ToList();
            m_nodesToDestroy = updates.NodesToDestroy.ToList();
            m_nodesToRename = updates.NodesToRename.ToList();
            // Create the dropdown list
            m_nodeNameToSuggest = new List<string>();
            m_nodeNameToSuggest.AddRange(m_nodesToCreate);
            m_nodeNameToSuggest.AddRange(m_nodesToRename);

            // Keep track of the selected combo option in each type
            selectedNodesToDestroy = new int[m_nodesToDestroy.Count];
            selectedNodesToRename = new int[m_nodesToRename.Count];

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

                for (int j = 0; j < m_nodeNameToSuggest.Count; j++)
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
                    StringPair stringpair = new StringPair();
                    stringpair.FBXObjectName = options[selectedNodesToDestroy[i]].text;
                    stringpair.UnityObjectName = m_nodesToDestroy[i];

                    m_fbxPrefab.NameMapping.Add(stringpair);

                    if (Verbose)
                    {
                        Debug.Log("Mapped Unity: " + stringpair.UnityObjectName + " to FBX: " + stringpair.FBXObjectName);
                    }
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
                        StringPair stringpair = new StringPair();
                        stringpair.FBXObjectName = options[selectedNodesToRename[i]].text;
                        stringpair.UnityObjectName = currentUnityNodeName;
                        m_fbxPrefab.NameMapping.Add(stringpair);

                        if (Verbose)
                        {
                            Debug.Log("Mapped Unity: " + stringpair.UnityObjectName + " to FBX: " + stringpair.FBXObjectName);
                        }
                    }
                    else
                    {
                        if (Verbose)
                        {
                            Debug.Log("ALREADY Mapped Unity: " + currentUnityNodeName + " to FBX: " + options[selectedNodesToRename[i]].text);
                        }
                    }
                }
            }

            m_fbxPrefabUtility.SyncPrefab();
        }
    }
}