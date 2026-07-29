using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro.Examples;

public class TMP_TextInfoDebugTool : MonoBehaviour
{
	public bool ShowCharacters;

	public bool ShowWords;

	public bool ShowLinks;

	public bool ShowLines;

	public bool ShowMeshBounds;

	public bool ShowTextBounds;

	[Space(10f)]
	[TextArea(2, 2)]
	public string ObjectStats;

	[SerializeField]
	private TMP_Text m_TextComponent;

	private Transform m_Transform;

	private TMP_TextInfo m_TextInfo;

	private float m_ScaleMultiplier;

	private float m_HandleSize;

	private void OnDrawGizmos()
	{
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)m_TextComponent == (Object)null)
		{
			m_TextComponent = ((Component)this).GetComponent<TMP_Text>();
			if ((Object)(object)m_TextComponent == (Object)null)
			{
				return;
			}
		}
		m_Transform = m_TextComponent.transform;
		m_TextInfo = m_TextComponent.textInfo;
		ObjectStats = "Characters: " + m_TextInfo.characterCount + "   Words: " + m_TextInfo.wordCount + "   Spaces: " + m_TextInfo.spaceCount + "   Sprites: " + m_TextInfo.spriteCount + "   Links: " + m_TextInfo.linkCount + "\nLines: " + m_TextInfo.lineCount + "   Pages: " + m_TextInfo.pageCount;
		m_ScaleMultiplier = ((((object)m_TextComponent).GetType() == typeof(TextMeshPro)) ? 1f : 0.1f);
		m_HandleSize = HandleUtility.GetHandleSize(m_Transform.position) * m_ScaleMultiplier;
		if (ShowLines)
		{
			DrawLineBounds();
		}
		if (ShowWords)
		{
			DrawWordBounds();
		}
		if (ShowCharacters)
		{
			DrawCharactersBounds();
		}
		if (ShowLinks)
		{
			DrawLinkBounds();
		}
		if (ShowMeshBounds)
		{
			DrawBounds();
		}
		if (ShowTextBounds)
		{
			DrawTextBounds();
		}
	}

	private void DrawCharactersBounds()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0400: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_040f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_0443: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04df: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Expected O, but got Unknown
		//IL_0532: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0582: Unknown result type (might be due to invalid IL or missing references)
		//IL_0587: Unknown result type (might be due to invalid IL or missing references)
		//IL_0592: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05da: Unknown result type (might be due to invalid IL or missing references)
		//IL_05df: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0614: Unknown result type (might be due to invalid IL or missing references)
		//IL_0619: Unknown result type (might be due to invalid IL or missing references)
		//IL_061e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
		//IL_0647: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0651: Unknown result type (might be due to invalid IL or missing references)
		//IL_065c: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Unknown result type (might be due to invalid IL or missing references)
		//IL_067f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0684: Unknown result type (might be due to invalid IL or missing references)
		//IL_068f: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c2: Unknown result type (might be due to invalid IL or missing references)
		int characterCount = m_TextInfo.characterCount;
		Vector3 val7 = default(Vector3);
		Vector3 val8 = default(Vector3);
		Vector3 val9 = default(Vector3);
		Vector3 val10 = default(Vector3);
		for (int i = 0; i < characterCount; i++)
		{
			TMP_CharacterInfo val = m_TextInfo.characterInfo[i];
			bool flag = i < m_TextComponent.maxVisibleCharacters && val.lineNumber < m_TextComponent.maxVisibleLines && i >= m_TextComponent.firstVisibleCharacter;
			if ((int)m_TextComponent.overflowMode == 5)
			{
				flag = flag && val.pageNumber + 1 == m_TextComponent.pageToDisplay;
			}
			if (!flag)
			{
				continue;
			}
			float num = 6f;
			Vector3 bottomLeft = m_Transform.TransformPoint(val.bottomLeft);
			Vector3 val2 = m_Transform.TransformPoint(new Vector3(val.topLeft.x, val.topLeft.y, 0f));
			Vector3 val3 = m_Transform.TransformPoint(val.topRight);
			Vector3 val4 = m_Transform.TransformPoint(new Vector3(val.bottomRight.x, val.bottomRight.y, 0f));
			if (val.isVisible)
			{
				Color green = Color.green;
				DrawDottedRectangle(bottomLeft, val3, green);
			}
			else
			{
				Color grey = Color.grey;
				float num2 = ((Math.Abs(val.origin - val.xAdvance) > 0.01f) ? val.xAdvance : (val.origin + (val.ascender - val.descender) * 0.03f));
				DrawDottedRectangle(m_Transform.TransformPoint(new Vector3(val.origin, val.descender, 0f)), m_Transform.TransformPoint(new Vector3(num2, val.ascender, 0f)), grey, 4f);
			}
			float origin = val.origin;
			float xAdvance = val.xAdvance;
			float ascender = val.ascender;
			float baseLine = val.baseLine;
			float descender = val.descender;
			Vector3 val5 = m_Transform.TransformPoint(new Vector3(origin, ascender, 0f));
			Vector3 val6 = m_Transform.TransformPoint(new Vector3(xAdvance, ascender, 0f));
			Handles.color = Color.cyan;
			Handles.DrawDottedLine(val5, val6, num);
			FaceInfo faceInfo;
			float num3;
			if (!((Object)(object)val.fontAsset == (Object)null))
			{
				faceInfo = val.fontAsset.faceInfo;
				num3 = baseLine + ((FaceInfo)(ref faceInfo)).capLine * val.scale;
			}
			else
			{
				num3 = 0f;
			}
			float num4 = num3;
			((Vector3)(ref val7))._002Ector(val2.x, m_Transform.TransformPoint(new Vector3(0f, num4, 0f)).y, 0f);
			((Vector3)(ref val8))._002Ector(val3.x, m_Transform.TransformPoint(new Vector3(0f, num4, 0f)).y, 0f);
			float num5;
			if (!((Object)(object)val.fontAsset == (Object)null))
			{
				faceInfo = val.fontAsset.faceInfo;
				num5 = baseLine + ((FaceInfo)(ref faceInfo)).meanLine * val.scale;
			}
			else
			{
				num5 = 0f;
			}
			float num6 = num5;
			((Vector3)(ref val9))._002Ector(val2.x, m_Transform.TransformPoint(new Vector3(0f, num6, 0f)).y, 0f);
			((Vector3)(ref val10))._002Ector(val3.x, m_Transform.TransformPoint(new Vector3(0f, num6, 0f)).y, 0f);
			if (val.isVisible)
			{
				Handles.color = Color.cyan;
				Handles.DrawDottedLine(val7, val8, num);
				Handles.color = Color.cyan;
				Handles.DrawDottedLine(val9, val10, num);
			}
			Vector3 val11 = m_Transform.TransformPoint(new Vector3(origin, baseLine, 0f));
			Vector3 val12 = m_Transform.TransformPoint(new Vector3(xAdvance, baseLine, 0f));
			Handles.color = Color.cyan;
			Handles.DrawDottedLine(val11, val12, num);
			Vector3 val13 = m_Transform.TransformPoint(new Vector3(origin, descender, 0f));
			Vector3 val14 = m_Transform.TransformPoint(new Vector3(xAdvance, descender, 0f));
			Handles.color = Color.cyan;
			Handles.DrawDottedLine(val13, val14, num);
			Vector3 position = m_Transform.TransformPoint(new Vector3(origin, baseLine, 0f));
			DrawCrosshair(position, 0.05f / m_ScaleMultiplier, Color.cyan);
			Vector3 position2 = m_Transform.TransformPoint(new Vector3(xAdvance, baseLine, 0f));
			DrawSquare(position2, 0.025f / m_ScaleMultiplier, Color.yellow);
			DrawCrosshair(position2, 0.0125f / m_ScaleMultiplier, Color.yellow);
			if (m_HandleSize < 0.5f)
			{
				GUIStyle val15 = new GUIStyle(GUI.skin.GetStyle("Label"));
				val15.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
				val15.fontSize = 12;
				val15.fixedWidth = 200f;
				val15.fixedHeight = 20f;
				float num7 = (origin + xAdvance) / 2f;
				Vector3 val16 = m_Transform.TransformPoint(new Vector3(num7, ascender, 0f));
				val15.alignment = (TextAnchor)1;
				Handles.Label(val16, "Ascent Line", val15);
				val16 = m_Transform.TransformPoint(new Vector3(num7, baseLine, 0f));
				Handles.Label(val16, "Base Line", val15);
				val16 = m_Transform.TransformPoint(new Vector3(num7, descender, 0f));
				Handles.Label(val16, "Descent Line", val15);
				if (val.isVisible)
				{
					val16 = m_Transform.TransformPoint(new Vector3(num7, num4, 0f));
					val15.alignment = (TextAnchor)1;
					Handles.Label(val16, "Cap Line", val15);
					val16 = m_Transform.TransformPoint(new Vector3(num7, num6, 0f));
					val15.alignment = (TextAnchor)1;
					Handles.Label(val16, "Mean Line", val15);
					val16 = m_Transform.TransformPoint(new Vector3(origin, baseLine, 0f));
					val15.alignment = (TextAnchor)2;
					Handles.Label(val16, "Origin ", val15);
					val16 = m_Transform.TransformPoint(new Vector3(xAdvance, baseLine, 0f));
					val15.alignment = (TextAnchor)0;
					Handles.Label(val16, "  Advance", val15);
				}
			}
		}
	}

	private void DrawWordBounds()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Invalid comparison between Unknown and I4
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < m_TextInfo.wordCount; i++)
		{
			TMP_WordInfo val = m_TextInfo.wordInfo[i];
			bool flag = false;
			Vector3 val2 = Vector3.zero;
			Vector3 val3 = Vector3.zero;
			Vector3 zero = Vector3.zero;
			Vector3 zero2 = Vector3.zero;
			float num = float.NegativeInfinity;
			float num2 = float.PositiveInfinity;
			Color green = Color.green;
			for (int j = 0; j < val.characterCount; j++)
			{
				int num3 = val.firstCharacterIndex + j;
				TMP_CharacterInfo val4 = m_TextInfo.characterInfo[num3];
				int lineNumber = val4.lineNumber;
				bool flag2 = ((num3 <= m_TextComponent.maxVisibleCharacters && val4.lineNumber <= m_TextComponent.maxVisibleLines && ((int)m_TextComponent.overflowMode != 5 || val4.pageNumber + 1 == m_TextComponent.pageToDisplay)) ? true : false);
				num = Mathf.Max(num, val4.ascender);
				num2 = Mathf.Min(num2, val4.descender);
				if (!flag && flag2)
				{
					flag = true;
					((Vector3)(ref val2))._002Ector(val4.bottomLeft.x, val4.descender, 0f);
					((Vector3)(ref val3))._002Ector(val4.bottomLeft.x, val4.ascender, 0f);
					if (val.characterCount == 1)
					{
						flag = false;
						val3 = m_Transform.TransformPoint(new Vector3(val3.x, num, 0f));
						val2 = m_Transform.TransformPoint(new Vector3(val2.x, num2, 0f));
						zero = m_Transform.TransformPoint(new Vector3(val4.topRight.x, num2, 0f));
						zero2 = m_Transform.TransformPoint(new Vector3(val4.topRight.x, num, 0f));
						DrawRectangle(val2, val3, zero2, zero, green);
					}
				}
				if (flag && j == val.characterCount - 1)
				{
					flag = false;
					val3 = m_Transform.TransformPoint(new Vector3(val3.x, num, 0f));
					val2 = m_Transform.TransformPoint(new Vector3(val2.x, num2, 0f));
					zero = m_Transform.TransformPoint(new Vector3(val4.topRight.x, num2, 0f));
					zero2 = m_Transform.TransformPoint(new Vector3(val4.topRight.x, num, 0f));
					DrawRectangle(val2, val3, zero2, zero, green);
				}
				else if (flag && lineNumber != m_TextInfo.characterInfo[num3 + 1].lineNumber)
				{
					flag = false;
					val3 = m_Transform.TransformPoint(new Vector3(val3.x, num, 0f));
					val2 = m_Transform.TransformPoint(new Vector3(val2.x, num2, 0f));
					zero = m_Transform.TransformPoint(new Vector3(val4.topRight.x, num2, 0f));
					zero2 = m_Transform.TransformPoint(new Vector3(val4.topRight.x, num, 0f));
					DrawRectangle(val2, val3, zero2, zero, green);
					num = float.NegativeInfinity;
					num2 = float.PositiveInfinity;
				}
			}
		}
	}

	private void DrawLinkBounds()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Invalid comparison between Unknown and I4
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		TMP_TextInfo textInfo = m_TextComponent.textInfo;
		for (int i = 0; i < textInfo.linkCount; i++)
		{
			TMP_LinkInfo val = textInfo.linkInfo[i];
			bool flag = false;
			Vector3 val2 = Vector3.zero;
			Vector3 val3 = Vector3.zero;
			Vector3 zero = Vector3.zero;
			Vector3 zero2 = Vector3.zero;
			float num = float.NegativeInfinity;
			float num2 = float.PositiveInfinity;
			Color32 val4 = Color32.op_Implicit(Color.cyan);
			for (int j = 0; j < val.linkTextLength; j++)
			{
				int num3 = val.linkTextfirstCharacterIndex + j;
				TMP_CharacterInfo val5 = textInfo.characterInfo[num3];
				int lineNumber = val5.lineNumber;
				bool flag2 = ((num3 <= m_TextComponent.maxVisibleCharacters && val5.lineNumber <= m_TextComponent.maxVisibleLines && ((int)m_TextComponent.overflowMode != 5 || val5.pageNumber + 1 == m_TextComponent.pageToDisplay)) ? true : false);
				num = Mathf.Max(num, val5.ascender);
				num2 = Mathf.Min(num2, val5.descender);
				if (!flag && flag2)
				{
					flag = true;
					((Vector3)(ref val2))._002Ector(val5.bottomLeft.x, val5.descender, 0f);
					((Vector3)(ref val3))._002Ector(val5.bottomLeft.x, val5.ascender, 0f);
					if (val.linkTextLength == 1)
					{
						flag = false;
						val3 = m_Transform.TransformPoint(new Vector3(val3.x, num, 0f));
						val2 = m_Transform.TransformPoint(new Vector3(val2.x, num2, 0f));
						zero = m_Transform.TransformPoint(new Vector3(val5.topRight.x, num2, 0f));
						zero2 = m_Transform.TransformPoint(new Vector3(val5.topRight.x, num, 0f));
						DrawRectangle(val2, val3, zero2, zero, Color32.op_Implicit(val4));
					}
				}
				if (flag && j == val.linkTextLength - 1)
				{
					flag = false;
					val3 = m_Transform.TransformPoint(new Vector3(val3.x, num, 0f));
					val2 = m_Transform.TransformPoint(new Vector3(val2.x, num2, 0f));
					zero = m_Transform.TransformPoint(new Vector3(val5.topRight.x, num2, 0f));
					zero2 = m_Transform.TransformPoint(new Vector3(val5.topRight.x, num, 0f));
					DrawRectangle(val2, val3, zero2, zero, Color32.op_Implicit(val4));
				}
				else if (flag && lineNumber != textInfo.characterInfo[num3 + 1].lineNumber)
				{
					flag = false;
					val3 = m_Transform.TransformPoint(new Vector3(val3.x, num, 0f));
					val2 = m_Transform.TransformPoint(new Vector3(val2.x, num2, 0f));
					zero = m_Transform.TransformPoint(new Vector3(val5.topRight.x, num2, 0f));
					zero2 = m_Transform.TransformPoint(new Vector3(val5.topRight.x, num, 0f));
					DrawRectangle(val2, val3, zero2, zero, Color32.op_Implicit(val4));
					num = float.NegativeInfinity;
					num2 = float.PositiveInfinity;
				}
			}
		}
	}

	private void DrawLineBounds()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Expected O, but got Unknown
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Expected O, but got Unknown
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		int lineCount = m_TextInfo.lineCount;
		for (int i = 0; i < lineCount; i++)
		{
			TMP_LineInfo val = m_TextInfo.lineInfo[i];
			TMP_CharacterInfo val2 = m_TextInfo.characterInfo[val.firstCharacterIndex];
			TMP_CharacterInfo val3 = m_TextInfo.characterInfo[val.lastCharacterIndex];
			if ((val.characterCount != 1 || (val2.character != '\n' && val2.character != '\v' && val2.character != '\u2028' && val2.character != '\u2029')) && i <= m_TextComponent.maxVisibleLines && (((int)m_TextComponent.overflowMode != 5 || val2.pageNumber + 1 == m_TextComponent.pageToDisplay) ? true : false))
			{
				float x = val2.bottomLeft.x;
				float x2 = val3.topRight.x;
				float ascender = val.ascender;
				float baseline = val.baseline;
				float descender = val.descender;
				float num = 12f;
				DrawDottedRectangle(m_Transform.TransformPoint(Vector2.op_Implicit(val.lineExtents.min)), m_Transform.TransformPoint(Vector2.op_Implicit(val.lineExtents.max)), Color.green, 4f);
				Vector3 val4 = m_Transform.TransformPoint(new Vector3(x, ascender, 0f));
				Vector3 val5 = m_Transform.TransformPoint(new Vector3(x2, ascender, 0f));
				Handles.color = Color.yellow;
				Handles.DrawDottedLine(val4, val5, num);
				Vector3 val6 = m_Transform.TransformPoint(new Vector3(x, baseline, 0f));
				Vector3 val7 = m_Transform.TransformPoint(new Vector3(x2, baseline, 0f));
				Handles.color = Color.yellow;
				Handles.DrawDottedLine(val6, val7, num);
				Vector3 val8 = m_Transform.TransformPoint(new Vector3(x, descender, 0f));
				Vector3 val9 = m_Transform.TransformPoint(new Vector3(x2, descender, 0f));
				Handles.color = Color.yellow;
				Handles.DrawDottedLine(val8, val9, num);
				if (m_HandleSize < 1f)
				{
					GUIStyle val10 = new GUIStyle();
					val10.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
					val10.fontSize = 12;
					val10.fixedWidth = 200f;
					val10.fixedHeight = 20f;
					Vector3 val11 = m_Transform.TransformPoint(new Vector3(x, ascender, 0f));
					val10.padding = new RectOffset(0, 10, 0, 5);
					val10.alignment = (TextAnchor)5;
					Handles.Label(val11, "Ascent Line", val10);
					val11 = m_Transform.TransformPoint(new Vector3(x, baseline, 0f));
					Handles.Label(val11, "Base Line", val10);
					val11 = m_Transform.TransformPoint(new Vector3(x, descender, 0f));
					Handles.Label(val11, "Descent Line", val10);
				}
			}
		}
	}

	private void DrawBounds()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		Bounds bounds = m_TextComponent.bounds;
		Vector3 bL = m_TextComponent.transform.position + ((Bounds)(ref bounds)).min;
		Vector3 tR = m_TextComponent.transform.position + ((Bounds)(ref bounds)).max;
		DrawRectangle(bL, tR, new Color(1f, 0.5f, 0f));
	}

	private void DrawTextBounds()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		Bounds textBounds = m_TextComponent.textBounds;
		Vector3 bL = m_TextComponent.transform.position + (((Bounds)(ref textBounds)).center - ((Bounds)(ref textBounds)).extents);
		Vector3 tR = m_TextComponent.transform.position + (((Bounds)(ref textBounds)).center + ((Bounds)(ref textBounds)).extents);
		DrawRectangle(bL, tR, new Color(0f, 0.5f, 0.5f));
	}

	private void DrawRectangle(Vector3 BL, Vector3 TR, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		Gizmos.color = color;
		Gizmos.DrawLine(new Vector3(BL.x, BL.y, 0f), new Vector3(BL.x, TR.y, 0f));
		Gizmos.DrawLine(new Vector3(BL.x, TR.y, 0f), new Vector3(TR.x, TR.y, 0f));
		Gizmos.DrawLine(new Vector3(TR.x, TR.y, 0f), new Vector3(TR.x, BL.y, 0f));
		Gizmos.DrawLine(new Vector3(TR.x, BL.y, 0f), new Vector3(BL.x, BL.y, 0f));
	}

	private void DrawDottedRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Handles.color = color;
		Handles.DrawDottedLine(bottomLeft, new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), size);
		Handles.DrawDottedLine(new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), topRight, size);
		Handles.DrawDottedLine(topRight, new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), size);
		Handles.DrawDottedLine(new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), bottomLeft, size);
	}

	private void DrawSolidRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		Handles.color = color;
		Rect val = default(Rect);
		((Rect)(ref val))._002Ector(Vector2.op_Implicit(bottomLeft), Vector2.op_Implicit(topRight - bottomLeft));
		Handles.DrawSolidRectangleWithOutline(val, color, Color.black);
	}

	private void DrawSquare(Vector3 position, float size, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		Handles.color = color;
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(position.x - size, position.y - size, position.z);
		Vector3 val2 = default(Vector3);
		((Vector3)(ref val2))._002Ector(position.x - size, position.y + size, position.z);
		Vector3 val3 = default(Vector3);
		((Vector3)(ref val3))._002Ector(position.x + size, position.y + size, position.z);
		Vector3 val4 = default(Vector3);
		((Vector3)(ref val4))._002Ector(position.x + size, position.y - size, position.z);
		Handles.DrawLine(val, val2);
		Handles.DrawLine(val2, val3);
		Handles.DrawLine(val3, val4);
		Handles.DrawLine(val4, val);
	}

	private void DrawCrosshair(Vector3 position, float size, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		Handles.color = color;
		Handles.DrawLine(new Vector3(position.x - size, position.y, position.z), new Vector3(position.x + size, position.y, position.z));
		Handles.DrawLine(new Vector3(position.x, position.y - size, position.z), new Vector3(position.x, position.y + size, position.z));
	}

	private void DrawRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Gizmos.color = color;
		Gizmos.DrawLine(bl, tl);
		Gizmos.DrawLine(tl, tr);
		Gizmos.DrawLine(tr, br);
		Gizmos.DrawLine(br, bl);
	}

	private void DrawDottedRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		Camera current = Camera.current;
		float num = (current.WorldToScreenPoint(br).x - current.WorldToScreenPoint(bl).x) / 75f;
		Handles.color = color;
		Handles.DrawDottedLine(bl, tl, num);
		Handles.DrawDottedLine(tl, tr, num);
		Handles.DrawDottedLine(tr, br, num);
		Handles.DrawDottedLine(br, bl, num);
	}
}
