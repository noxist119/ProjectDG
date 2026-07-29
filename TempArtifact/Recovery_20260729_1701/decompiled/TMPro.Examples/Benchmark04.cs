using UnityEngine;
using UnityEngine.UI;

namespace TMPro.Examples;

public class Benchmark04 : MonoBehaviour
{
	public int SpawnType = 0;

	public int MinPointSize = 12;

	public int MaxPointSize = 64;

	public int Steps = 4;

	private Transform m_Transform;

	private void Start()
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		m_Transform = ((Component)this).transform;
		float num = 0f;
		float num2 = (Camera.main.orthographicSize = Screen.height / 2);
		float num4 = num2;
		float num5 = (float)Screen.width / (float)Screen.height;
		for (int i = MinPointSize; i <= MaxPointSize; i += Steps)
		{
			if (SpawnType == 0)
			{
				GameObject val = new GameObject("Text - " + i + " Pts");
				if (num > num4 * 2f)
				{
					break;
				}
				val.transform.position = m_Transform.position + new Vector3(num5 * (0f - num4) * 0.975f, num4 * 0.975f - num, 0f);
				TextMeshPro val2 = val.AddComponent<TextMeshPro>();
				((TMP_Text)val2).rectTransform.pivot = new Vector2(0f, 0.5f);
				((TMP_Text)val2).enableWordWrapping = false;
				((TMP_Text)val2).extraPadding = true;
				((TMP_Text)val2).isOrthographic = true;
				((TMP_Text)val2).fontSize = i;
				((TMP_Text)val2).text = i + " pts - Lorem ipsum dolor sit...";
				((Graphic)val2).color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
				num += (float)i;
			}
		}
	}
}
