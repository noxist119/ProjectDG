using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DefenseGame.Editor
{
	public static class AndroidApkBuilder
	{
		private const string DefaultScenePath = "Assets/Scenes/DG.unity";

		private const string OutputDirectory = "Builds/Android";

		private const string OutputApkPath = "Builds/Android/ProjectDG.apk";

		private const string AndroidApplicationId = "com.noxis.projectdg";

		[MenuItem("Defense Game/Build Android APK")]
		public static void BuildDebugApk()
		{
			string[] scenes = ResolveScenes();
			ConfigureAndroidPlayerSettings();
			Directory.CreateDirectory("Builds/Android");
			EditorUserBuildSettings.buildAppBundle = false;
			BuildPlayerOptions options = new BuildPlayerOptions
			{
				scenes = scenes,
				locationPathName = "Builds/Android/ProjectDG.apk",
				target = BuildTarget.Android,
				options = BuildOptions.None
			};
			BuildReport report = BuildPipeline.BuildPlayer(options);
			if (report.summary.result != BuildResult.Succeeded)
			{
				throw new BuildFailedException("Android APK build failed. See Unity build log for details.");
			}
			Debug.Log("Android APK build succeeded: " + Path.GetFullPath("Builds/Android/ProjectDG.apk"));
		}

		private static string[] ResolveScenes()
		{
			string[] enabledScenes = (from scene in EditorBuildSettings.scenes
				where scene.enabled && !string.IsNullOrWhiteSpace(scene.path)
				select scene.path).ToArray();
			if (enabledScenes.Length != 0)
			{
				return enabledScenes;
			}
			if (File.Exists("Assets/Scenes/DG.unity"))
			{
				return new string[1] { "Assets/Scenes/DG.unity" };
			}
			string[] fallbackScenes = (Directory.Exists("Assets/Scenes") ? Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories) : new string[0]);
			if (fallbackScenes.Length != 0)
			{
				return fallbackScenes;
			}
			throw new BuildFailedException("No scenes available for Android build.");
		}

		private static void ConfigureAndroidPlayerSettings()
		{
			PlayerSettings.companyName = "Noxis";
			PlayerSettings.productName = "ProjectDG";
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.noxis.projectdg");
			PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
			PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
			PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			PlayerSettings.Android.buildApkPerCpuArchitecture = false;
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
		}
	}
}
