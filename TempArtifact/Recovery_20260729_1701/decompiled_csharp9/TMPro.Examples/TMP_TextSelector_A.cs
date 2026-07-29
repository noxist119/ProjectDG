using UnityEngine;
using UnityEngine.EventSystems;

namespace TMPro.Examples
{
	public class TMP_TextSelector_A : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		private TextMeshPro m_TextMeshPro;

		private Camera m_Camera;

		private bool m_isHoveringObject;

		private int m_selectedLink = -1;

		private int m_lastCharIndex = -1;

		private int m_lastWordIndex = -1;

		private void Awake()
		{
			m_TextMeshPro = base.gameObject.GetComponent<TextMeshPro>();
			m_Camera = Camera.main;
			((TMP_Text)m_TextMeshPro).ForceMeshUpdate(false, false);
		}

		private void LateUpdate()
		{
			//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_028b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_0387: Unknown result type (might be due to invalid IL or missing references)
			//IL_0336: Unknown result type (might be due to invalid IL or missing references)
			m_isHoveringObject = false;
			if (TMP_TextUtilities.IsIntersectingRectTransform(((TMP_Text)m_TextMeshPro).rectTransform, Input.mousePosition, Camera.main))
			{
				m_isHoveringObject = true;
			}
			if (!m_isHoveringObject)
			{
				return;
			}
			int charIndex = TMP_TextUtilities.FindIntersectingCharacter((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, Camera.main, true);
			if (charIndex != -1 && charIndex != m_lastCharIndex && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
			{
				m_lastCharIndex = charIndex;
				int meshIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[charIndex].materialReferenceIndex;
				int vertexIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[charIndex].vertexIndex;
				Color32 c = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), byte.MaxValue);
				Color32[] vertexColors = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[meshIndex].colors32;
				vertexColors[vertexIndex] = c;
				vertexColors[vertexIndex + 1] = c;
				vertexColors[vertexIndex + 2] = c;
				vertexColors[vertexIndex + 3] = c;
				((TMP_Text)m_TextMeshPro).textInfo.meshInfo[meshIndex].mesh.colors32 = vertexColors;
			}
			int linkIndex = TMP_TextUtilities.FindIntersectingLink((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, m_Camera);
			if ((linkIndex == -1 && m_selectedLink != -1) || linkIndex != m_selectedLink)
			{
				m_selectedLink = -1;
			}
			if (linkIndex != -1 && linkIndex != m_selectedLink)
			{
				m_selectedLink = linkIndex;
				TMP_LinkInfo linkInfo = ((TMP_Text)m_TextMeshPro).textInfo.linkInfo[linkIndex];
				RectTransformUtility.ScreenPointToWorldPointInRectangle(((TMP_Text)m_TextMeshPro).rectTransform, Input.mousePosition, m_Camera, out var _);
				string linkID = ((TMP_LinkInfo)(ref linkInfo)).GetLinkID();
				string text = linkID;
				if (!(text == "id_01") && text == "id_02")
				{
				}
			}
			int wordIndex = TMP_TextUtilities.FindIntersectingWord((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, Camera.main);
			if (wordIndex != -1 && wordIndex != m_lastWordIndex)
			{
				m_lastWordIndex = wordIndex;
				TMP_WordInfo wInfo = ((TMP_Text)m_TextMeshPro).textInfo.wordInfo[wordIndex];
				Vector3 wordPOS = m_TextMeshPro.transform.TransformPoint(((TMP_Text)m_TextMeshPro).textInfo.characterInfo[wInfo.firstCharacterIndex].bottomLeft);
				wordPOS = Camera.main.WorldToScreenPoint(wordPOS);
				Color32[] vertexColors2 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[0].colors32;
				Color32 c2 = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), byte.MaxValue);
				for (int i = 0; i < wInfo.characterCount; i++)
				{
					int vertexIndex2 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[wInfo.firstCharacterIndex + i].vertexIndex;
					vertexColors2[vertexIndex2] = c2;
					vertexColors2[vertexIndex2 + 1] = c2;
					vertexColors2[vertexIndex2 + 2] = c2;
					vertexColors2[vertexIndex2 + 3] = c2;
				}
				((TMP_Text)m_TextMeshPro).mesh.colors32 = vertexColors2;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Debug.Log("OnPointerEnter()");
			m_isHoveringObject = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Debug.Log("OnPointerExit()");
			m_isHoveringObject = false;
		}
	}
}
