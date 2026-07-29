using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame;

public static class RuntimeCombatFeedback
{
	private static Material lineMaterial;

	public static void ShowGroundPulse(Vector3 position, Color color, float radius, float duration = 0.45f, float yOffset = 0.08f)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		CreateRing("GroundPulse", position, color, Mathf.Max(0.12f, radius), Mathf.Max(0.12f, duration), yOffset, expand: true);
	}

	public static void ShowGroundWarning(Vector3 position, Color color, float radius, float duration = 0.9f, float yOffset = 0.09f)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		CreateRing("GroundWarning", position, color, Mathf.Max(0.16f, radius), Mathf.Max(0.18f, duration), yOffset, expand: false);
	}

	public static void ShowBossDefeat(Vector3 position, Color color, float radius, float duration)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		ShowGroundPulse(position, color, radius, duration, 0.1f);
		ShowGroundWarning(position, Color.Lerp(color, Color.white, 0.28f), radius * 1.45f, duration * 0.85f, 0.12f);
	}

	public static void ShowHitRim(Transform target, Color color, bool critical)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying && !((Object)(object)target == (Object)null))
		{
			Bounds val = ResolveTargetBounds(target);
			float rimRadius = Mathf.Clamp(Mathf.Max(((Bounds)(ref val)).extents.x, ((Bounds)(ref val)).extents.z) * 1.18f, 0.38f, critical ? 1.65f : 1.35f);
			float rimHeight = Mathf.Clamp(((Bounds)(ref val)).size.y * 0.68f, 0.78f, critical ? 2.2f : 1.8f);
			float lifetime = (critical ? 0.28f : 0.2f);
			GameObject val2 = new GameObject(critical ? "CriticalHitRim" : "HitRim");
			val2.transform.position = ((Bounds)(ref val)).center;
			LineRenderer val3 = val2.AddComponent<LineRenderer>();
			val3.useWorldSpace = false;
			val3.loop = true;
			val3.positionCount = (critical ? 88 : 72);
			val3.widthMultiplier = (critical ? 0.115f : 0.082f);
			val3.numCornerVertices = 4;
			val3.numCapVertices = 4;
			((Renderer)val3).shadowCastingMode = (ShadowCastingMode)0;
			((Renderer)val3).receiveShadows = false;
			val3.alignment = (LineAlignment)0;
			((Renderer)val3).material = ResolveLineMaterial();
			RuntimeHitRim runtimeHitRim = val2.AddComponent<RuntimeHitRim>();
			runtimeHitRim.Initialize(val3, target, ((Bounds)(ref val)).center, color, rimRadius, rimHeight, lifetime, critical);
		}
	}

	private static void CreateRing(string name, Vector3 position, Color color, float radius, float duration, float yOffset, bool expand)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying)
		{
			GameObject val = new GameObject(name);
			val.transform.position = position + Vector3.up * yOffset;
			LineRenderer val2 = val.AddComponent<LineRenderer>();
			val2.useWorldSpace = false;
			val2.loop = true;
			val2.positionCount = 80;
			val2.widthMultiplier = (expand ? 0.075f : 0.06f);
			((Renderer)val2).shadowCastingMode = (ShadowCastingMode)0;
			((Renderer)val2).receiveShadows = false;
			((Renderer)val2).material = ResolveLineMaterial();
			RuntimeGroundRing runtimeGroundRing = val.AddComponent<RuntimeGroundRing>();
			runtimeGroundRing.Initialize(val2, color, radius, duration, expand);
		}
	}

	private static Bounds ResolveTargetBounds(Transform target)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		Renderer[] componentsInChildren = ((Component)target).GetComponentsInChildren<Renderer>(true);
		Bounds bounds = default(Bounds);
		((Bounds)(ref bounds))._002Ector(target.position + Vector3.up * 0.85f, new Vector3(0.8f, 1.45f, 0.8f));
		bool flag = false;
		foreach (Renderer val in componentsInChildren)
		{
			if (!((Object)(object)val == (Object)null) && !(val is LineRenderer) && val.enabled)
			{
				if (!flag)
				{
					bounds = val.bounds;
					flag = true;
				}
				else
				{
					((Bounds)(ref bounds)).Encapsulate(val.bounds);
				}
			}
		}
		return bounds;
	}

	private static Material ResolveLineMaterial()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		if ((Object)(object)lineMaterial != (Object)null)
		{
			return lineMaterial;
		}
		Shader val = Shader.Find("Sprites/Default");
		if ((Object)(object)val == (Object)null)
		{
			val = Shader.Find("Unlit/Color");
		}
		lineMaterial = new Material(val);
		((Object)lineMaterial).name = "RuntimeCombatFeedbackLine";
		return lineMaterial;
	}
}
