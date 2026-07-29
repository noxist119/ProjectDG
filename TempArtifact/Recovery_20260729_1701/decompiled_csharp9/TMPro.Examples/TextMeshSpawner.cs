using UnityEngine;
using UnityEngine.UI;

namespace TMPro.Examples
{
	public class TextMeshSpawner : MonoBehaviour
	{
		public int SpawnType = 0;

		public int NumberOfNPC = 12;

		public Font TheFont;

		private TextMeshProFloatingText floatingText_Script;

		private void Awake()
		{
		}

		private void Start()
		{
			for (int i = 0; i < NumberOfNPC; i++)
			{
				if (SpawnType == 0)
				{
					GameObject go = new GameObject();
					go.transform.position = new Vector3(Random.Range(-95f, 95f), 0.5f, Random.Range(-95f, 95f));
					TextMeshPro textMeshPro = go.AddComponent<TextMeshPro>();
					((TMP_Text)textMeshPro).fontSize = 96f;
					((TMP_Text)textMeshPro).text = "!";
					((Graphic)textMeshPro).color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
					floatingText_Script = go.AddComponent<TextMeshProFloatingText>();
					floatingText_Script.SpawnType = 0;
				}
				else
				{
					GameObject go2 = new GameObject();
					go2.transform.position = new Vector3(Random.Range(-95f, 95f), 0.5f, Random.Range(-95f, 95f));
					TextMesh textMesh = go2.AddComponent<TextMesh>();
					textMesh.GetComponent<Renderer>().sharedMaterial = TheFont.material;
					textMesh.font = TheFont;
					textMesh.anchor = TextAnchor.LowerCenter;
					textMesh.fontSize = 96;
					textMesh.color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
					textMesh.text = "!";
					floatingText_Script = go2.AddComponent<TextMeshProFloatingText>();
					floatingText_Script.SpawnType = 1;
				}
			}
		}
	}
}
