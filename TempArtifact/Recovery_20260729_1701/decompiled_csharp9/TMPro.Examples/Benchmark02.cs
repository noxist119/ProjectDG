using UnityEngine;
using UnityEngine.UI;

namespace TMPro.Examples
{
	public class Benchmark02 : MonoBehaviour
	{
		public int SpawnType = 0;

		public int NumberOfNPC = 12;

		public bool IsTextObjectScaleStatic;

		private TextMeshProFloatingText floatingText_Script;

		private void Start()
		{
			for (int i = 0; i < NumberOfNPC; i++)
			{
				if (SpawnType == 0)
				{
					GameObject go = new GameObject();
					go.transform.position = new Vector3(Random.Range(-95f, 95f), 0.25f, Random.Range(-95f, 95f));
					TextMeshPro textMeshPro = go.AddComponent<TextMeshPro>();
					((TMP_Text)textMeshPro).autoSizeTextContainer = true;
					((TMP_Text)textMeshPro).rectTransform.pivot = new Vector2(0.5f, 0f);
					((TMP_Text)textMeshPro).alignment = (TextAlignmentOptions)1026;
					((TMP_Text)textMeshPro).fontSize = 96f;
					((TMP_Text)textMeshPro).enableKerning = false;
					((Graphic)textMeshPro).color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
					((TMP_Text)textMeshPro).text = "!";
					((TMP_Text)textMeshPro).isTextObjectScaleStatic = IsTextObjectScaleStatic;
					floatingText_Script = go.AddComponent<TextMeshProFloatingText>();
					floatingText_Script.SpawnType = 0;
					floatingText_Script.IsTextObjectScaleStatic = IsTextObjectScaleStatic;
				}
				else if (SpawnType == 1)
				{
					GameObject go2 = new GameObject();
					go2.transform.position = new Vector3(Random.Range(-95f, 95f), 0.25f, Random.Range(-95f, 95f));
					TextMesh textMesh = go2.AddComponent<TextMesh>();
					textMesh.font = Resources.Load<Font>("Fonts/ARIAL");
					textMesh.GetComponent<Renderer>().sharedMaterial = textMesh.font.material;
					textMesh.anchor = TextAnchor.LowerCenter;
					textMesh.fontSize = 96;
					textMesh.color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
					textMesh.text = "!";
					floatingText_Script = go2.AddComponent<TextMeshProFloatingText>();
					floatingText_Script.SpawnType = 1;
				}
				else if (SpawnType == 2)
				{
					GameObject go3 = new GameObject();
					Canvas canvas = go3.AddComponent<Canvas>();
					canvas.worldCamera = Camera.main;
					go3.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
					go3.transform.position = new Vector3(Random.Range(-95f, 95f), 5f, Random.Range(-95f, 95f));
					TextMeshProUGUI textObject = new GameObject().AddComponent<TextMeshProUGUI>();
					((TMP_Text)textObject).rectTransform.SetParent(go3.transform, worldPositionStays: false);
					((Graphic)textObject).color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
					((TMP_Text)textObject).alignment = (TextAlignmentOptions)1026;
					((TMP_Text)textObject).fontSize = 96f;
					((TMP_Text)textObject).text = "!";
					floatingText_Script = go3.AddComponent<TextMeshProFloatingText>();
					floatingText_Script.SpawnType = 0;
				}
			}
		}
	}
}
