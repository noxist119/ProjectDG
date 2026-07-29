using UnityEngine;

namespace TMPro.Examples;

public class SimpleScript : MonoBehaviour
{
	private TextMeshPro m_textMeshPro;

	private const string label = "The <#0050FF>count is: </color>{0:2}";

	private float m_frame;

	private void Start()
	{
		m_textMeshPro = ((Component)this).gameObject.AddComponent<TextMeshPro>();
		((TMP_Text)m_textMeshPro).autoSizeTextContainer = true;
		((TMP_Text)m_textMeshPro).fontSize = 48f;
		((TMP_Text)m_textMeshPro).alignment = (TextAlignmentOptions)514;
		((TMP_Text)m_textMeshPro).enableWordWrapping = false;
	}

	private void Update()
	{
		((TMP_Text)m_textMeshPro).SetText("The <#0050FF>count is: </color>{0:2}", m_frame % 1000f);
		m_frame += 1f * Time.deltaTime;
	}
}
