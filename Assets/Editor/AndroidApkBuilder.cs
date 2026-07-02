using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DefenseGame.Editor
{
    public static class AndroidApkBuilder
    {
        private const string DefaultScenePath = "Assets/Scenes/DG.unity";
        private const string OutputDirectory = "Builds/Android";
        private const string OutputApkPath = OutputDirectory + "/ProjectDG.apk";
        private const string AndroidApplicationId = "com.noxis.projectdg";

        [MenuItem("Defense Game/Build Android APK")]
        public static void BuildDebugApk()
        {
            string[] scenes = ResolveScenes();
            ConfigureAndroidPlayerSettings();
            Directory.CreateDirectory(OutputDirectory);

            EditorUserBuildSettings.buildAppBundle = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Android APK build failed. See Unity build log for details.");
            }

            UnityEngine.Debug.Log("Android APK build succeeded: " + Path.GetFullPath(OutputApkPath));
        }

        private static string[] ResolveScenes()
        {
            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (enabledScenes.Length > 0)
            {
                return enabledScenes;
            }

            if (File.Exists(DefaultScenePath))
            {
                return new[] { DefaultScenePath };
            }

            string[] fallbackScenes = Directory.Exists("Assets/Scenes")
                ? Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories)
                : new string[0];

            if (fallbackScenes.Length > 0)
            {
                return fallbackScenes;
            }

            throw new BuildFailedException("No scenes available for Android build.");
        }

        private static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.companyName = "Noxis";
            PlayerSettings.productName = "ProjectDG";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidApplicationId);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        }
    }
}
