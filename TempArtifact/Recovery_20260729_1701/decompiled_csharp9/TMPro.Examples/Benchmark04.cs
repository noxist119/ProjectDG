using UnityEngine;
using UnityEngine.UI;

namespace TMPro.Examples
{
	public class Benchmark04 : MonoBehaviour
	{
		public int SpawnType = 0;

		public int MinPointSize = 12;

		public int MaxPointSize = 64;

		public int Steps = 4;

		private Transform m_Transform;

		private void Start()
		{
			m_Transform = base.transform;
			float lineHeight = 0f;
			float num = (Camera.main.orthographicSize = Screen.height / 2);
			float orthoSize = num;
			float ratio = (float)Screen.width / (float)Screen.height;
			for (int i = MinPointSize; i <= MaxPointSize; i += Steps)
			{
				if (SpawnType == 0)
				{
					GameObject go = new GameObject("Text - " + i + " Pts");
					if (lineHeight > orthoSize * 2f)
					{
						break;
					}
					go.transform.position = m_Transform.position + new Vector3(ratio * (0f - orthoSize) * 0.975f, orthoSize * 0.975f - lineHeight, 0f);
					TextMeshPro textMeshPro = go.AddComponent<TextMeshPro>();
					((TMP_Text)textMeshPro).rectTransform.pivot = new Vector2(0f, 0.5f);
					((TMP_Text)textMeshPro).enableWordWrapping = false;
					((TMP_Text)textMeshPro).extraPadding = true;
					((TMP_Text)textMeshPro).isOrthographic = true;
					((TMP_Text)textMeshPro).fontSize = i;
					((TMP_Text)textMeshPro).text = i + " pts - Lorem ipsum dolor sit...";
					((Graphic)textMeshPro).color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
					lineHeight += (float)i;
				}
			}
		}
	}
}
