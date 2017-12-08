using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace FbxExporters.Editor
{
	public class RepairMissingScripts
	{
		private const string m_forumPackageGUID = "2d81c55c4d9d85146b1d2de96e084b63";
		private const string m_currentPackageGUID = "628ffbda3fdf4df4588770785d91a698";

		private const string m_fbxPrefabDLLFileId = "69888640";

		private const string m_idFormat = "{{fileID: {0}, guid: {1}, type:";


		private static string m_forumPackageSearchID;

		private static string ForumPackageSearchID {
			get {
				if (string.IsNullOrEmpty (m_forumPackageSearchID)) {
					m_forumPackageSearchID = string.Format (m_idFormat, m_fbxPrefabDLLFileId, m_forumPackageGUID);
				}
				return m_forumPackageSearchID;
			}
		}

		private static string m_currentPackageSearchID;

		private static string CurrentPackageSearchID {
			get {
				if (string.IsNullOrEmpty (m_currentPackageSearchID)) {
					m_currentPackageSearchID = string.Format (m_idFormat, m_fbxPrefabDLLFileId, m_currentPackageGUID);
				}
				return m_currentPackageSearchID;
			}
		}

		[MenuItem ("File/Repair Missing Scripts", false)]
		public static void Menu ()
		{
			ReplaceGUIDInTextAssets ();
		}

		private static bool ReplaceGUIDInTextAssets ()
		{
			// search project for assets containing old GUID

            // ignore if forced binary
            if (UnityEditor.EditorSettings.serializationMode == SerializationMode.ForceBinary) {
                return false;
            }

			// check all scenes and prefabs
			string[] searchFilePatterns = new string[]{ "*.prefab", "*.unity" };

			bool replacedGUID = false;
			foreach (string searchPattern in searchFilePatterns) {
				foreach (string file in Directory.GetFiles(Application.dataPath, searchPattern, SearchOption.AllDirectories)) {
					replacedGUID |= ReplaceGUIDInFile (file);
				}
			}
			if (replacedGUID) {
				AssetDatabase.Refresh ();
			}
			return replacedGUID;
		}

		private static bool ReplaceGUIDInFile (string path)
		{
			// try to read file, assume it's a text file for now
			int modified = 0;

			try {
				var sr = new StreamReader (path);
				var sw = new StreamWriter (path + ".remap", false);

				while (sr.Peek () > -1) {
					var line = sr.ReadLine ();

					if (line.Contains (ForumPackageSearchID)) {
						modified++;
						line = line.Replace (ForumPackageSearchID, CurrentPackageSearchID);
					}

					sw.WriteLine (line);
				}

				sr.Close ();
				sw.Close ();

				if (modified > 0) {
					File.Delete (path);
					File.Move (path + ".remap", path);
					return true;
				} else {
					File.Delete (path + ".remap");
				}
			} catch (IOException e) {
				Debug.LogError (string.Format ("Failed to replace GUID in file {0} (error={1})", path, e));
			}

			return false;
		}
	}
}