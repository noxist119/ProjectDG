using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenseGame
{
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
				if (cachedWorldCamera == null || !cachedWorldCamera.isActiveAndEnabled)
				{
					cachedWorldCamera = Camera.main;
					if (sharedCanvas != null)
					{
						sharedCanvas.worldCamera = cachedWorldCamera;
					}
				}
				return cachedWorldCamera;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
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
			if (sharedCanvas != null)
			{
				return sharedCanvas;
			}
			SharedFloatingCombatCanvas existing = Object.FindObjectOfType<SharedFloatingCombatCanvas>();
			if (existing != null)
			{
				instance = existing;
				sharedCanvas = existing.GetComponent<Canvas>();
				return sharedCanvas;
			}
			GameObject root = new GameObject("SharedFloatingCombatCanvas", typeof(RectTransform));
			root.layer = 5;
			if (sceneOwner != null && sceneOwner.gameObject.scene.IsValid())
			{
				SceneManager.MoveGameObjectToScene(root, sceneOwner.gameObject.scene);
			}
			RectTransform rootRect = root.GetComponent<RectTransform>();
			rootRect.sizeDelta = new Vector2(20000f, 20000f);
			root.transform.position = Vector3.zero;
			root.transform.rotation = Quaternion.identity;
			root.transform.localScale = Vector3.one * 0.01f;
			sharedCanvas = root.AddComponent<Canvas>();
			sharedCanvas.renderMode = RenderMode.WorldSpace;
			sharedCanvas.overrideSorting = true;
			sharedCanvas.sortingOrder = 85;
			sharedCanvas.worldCamera = Camera.main;
			CanvasScaler scaler = root.AddComponent<CanvasScaler>();
			scaler.dynamicPixelsPerUnit = 48f;
			instance = root.AddComponent<SharedFloatingCombatCanvas>();
			cachedWorldCamera = sharedCanvas.worldCamera;
			return sharedCanvas;
		}

		public static bool TryGet(Transform target, out FloatingCombatUI ui)
		{
			ui = null;
			if (target == null)
			{
				return false;
			}
			int targetId = target.GetInstanceID();
			if (!UiByTargetId.TryGetValue(targetId, out var registered))
			{
				return false;
			}
			if (registered == null)
			{
				UiByTargetId.Remove(targetId);
				return false;
			}
			ui = registered;
			return true;
		}

		public static void Register(Transform target, FloatingCombatUI ui)
		{
			if (!(target == null) && !(ui == null))
			{
				UiByTargetId[target.GetInstanceID()] = ui;
			}
		}

		public static void Unregister(int targetId, FloatingCombatUI ui)
		{
			if (targetId != 0 && UiByTargetId.TryGetValue(targetId, out var registered) && !(registered != ui))
			{
				UiByTargetId.Remove(targetId);
			}
		}

		public static bool ShouldRefreshPoseThisFrame()
		{
			int frame = Time.frameCount;
			if (evaluatedPoseFrame == frame)
			{
				return refreshPoseThisFrame;
			}
			evaluatedPoseFrame = frame;
			float interval = MobileFrameRateController.WorldUiPoseInterval;
			float now = Time.unscaledTime;
			refreshPoseThisFrame = nextPoseRefreshTime <= 0f || now + 0.0001f >= nextPoseRefreshTime;
			if (!refreshPoseThisFrame)
			{
				return false;
			}
			if (nextPoseRefreshTime <= 0f)
			{
				nextPoseRefreshTime = now + interval;
			}
			else
			{
				nextPoseRefreshTime += interval;
				if (nextPoseRefreshTime < now)
				{
					nextPoseRefreshTime = now + interval;
				}
			}
			return true;
		}

		private void OnDestroy()
		{
			if (!(instance != this))
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
}
