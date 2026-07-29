using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame
{
	public static class RuntimeCombatFeedback
	{
		private static Material lineMaterial;

		public static void ShowGroundPulse(Vector3 position, Color color, float radius, float duration = 0.45f, float yOffset = 0.08f)
		{
			CreateRing("GroundPulse", position, color, Mathf.Max(0.12f, radius), Mathf.Max(0.12f, duration), yOffset, expand: true);
		}

		public static void ShowGroundWarning(Vector3 position, Color color, float radius, float duration = 0.9f, float yOffset = 0.09f)
		{
			CreateRing("GroundWarning", position, color, Mathf.Max(0.16f, radius), Mathf.Max(0.18f, duration), yOffset, expand: false);
		}

		public static void ShowBossDefeat(Vector3 position, Color color, float radius, float duration)
		{
			ShowGroundPulse(position, color, radius, duration, 0.1f);
			ShowGroundWarning(position, Color.Lerp(color, Color.white, 0.28f), radius * 1.45f, duration * 0.85f, 0.12f);
		}

		public static void ShowHitRim(Transform target, Color color, bool critical)
		{
			if (Application.isPlaying && !(target == null))
			{
				Bounds bounds = ResolveTargetBounds(target);
				float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.18f, 0.38f, critical ? 1.65f : 1.35f);
				float height = Mathf.Clamp(bounds.size.y * 0.68f, 0.78f, critical ? 2.2f : 1.8f);
				float duration = (critical ? 0.28f : 0.2f);
				GameObject rimObject = new GameObject(critical ? "CriticalHitRim" : "HitRim");
				rimObject.transform.position = bounds.center;
				LineRenderer line = rimObject.AddComponent<LineRenderer>();
				line.useWorldSpace = false;
				line.loop = true;
				line.positionCount = (critical ? 88 : 72);
				line.widthMultiplier = (critical ? 0.115f : 0.082f);
				line.numCornerVertices = 4;
				line.numCapVertices = 4;
				line.shadowCastingMode = ShadowCastingMode.Off;
				line.receiveShadows = false;
				line.alignment = LineAlignment.View;
				line.material = ResolveLineMaterial();
				RuntimeHitRim rim = rimObject.AddComponent<RuntimeHitRim>();
				rim.Initialize(line, target, bounds.center, color, radius, height, duration, critical);
			}
		}

		private static void CreateRing(string name, Vector3 position, Color color, float radius, float duration, float yOffset, bool expand)
		{
			if (Application.isPlaying)
			{
				GameObject ringObject = new GameObject(name);
				ringObject.transform.position = position + Vector3.up * yOffset;
				LineRenderer line = ringObject.AddComponent<LineRenderer>();
				line.useWorldSpace = false;
				line.loop = true;
				line.positionCount = 80;
				line.widthMultiplier = (expand ? 0.075f : 0.06f);
				line.shadowCastingMode = ShadowCastingMode.Off;
				line.receiveShadows = false;
				line.material = ResolveLineMaterial();
				RuntimeGroundRing ring = ringObject.AddComponent<RuntimeGroundRing>();
				ring.Initialize(line, color, radius, duration, expand);
			}
		}

		private static Bounds ResolveTargetBounds(Transform target)
		{
			Renderer[] renderers = target.GetComponentsInChildren<Renderer>(includeInactive: true);
			Bounds bounds = new Bounds(target.position + Vector3.up * 0.85f, new Vector3(0.8f, 1.45f, 0.8f));
			bool hasBounds = false;
			foreach (Renderer renderer in renderers)
			{
				if (!(renderer == null) && !(renderer is LineRenderer) && renderer.enabled)
				{
					if (!hasBounds)
					{
						bounds = renderer.bounds;
						hasBounds = true;
					}
					else
					{
						bounds.Encapsulate(renderer.bounds);
					}
				}
			}
			return bounds;
		}

		private static Material ResolveLineMaterial()
		{
			if (lineMaterial != null)
			{
				return lineMaterial;
			}
			Shader shader = Shader.Find("Sprites/Default");
			if (shader == null)
			{
				shader = Shader.Find("Unlit/Color");
			}
			lineMaterial = new Material(shader);
			lineMaterial.name = "RuntimeCombatFeedbackLine";
			return lineMaterial;
		}
	}
}
