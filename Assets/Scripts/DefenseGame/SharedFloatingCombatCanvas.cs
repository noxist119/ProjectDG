using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefenseGame
{
	[DefaultExecutionOrder(-200)]
	public sealed class SharedFloatingCombatCanvas : MonoBehaviour
	{
		private const int CombatHudSortingOrder = -10;

		private const float PoseTimeEpsilon = 0.0001f;

		private static readonly Dictionary<int, FloatingCombatUI> UiByTargetId = new Dictionary<int, FloatingCombatUI>(96);

		private static readonly HashSet<int> PoseRefreshOverrideTargetIds = new HashSet<int>();

		private static readonly HashSet<int> PendingPoseRefreshTargetIds = new HashSet<int>();

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
				}
				return cachedWorldCamera;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			UiByTargetId.Clear();
			PoseRefreshOverrideTargetIds.Clear();
			PendingPoseRefreshTargetIds.Clear();
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
				ConfigureScreenSpaceCanvas(sharedCanvas);
				return sharedCanvas;
			}
			SharedFloatingCombatCanvas existing = Object.FindObjectOfType<SharedFloatingCombatCanvas>();
			if (existing != null)
			{
				instance = existing;
				sharedCanvas = existing.GetComponent<Canvas>();
				ConfigureScreenSpaceCanvas(sharedCanvas);
				return sharedCanvas;
			}
			GameObject root = new GameObject("SharedFloatingCombatCanvas", typeof(RectTransform));
			root.layer = 5;
			if (sceneOwner != null && sceneOwner.gameObject.scene.IsValid())
			{
				SceneManager.MoveGameObjectToScene(root, sceneOwner.gameObject.scene);
			}
			RectTransform rootRect = root.GetComponent<RectTransform>();
			rootRect.anchorMin = Vector2.zero;
			rootRect.anchorMax = Vector2.one;
			rootRect.offsetMin = Vector2.zero;
			rootRect.offsetMax = Vector2.zero;
			root.transform.localScale = Vector3.one;
			sharedCanvas = root.AddComponent<Canvas>();
			ConfigureScreenSpaceCanvas(sharedCanvas);
			instance = root.AddComponent<SharedFloatingCombatCanvas>();
			cachedWorldCamera = null;
			return sharedCanvas;
		}

		private static void ConfigureScreenSpaceCanvas(Canvas targetCanvas)
		{
			if (targetCanvas == null)
			{
				return;
			}
			targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
			targetCanvas.worldCamera = null;
			targetCanvas.overrideSorting = true;
			targetCanvas.sortingOrder = CombatHudSortingOrder;
			CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
			if (scaler == null)
			{
				scaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();
			}
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			scaler.matchWidthOrHeight = 0.84f;
		}

        public static bool TryConvertWorldPointToCanvasPoint(Canvas canvas, Vector3 worldPoint, out Vector2 canvasPoint)
        {
            canvasPoint = Vector2.zero;
            if (canvas == null || !(canvas.transform is RectTransform root))
            {
                return false;
            }

            Camera worldCamera = WorldCamera;
            if (worldCamera == null)
            {
                return false;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPoint);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, null, out canvasPoint);
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
			PoseRefreshOverrideTargetIds.Remove(targetId);
			PendingPoseRefreshTargetIds.Remove(targetId);
		}

		public static void SetPoseRefreshOverride(Transform target, bool enabled)
		{
			if (target == null) return;
			int targetId = target.GetInstanceID();
			if (enabled) PoseRefreshOverrideTargetIds.Add(targetId); else PoseRefreshOverrideTargetIds.Remove(targetId);
			PendingPoseRefreshTargetIds.Add(targetId);
		}

		public static bool IsPoseRefreshOverrideActive(Transform target)
		{
			return target != null && PoseRefreshOverrideTargetIds.Contains(target.GetInstanceID());
		}

		public static bool ShouldRefreshPoseThisFrame(Transform target)
		{
			if (target != null)
			{
				int targetId = target.GetInstanceID();
				if (PoseRefreshOverrideTargetIds.Contains(targetId) || PendingPoseRefreshTargetIds.Remove(targetId)) return true;
			}
			return ShouldRefreshPoseThisFrame();
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
				PoseRefreshOverrideTargetIds.Clear();
				PendingPoseRefreshTargetIds.Clear();
				instance = null;
				sharedCanvas = null;
				cachedWorldCamera = null;
				evaluatedPoseFrame = -1;
				nextPoseRefreshTime = 0f;
			}
		}
	}
}
