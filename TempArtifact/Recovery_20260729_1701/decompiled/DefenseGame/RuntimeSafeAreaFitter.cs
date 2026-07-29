using UnityEngine;

namespace DefenseGame;

[RequireComponent(typeof(RectTransform))]
public class RuntimeSafeAreaFitter : MonoBehaviour
{
	private RectTransform rectTransform;

	private Rect lastSafeArea;

	private Vector2Int lastScreenSize;

	private void Awake()
	{
		rectTransform = ((Component)this).GetComponent<RectTransform>();
	}

	private void OnEnable()
	{
		ApplySafeArea(force: true);
	}

	private void Update()
	{
		ApplySafeArea(force: false);
	}

	private void ApplySafeArea(bool force)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)rectTransform == (Object)null)
		{
			rectTransform = ((Component)this).GetComponent<RectTransform>();
		}
		Rect safeArea = Screen.safeArea;
		Vector2Int val = default(Vector2Int);
		((Vector2Int)(ref val))._002Ector(Screen.width, Screen.height);
		if (force || !(safeArea == lastSafeArea) || !(val == lastScreenSize))
		{
			lastSafeArea = safeArea;
			lastScreenSize = val;
			if (((Vector2Int)(ref val)).x <= 0 || ((Vector2Int)(ref val)).y <= 0)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
			}
			else
			{
				CalculateSafeAreaAnchors(safeArea, val, out var anchorMin, out var anchorMax);
				rectTransform.anchorMin = anchorMin;
				rectTransform.anchorMax = anchorMax;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
			}
		}
	}

	public static void CalculateSafeAreaAnchors(Rect safeArea, Vector2Int screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (((Vector2Int)(ref screenSize)).x <= 0 || ((Vector2Int)(ref screenSize)).y <= 0)
		{
			anchorMin = Vector2.zero;
			anchorMax = Vector2.one;
			return;
		}
		float num = Mathf.Clamp(((Rect)(ref safeArea)).xMin, 0f, (float)((Vector2Int)(ref screenSize)).x);
		float num2 = Mathf.Clamp(((Rect)(ref safeArea)).yMin, 0f, (float)((Vector2Int)(ref screenSize)).y);
		float num3 = Mathf.Clamp(((Rect)(ref safeArea)).xMax, num, (float)((Vector2Int)(ref screenSize)).x);
		float num4 = Mathf.Clamp(((Rect)(ref safeArea)).yMax, num2, (float)((Vector2Int)(ref screenSize)).y);
		anchorMin = new Vector2(num / (float)((Vector2Int)(ref screenSize)).x, num2 / (float)((Vector2Int)(ref screenSize)).y);
		anchorMax = new Vector2(num3 / (float)((Vector2Int)(ref screenSize)).x, num4 / (float)((Vector2Int)(ref screenSize)).y);
	}
}
