using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
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
			label = targetLabel;
			background = targetBackground;
			message = statusMessage;
			labelColor = color;
			backgroundColor = (((Object)(object)targetBackground != null) ? targetBackground.color : Color.clear);
			duration = Mathf.Max(0.1f, statusDuration);
			remaining = duration;
			UpdateLabel();
		}

		private void Update()
		{
			remaining -= Time.deltaTime;
			UpdateLabel();
			float fade = Mathf.Clamp01(remaining / Mathf.Min(0.35f, duration));
			if ((Object)(object)label != null)
			{
				Color color = labelColor;
				color.a *= fade;
				((Graphic)label).color = color;
			}
			if ((Object)(object)background != null)
			{
				Color color2 = backgroundColor;
				color2.a *= fade;
				background.color = color2;
			}
			base.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 8f) * 0.025f);
			if (remaining <= 0f)
			{
				Object.Destroy(base.gameObject);
			}
		}

		private void UpdateLabel()
		{
			if (!((Object)(object)label == null))
			{
				int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
				label.text = message + " " + seconds + "초";
			}
		}
	}
}
