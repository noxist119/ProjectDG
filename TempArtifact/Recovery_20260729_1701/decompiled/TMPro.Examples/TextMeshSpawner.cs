using UnityEngine;
using UnityEngine.UI;

namespace TMPro.Examples;

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
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < NumberOfNPC; i++)
		{
			if (SpawnType == 0)
			{
				GameObject val = new GameObject();
				val.transform.position = new Vector3(Random.Range(-95f, 95f), 0.5f, Random.Range(-95f, 95f));
				TextMeshPro val2 = val.AddComponent<TextMeshPro>();
				((TMP_Text)val2).fontSize = 96f;
				((TMP_Text)val2).text = "!";
				((Graphic)val2).color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue));
				floatingText_Script = val.AddComponent<TextMeshProFloatingText>();
				floatingText_Script.SpawnType = 0;
			}
			else
			{
				GameObject val3 = new GameObject();
				val3.transform.position = new Vector3(Random.Range(-95f, 95f), 0.5f, Random.Range(-95f, 95f));
				TextMesh val4 = val3.AddComponent<TextMesh>();
				((Component)val4).GetComponent<Renderer>().sharedMaterial = TheFont.material;
				val4.font = TheFont;
				val4.anchor = (TextAnchor)7;
				val4.fontSize = 96;
				val4.color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue));
				val4.text = "!";
				floatingText_Script = val3.AddComponent<TextMeshProFloatingText>();
				floatingText_Script.SpawnType = 1;
			}
		}
	}
}
