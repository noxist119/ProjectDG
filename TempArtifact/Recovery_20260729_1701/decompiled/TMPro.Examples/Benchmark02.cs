using UnityEngine;
using UnityEngine.UI;

namespace TMPro.Examples;

public class Benchmark02 : MonoBehaviour
{
	public int SpawnType = 0;

	public int NumberOfNPC = 12;

	public bool IsTextObjectScaleStatic;

	private TextMeshProFloatingText floatingText_Script;

	private void Start()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Expected O, but got Unknown
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < NumberOfNPC; i++)
		{
			if (SpawnType == 0)
			{
				GameObject val = new GameObject();
				val.transform.position = new Vector3(Random.Range(-95f, 95f), 0.25f, Random.Range(-95f, 95f));
				TextMeshPro val2 = val.AddComponent<TextMeshPro>();
				((TMP_Text)val2).autoSizeTextContainer = true;
				((TMP_Text)val2).rectTransform.pivot = new Vector2(0.5f, 0f);
				((TMP_Text)val2).alignment = (TextAlignmentOptions)1026;
				((TMP_Text)val2).fontSize = 96f;
				((TMP_Text)val2).enableKerning = false;
				((Graphic)val2).color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue));
				((TMP_Text)val2).text = "!";
				((TMP_Text)val2).isTextObjectScaleStatic = IsTextObjectScaleStatic;
				floatingText_Script = val.AddComponent<TextMeshProFloatingText>();
				floatingText_Script.SpawnType = 0;
				floatingText_Script.IsTextObjectScaleStatic = IsTextObjectScaleStatic;
			}
			else if (SpawnType == 1)
			{
				GameObject val3 = new GameObject();
				val3.transform.position = new Vector3(Random.Range(-95f, 95f), 0.25f, Random.Range(-95f, 95f));
				TextMesh val4 = val3.AddComponent<TextMesh>();
				val4.font = Resources.Load<Font>("Fonts/ARIAL");
				((Component)val4).GetComponent<Renderer>().sharedMaterial = val4.font.material;
				val4.anchor = (TextAnchor)7;
				val4.fontSize = 96;
				val4.color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue));
				val4.text = "!";
				floatingText_Script = val3.AddComponent<TextMeshProFloatingText>();
				floatingText_Script.SpawnType = 1;
			}
			else if (SpawnType == 2)
			{
				GameObject val5 = new GameObject();
				Canvas val6 = val5.AddComponent<Canvas>();
				val6.worldCamera = Camera.main;
				val5.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
				val5.transform.position = new Vector3(Random.Range(-95f, 95f), 5f, Random.Range(-95f, 95f));
				TextMeshProUGUI val7 = new GameObject().AddComponent<TextMeshProUGUI>();
				((Transform)((TMP_Text)val7).rectTransform).SetParent(val5.transform, false);
				((Graphic)val7).color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue));
				((TMP_Text)val7).alignment = (TextAlignmentOptions)1026;
				((TMP_Text)val7).fontSize = 96f;
				((TMP_Text)val7).text = "!";
				floatingText_Script = val5.AddComponent<TextMeshProFloatingText>();
				floatingText_Script.SpawnType = 0;
			}
		}
	}
}
