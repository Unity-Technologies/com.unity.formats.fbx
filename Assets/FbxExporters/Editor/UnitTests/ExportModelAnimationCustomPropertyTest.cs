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
            UnityEditor.Animations.AnimatorController animatorController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath("Assets/StateMachineTransitions.controller");

            // Add parameters
            animatorController.AddParameter("Temperature", AnimatorControllerParameterType.Float);

            // Add StateMachines
            UnityEditor.Animations.AnimatorStateMachine rootStateMachine = animatorController.layers[0].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine stateMachineA = rootStateMachine.AddStateMachine("Temperature");

            // Add States
            UnityEditor.Animations.AnimatorState stateA1 = stateMachineA.AddState("stateA1");

            // Assign Controller to component
            animatorComponent.runtimeAnimatorController = animatorController;

            AnimationClip clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "position.x", AnimationCurve.EaseInOut(0, 0, 1, 1));
            clip.SetCurve("", typeof(Transform), "position.x", AnimationCurve.EaseInOut(1, 1, 0, 0));

            clip.SetCurve("", typeof(UnityEngine.Animator), "Temperature", AnimationCurve.Constant(0, 5, 102));
            clip.SetCurve("", typeof(UnityEngine.Animator), "Temperature", AnimationCurve.Constant(50, 10, 2));
            clip.SetCurve("", typeof(UnityEngine.Animator), "Temperature", AnimationCurve.Constant(100, 15, 52));

            AssetDatabase.CreateAsset(clip, "Assets/StateMachineTransitions.anim");
            UnityEditor.Animations.AnimatorState playMotion = animatorController.AddMotion(clip);

            var filename = GetRandomFileNamePath();


            // Export GameObject Capsule to Model
            GameObject m_fbx = ExportSelection(filename, capsule, exportSettings);
            Dictionary<string, AnimationClip> clipsDictionary = GetClipsFromFbx(filename);
            Assert.IsTrue (clipsDictionary.ContainsKey ("Temperature"));


        }


        public static Dictionary<string, AnimationClip> GetClipsFromFbx(string path, bool setLegacy = false){
            //acquire imported object from exported file
            Object [] goAssetImported = AssetDatabase.LoadAllAssetsAtPath (path);
            Assert.That (goAssetImported, Is.Not.Null);

            // TODO : configure object so that it imports w Legacy Animation

            var animClips = new Dictionary<string, AnimationClip> ();
            foreach (Object o in goAssetImported) {
                var animClipImported = o as AnimationClip;
                if (animClipImported && !animClipImported.name.StartsWith("__preview__")) {
                    // TODO : configure import settings so we don't need to force legacy
                    animClipImported.legacy = setLegacy;
                    animClips.Add (animClipImported.name, animClipImported);
                }
            }
            Assert.That (animClips, Is.Not.Empty, "expected imported clips");

            return animClips;
        }
    }
}
