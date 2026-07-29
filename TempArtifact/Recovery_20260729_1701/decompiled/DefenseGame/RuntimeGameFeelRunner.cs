using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

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
			((MonoBehaviour)this).StopCoroutine(hitStopRoutine);
			if (!DefenseGameController.IsDefeatSlowMotionActive)
			{
				Time.timeScale = 1f;
				Time.fixedDeltaTime = baseFixedDeltaTime;
			}
		}
		hitStopRoutine = ((MonoBehaviour)this).StartCoroutine(HitStopRoutine(Mathf.Clamp(targetScale, 0.08f, 1f), Mathf.Max(0.02f, duration)));
	}

	public void DelayedPulse(Vector3 position, Color color, float radius, float delay)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(DelayedPulseRoutine(position, color, radius, delay));
	}

	public void ShowJackpotReveal(string title, string gradeLabel, string unitName, Color color, string detail, float duration)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(JackpotRevealRoutine(title, gradeLabel, unitName, color, detail, Mathf.Max(1.2f, duration)));
	}

	public void PlayHighGradeSummonVfx(Vector3 position, Color color, CharacterGrade grade)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(HighGradeSummonVfxRoutine(position, color, grade));
	}

	public void PlaySummonArrivalVfx(Vector3 position, Color color, CharacterGrade grade)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(SummonArrivalVfxRoutine(position, color, grade));
	}

	public void PlayMergeResultVfx(Vector3 position, Color color, CharacterGrade grade, bool ultimate)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(MergeResultVfxRoutine(position, color, grade, ultimate));
	}

	private IEnumerator HitStopRoutine(float targetScale, float duration)
	{
		float previousScale = Time.timeScale;
		Time.timeScale = targetScale;
		Time.fixedDeltaTime = baseFixedDeltaTime * targetScale;
		yield return (object)new WaitForSecondsRealtime(duration);
		if (!DefenseGameController.IsDefeatSlowMotionActive)
		{
			Time.timeScale = (Mathf.Approximately(previousScale, 0f) ? 1f : previousScale);
			Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
		}
		hitStopRoutine = null;
	}

	private IEnumerator DelayedPulseRoutine(Vector3 position, Color color, float radius, float delay)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		yield return (object)new WaitForSecondsRealtime(Mathf.Max(0f, delay));
		RuntimeCombatFeedback.ShowGroundPulse(position, color, radius, 0.34f, 0.13f);
	}

	private IEnumerator SummonArrivalVfxRoutine(Vector3 position, Color color, CharacterGrade grade)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		bool rareOrBetter = grade >= CharacterGrade.Rare;
		float duration = (rareOrBetter ? 0.82f : 0.58f);
		Color bright = Color.Lerp(color, Color.white, rareOrBetter ? 0.46f : 0.34f);
		GameObject root = new GameObject("SummonArrivalVfx");
		root.transform.position = position + Vector3.up * 0.08f;
		LineRenderer outerRing = CreateWorldRing(root.transform, "LandingRing", bright, rareOrBetter ? 0.62f : 0.48f, rareOrBetter ? 0.045f : 0.034f, 72, 0f);
		LineRenderer innerRing = CreateWorldRing(root.transform, "FocusRing", color, rareOrBetter ? 0.36f : 0.28f, rareOrBetter ? 0.034f : 0.026f, 56, 0.025f);
		LineRenderer beam = CreateVerticalBeam(root.transform, "ArrivalBeam", Color.Lerp(bright, Color.white, 0.22f), Vector3.zero, rareOrBetter ? 1.75f : 1.2f, rareOrBetter ? 0.034f : 0.024f);
		LineRenderer[] ticks = (LineRenderer[])(object)new LineRenderer[4];
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
			((Component)outerRing).transform.localScale = Vector3.one * Mathf.Lerp(0.32f, rareOrBetter ? 1.34f : 1.18f, easeOut);
			((Component)innerRing).transform.localScale = Vector3.one * Mathf.Lerp(0.22f, rareOrBetter ? 0.98f : 0.84f, easeOut);
			((Component)beam).transform.localScale = new Vector3(1f, Mathf.Lerp(0.72f, 0.2f, t), 1f);
			SetLineAlpha(outerRing, bright, fade * (0.42f + flash * 0.55f));
			SetLineAlpha(innerRing, color, fade * 0.82f);
			SetLineAlpha(beam, Color.Lerp(bright, Color.white, 0.22f), fade * 0.72f);
			for (int j = 0; j < ticks.Length; j++)
			{
				SetLineAlpha(ticks[j], bright, fade * Mathf.Lerp(0.95f, 0.25f, t));
			}
			yield return null;
		}
		Object.Destroy((Object)(object)root);
	}

	private IEnumerator MergeResultVfxRoutine(Vector3 position, Color color, CharacterGrade grade, bool ultimate)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		bool major = ultimate || grade >= CharacterGrade.Epic;
		float duration = (ultimate ? 1.22f : (major ? 0.96f : 0.78f));
		Color bright = Color.Lerp(color, Color.white, ultimate ? 0.34f : 0.42f);
		GameObject root = new GameObject(ultimate ? "UltimateMergeResultVfx" : "MergeResultVfx");
		root.transform.position = position + Vector3.up * 0.1f;
		LineRenderer outerRing = CreateWorldRing(root.transform, "MergeOuterRing", bright, ultimate ? 0.92f : 0.72f, ultimate ? 0.058f : 0.044f, 96, 0f);
		LineRenderer innerRing = CreateWorldRing(root.transform, "MergeInnerRing", color, ultimate ? 0.54f : 0.4f, ultimate ? 0.042f : 0.032f, 72, 0.035f);
		LineRenderer crossA = CreateLocalLine(root.transform, "MergeCrossA", bright, ultimate ? 0.046f : 0.034f, new Vector3(-0.72f, 0.06f, 0f), new Vector3(0.72f, 0.06f, 0f));
		LineRenderer crossB = CreateLocalLine(root.transform, "MergeCrossB", bright, ultimate ? 0.046f : 0.034f, new Vector3(0f, 0.06f, -0.72f), new Vector3(0f, 0.06f, 0.72f));
		LineRenderer[] beams = (LineRenderer[])(object)new LineRenderer[major ? 4 : 3];
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
			((Component)outerRing).transform.localScale = Vector3.one * Mathf.Lerp(0.42f, ultimate ? 1.72f : (major ? 1.38f : 1.12f), easeOut);
			((Component)innerRing).transform.localScale = Vector3.one * Mathf.Lerp(0.26f, ultimate ? 1.16f : (major ? 0.98f : 0.82f), easeOut);
			SetLineAlpha(outerRing, bright, fade * (0.45f + flash * 0.55f));
			SetLineAlpha(innerRing, color, fade * 0.92f);
			SetLineAlpha(crossA, bright, fade * Mathf.Lerp(0.95f, 0.2f, t));
			SetLineAlpha(crossB, bright, fade * Mathf.Lerp(0.95f, 0.2f, t));
			for (int j = 0; j < beams.Length; j++)
			{
				((Component)beams[j]).transform.localScale = new Vector3(1f, Mathf.Lerp(0.95f, 0.3f, t), 1f);
				SetLineAlpha(beams[j], Color.Lerp(bright, Color.white, 0.18f), fade * Mathf.Lerp(0.9f, 0.16f, t));
			}
			yield return null;
		}
		Object.Destroy((Object)(object)root);
	}

	private IEnumerator HighGradeSummonVfxRoutine(Vector3 position, Color color, CharacterGrade grade)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		bool transcendent = grade == CharacterGrade.Transcendent;
		float duration = (transcendent ? 1.65f : 1.25f);
		Color bright = Color.Lerp(color, Color.white, 0.32f);
		GameObject root = new GameObject(transcendent ? "TranscendentSummonVfx" : "MythicSummonVfx");
		root.transform.position = position + Vector3.up * 0.12f;
		LineRenderer outerRing = CreateWorldRing(root.transform, "OuterRing", bright, transcendent ? 1.35f : 1.05f, 0.06f, 96, 0f);
		LineRenderer innerRing = CreateWorldRing(root.transform, "InnerRing", color, transcendent ? 0.82f : 0.64f, 0.045f, 80, 0.04f);
		LineRenderer[] beams = (LineRenderer[])(object)new LineRenderer[transcendent ? 5 : 3];
		for (int i = 0; i < beams.Length; i++)
		{
			float angle = (float)i * MathF.PI * 2f / (float)beams.Length;
			Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (transcendent ? 0.28f : 0.2f);
			beams[i] = CreateVerticalBeam(root.transform, "Beam_" + i, Color.Lerp(bright, Color.white, 0.25f), offset, transcendent ? 3.4f : 2.6f, transcendent ? 0.035f : 0.026f);
		}
		int shardCount = (transcendent ? 12 : 8);
		Transform[] shards = (Transform[])(object)new Transform[shardCount];
		Vector3[] directions = (Vector3[])(object)new Vector3[shardCount];
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
			((Component)outerRing).transform.localScale = Vector3.one * Mathf.Lerp(0.35f, transcendent ? 1.45f : 1.25f, easeOut);
			((Component)outerRing).transform.localRotation = Quaternion.Euler(0f, t * 420f, 0f);
			((Component)innerRing).transform.localScale = Vector3.one * Mathf.Lerp(0.2f, transcendent ? 1.08f : 0.92f, easeOut);
			((Component)innerRing).transform.localRotation = Quaternion.Euler(0f, (0f - t) * 520f, 0f);
			SetLineAlpha(outerRing, bright, fade * (0.35f + flash * 0.65f));
			SetLineAlpha(innerRing, color, fade * 0.92f);
			for (int k = 0; k < beams.Length; k++)
			{
				SetLineAlpha(alpha: fade * Mathf.Lerp(0.95f, 0.2f, t), line: beams[k], color: Color.Lerp(bright, Color.white, 0.25f));
			}
			for (int l = 0; l < shards.Length; l++)
			{
				Transform shard = shards[l];
				if (!((Object)(object)shard == (Object)null))
				{
					Vector3 direction = directions[l];
					float distance = Mathf.Lerp(0.18f, transcendent ? 1.55f : 1.18f, easeOut);
					shard.localPosition = direction * distance + Vector3.up * Mathf.Lerp(0.18f, transcendent ? 1.3f : 0.88f, flash);
					shard.localRotation = Quaternion.Euler(70f + t * 410f, (float)l * 31f + t * 540f, 18f + t * 270f);
					shard.localScale = new Vector3(0.08f, Mathf.Lerp(0.3f, 0.1f, t), 0.08f) * (transcendent ? 1.18f : 1f);
					Renderer shardRenderer = ((Component)shard).GetComponent<Renderer>();
					SetRendererColor(shardRenderer, ColorWithAlpha(Color.Lerp(color, Color.white, (l % 2 == 0) ? 0.16f : 0.42f), fade));
				}
			}
			yield return null;
		}
		Object.Destroy((Object)(object)root);
	}

	private IEnumerator JackpotRevealRoutine(string title, string gradeLabel, string unitName, Color color, string detail, float duration)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		GameObject root = CreateJackpotReveal(title, gradeLabel, unitName, color, detail);
		if ((Object)(object)root == (Object)null)
		{
			yield break;
		}
		CanvasGroup group = root.GetComponent<CanvasGroup>();
		Transform obj = root.transform.Find("Card");
		RectTransform rect = (RectTransform)(object)((obj is RectTransform) ? obj : null);
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float fadeIn = Mathf.Clamp01(t / 0.12f);
			float fadeOut = Mathf.Clamp01((1f - t) / 0.22f);
			float alpha = Mathf.Min(fadeIn, fadeOut);
			if ((Object)(object)group != (Object)null)
			{
				group.alpha = alpha;
			}
			if ((Object)(object)rect != (Object)null)
			{
				float pop = Mathf.Sin(Mathf.Clamp01(t * 3.7f) * MathF.PI);
				float settle = Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(t / 0.2f));
				((Transform)rect).localScale = Vector3.one * (settle + pop * 0.075f);
			}
			yield return null;
		}
		Object.Destroy((Object)(object)root);
	}

	private GameObject CreateJackpotReveal(string title, string gradeLabel, string unitName, Color color, string detail)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_046f: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0508: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0549: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_059c: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d8: Unknown result type (might be due to invalid IL or missing references)
		Font font = ResolveRuntimeFont();
		GameObject val = new GameObject("JackpotReveal", new Type[1] { typeof(RectTransform) });
		Object.DontDestroyOnLoad((Object)(object)val);
		Canvas val2 = val.AddComponent<Canvas>();
		val2.renderMode = (RenderMode)0;
		val2.sortingOrder = 32000;
		CanvasScaler val3 = val.AddComponent<CanvasScaler>();
		val3.uiScaleMode = (ScaleMode)1;
		val3.referenceResolution = new Vector2(1080f, 1920f);
		val3.matchWidthOrHeight = 1f;
		CanvasGroup val4 = val.AddComponent<CanvasGroup>();
		val4.alpha = 0f;
		val4.interactable = false;
		val4.blocksRaycasts = false;
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		CreatePanel(val.transform, "Dim", Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.08f, 0.36f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
		CreatePanel(val.transform, "RevealFlash", Vector2.zero, Vector2.zero, new Color(color.r, color.g, color.b, 0.14f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
		Image val5 = CreatePanel(val.transform, "Card", new Vector2(0f, 150f), new Vector2(720f, 370f), new Color(0.05f, 0.08f, 0.22f, 0.97f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
		Outline val6 = ((Component)val5).gameObject.AddComponent<Outline>();
		((Shadow)val6).effectColor = Color.Lerp(color, Color.white, 0.18f);
		((Shadow)val6).effectDistance = new Vector2(7f, -7f);
		CreatePanel(((Component)val5).transform, "TopGlow", new Vector2(0f, -18f), new Vector2(650f, 52f), new Color(color.r, color.g, color.b, 0.7f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
		CreatePanel(((Component)val5).transform, "Badge", new Vector2(0f, -74f), new Vector2(260f, 58f), Color.Lerp(color, Color.white, 0.08f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
		CreateText(((Component)val5).transform, font, Color.white, "Grade", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(248f, 54f), SafeText(gradeLabel, "RARE"), 29, (TextAnchor)4);
		CreateText(((Component)val5).transform, font, Color.Lerp(color, Color.white, 0.45f), "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(580f, 56f), SafeText(title, "대박!"), 39, (TextAnchor)4);
		Image val7 = CreatePanel(((Component)val5).transform, "Portrait", new Vector2(-232f, -46f), new Vector2(154f, 154f), Color.Lerp(color, new Color(0.03f, 0.05f, 0.18f, 1f), 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
		Outline val8 = ((Component)val7).gameObject.AddComponent<Outline>();
		((Shadow)val8).effectColor = Color.white;
		((Shadow)val8).effectDistance = new Vector2(4f, -4f);
		CreateText(((Component)val7).transform, font, Color.white, "Initial", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, BuildInitials(unitName), 48, (TextAnchor)4);
		CreateText(((Component)val5).transform, font, Color.white, "Name", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(92f, -14f), new Vector2(424f, 76f), SafeText(unitName, "Unit"), 41, (TextAnchor)4);
		CreateText(((Component)val5).transform, font, new Color(1f, 0.94f, 0.74f), "Detail", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(64f, 68f), new Vector2(490f, 48f), SafeText(detail, "전력 상승"), 25, (TextAnchor)4);
		return val;
	}

	private static LineRenderer CreateWorldRing(Transform parent, string name, Color color, float radius, float width, int segments, float yOffset)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		LineRenderer val2 = val.AddComponent<LineRenderer>();
		((Renderer)val2).sharedMaterial = ResolveHighGradeVfxMaterial();
		val2.useWorldSpace = false;
		val2.loop = true;
		val2.positionCount = Mathf.Max(12, segments);
		val2.startWidth = width;
		val2.endWidth = width;
		val2.numCapVertices = 4;
		val2.numCornerVertices = 4;
		SetLineAlpha(val2, color, color.a);
		for (int i = 0; i < val2.positionCount; i++)
		{
			float num = (float)i * MathF.PI * 2f / (float)val2.positionCount;
			val2.SetPosition(i, new Vector3(Mathf.Cos(num) * radius, yOffset, Mathf.Sin(num) * radius));
		}
		return val2;
	}

	private static LineRenderer CreateVerticalBeam(Transform parent, string name, Color color, Vector3 offset, float height, float width)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		val.transform.localPosition = offset;
		LineRenderer val2 = val.AddComponent<LineRenderer>();
		((Renderer)val2).sharedMaterial = ResolveHighGradeVfxMaterial();
		val2.useWorldSpace = false;
		val2.positionCount = 2;
		val2.startWidth = width;
		val2.endWidth = width * 0.55f;
		val2.numCapVertices = 3;
		val2.SetPosition(0, Vector3.up * 0.05f);
		val2.SetPosition(1, Vector3.up * Mathf.Max(0.35f, height));
		SetLineAlpha(val2, color, color.a);
		return val2;
	}

	private static LineRenderer CreateLocalLine(Transform parent, string name, Color color, float width, params Vector3[] points)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		LineRenderer val2 = val.AddComponent<LineRenderer>();
		((Renderer)val2).sharedMaterial = ResolveHighGradeVfxMaterial();
		val2.useWorldSpace = false;
		val2.positionCount = Mathf.Max(2, (points != null) ? points.Length : 0);
		val2.startWidth = width;
		val2.endWidth = width * 0.72f;
		val2.numCapVertices = 4;
		val2.numCornerVertices = 4;
		for (int i = 0; i < val2.positionCount; i++)
		{
			Vector3 val3 = ((points != null && i < points.Length) ? points[i] : Vector3.zero);
			val2.SetPosition(i, val3);
		}
		SetLineAlpha(val2, color, color.a);
		return val2;
	}

	private static Transform CreateSummonShard(Transform parent, string name, Color color, Vector3 direction, bool transcendent)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = GameObject.CreatePrimitive((PrimitiveType)3);
		((Object)val).name = name;
		val.transform.SetParent(parent, false);
		val.transform.localPosition = direction * 0.12f + Vector3.up * 0.16f;
		val.transform.localScale = new Vector3(0.08f, transcendent ? 0.36f : 0.28f, 0.08f);
		Collider component = val.GetComponent<Collider>();
		if ((Object)(object)component != (Object)null)
		{
			Object.Destroy((Object)(object)component);
		}
		Renderer component2 = val.GetComponent<Renderer>();
		if ((Object)(object)component2 != (Object)null)
		{
			component2.sharedMaterial = ResolveHighGradeVfxMaterial();
			SetRendererColor(component2, color);
		}
		return val.transform;
	}

	private static Material ResolveHighGradeVfxMaterial()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		if ((Object)(object)highGradeVfxMaterial != (Object)null)
		{
			return highGradeVfxMaterial;
		}
		Shader val = Shader.Find("Sprites/Default");
		if ((Object)(object)val == (Object)null)
		{
			val = Shader.Find("Unlit/Color");
		}
		highGradeVfxMaterial = new Material(val);
		((Object)highGradeVfxMaterial).name = "RuntimeHighGradeSummonVfx";
		return highGradeVfxMaterial;
	}

	private static void SetLineAlpha(LineRenderer line, Color color, float alpha)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)line == (Object)null))
		{
			Color endColor = (line.startColor = ColorWithAlpha(color, alpha));
			line.endColor = endColor;
		}
	}

	private static void SetRendererColor(Renderer renderer, Color color)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)renderer == (Object)null))
		{
			MaterialPropertyBlock val = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(val);
			val.SetColor(ColorPropertyId, color);
			renderer.SetPropertyBlock(val);
		}
	}

	private static Color ColorWithAlpha(Color color, float alpha)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		color.a = Mathf.Clamp01(alpha);
		return color;
	}

	private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = pivot;
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = size;
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = color;
		((Graphic)val2).raycastTarget = false;
		return val2;
	}

	private static Text CreateText(Transform parent, Font font, Color color, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string value, int fontSize, TextAnchor alignment)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = pivot;
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = size;
		Text val2 = val.AddComponent<Text>();
		val2.font = font;
		val2.text = value;
		((Graphic)val2).color = color;
		val2.fontSize = fontSize;
		val2.fontStyle = (FontStyle)1;
		val2.alignment = alignment;
		val2.resizeTextForBestFit = true;
		val2.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt((float)fontSize * 0.52f));
		val2.resizeTextMaxSize = fontSize;
		((Graphic)val2).raycastTarget = false;
		Outline val3 = val.AddComponent<Outline>();
		((Shadow)val3).effectColor = new Color(0f, 0f, 0f, 0.76f);
		((Shadow)val3).effectDistance = new Vector2(2f, -2f);
		return val2;
	}

	private static Font ResolveRuntimeFont()
	{
		Font builtinResource = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		if ((Object)(object)builtinResource == (Object)null)
		{
			builtinResource = Resources.GetBuiltinResource<Font>("Arial.ttf");
		}
		return builtinResource;
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
		string text = value.Trim();
		return (text.Length <= 2) ? text.ToUpperInvariant() : text.Substring(0, 1).ToUpperInvariant();
	}
}
