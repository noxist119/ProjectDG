using UnityEngine;

namespace TMPro.Examples;

public class TMP_ExampleScript_01 : MonoBehaviour
{
	public enum objectType
	{
		TextMeshPro,
		TextMeshProUGUI
	}

	public objectType ObjectType;

	public bool isStatic;

	private TMP_Text m_text;

	private const string k_label = "The count is <#0080ff>{0}</color>";

	private int count;

	private void Awake()
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		if (ObjectType == objectType.TextMeshPro)
		{
			m_text = (TMP_Text)(object)(((Component)this).GetComponent<TextMeshPro>() ?? ((Component)this).gameObject.AddComponent<TextMeshPro>());
		}
		else
		{
			m_text = (TMP_Text)(object)(((Component)this).GetComponent<TextMeshProUGUI>() ?? ((Component)this).gameObject.AddComponent<TextMeshProUGUI>());
		}
		m_text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
		m_text.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/Anton SDF - Drop Shadow");
		m_text.fontSize = 120f;
		m_text.text = "A <#0080ff>simple</color> line of text.";
		Vector2 preferredValues = m_text.GetPreferredValues(float.PositiveInfinity, float.PositiveInfinity);
		m_text.rectTransform.sizeDelta = new Vector2(preferredValues.x, preferredValues.y);
	}

	private void Update()
	{
		if (!isStatic)
		{
			m_text.SetText("The count is <#0080ff>{0}</color>", (float)(count % 1000));
			count++;
		}
	}
}
