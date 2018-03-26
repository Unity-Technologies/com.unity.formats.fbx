using UnityEngine;
using Unity.FbxSdk;
using FbxExporters.Editor;
using UnityEditor;

[InitializeOnLoad]
public class EditorScript {

    private static bool m_FbxIsLoaded = false;

	public static bool FbxEnabled { get { return m_FbxIsLoaded; } }

    static EditorScript()
    {
        TryLoadFbxSupport();
    }

    static void TryLoadFbxSupport()
	{
        if (m_FbxIsLoaded)
        {
            return;
        }

        ModelExporter.AddProperty += AddCustomProperty;

		m_FbxIsLoaded = true;
	}
        

    private static FbxNode AddCustomProperty(GameObject unityGo, FbxNode fbxNode)
	{
        Animator animator = unityGo.GetComponent<Animator> ();
        if (animator != null)
        {
            UnityEditor.Animations.AnimatorController animatorController = (UnityEditor.Animations.AnimatorController) animator.runtimeAnimatorController;
            if (animatorController != null)
            {
                for (int i=0; i <  animatorController.parameters.Length; i++)
                {
                    AnimatorControllerParameter param = animatorController.parameters[i];
                    Unity.FbxSdk.FbxProperty fbxProperty = null;
                    if (param.type == AnimatorControllerParameterType.Int)
                    {
                        fbxProperty = FbxProperty.Create(fbxNode, Globals.FbxIntDT, param.name);
                        if (!fbxProperty.IsValid()) {
                            throw new System.NullReferenceException();
                        }
                        Debug.Log("DELEGATE " + param.name + " value" +  param.defaultInt);
                        fbxProperty.Set(param.defaultInt);
                    }
                    else if (param.type == AnimatorControllerParameterType.Float)
                    {
                        fbxProperty = FbxProperty.Create(fbxNode, Globals.FbxFloatDT, param.name);
                        if (!fbxProperty.IsValid()) {
                            throw new System.NullReferenceException();
                        }
                        Debug.Log("DELEGATE " + param.name + " value" +  param.defaultFloat);
                        fbxProperty.Set(param.defaultFloat);
                    }
                    else if (param.type == AnimatorControllerParameterType.Bool)
                    {
                        fbxProperty = FbxProperty.Create(fbxNode, Globals.FbxBoolDT, param.name);
                        if (!fbxProperty.IsValid()) {
                            throw new System.NullReferenceException();
                        }
                        Debug.Log("DELEGATE " + param.name + " value" +  param.defaultBool);
                        fbxProperty.Set(System.Convert.ToInt32(param.defaultBool));
                    }

                    fbxProperty.ModifyFlag(FbxPropertyFlags.EFlags.eUserDefined, true);
                    fbxProperty.ModifyFlag(FbxPropertyFlags.EFlags.eAnimatable, true);
                }
            }
        }
		return fbxNode;
	}
}