using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TMPro.Examples;

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
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		m_TextMeshPro = ((Component)this).gameObject.GetComponent<TextMeshProUGUI>();
		m_Canvas = ((Component)this).gameObject.GetComponentInParent<Canvas>();
		if ((int)m_Canvas.renderMode == 0)
		{
			m_Camera = null;
		}
		else
		{
			m_Camera = m_Canvas.worldCamera;
		}
		m_TextPopup_RectTransform = Object.Instantiate<RectTransform>(TextPopup_Prefab_01);
		((Transform)m_TextPopup_RectTransform).SetParent(((Component)m_Canvas).transform, false);
		m_TextPopup_TMPComponent = ((Component)m_TextPopup_RectTransform).GetComponentInChildren<TextMeshProUGUI>();
		((Component)m_TextPopup_RectTransform).gameObject.SetActive(false);
	}

	private void OnEnable()
	{
		TMPro_EventManager.TEXT_CHANGED_EVENT.Add((Action<Object>)ON_TEXT_CHANGED);
	}

	private void OnDisable()
	{
		TMPro_EventManager.TEXT_CHANGED_EVENT.Remove((Action<Object>)ON_TEXT_CHANGED);
	}

	private void ON_TEXT_CHANGED(Object obj)
	{
		if (obj == (Object)(object)m_TextMeshPro)
		{
			m_cachedMeshInfoVertexData = ((TMP_Text)m_TextMeshPro).textInfo.CopyMeshInfoVertexData();
		}
	}

	private void LateUpdate()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_056a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0574: Unknown result type (might be due to invalid IL or missing references)
		//IL_0579: Unknown result type (might be due to invalid IL or missing references)
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_058c: Unknown result type (might be due to invalid IL or missing references)
		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0599: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
		//IL_0668: Unknown result type (might be due to invalid IL or missing references)
		//IL_0675: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Unknown result type (might be due to invalid IL or missing references)
		//IL_06be: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f1: Unknown result type (might be due to invalid IL or missing references)
		if (isHoveringObject)
		{
			int num = TMP_TextUtilities.FindIntersectingCharacter((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, m_Camera, true);
			if (num == -1 || num != m_lastIndex)
			{
				RestoreCachedVertexAttributes(m_lastIndex);
				m_lastIndex = -1;
			}
			if (num != -1 && num != m_lastIndex && (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303)))
			{
				m_lastIndex = num;
				int materialReferenceIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[num].materialReferenceIndex;
				int vertexIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[num].vertexIndex;
				Vector3[] vertices = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].vertices;
				Vector2 val = Vector2.op_Implicit((vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2f);
				Vector3 val2 = Vector2.op_Implicit(val);
				vertices[vertexIndex] -= val2;
				vertices[vertexIndex + 1] = vertices[vertexIndex + 1] - val2;
				vertices[vertexIndex + 2] = vertices[vertexIndex + 2] - val2;
				vertices[vertexIndex + 3] = vertices[vertexIndex + 3] - val2;
				float num2 = 1.5f;
				m_matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * num2);
				vertices[vertexIndex] = ((Matrix4x4)(ref m_matrix)).MultiplyPoint3x4(vertices[vertexIndex]);
				vertices[vertexIndex + 1] = ((Matrix4x4)(ref m_matrix)).MultiplyPoint3x4(vertices[vertexIndex + 1]);
				vertices[vertexIndex + 2] = ((Matrix4x4)(ref m_matrix)).MultiplyPoint3x4(vertices[vertexIndex + 2]);
				vertices[vertexIndex + 3] = ((Matrix4x4)(ref m_matrix)).MultiplyPoint3x4(vertices[vertexIndex + 3]);
				vertices[vertexIndex] += val2;
				vertices[vertexIndex + 1] = vertices[vertexIndex + 1] + val2;
				vertices[vertexIndex + 2] = vertices[vertexIndex + 2] + val2;
				vertices[vertexIndex + 3] = vertices[vertexIndex + 3] + val2;
				Color32 val3 = default(Color32);
				((Color32)(ref val3))._002Ector(byte.MaxValue, byte.MaxValue, (byte)192, byte.MaxValue);
				Color32[] colors = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].colors32;
				colors[vertexIndex] = val3;
				colors[vertexIndex + 1] = val3;
				colors[vertexIndex + 2] = val3;
				colors[vertexIndex + 3] = val3;
				TMP_MeshInfo val4 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex];
				int num3 = vertices.Length - 4;
				((TMP_MeshInfo)(ref val4)).SwapVertexData(vertexIndex, num3);
				((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
			}
			int num4 = TMP_TextUtilities.FindIntersectingWord((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, m_Camera);
			if ((Object)(object)m_TextPopup_RectTransform != (Object)null && m_selectedWord != -1 && (num4 == -1 || num4 != m_selectedWord))
			{
				TMP_WordInfo val5 = ((TMP_Text)m_TextMeshPro).textInfo.wordInfo[m_selectedWord];
				for (int i = 0; i < val5.characterCount; i++)
				{
					int num5 = val5.firstCharacterIndex + i;
					int materialReferenceIndex2 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[num5].materialReferenceIndex;
					int vertexIndex2 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[num5].vertexIndex;
					Color32[] colors2 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex2].colors32;
					colors2[vertexIndex2 + 3] = (colors2[vertexIndex2 + 2] = (colors2[vertexIndex2 + 1] = (colors2[vertexIndex2] = TMPro_ExtensionMethods.Tint(colors2[vertexIndex2], 1.33333f))));
				}
				((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
				m_selectedWord = -1;
			}
			if (num4 != -1 && num4 != m_selectedWord && !Input.GetKey((KeyCode)304) && !Input.GetKey((KeyCode)303))
			{
				m_selectedWord = num4;
				TMP_WordInfo val6 = ((TMP_Text)m_TextMeshPro).textInfo.wordInfo[num4];
				for (int j = 0; j < val6.characterCount; j++)
				{
					int num6 = val6.firstCharacterIndex + j;
					int materialReferenceIndex3 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[num6].materialReferenceIndex;
					int vertexIndex3 = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[num6].vertexIndex;
					Color32[] colors3 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex3].colors32;
					colors3[vertexIndex3 + 3] = (colors3[vertexIndex3 + 2] = (colors3[vertexIndex3 + 1] = (colors3[vertexIndex3] = TMPro_ExtensionMethods.Tint(colors3[vertexIndex3], 0.75f))));
				}
				((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
			}
			int num7 = TMP_TextUtilities.FindIntersectingLink((TMP_Text)(object)m_TextMeshPro, Input.mousePosition, m_Camera);
			if ((num7 == -1 && m_selectedLink != -1) || num7 != m_selectedLink)
			{
				((Component)m_TextPopup_RectTransform).gameObject.SetActive(false);
				m_selectedLink = -1;
			}
			if (num7 == -1 || num7 == m_selectedLink)
			{
				return;
			}
			m_selectedLink = num7;
			TMP_LinkInfo val7 = ((TMP_Text)m_TextMeshPro).textInfo.linkInfo[num7];
			Vector3 position = default(Vector3);
			RectTransformUtility.ScreenPointToWorldPointInRectangle(((TMP_Text)m_TextMeshPro).rectTransform, Vector2.op_Implicit(Input.mousePosition), m_Camera, ref position);
			string linkID = ((TMP_LinkInfo)(ref val7)).GetLinkID();
			string text = linkID;
			if (!(text == "id_01"))
			{
				if (text == "id_02")
				{
					((Transform)m_TextPopup_RectTransform).position = position;
					((Component)m_TextPopup_RectTransform).gameObject.SetActive(true);
					((TMP_Text)m_TextPopup_TMPComponent).text = "You have selected link <#ffff00> ID 02";
				}
			}
			else
			{
				((Transform)m_TextPopup_RectTransform).position = position;
				((Component)m_TextPopup_RectTransform).gameObject.SetActive(true);
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
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		if (index != -1 && index <= ((TMP_Text)m_TextMeshPro).textInfo.characterCount - 1)
		{
			int materialReferenceIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[index].materialReferenceIndex;
			int vertexIndex = ((TMP_Text)m_TextMeshPro).textInfo.characterInfo[index].vertexIndex;
			Vector3[] vertices = m_cachedMeshInfoVertexData[materialReferenceIndex].vertices;
			Vector3[] vertices2 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].vertices;
			vertices2[vertexIndex] = vertices[vertexIndex];
			vertices2[vertexIndex + 1] = vertices[vertexIndex + 1];
			vertices2[vertexIndex + 2] = vertices[vertexIndex + 2];
			vertices2[vertexIndex + 3] = vertices[vertexIndex + 3];
			Color32[] colors = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].colors32;
			Color32[] colors2 = m_cachedMeshInfoVertexData[materialReferenceIndex].colors32;
			colors[vertexIndex] = colors2[vertexIndex];
			colors[vertexIndex + 1] = colors2[vertexIndex + 1];
			colors[vertexIndex + 2] = colors2[vertexIndex + 2];
			colors[vertexIndex + 3] = colors2[vertexIndex + 3];
			Vector2[] uvs = m_cachedMeshInfoVertexData[materialReferenceIndex].uvs0;
			Vector2[] uvs2 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].uvs0;
			uvs2[vertexIndex] = uvs[vertexIndex];
			uvs2[vertexIndex + 1] = uvs[vertexIndex + 1];
			uvs2[vertexIndex + 2] = uvs[vertexIndex + 2];
			uvs2[vertexIndex + 3] = uvs[vertexIndex + 3];
			Vector2[] uvs3 = m_cachedMeshInfoVertexData[materialReferenceIndex].uvs2;
			Vector2[] uvs4 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].uvs2;
			uvs4[vertexIndex] = uvs3[vertexIndex];
			uvs4[vertexIndex + 1] = uvs3[vertexIndex + 1];
			uvs4[vertexIndex + 2] = uvs3[vertexIndex + 2];
			uvs4[vertexIndex + 3] = uvs3[vertexIndex + 3];
			int num = (vertices.Length / 4 - 1) * 4;
			vertices2[num] = vertices[num];
			vertices2[num + 1] = vertices[num + 1];
			vertices2[num + 2] = vertices[num + 2];
			vertices2[num + 3] = vertices[num + 3];
			colors2 = m_cachedMeshInfoVertexData[materialReferenceIndex].colors32;
			colors = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].colors32;
			colors[num] = colors2[num];
			colors[num + 1] = colors2[num + 1];
			colors[num + 2] = colors2[num + 2];
			colors[num + 3] = colors2[num + 3];
			uvs = m_cachedMeshInfoVertexData[materialReferenceIndex].uvs0;
			uvs2 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].uvs0;
			uvs2[num] = uvs[num];
			uvs2[num + 1] = uvs[num + 1];
			uvs2[num + 2] = uvs[num + 2];
			uvs2[num + 3] = uvs[num + 3];
			uvs3 = m_cachedMeshInfoVertexData[materialReferenceIndex].uvs2;
			uvs4 = ((TMP_Text)m_TextMeshPro).textInfo.meshInfo[materialReferenceIndex].uvs2;
			uvs4[num] = uvs3[num];
			uvs4[num + 1] = uvs3[num + 1];
			uvs4[num + 2] = uvs3[num + 2];
			uvs4[num + 3] = uvs3[num + 3];
			((TMP_Text)m_TextMeshPro).UpdateVertexData((TMP_VertexDataUpdateFlags)255);
		}
	}
}
