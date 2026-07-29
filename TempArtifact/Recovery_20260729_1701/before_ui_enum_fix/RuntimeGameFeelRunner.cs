using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
	public class RuntimeGameFeelRunner : MonoBehaviour
	{
		private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

		private static Material highGradeVfxMaterial;

		private Coroutine hitStopRoutine;

		private float baseFixedDeltaTime;

		private void Awake()
		{
			baseFixedDeltaTime = Time.fixedDeltaTime;
		}

		public void HitStop(float targetScale, float duration)
		{
			if (duration <= 0f || DefenseGameController.IsDefeatSlowMotionActive)
			{
				return;
			}
			if (hitStopRoutine != null)
			{
				StopCoroutine(hitStopRoutine);
				if (!DefenseGameController.IsDefeatSlowMotionActive)
				{
					Time.timeScale = 1f;
					Time.fixedDeltaTime = baseFixedDeltaTime;
				}
			}
			hitStopRoutine = StartCoroutine(HitStopRoutine(Mathf.Clamp(targetScale, 0.08f, 1f), Mathf.Max(0.02f, duration)));
		}

		public void DelayedPulse(Vector3 position, Color color, float radius, float delay)
		{
			StartCoroutine(DelayedPulseRoutine(position, color, radius, delay));
		}

		public void ShowJackpotReveal(string title, string gradeLabel, string unitName, Color color, string detail, float duration)
		{
			StartCoroutine(JackpotRevealRoutine(title, gradeLabel, unitName, color, detail, Mathf.Max(1.2f, duration)));
		}

		public void PlayHighGradeSummonVfx(Vector3 position, Color color, CharacterGrade grade)
		{
			StartCoroutine(HighGradeSummonVfxRoutine(position, color, grade));
		}

		public void PlaySummonArrivalVfx(Vector3 position, Color color, CharacterGrade grade)
		{
			StartCoroutine(SummonArrivalVfxRoutine(position, color, grade));
		}

		public void PlayMergeResultVfx(Vector3 position, Color color, CharacterGrade grade, bool ultimate)
		{
			StartCoroutine(MergeResultVfxRoutine(position, color, grade, ultimate));
		}

		private IEnumerator HitStopRoutine(float targetScale, float duration)
		{
			float previousScale = Time.timeScale;
			Time.timeScale = targetScale;
			Time.fixedDeltaTime = baseFixedDeltaTime * targetScale;
			yield return new WaitForSecondsRealtime(duration);
			if (!DefenseGameController.IsDefeatSlowMotionActive)
			{
				Time.timeScale = (Mathf.Approximately(previousScale, 0f) ? 1f : previousScale);
				Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
			}
			hitStopRoutine = null;
		}

		private IEnumerator DelayedPulseRoutine(Vector3 position, Color color, float radius, float delay)
		{
			yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
			RuntimeCombatFeedback.ShowGroundPulse(position, color, radius, 0.34f, 0.13f);
		}

		private IEnumerator SummonArrivalVfxRoutine(Vector3 position, Color color, CharacterGrade grade)
		{
			bool rareOrBetter = grade >= CharacterGrade.Rare;
			float duration = (rareOrBetter ? 0.82f : 0.58f);
			Color bright = Color.Lerp(color, Color.white, rareOrBetter ? 0.46f : 0.34f);
			GameObject root = new GameObject("SummonArrivalVfx");
			root.transform.position = position + Vector3.up * 0.08f;
			LineRenderer outerRing = CreateWorldRing(root.transform, "LandingRing", bright, rareOrBetter ? 0.62f : 0.48f, rareOrBetter ? 0.045f : 0.034f, 72, 0f);
			LineRenderer innerRing = CreateWorldRing(root.transform, "FocusRing", color, rareOrBetter ? 0.36f : 0.28f, rareOrBetter ? 0.034f : 0.026f, 56, 0.025f);
			LineRenderer beam = CreateVerticalBeam(root.transform, "ArrivalBeam", Color.Lerp(bright, Color.white, 0.22f), Vector3.zero, rareOrBetter ? 1.75f : 1.2f, rareOrBetter ? 0.034f : 0.024f);
			LineRenderer[] ticks = new LineRenderer[4];
			for (int i = 0; i < ticks.Length; i++)
			{
				float angle = (45f + (float)i * 90f) * (MathF.PI / 180f);
				Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
				ticks[i] = CreateLocalLine(root.transform, "LandingTick_" + i, bright, 0.026f, direction * 0.46f, direction * 0.72f);
			}
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float easeOut = 1f - Mathf.Pow(1f - t, 3f);
				float fade = Mathf.Clamp01((1f - t) / 0.28f);
				float flash = Mathf.Sin(Mathf.Clamp01(t * 2.2f) * MathF.PI);
				root.transform.localRotation = Quaternion.Euler(0f, t * (rareOrBetter ? 160f : 105f), 0f);
				outerRing.transform.localScale = Vector3.one * Mathf.Lerp(0.32f, rareOrBetter ? 1.34f : 1.18f, easeOut);
				innerRing.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, rareOrBetter ? 0.98f : 0.84f, easeOut);
				beam.transform.localScale = new Vector3(1f, Mathf.Lerp(0.72f, 0.2f, t), 1f);
				SetLineAlpha(outerRing, bright, fade * (0.42f + flash * 0.55f));
				SetLineAlpha(innerRing, color, fade * 0.82f);
				SetLineAlpha(beam, Color.Lerp(bright, Color.white, 0.22f), fade * 0.72f);
				for (int j = 0; j < ticks.Length; j++)
				{
					SetLineAlpha(ticks[j], bright, fade * Mathf.Lerp(0.95f, 0.25f, t));
				}
				yield return null;
			}
			UnityEngine.Object.Destroy(root);
		}

		private IEnumerator MergeResultVfxRoutine(Vector3 position, Color color, CharacterGrade grade, bool ultimate)
		{
			bool major = ultimate || grade >= CharacterGrade.Epic;
			float duration = (ultimate ? 1.22f : (major ? 0.96f : 0.78f));
			Color bright = Color.Lerp(color, Color.white, ultimate ? 0.34f : 0.42f);
			GameObject root = new GameObject(ultimate ? "UltimateMergeResultVfx" : "MergeResultVfx");
			root.transform.position = position + Vector3.up * 0.1f;
			LineRenderer outerRing = CreateWorldRing(root.transform, "MergeOuterRing", bright, ultimate ? 0.92f : 0.72f, ultimate ? 0.058f : 0.044f, 96, 0f);
			LineRenderer innerRing = CreateWorldRing(root.transform, "MergeInnerRing", color, ultimate ? 0.54f : 0.4f, ultimate ? 0.042f : 0.032f, 72, 0.035f);
			LineRenderer crossA = CreateLocalLine(root.transform, "MergeCrossA", bright, ultimate ? 0.046f : 0.034f, new Vector3(-0.72f, 0.06f, 0f), new Vector3(0.72f, 0.06f, 0f));
			LineRenderer crossB = CreateLocalLine(root.transform, "MergeCrossB", bright, ultimate ? 0.046f : 0.034f, new Vector3(0f, 0.06f, -0.72f), new Vector3(0f, 0.06f, 0.72f));
			LineRenderer[] beams = new LineRenderer[major ? 4 : 3];
			for (int i = 0; i < beams.Length; i++)
			{
				float angle = (float)i * MathF.PI * 2f / (float)beams.Length;
				Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ultimate ? 0.24f : 0.16f);
				beams[i] = CreateVerticalBeam(root.transform, "MergeBeam_" + i, Color.Lerp(bright, Color.white, 0.18f), offset, ultimate ? 2.55f : (major ? 2.05f : 1.55f), ultimate ? 0.035f : 0.026f);
			}
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float easeOut = 1f - Mathf.Pow(1f - t, 3f);
				float fade = Mathf.Clamp01((1f - t) / 0.3f);
				float flash = Mathf.Sin(Mathf.Clamp01(t * 2.7f) * MathF.PI);
				root.transform.localRotation = Quaternion.Euler(0f, t * (ultimate ? 360f : 240f), 0f);
				outerRing.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, ultimate ? 1.72f : (major ? 1.38f : 1.12f), easeOut);
				innerRing.transform.localScale = Vector3.one * Mathf.Lerp(0.26f, ultimate ? 1.16f : (major ? 0.98f : 0.82f), easeOut);
				SetLineAlpha(outerRing, bright, fade * (0.45f + flash * 0.55f));
				SetLineAlpha(innerRing, color, fade * 0.92f);
				SetLineAlpha(crossA, bright, fade * Mathf.Lerp(0.95f, 0.2f, t));
				SetLineAlpha(crossB, bright, fade * Mathf.Lerp(0.95f, 0.2f, t));
				for (int j = 0; j < beams.Length; j++)
				{
					beams[j].transform.localScale = new Vector3(1f, Mathf.Lerp(0.95f, 0.3f, t), 1f);
					SetLineAlpha(beams[j], Color.Lerp(bright, Color.white, 0.18f), fade * Mathf.Lerp(0.9f, 0.16f, t));
				}
				yield return null;
			}
			UnityEngine.Object.Destroy(root);
		}

		private IEnumerator HighGradeSummonVfxRoutine(Vector3 position, Color color, CharacterGrade grade)
		{
			bool transcendent = grade == CharacterGrade.Transcendent;
			float duration = (transcendent ? 1.65f : 1.25f);
			Color bright = Color.Lerp(color, Color.white, 0.32f);
			GameObject root = new GameObject(transcendent ? "TranscendentSummonVfx" : "MythicSummonVfx");
			root.transform.position = position + Vector3.up * 0.12f;
			LineRenderer outerRing = CreateWorldRing(root.transform, "OuterRing", bright, transcendent ? 1.35f : 1.05f, 0.06f, 96, 0f);
			LineRenderer innerRing = CreateWorldRing(root.transform, "InnerRing", color, transcendent ? 0.82f : 0.64f, 0.045f, 80, 0.04f);
			LineRenderer[] beams = new LineRenderer[transcendent ? 5 : 3];
			for (int i = 0; i < beams.Length; i++)
			{
				float angle = (float)i * MathF.PI * 2f / (float)beams.Length;
				Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (transcendent ? 0.28f : 0.2f);
				beams[i] = CreateVerticalBeam(root.transform, "Beam_" + i, Color.Lerp(bright, Color.white, 0.25f), offset, transcendent ? 3.4f : 2.6f, transcendent ? 0.035f : 0.026f);
			}
			int shardCount = (transcendent ? 12 : 8);
			Transform[] shards = new Transform[shardCount];
			Vector3[] directions = new Vector3[shardCount];
			for (int j = 0; j < shardCount; j++)
			{
				float angle2 = (float)j * MathF.PI * 2f / (float)shardCount;
				directions[j] = new Vector3(Mathf.Cos(angle2), 0f, Mathf.Sin(angle2));
				shards[j] = CreateSummonShard(root.transform, "Shard_" + j, Color.Lerp(color, Color.white, (j % 2 == 0) ? 0.12f : 0.42f), directions[j], transcendent);
			}
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float easeOut = 1f - Mathf.Pow(1f - t, 3f);
				float fade = Mathf.Clamp01((1f - t) / 0.32f);
				float flash = Mathf.Sin(Mathf.Clamp01(t * 2.2f) * MathF.PI);
				outerRing.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, transcendent ? 1.45f : 1.25f, easeOut);
				outerRing.transform.localRotation = Quaternion.Euler(0f, t * 420f, 0f);
				innerRing.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, transcendent ? 1.08f : 0.92f, easeOut);
				innerRing.transform.localRotation = Quaternion.Euler(0f, (0f - t) * 520f, 0f);
				SetLineAlpha(outerRing, bright, fade * (0.35f + flash * 0.65f));
				SetLineAlpha(innerRing, color, fade * 0.92f);
				for (int k = 0; k < beams.Length; k++)
				{
					SetLineAlpha(alpha: fade * Mathf.Lerp(0.95f, 0.2f, t), line: beams[k], color: Color.Lerp(bright, Color.white, 0.25f));
				}
				for (int l = 0; l < shards.Length; l++)
				{
					Transform shard = shards[l];
					if (!(shard == null))
					{
						Vector3 direction = directions[l];
						float distance = Mathf.Lerp(0.18f, transcendent ? 1.55f : 1.18f, easeOut);
						shard.localPosition = direction * distance + Vector3.up * Mathf.Lerp(0.18f, transcendent ? 1.3f : 0.88f, flash);
						shard.localRotation = Quaternion.Euler(70f + t * 410f, (float)l * 31f + t * 540f, 18f + t * 270f);
						shard.localScale = new Vector3(0.08f, Mathf.Lerp(0.3f, 0.1f, t), 0.08f) * (transcendent ? 1.18f : 1f);
						Renderer shardRenderer = shard.GetComponent<Renderer>();
						SetRendererColor(shardRenderer, ColorWithAlpha(Color.Lerp(color, Color.white, (l % 2 == 0) ? 0.16f : 0.42f), fade));
					}
				}
				yield return null;
			}
			UnityEngine.Object.Destroy(root);
		}

		private IEnumerator JackpotRevealRoutine(string title, string gradeLabel, string unitName, Color color, string detail, float duration)
		{
			GameObject root = CreateJackpotReveal(title, gradeLabel, unitName, color, detail);
			if (root == null)
			{
				yield break;
			}
			CanvasGroup group = root.GetComponent<CanvasGroup>();
			RectTransform rect = root.transform.Find("Card") as RectTransform;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float fadeIn = Mathf.Clamp01(t / 0.12f);
				float fadeOut = Mathf.Clamp01((1f - t) / 0.22f);
				float alpha = Mathf.Min(fadeIn, fadeOut);
				if (group != null)
				{
					group.alpha = alpha;
				}
				if (rect != null)
				{
					float pop = Mathf.Sin(Mathf.Clamp01(t * 3.7f) * MathF.PI);
					float settle = Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(t / 0.2f));
					rect.localScale = Vector3.one * (settle + pop * 0.075f);
				}
				yield return null;
			}
			UnityEngine.Object.Destroy(root);
		}

		private GameObject CreateJackpotReveal(string title, string gradeLabel, string unitName, Color color, string detail)
		{
			Font font = ResolveRuntimeFont();
			GameObject root = new GameObject("JackpotReveal", typeof(RectTransform));
			UnityEngine.Object.DontDestroyOnLoad(root);
			Canvas canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 32000;
			CanvasScaler scaler = root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = (ScaleMode)1;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			scaler.matchWidthOrHeight = 1f;
			CanvasGroup group = root.AddComponent<CanvasGroup>();
			group.alpha = 0f;
			group.interactable = false;
			group.blocksRaycasts = false;
			RectTransform rootRect = root.GetComponent<RectTransform>();
			rootRect.anchorMin = Vector2.zero;
			rootRect.anchorMax = Vector2.one;
			rootRect.offsetMin = Vector2.zero;
			rootRect.offsetMax = Vector2.zero;
			CreatePanel(root.transform, "Dim", Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.08f, 0.36f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
			CreatePanel(root.transform, "RevealFlash", Vector2.zero, Vector2.zero, new Color(color.r, color.g, color.b, 0.14f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
			Image card = CreatePanel(root.transform, "Card", new Vector2(0f, 150f), new Vector2(720f, 370f), new Color(0.05f, 0.08f, 0.22f, 0.97f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
			Outline cardOutline = ((Component)(object)card).gameObject.AddComponent<Outline>();
			((Shadow)cardOutline).effectColor = Color.Lerp(color, Color.white, 0.18f);
			((Shadow)cardOutline).effectDistance = new Vector2(7f, -7f);
			CreatePanel(((Component)(object)card).transform, "TopGlow", new Vector2(0f, -18f), new Vector2(650f, 52f), new Color(color.r, color.g, color.b, 0.7f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
			CreatePanel(((Component)(object)card).transform, "Badge", new Vector2(0f, -74f), new Vector2(260f, 58f), Color.Lerp(color, Color.white, 0.08f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
			CreateText(((Component)(object)card).transform, font, Color.white, "Grade", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(248f, 54f), SafeText(gradeLabel, "RARE"), 29, TextAnchor.MiddleCenter);
			CreateText(((Component)(object)card).transform, font, Color.Lerp(color, Color.white, 0.45f), "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(580f, 56f), SafeText(title, "대박!"), 39, TextAnchor.MiddleCenter);
			Image portrait = CreatePanel(((Component)(object)card).transform, "Portrait", new Vector2(-232f, -46f), new Vector2(154f, 154f), Color.Lerp(color, new Color(0.03f, 0.05f, 0.18f, 1f), 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
			Outline portraitOutline = ((Component)(object)portrait).gameObject.AddComponent<Outline>();
			((Shadow)portraitOutline).effectColor = Color.white;
			((Shadow)portraitOutline).effectDistance = new Vector2(4f, -4f);
			CreateText(((Component)(object)portrait).transform, font, Color.white, "Initial", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, BuildInitials(unitName), 48, TextAnchor.MiddleCenter);
			CreateText(((Component)(object)card).transform, font, Color.white, "Name", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(92f, -14f), new Vector2(424f, 76f), SafeText(unitName, "Unit"), 41, TextAnchor.MiddleCenter);
			CreateText(((Component)(object)card).transform, font, new Color(1f, 0.94f, 0.74f), "Detail", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(64f, 68f), new Vector2(490f, 48f), SafeText(detail, "전력 상승"), 25, TextAnchor.MiddleCenter);
			return root;
		}

		private static LineRenderer CreateWorldRing(Transform parent, string name, Color color, float radius, float width, int segments, float yOffset)
		{
			GameObject ringObject = new GameObject(name);
			ringObject.transform.SetParent(parent, worldPositionStays: false);
			LineRenderer line = ringObject.AddComponent<LineRenderer>();
			line.sharedMaterial = ResolveHighGradeVfxMaterial();
			line.useWorldSpace = false;
			line.loop = true;
			line.positionCount = Mathf.Max(12, segments);
			line.startWidth = width;
			line.endWidth = width;
			line.numCapVertices = 4;
			line.numCornerVertices = 4;
			SetLineAlpha(line, color, color.a);
			for (int i = 0; i < line.positionCount; i++)
			{
				float angle = (float)i * MathF.PI * 2f / (float)line.positionCount;
				line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, yOffset, Mathf.Sin(angle) * radius));
			}
			return line;
		}

		private static LineRenderer CreateVerticalBeam(Transform parent, string name, Color color, Vector3 offset, float height, float width)
		{
			GameObject beamObject = new GameObject(name);
			beamObject.transform.SetParent(parent, worldPositionStays: false);
			beamObject.transform.localPosition = offset;
			LineRenderer line = beamObject.AddComponent<LineRenderer>();
			line.sharedMaterial = ResolveHighGradeVfxMaterial();
			line.useWorldSpace = false;
			line.positionCount = 2;
			line.startWidth = width;
			line.endWidth = width * 0.55f;
			line.numCapVertices = 3;
			line.SetPosition(0, Vector3.up * 0.05f);
			line.SetPosition(1, Vector3.up * Mathf.Max(0.35f, height));
			SetLineAlpha(line, color, color.a);
			return line;
		}

		private static LineRenderer CreateLocalLine(Transform parent, string name, Color color, float width, params Vector3[] points)
		{
			GameObject lineObject = new GameObject(name);
			lineObject.transform.SetParent(parent, worldPositionStays: false);
			LineRenderer line = lineObject.AddComponent<LineRenderer>();
			line.sharedMaterial = ResolveHighGradeVfxMaterial();
			line.useWorldSpace = false;
			line.positionCount = Mathf.Max(2, (points != null) ? points.Length : 0);
			line.startWidth = width;
			line.endWidth = width * 0.72f;
			line.numCapVertices = 4;
			line.numCornerVertices = 4;
			for (int i = 0; i < line.positionCount; i++)
			{
				Vector3 point = ((points != null && i < points.Length) ? points[i] : Vector3.zero);
				line.SetPosition(i, point);
			}
			SetLineAlpha(line, color, color.a);
			return line;
		}

		private static Transform CreateSummonShard(Transform parent, string name, Color color, Vector3 direction, bool transcendent)
		{
			GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
			shard.name = name;
			shard.transform.SetParent(parent, worldPositionStays: false);
			shard.transform.localPosition = direction * 0.12f + Vector3.up * 0.16f;
			shard.transform.localScale = new Vector3(0.08f, transcendent ? 0.36f : 0.28f, 0.08f);
			Collider collider = shard.GetComponent<Collider>();
			if (collider != null)
			{
				UnityEngine.Object.Destroy(collider);
			}
			Renderer renderer = shard.GetComponent<Renderer>();
			if (renderer != null)
			{
				renderer.sharedMaterial = ResolveHighGradeVfxMaterial();
				SetRendererColor(renderer, color);
			}
			return shard.transform;
		}

		private static Material ResolveHighGradeVfxMaterial()
		{
			if (highGradeVfxMaterial != null)
			{
				return highGradeVfxMaterial;
			}
			Shader shader = Shader.Find("Sprites/Default");
			if (shader == null)
			{
				shader = Shader.Find("Unlit/Color");
			}
			highGradeVfxMaterial = new Material(shader);
			highGradeVfxMaterial.name = "RuntimeHighGradeSummonVfx";
			return highGradeVfxMaterial;
		}

		private static void SetLineAlpha(LineRenderer line, Color color, float alpha)
		{
			if (!(line == null))
			{
				Color visible = (line.startColor = ColorWithAlpha(color, alpha));
				line.endColor = visible;
			}
		}

		private static void SetRendererColor(Renderer renderer, Color color)
		{
			if (!(renderer == null))
			{
				MaterialPropertyBlock block = new MaterialPropertyBlock();
				renderer.GetPropertyBlock(block);
				block.SetColor(ColorPropertyId, color);
				renderer.SetPropertyBlock(block);
			}
		}

		private static Color ColorWithAlpha(Color color, float alpha)
		{
			color.a = Mathf.Clamp01(alpha);
			return color;
		}

		private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
		{
			GameObject panelObject = new GameObject(name, typeof(RectTransform));
			panelObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rect = panelObject.GetComponent<RectTransform>();
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			Image image = panelObject.AddComponent<Image>();
			((Graphic)image).color = color;
			((Graphic)image).raycastTarget = false;
			return image;
		}

		private static Text CreateText(Transform parent, Font font, Color color, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string value, int fontSize, TextAnchor alignment)
		{
			GameObject textObject = new GameObject(name, typeof(RectTransform));
			textObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rect = textObject.GetComponent<RectTransform>();
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			Text text = textObject.AddComponent<Text>();
			text.font = font;
			text.text = value;
			((Graphic)text).color = color;
			text.fontSize = fontSize;
			text.fontStyle = FontStyle.Bold;
			text.alignment = alignment;
			text.resizeTextForBestFit = true;
			text.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt((float)fontSize * 0.52f));
			text.resizeTextMaxSize = fontSize;
			((Graphic)text).raycastTarget = false;
			Outline outline = textObject.AddComponent<Outline>();
			((Shadow)outline).effectColor = new Color(0f, 0f, 0f, 0.76f);
			((Shadow)outline).effectDistance = new Vector2(2f, -2f);
			return text;
		}

		private static Font ResolveRuntimeFont()
		{
			Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			if (font == null)
			{
				font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
			return font;
		}

		private static string SafeText(string value, string fallback)
		{
			return string.IsNullOrWhiteSpace(value) ? fallback : value;
		}

		private static string BuildInitials(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "?";
			}
			string trimmed = value.Trim();
			return (trimmed.Length <= 2) ? trimmed.ToUpperInvariant() : trimmed.Substring(0, 1).ToUpperInvariant();
		}
	}
}
