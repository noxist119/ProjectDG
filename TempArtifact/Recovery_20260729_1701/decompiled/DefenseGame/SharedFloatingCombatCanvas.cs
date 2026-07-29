using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenseGame;

[DefaultExecutionOrder(-200)]
public sealed class SharedFloatingCombatCanvas : MonoBehaviour
{
	public const float CanvasWorldScale = 0.01f;

	private const int WorldUiSortingOrder = 85;

	private const float PoseTimeEpsilon = 0.0001f;

	private static readonly Dictionary<int, FloatingCombatUI> UiByTargetId = new Dictionary<int, FloatingCombatUI>(96);

	private static SharedFloatingCombatCanvas instance;

	private static Canvas sharedCanvas;

	private static Camera cachedWorldCamera;

	private static int evaluatedPoseFrame = -1;

	private static bool refreshPoseThisFrame;

	private static float nextPoseRefreshTime;

	public static int ActiveUiCount => UiByTargetId.Count;

	public static Camera WorldCamera
	{
		get
		{
			if ((Object)(object)cachedWorldCamera == (Object)null || !((Behaviour)cachedWorldCamera).isActiveAndEnabled)
			{
				cachedWorldCamera = Camera.main;
				if ((Object)(object)sharedCanvas != (Object)null)
				{
					sharedCanvas.worldCamera = cachedWorldCamera;
				}
			}
			return cachedWorldCamera;
		}
	}

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void ResetStatics()
	{
		UiByTargetId.Clear();
		instance = null;
		sharedCanvas = null;
		cachedWorldCamera = null;
		evaluatedPoseFrame = -1;
		refreshPoseThisFrame = true;
		nextPoseRefreshTime = 0f;
	}

	public static Canvas GetOrCreate(Transform sceneOwner)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)sharedCanvas != (Object)null)
		{
			return sharedCanvas;
		}
		SharedFloatingCombatCanvas sharedFloatingCombatCanvas = Object.FindObjectOfType<SharedFloatingCombatCanvas>();
		if ((Object)(object)sharedFloatingCombatCanvas != (Object)null)
		{
			instance = sharedFloatingCombatCanvas;
			sharedCanvas = ((Component)sharedFloatingCombatCanvas).GetComponent<Canvas>();
			return sharedCanvas;
		}
		GameObject val = new GameObject("SharedFloatingCombatCanvas", new Type[1] { typeof(RectTransform) });
		val.layer = 5;
		if ((Object)(object)sceneOwner != (Object)null)
		{
			Scene scene = ((Component)sceneOwner).gameObject.scene;
			if (((Scene)(ref scene)).IsValid())
			{
				SceneManager.MoveGameObjectToScene(val, ((Component)sceneOwner).gameObject.scene);
			}
		}
		RectTransform component = val.GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(20000f, 20000f);
		val.transform.position = Vector3.zero;
		val.transform.rotation = Quaternion.identity;
		val.transform.localScale = Vector3.one * 0.01f;
		sharedCanvas = val.AddComponent<Canvas>();
		sharedCanvas.renderMode = (RenderMode)2;
		sharedCanvas.overrideSorting = true;
		sharedCanvas.sortingOrder = 85;
		sharedCanvas.worldCamera = Camera.main;
		CanvasScaler val2 = val.AddComponent<CanvasScaler>();
		val2.dynamicPixelsPerUnit = 48f;
		instance = val.AddComponent<SharedFloatingCombatCanvas>();
		cachedWorldCamera = sharedCanvas.worldCamera;
		return sharedCanvas;
	}

	public static bool TryGet(Transform target, out FloatingCombatUI ui)
	{
		ui = null;
		if ((Object)(object)target == (Object)null)
		{
			return false;
		}
		int instanceID = ((Object)target).GetInstanceID();
		if (!UiByTargetId.TryGetValue(instanceID, out var value))
		{
			return false;
		}
		if ((Object)(object)value == (Object)null)
		{
			UiByTargetId.Remove(instanceID);
			return false;
		}
		ui = value;
		return true;
	}

	public static void Register(Transform target, FloatingCombatUI ui)
	{
		if (!((Object)(object)target == (Object)null) && !((Object)(object)ui == (Object)null))
		{
			UiByTargetId[((Object)target).GetInstanceID()] = ui;
		}
	}

	public static void Unregister(int targetId, FloatingCombatUI ui)
	{
		if (targetId != 0 && UiByTargetId.TryGetValue(targetId, out var value) && !((Object)(object)value != (Object)(object)ui))
		{
			UiByTargetId.Remove(targetId);
		}
	}

	public static bool ShouldRefreshPoseThisFrame()
	{
		int frameCount = Time.frameCount;
		if (evaluatedPoseFrame == frameCount)
		{
			return refreshPoseThisFrame;
		}
		evaluatedPoseFrame = frameCount;
		float worldUiPoseInterval = MobileFrameRateController.WorldUiPoseInterval;
		float unscaledTime = Time.unscaledTime;
		refreshPoseThisFrame = nextPoseRefreshTime <= 0f || unscaledTime + 0.0001f >= nextPoseRefreshTime;
		if (!refreshPoseThisFrame)
		{
			return false;
		}
		if (nextPoseRefreshTime <= 0f)
		{
			nextPoseRefreshTime = unscaledTime + worldUiPoseInterval;
		}
		else
		{
			nextPoseRefreshTime += worldUiPoseInterval;
			if (nextPoseRefreshTime < unscaledTime)
			{
				nextPoseRefreshTime = unscaledTime + worldUiPoseInterval;
			}
		}
		return true;
	}

	private void OnDestroy()
	{
		if (!((Object)(object)instance != (Object)(object)this))
		{
			UiByTargetId.Clear();
			instance = null;
			sharedCanvas = null;
			cachedWorldCamera = null;
			evaluatedPoseFrame = -1;
			nextPoseRefreshTime = 0f;
		}
	}
}
