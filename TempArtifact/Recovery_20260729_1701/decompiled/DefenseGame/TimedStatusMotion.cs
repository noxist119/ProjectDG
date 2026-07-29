using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

public class TimedStatusMotion : MonoBehaviour
{
	private Text label;

	private Graphic background;

	private string message;

	private Color labelColor;

	private Color backgroundColor;

	private float remaining;

	private float duration;

	public void Initialize(Text targetLabel, Graphic targetBackground, string statusMessage, Color color, float statusDuration)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		label = targetLabel;
		background = targetBackground;
		message = statusMessage;
		labelColor = color;
		backgroundColor = (((Object)(object)targetBackground != (Object)null) ? targetBackground.color : Color.clear);
		duration = Mathf.Max(0.1f, statusDuration);
		remaining = duration;
		UpdateLabel();
	}

	private void Update()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		remaining -= Time.deltaTime;
		UpdateLabel();
		float num = Mathf.Clamp01(remaining / Mathf.Min(0.35f, duration));
		if ((Object)(object)label != (Object)null)
		{
			Color color = labelColor;
			color.a *= num;
			((Graphic)label).color = color;
		}
		if ((Object)(object)background != (Object)null)
		{
			Color color2 = backgroundColor;
			color2.a *= num;
			background.color = color2;
		}
		((Component)this).transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 8f) * 0.025f);
		if (remaining <= 0f)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void UpdateLabel()
	{
		if (!((Object)(object)label == (Object)null))
		{
			int num = Mathf.Max(0, Mathf.CeilToInt(remaining));
			label.text = message + " " + num + "초";
		}
	}
}
