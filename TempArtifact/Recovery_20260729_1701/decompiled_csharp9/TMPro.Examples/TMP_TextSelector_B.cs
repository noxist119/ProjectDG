using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TMPro.Examples
{
	public class TMP_TextSelector_B : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler, IPointerUpHandler
	{
		public RectTransform TextPopup_Prefab_01;

		private RectTransform m_TextPopup_RectTransform;

		private TextMeshProUGUI m_TextPopup_TMPComponent;

		private const string k_LinkText = "You have selected link <#ffff00>";

		private const string k_WordText = "Word Index: <#ffff00>";

		private TextMeshProUGUI m_TextMeshPro;

		private Canvas m_Canvas;

		private Camera m_Camera;

		private bool isHoveringObject;

		private int m_selectedWord = -1;

		private int m_selectedLink = -1;

		private int m_lastIndex = -1;

		private Matrix4x4 m_matrix;

		private TMP_MeshInfo[] m_cachedMeshInfoVertexData;

		private void Awake()
		{
			m_TextMeshPro = base.gameObject.GetComponent<TextMeshProUGUI>();
			m_Canvas = base.gameObject.GetComponentInParent<Canvas>();
			if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				m_Camera = null;
			}
			else
			{
				m_Camera = m_Canvas.worldCamera;
			}
			m_TextPopup_RectTransform = UnityEngine.Object.Instantiate(TextPopup_Prefab_01);
			m_TextPopup_RectTransform.SetParent(m_Canvas.transform, worldPositionStays: false);
			m_TextPopup_TMPComponent = m_TextPopup_RectTransform.GetComponentInChildren<TextMeshProUGUI>();
			m_TextPopup_RectTransform.gameObject.SetActive(value: false);
		}

		private void OnEnable()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add((Action<UnityEngine.Object>)ON_TEXT_CHANGED);
		}

		private void OnDisable()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove((Action<UnityEngine.Object>)ON_TEXT_CHANGED);
		}

		private void ON_TEXT_CHANGED(UnityEngine.Object obj)
		{
			if (obj == (UnityEngine.Object)(object)m_TextMeshPro)
			{
				m_cachedMeshInfoVertexData = ((TMP_Text)m_TextMeshPro).textInfo.CopyMeshInfoVertexData();
			}
		}

		private void LateUpdate()
		{
			//IL_0320: Unknown result type (might be due to invalid IL or missing references)
			//IL_0325: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0477: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0500: Unknown result type (might be due to invalid IL or missing references)
			//IL_0663: Unknown result type (might be due to invalid IL or missing references)
			//IL_0668: Unknown result type (might be due to invalid IL or missing references)
			if (isHoveringObject)
			{
				int charIndex = TMP_TextUtilities.FindIntersectingCharacter((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, m_Camera, true);
				if (charIndex == -1 || charIndex != m_lastIndex)
				{
					RestoreCachedVertexAttributes(m_lastIndex);
					m_lastIndex = -1;
				}
				if (charIndex != -1 && charIndex != m_lastIndex && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
				{
					m_lastIndex = charIndex;
					int materialIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[charIndex].materialReferenceIndex;
					int vertexIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[charIndex].vertexIndex;
					Vector3[] vertices = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].vertices;
					Vector2 charMidBasline = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2f;
					Vector3 offset = charMidBasline;
					vertices[vertexIndex] -= offset;
					vertices[vertexIndex + 1] -= offset;
					vertices[vertexIndex + 2] -= offset;
					vertices[vertexIndex + 3] -= offset;
					float zoomFactor = 1.5f;
					m_matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * zoomFactor);
					vertices[vertexIndex] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex]);
					vertices[vertexIndex + 1] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
					vertices[vertexIndex + 2] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
					vertices[vertexIndex + 3] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);
					vertices[vertexIndex] += offset;
					vertices[vertexIndex + 1] += offset;
					vertices[vertexIndex + 2] += offset;
					vertices[vertexIndex + 3] += offset;
					Color32 c = new Color32(byte.MaxValue, byte.MaxValue, 192, byte.MaxValue);
					Color32[] vertexColors = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].colors32;
					vertexColors[vertexIndex] = c;
					vertexColors[vertexIndex + 1] = c;
					vertexColors[vertexIndex + 2] = c;
					vertexColors[vertexIndex + 3] = c;
					TMP_MeshInfo meshInfo = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex];
					int lastVertexIndex = vertices.Length - 4;
					((TMP_MeshInfo)(ref meshInfo)).SwapVertexData(vertexIndex, lastVertexIndex);
					((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
				}
				int wordIndex = TMP_TextUtilities.FindIntersectingWord((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, m_Camera);
				if (m_TextPopup_RectTransform != null && m_selectedWord != -1 && (wordIndex == -1 || wordIndex != m_selectedWord))
				{
					TMP_WordInfo wInfo = ((TMP_Text)m_TextMeshPro).textInfo.wordInfo[m_selectedWord];
					for (int i = 0; i < wInfo.characterCount; i++)
					{
						int characterIndex = wInfo.firstCharacterIndex + i;
						int meshIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[characterIndex].materialReferenceIndex;
						int vertexIndex2 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[characterIndex].vertexIndex;
						Color32[] vertexColors2 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[meshIndex].colors32;
						vertexColors2[vertexIndex2 + 3] = (vertexColors2[vertexIndex2 + 2] = (vertexColors2[vertexIndex2 + 1] = (vertexColors2[vertexIndex2] = TMPro_ExtensionMethods.Tint(vertexColors2[vertexIndex2], 1.33333f))));
					}
					((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
					m_selectedWord = -1;
				}
				if (wordIndex != -1 && wordIndex != m_selectedWord && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
				{
					m_selectedWord = wordIndex;
					TMP_WordInfo wInfo2 = ((TMP_Text)m_TextMeshPro).textInfo.wordInfo[wordIndex];
					for (int j = 0; j < wInfo2.characterCount; j++)
					{
						int characterIndex2 = wInfo2.firstCharacterIndex + j;
						int meshIndex2 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[characterIndex2].materialReferenceIndex;
						int vertexIndex3 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[characterIndex2].vertexIndex;
						Color32[] vertexColors3 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[meshIndex2].colors32;
						vertexColors3[vertexIndex3 + 3] = (vertexColors3[vertexIndex3 + 2] = (vertexColors3[vertexIndex3 + 1] = (vertexColors3[vertexIndex3] = TMPro_ExtensionMethods.Tint(vertexColors3[vertexIndex3], 0.75f))));
					}
					((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
				}
				int linkIndex = TMP_TextUtilities.FindIntersectingLink((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, m_Camera);
				if ((linkIndex == -1 && m_selectedLink != -1) || linkIndex != m_selectedLink)
				{
					m_TextPopup_RectTransform.gameObject.SetActive(value: false);
					m_selectedLink = -1;
				}
				if (linkIndex == -1 || linkIndex == m_selectedLink)
				{
					return;
				}
				m_selectedLink = linkIndex;
				TMP_LinkInfo linkInfo = ((TMP_Text)m_TextMeshPro).textInfo.linkInfo[linkIndex];
				RectTransformUtility.ScreenPointToWorldPointInRectangle(((TMP_Text)m_TextMeshPro).rectTransform, Input.mousePosition, m_Camera, out var worldPointInRectangle);
				string linkID = ((TMP_LinkInfo)(ref linkInfo)).GetLinkID();
				string text = linkID;
				if (!(text == "id_01"))
				{
					if (text == "id_02")
					{
						m_TextPopup_RectTransform.position = worldPointInRectangle;
						m_TextPopup_RectTransform.gameObject.SetActive(value: true);
						((TMP_Text)m_TextPopup_TMPComponent).text = "You have selected link <#ffff00> ID 02";
					}
				}
				else
				{
					m_TextPopup_RectTransform.position = worldPointInRectangle;
					m_TextPopup_RectTransform.gameObject.SetActive(value: true);
					((TMP_Text)m_TextPopup_TMPComponent).text = "You have selected link <#ffff00> ID 01";
				}
			}
			else if (m_lastIndex != -1)
			{
				RestoreCachedVertexAttributes(m_lastIndex);
				m_lastIndex = -1;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			isHoveringObject = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			isHoveringObject = false;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
		}

		public void OnPointerUp(PointerEventData eventData)
		{
		}

		private void RestoreCachedVertexAttributes(int index)
		{
			if (index != -1 && index <= ((TMP_Text)m_TextMeshPro).textInfo.characterCount - 1)
			{
				int materialIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[index].materialReferenceIndex;
				int vertexIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[index].vertexIndex;
				Vector3[] src_vertices = m_cachedMeshInfoVertexData[materialIndex].vertices;
				Vector3[] dst_vertices = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].vertices;
				dst_vertices[vertexIndex] = src_vertices[vertexIndex];
				dst_vertices[vertexIndex + 1] = src_vertices[vertexIndex + 1];
				dst_vertices[vertexIndex + 2] = src_vertices[vertexIndex + 2];
				dst_vertices[vertexIndex + 3] = src_vertices[vertexIndex + 3];
				Color32[] dst_colors = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].colors32;
				Color32[] src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;
				dst_colors[vertexIndex] = src_colors[vertexIndex];
				dst_colors[vertexIndex + 1] = src_colors[vertexIndex + 1];
				dst_colors[vertexIndex + 2] = src_colors[vertexIndex + 2];
				dst_colors[vertexIndex + 3] = src_colors[vertexIndex + 3];
				Vector2[] src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
				Vector2[] dst_uv0s = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].uvs0;
				dst_uv0s[vertexIndex] = src_uv0s[vertexIndex];
				dst_uv0s[vertexIndex + 1] = src_uv0s[vertexIndex + 1];
				dst_uv0s[vertexIndex + 2] = src_uv0s[vertexIndex + 2];
				dst_uv0s[vertexIndex + 3] = src_uv0s[vertexIndex + 3];
				Vector2[] src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
				Vector2[] dst_uv2s = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].uvs2;
				dst_uv2s[vertexIndex] = src_uv2s[vertexIndex];
				dst_uv2s[vertexIndex + 1] = src_uv2s[vertexIndex + 1];
				dst_uv2s[vertexIndex + 2] = src_uv2s[vertexIndex + 2];
				dst_uv2s[vertexIndex + 3] = src_uv2s[vertexIndex + 3];
				int lastIndex = (src_vertices.Length / 4 - 1) * 4;
				dst_vertices[lastIndex] = src_vertices[lastIndex];
				dst_vertices[lastIndex + 1] = src_vertices[lastIndex + 1];
				dst_vertices[lastIndex + 2] = src_vertices[lastIndex + 2];
				dst_vertices[lastIndex + 3] = src_vertices[lastIndex + 3];
				src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;
				dst_colors = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].colors32;
				dst_colors[lastIndex] = src_colors[lastIndex];
				dst_colors[lastIndex + 1] = src_colors[lastIndex + 1];
				dst_colors[lastIndex + 2] = src_colors[lastIndex + 2];
				dst_colors[lastIndex + 3] = src_colors[lastIndex + 3];
				src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
				dst_uv0s = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].uvs0;
				dst_uv0s[lastIndex] = src_uv0s[lastIndex];
				dst_uv0s[lastIndex + 1] = src_uv0s[lastIndex + 1];
				dst_uv0s[lastIndex + 2] = src_uv0s[lastIndex + 2];
				dst_uv0s[lastIndex + 3] = src_uv0s[lastIndex + 3];
				src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
				dst_uv2s = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialIndex].uvs2;
				dst_uv2s[lastIndex] = src_uv2s[lastIndex];
				dst_uv2s[lastIndex + 1] = src_uv2s[lastIndex + 1];
				dst_uv2s[lastIndex + 2] = src_uv2s[lastIndex + 2];
				dst_uv2s[lastIndex + 3] = src_uv2s[lastIndex + 3];
				((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
			}
		}
	}
}
