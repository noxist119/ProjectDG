using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Editor
{
	public static class UiSpriteCacheReloadSmoke
	{
		[Serializable]
		private sealed class SmokeReport
		{
			public string status;

			public bool passed;

			public int sessionCount;

			public SessionReport[] sessions = Array.Empty<SessionReport>();

			public string[] notes = Array.Empty<string>();
		}

		[Serializable]
		private sealed class SessionReport
		{
			public int session;

			public int rollRollCacheBeforeLoad;

			public int rollRollCacheAfterLoad;

			public int skinSlicedCacheBeforeLoad;

			public int skinSlicedCacheAfterLoad;

			public bool characterSpriteLoaded;

			public bool characterSpriteRecoveredAfterDestroy;

			public bool skinLoaded;

			public bool skinImageApplied;

			public bool runtimeSlicedSpriteRecoveredAfterDestroy;

			public bool passed;

			public List<string> notes = new List<string>();
		}

		private const string OutputDirectoryName = "BatchPlaytestResults";

		private const string OutputFileName = "UiSpriteCacheReloadSmoke.json";

		private const string SkinAssetPath = "Assets/Data/DefenseGameUiSkin_2DGameUIKit.asset";

		private static readonly List<SessionReport> sessions = new List<SessionReport>();

		private static readonly List<string> notes = new List<string>();

		private static bool running;

		private static bool failed;

		private static int playSession;

		private static bool previousEnterPlayModeOptionsEnabled;

		private static EnterPlayModeOptions previousEnterPlayModeOptions;

		private static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BatchPlaytestResults", "UiSpriteCacheReloadSmoke.json"));

		[MenuItem("DefenseGame/Smoke Tests/UI Sprite Cache Reload")]
		public static void RunUiSpriteCacheReloadSmoke()
		{
			if (!running)
			{
				running = true;
				failed = false;
				playSession = 0;
				sessions.Clear();
				notes.Clear();
				Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? string.Empty);
				if (File.Exists(OutputPath))
				{
					File.Delete(OutputPath);
				}
				previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
				previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
				EditorSettings.enterPlayModeOptionsEnabled = true;
				EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
				EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
				EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
				EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, new EditorApplication.CallbackFunction(StartPlayMode));
			}
		}

		private static void StartPlayMode()
		{
			if (running)
			{
				EditorApplication.isPlaying = true;
			}
		}

		private static void HandlePlayModeStateChanged(PlayModeStateChange state)
		{
			if (!running)
			{
				return;
			}
			switch (state)
			{
			case PlayModeStateChange.EnteredPlayMode:
				playSession++;
				try
				{
					sessions.Add(RunSessionCheck(playSession));
				}
				catch (Exception ex)
				{
					failed = true;
					notes.Add(ex.ToString());
				}
				EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, new EditorApplication.CallbackFunction(StopPlayMode));
				break;
			case PlayModeStateChange.EnteredEditMode:
				if (!failed && playSession < 2)
				{
					EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, new EditorApplication.CallbackFunction(StartPlayMode));
				}
				else
				{
					Finish();
				}
				break;
			}
		}

		private static void StopPlayMode()
		{
			if (running && EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
			}
		}

		private static SessionReport RunSessionCheck(int sessionIndex)
		{
			SessionReport report = new SessionReport
			{
				session = sessionIndex,
				rollRollCacheBeforeLoad = GetRollRollCacheCount(),
				skinSlicedCacheBeforeLoad = GetSkinSlicedCacheCount()
			};
			if (sessionIndex == 2 && report.rollRollCacheBeforeLoad != 0)
			{
				report.notes.Add("roll_roll_cache_not_reset_before_second_load=" + report.rollRollCacheBeforeLoad);
			}
			if (sessionIndex == 2 && report.skinSlicedCacheBeforeLoad != 0)
			{
				report.notes.Add("skin_sliced_cache_not_reset_before_second_load=" + report.skinSlicedCacheBeforeLoad);
			}
			Sprite characterSprite = RollRollUiResource.LoadSprite("Minimi/minimi_fire", false);
			report.characterSpriteLoaded = IsSpriteUsable(characterSprite);
			UnityEngine.Object.DestroyImmediate(characterSprite);
			Sprite characterReloaded = RollRollUiResource.LoadSprite("Minimi/minimi_fire", false);
			report.characterSpriteRecoveredAfterDestroy = IsSpriteUsable(characterReloaded);
			UiSkinResources skin = AssetDatabase.LoadAssetAtPath<UiSkinResources>("Assets/Data/DefenseGameUiSkin_2DGameUIKit.asset");
			report.skinLoaded = (UnityEngine.Object)(object)skin != null;
			report.skinImageApplied = CheckSkinImageApplied(skin);
			report.runtimeSlicedSpriteRecoveredAfterDestroy = CheckRuntimeSlicedSpriteRecovery();
			report.rollRollCacheAfterLoad = GetRollRollCacheCount();
			report.skinSlicedCacheAfterLoad = GetSkinSlicedCacheCount();
			report.passed = report.characterSpriteLoaded && report.characterSpriteRecoveredAfterDestroy && report.skinLoaded && report.skinImageApplied && report.runtimeSlicedSpriteRecoveredAfterDestroy && (sessionIndex != 2 || (report.rollRollCacheBeforeLoad == 0 && report.skinSlicedCacheBeforeLoad == 0));
			if (!report.passed)
			{
				failed = true;
			}
			return report;
		}

		private static bool CheckSkinImageApplied(UiSkinResources skin)
		{
			if ((UnityEngine.Object)(object)skin == null)
			{
				return false;
			}
			GameObject target = new GameObject("UiSpriteCacheReloadSmoke_SkinImage", typeof(RectTransform));
			Image image = target.AddComponent<Image>();
			RuntimeUiSkinUtility.ApplyImageSkin(image, skin, "LobbyBattleButton", true, true, false);
			bool applied = IsSpriteUsable(image.sprite);
			UnityEngine.Object.DestroyImmediate(target);
			return applied;
		}

		private static bool CheckRuntimeSlicedSpriteRecovery()
		{
			MethodInfo method = typeof(RuntimeUiSkinUtility).GetMethod("GetRuntimeSlicedSprite", BindingFlags.Static | BindingFlags.NonPublic);
			if (method == null)
			{
				return false;
			}
			Texture2D texture = new Texture2D(32, 32, TextureFormat.ARGB32, mipChain: false);
			texture.wrapMode = TextureWrapMode.Clamp;
			Color[] pixels = new Color[1024];
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = Color.white;
			}
			texture.SetPixels(pixels);
			texture.Apply();
			Sprite source = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 100f);
			Sprite first = method.Invoke(null, new object[1] { source }) as Sprite;
			bool firstLoaded = IsSpriteUsable(first);
			UnityEngine.Object.DestroyImmediate(first);
			Sprite second = method.Invoke(null, new object[1] { source }) as Sprite;
			bool secondLoaded = IsSpriteUsable(second);
			UnityEngine.Object.DestroyImmediate(second);
			UnityEngine.Object.DestroyImmediate(source);
			UnityEngine.Object.DestroyImmediate(texture);
			return firstLoaded && secondLoaded;
		}

		private static bool IsSpriteUsable(Sprite sprite)
		{
			return sprite != null && sprite.texture != null;
		}

		private static int GetRollRollCacheCount()
		{
			FieldInfo field = typeof(RollRollUiResource).GetField("SpriteCache", BindingFlags.Static | BindingFlags.NonPublic);
			object value = ((field != null) ? field.GetValue(null) : null);
			return (value is IDictionary dictionary) ? dictionary.Count : (-1);
		}

		private static int GetSkinSlicedCacheCount()
		{
			FieldInfo field = typeof(RuntimeUiSkinUtility).GetField("slicedSpriteCache", BindingFlags.Static | BindingFlags.NonPublic);
			object value = ((field != null) ? field.GetValue(null) : null);
			return (value is IDictionary dictionary) ? dictionary.Count : (-1);
		}

		private static void Finish()
		{
			SmokeReport report = new SmokeReport
			{
				status = ((!failed && sessions.Count == 2) ? "pass" : "fail"),
				passed = (!failed && sessions.Count == 2),
				sessionCount = sessions.Count,
				sessions = sessions.ToArray(),
				notes = notes.ToArray()
			};
			File.WriteAllText(OutputPath, JsonUtility.ToJson(report, prettyPrint: true));
			running = false;
			EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
			EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
			EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
			if (Application.isBatchMode)
			{
				EditorApplication.Exit((!report.passed) ? 1 : 0);
			}
		}
	}
}
