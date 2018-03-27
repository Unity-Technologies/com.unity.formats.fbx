using UnityEngine;
using NUnit.Framework;
using UnityEditor;
using FbxExporters.EditorTools;
using System.Collections.Generic;

namespace FbxExporters.UnitTests
{
    public class ExportModelAnimationCustomPropertyTest : ExporterTestBase
    {
         protected ExportModelSettingsSerialize exportSettings;
        [SetUp]
        public override void Init()
        {
            base.Init ();
            exportSettings = new ExportModelSettingsSerialize ();
            exportSettings.include = ExportSettings.Include.ModelAndAnim;
            exportSettings.exportFormat = ExportSettings.ExportFormat.ASCII;
        }

        [Test]
        public void ExportCustomPropertyTest()
        {
		    GameObject capsule = GameObject.CreatePrimitive (PrimitiveType.Capsule);
            Animator animatorComponent = capsule.AddComponent<Animator>();

            // Creates the controller
            UnityEditor.Animations.AnimatorController animatorController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath("Assets/AnimController.controller");

            // Add parameters
            animatorController.AddParameter("Temperature", AnimatorControllerParameterType.Float);

            // Assign Controller to component
            animatorComponent.runtimeAnimatorController = animatorController;

            AnimationClip originalClip = new AnimationClip();
            // We need an example curve so the animated custom properties in the FBX will be recognized as well
            originalClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.EaseInOut(0, 0, 5, 11));
            // Animated custom property
            originalClip.SetCurve("", typeof(UnityEngine.Animator), "Temperature", AnimationCurve.EaseInOut(0,0,5, 102));

            AssetDatabase.CreateAsset(originalClip, "Assets/Animation.anim");
            UnityEditor.Animations.AnimatorState playMotion = animatorController.AddMotion(originalClip);

            var filename = GetRandomFileNamePath();

            // Export GameObject Capsule to Model
            GameObject m_fbx = ExportSelection(filename, capsule, exportSettings);
            // Check the "Animated custom properties" checkbox
            ModelImporter modelImporter = AssetImporter.GetAtPath (filename) as ModelImporter;
            modelImporter.importAnimatedCustomProperties = true;

            // Get clips from exported FBX
            Dictionary<string, AnimationClip> clipsDictionary = FbxAnimationTest.AnimTester.GetClipsFromFbx(filename);
            foreach(KeyValuePair<string, AnimationClip> entry in clipsDictionary)
            {
                // Compare if they match with the original
                FbxAnimationTest.AnimTester.ClipTest (originalClip, entry.Value);
            }
        }




    }



}
