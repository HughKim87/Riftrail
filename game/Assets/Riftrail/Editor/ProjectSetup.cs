using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Riftrail.Editor
{
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Riftrail/Scenes/Bootstrap.unity";

        [MenuItem("Riftrail/Initialize Project")]
        public static void Initialize()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.productName = "RIFTRAIL";
            PlayerSettings.companyName = "Riftrail";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.riftrail.game");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.riftrail.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Riftrail/Scenes");
                AssetDatabase.Refresh();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.075f, 0.10f);
                new GameObject("Bootstrap", typeof(Bootstrap));
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("RIFTRAIL project initialization passed.");
        }

        [MenuItem("Riftrail/Validate Project")]
        public static void Validate()
        {
            if (!File.Exists(ScenePath))
                throw new InvalidOperationException("Bootstrap scene is missing.");
            if (PlayerSettings.productName != "RIFTRAIL")
                throw new InvalidOperationException("Unexpected product name.");
            if (EditorBuildSettings.scenes.Length != 1 || !EditorBuildSettings.scenes[0].enabled
                || EditorBuildSettings.scenes[0].path != ScenePath)
                throw new InvalidOperationException("Build must contain the bootstrap scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrapCount = 0;
            var cameraCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                bootstrapCount += root.GetComponentsInChildren<Bootstrap>(true).Length;
                cameraCount += root.GetComponentsInChildren<Camera>(true).Length;
            }
            if (bootstrapCount != 1 || cameraCount != 1)
                throw new InvalidOperationException("Expected one bootstrap and one camera.");
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                throw new InvalidOperationException("Expected portrait orientation.");
        }

        public static void BuildWebGL()
        {
            Validate();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))
                throw new InvalidOperationException("Unity WebGL Build Support module is not installed.");
            BattleChecks.Run();
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"-riftrail-webgl-output");
            var output=index>=0 && index+1<args.Length?Path.GetFullPath(args[index+1]):Path.GetFullPath(Path.Combine(Application.dataPath,"../../outputs/webgl"));
            Directory.CreateDirectory(output);
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.template="PROJECT:Riftrail";
            PlayerSettings.runInBackground=true;
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("WebGL build failed: "+report.summary.result);
            Debug.Log("RIFTRAIL WebGL build passed: "+output);
        }

        public static void BuildWindowsSmoke()
        {
            Validate();
            BattleChecks.Run();
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../work/builds/windows/Riftrail.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Smoke build failed: " + report.summary.result);
            Debug.Log("RIFTRAIL Windows smoke build passed.");
        }
    }
}
