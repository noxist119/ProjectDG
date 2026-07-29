using System;
using UnityEditor;
using UnityEngine;

namespace TMPro.Examples
{
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
			if ((UnityEngine.Object)(object)m_TextComponent == null)
			{
				m_TextComponent = GetComponent<TMP_Text>();
				if ((UnityEngine.Object)(object)m_TextComponent == null)
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
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0157: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0170: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0201: Unknown result type (might be due to invalid IL or missing references)
			//IL_0256: Unknown result type (might be due to invalid IL or missing references)
			//IL_019b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_0266: Unknown result type (might be due to invalid IL or missing references)
			//IL_027a: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0303: Unknown result type (might be due to invalid IL or missing references)
			//IL_0317: Unknown result type (might be due to invalid IL or missing references)
			//IL_0390: Unknown result type (might be due to invalid IL or missing references)
			//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
			int characterCount = m_TextInfo.characterCount;
			for (int i = 0; i < characterCount; i++)
			{
				TMP_CharacterInfo characterInfo = m_TextInfo.characterInfo[i];
				bool isCharacterVisible = i < m_TextComponent.maxVisibleCharacters && characterInfo.lineNumber < m_TextComponent.maxVisibleLines && i >= m_TextComponent.firstVisibleCharacter;
				if ((int)m_TextComponent.overflowMode == 5)
				{
					isCharacterVisible = isCharacterVisible && characterInfo.pageNumber + 1 == m_TextComponent.pageToDisplay;
				}
				if (!isCharacterVisible)
				{
					continue;
				}
				float dottedLineSize = 6f;
				Vector3 bottomLeft = m_Transform.TransformPoint(characterInfo.bottomLeft);
				Vector3 topLeft = m_Transform.TransformPoint(new Vector3(characterInfo.topLeft.x, characterInfo.topLeft.y, 0f));
				Vector3 topRight = m_Transform.TransformPoint(characterInfo.topRight);
				Vector3 bottomRight = m_Transform.TransformPoint(new Vector3(characterInfo.bottomRight.x, characterInfo.bottomRight.y, 0f));
				if (characterInfo.isVisible)
				{
					Color color = Color.green;
					DrawDottedRectangle(bottomLeft, topRight, color);
				}
				else
				{
					Color color2 = Color.grey;
					float whiteSpaceAdvance = ((Math.Abs(characterInfo.origin - characterInfo.xAdvance) > 0.01f) ? characterInfo.xAdvance : (characterInfo.origin + (characterInfo.ascender - characterInfo.descender) * 0.03f));
					DrawDottedRectangle(m_Transform.TransformPoint(new Vector3(characterInfo.origin, characterInfo.descender, 0f)), m_Transform.TransformPoint(new Vector3(whiteSpaceAdvance, characterInfo.ascender, 0f)), color2, 4f);
				}
				float origin = characterInfo.origin;
				float advance = characterInfo.xAdvance;
				float ascentline = characterInfo.ascender;
				float baseline = characterInfo.baseLine;
				float descentline = characterInfo.descender;
				Vector3 ascentlineStart = m_Transform.TransformPoint(new Vector3(origin, ascentline, 0f));
				Vector3 ascentlineEnd = m_Transform.TransformPoint(new Vector3(advance, ascentline, 0f));
				Handles.color = Color.cyan;
				Handles.DrawDottedLine(ascentlineStart, ascentlineEnd, dottedLineSize);
				float capline = (((UnityEngine.Object)(object)characterInfo.fontAsset == null) ? 0f : (baseline + characterInfo.fontAsset.faceInfo.capLine * characterInfo.scale));
				Vector3 capHeightStart = new Vector3(topLeft.x, m_Transform.TransformPoint(new Vector3(0f, capline, 0f)).y, 0f);
				Vector3 capHeightEnd = new Vector3(topRight.x, m_Transform.TransformPoint(new Vector3(0f, capline, 0f)).y, 0f);
				float meanline = (((UnityEngine.Object)(object)characterInfo.fontAsset == null) ? 0f : (baseline + characterInfo.fontAsset.faceInfo.meanLine * characterInfo.scale));
				Vector3 meanlineStart = new Vector3(topLeft.x, m_Transform.TransformPoint(new Vector3(0f, meanline, 0f)).y, 0f);
				Vector3 meanlineEnd = new Vector3(topRight.x, m_Transform.TransformPoint(new Vector3(0f, meanline, 0f)).y, 0f);
				if (characterInfo.isVisible)
				{
					Handles.color = Color.cyan;
					Handles.DrawDottedLine(capHeightStart, capHeightEnd, dottedLineSize);
					Handles.color = Color.cyan;
					Handles.DrawDottedLine(meanlineStart, meanlineEnd, dottedLineSize);
				}
				Vector3 baselineStart = m_Transform.TransformPoint(new Vector3(origin, baseline, 0f));
				Vector3 baselineEnd = m_Transform.TransformPoint(new Vector3(advance, baseline, 0f));
				Handles.color = Color.cyan;
				Handles.DrawDottedLine(baselineStart, baselineEnd, dottedLineSize);
				Vector3 descentlineStart = m_Transform.TransformPoint(new Vector3(origin, descentline, 0f));
				Vector3 descentlineEnd = m_Transform.TransformPoint(new Vector3(advance, descentline, 0f));
				Handles.color = Color.cyan;
				Handles.DrawDottedLine(descentlineStart, descentlineEnd, dottedLineSize);
				Vector3 originPosition = m_Transform.TransformPoint(new Vector3(origin, baseline, 0f));
				DrawCrosshair(originPosition, 0.05f / m_ScaleMultiplier, Color.cyan);
				Vector3 advancePosition = m_Transform.TransformPoint(new Vector3(advance, baseline, 0f));
				DrawSquare(advancePosition, 0.025f / m_ScaleMultiplier, Color.yellow);
				DrawCrosshair(advancePosition, 0.0125f / m_ScaleMultiplier, Color.yellow);
				if (m_HandleSize < 0.5f)
				{
					GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"));
					style.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
					style.fontSize = 12;
					style.fixedWidth = 200f;
					style.fixedHeight = 20f;
					float center = (origin + advance) / 2f;
					Vector3 labelPosition = m_Transform.TransformPoint(new Vector3(center, ascentline, 0f));
					style.alignment = TextAnchor.UpperCenter;
					Handles.Label(labelPosition, "Ascent Line", style);
					labelPosition = m_Transform.TransformPoint(new Vector3(center, baseline, 0f));
					Handles.Label(labelPosition, "Base Line", style);
					labelPosition = m_Transform.TransformPoint(new Vector3(center, descentline, 0f));
					Handles.Label(labelPosition, "Descent Line", style);
					if (characterInfo.isVisible)
					{
						labelPosition = m_Transform.TransformPoint(new Vector3(center, capline, 0f));
						style.alignment = TextAnchor.UpperCenter;
						Handles.Label(labelPosition, "Cap Line", style);
						labelPosition = m_Transform.TransformPoint(new Vector3(center, meanline, 0f));
						style.alignment = TextAnchor.UpperCenter;
						Handles.Label(labelPosition, "Mean Line", style);
						labelPosition = m_Transform.TransformPoint(new Vector3(origin, baseline, 0f));
						style.alignment = TextAnchor.UpperRight;
						Handles.Label(labelPosition, "Origin ", style);
						labelPosition = m_Transform.TransformPoint(new Vector3(advance, baseline, 0f));
						style.alignment = TextAnchor.UpperLeft;
						Handles.Label(labelPosition, "  Advance", style);
					}
				}
			}
		}

		private void DrawWordBounds()
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
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
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_024d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_031d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0342: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < m_TextInfo.wordCount; i++)
			{
				TMP_WordInfo wInfo = m_TextInfo.wordInfo[i];
				bool isBeginRegion = false;
				Vector3 bottomLeft = Vector3.zero;
				Vector3 topLeft = Vector3.zero;
				Vector3 bottomRight = Vector3.zero;
				Vector3 topRight = Vector3.zero;
				float maxAscender = float.NegativeInfinity;
				float minDescender = float.PositiveInfinity;
				Color wordColor = Color.green;
				for (int j = 0; j < wInfo.characterCount; j++)
				{
					int characterIndex = wInfo.firstCharacterIndex + j;
					TMP_CharacterInfo currentCharInfo = m_TextInfo.characterInfo[characterIndex];
					int currentLine = currentCharInfo.lineNumber;
					bool isCharacterVisible = ((characterIndex <= m_TextComponent.maxVisibleCharacters && currentCharInfo.lineNumber <= m_TextComponent.maxVisibleLines && ((int)m_TextComponent.overflowMode != 5 || currentCharInfo.pageNumber + 1 == m_TextComponent.pageToDisplay)) ? true : false);
					maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
					minDescender = Mathf.Min(minDescender, currentCharInfo.descender);
					if (!isBeginRegion && isCharacterVisible)
					{
						isBeginRegion = true;
						bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0f);
						topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0f);
						if (wInfo.characterCount == 1)
						{
							isBeginRegion = false;
							topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0f));
							bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0f));
							bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0f));
							topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0f));
							DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);
						}
					}
					if (isBeginRegion && j == wInfo.characterCount - 1)
					{
						isBeginRegion = false;
						topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0f));
						bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0f));
						bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0f));
						topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0f));
						DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);
					}
					else if (isBeginRegion && currentLine != m_TextInfo.characterInfo[characterIndex + 1].lineNumber)
					{
						isBeginRegion = false;
						topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0f));
						bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0f));
						bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0f));
						topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0f));
						DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);
						maxAscender = float.NegativeInfinity;
						minDescender = float.PositiveInfinity;
					}
				}
			}
		}

		private void DrawLinkBounds()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
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
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0200: Unknown result type (might be due to invalid IL or missing references)
			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_025f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0284: Unknown result type (might be due to invalid IL or missing references)
			//IL_0332: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			TMP_TextInfo textInfo = m_TextComponent.textInfo;
			for (int i = 0; i < textInfo.linkCount; i++)
			{
				TMP_LinkInfo linkInfo = textInfo.linkInfo[i];
				bool isBeginRegion = false;
				Vector3 bottomLeft = Vector3.zero;
				Vector3 topLeft = Vector3.zero;
				Vector3 bottomRight = Vector3.zero;
				Vector3 topRight = Vector3.zero;
				float maxAscender = float.NegativeInfinity;
				float minDescender = float.PositiveInfinity;
				Color32 linkColor = Color.cyan;
				for (int j = 0; j < linkInfo.linkTextLength; j++)
				{
					int characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
					TMP_CharacterInfo currentCharInfo = textInfo.characterInfo[characterIndex];
					int currentLine = currentCharInfo.lineNumber;
					bool isCharacterVisible = ((characterIndex <= m_TextComponent.maxVisibleCharacters && currentCharInfo.lineNumber <= m_TextComponent.maxVisibleLines && ((int)m_TextComponent.overflowMode != 5 || currentCharInfo.pageNumber + 1 == m_TextComponent.pageToDisplay)) ? true : false);
					maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
					minDescender = Mathf.Min(minDescender, currentCharInfo.descender);
					if (!isBeginRegion && isCharacterVisible)
					{
						isBeginRegion = true;
						bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0f);
						topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0f);
						if (linkInfo.linkTextLength == 1)
						{
							isBeginRegion = false;
							topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0f));
							bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0f));
							bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0f));
							topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0f));
							DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);
						}
					}
					if (isBeginRegion && j == linkInfo.linkTextLength - 1)
					{
						isBeginRegion = false;
						topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0f));
						bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0f));
						bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0f));
						topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0f));
						DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);
					}
					else if (isBeginRegion && currentLine != textInfo.characterInfo[characterIndex + 1].lineNumber)
					{
						isBeginRegion = false;
						topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0f));
						bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0f));
						bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0f));
						topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0f));
						DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);
						maxAscender = float.NegativeInfinity;
						minDescender = float.PositiveInfinity;
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
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			int lineCount = m_TextInfo.lineCount;
			for (int i = 0; i < lineCount; i++)
			{
				TMP_LineInfo lineInfo = m_TextInfo.lineInfo[i];
				TMP_CharacterInfo firstCharacterInfo = m_TextInfo.characterInfo[lineInfo.firstCharacterIndex];
				TMP_CharacterInfo lastCharacterInfo = m_TextInfo.characterInfo[lineInfo.lastCharacterIndex];
				if ((lineInfo.characterCount != 1 || (firstCharacterInfo.character != '\n' && firstCharacterInfo.character != '\v' && firstCharacterInfo.character != '\u2028' && firstCharacterInfo.character != '\u2029')) && i <= m_TextComponent.maxVisibleLines && (((int)m_TextComponent.overflowMode != 5 || firstCharacterInfo.pageNumber + 1 == m_TextComponent.pageToDisplay) ? true : false))
				{
					float lineBottomLeft = firstCharacterInfo.bottomLeft.x;
					float lineTopRight = lastCharacterInfo.topRight.x;
					float ascentline = lineInfo.ascender;
					float baseline = lineInfo.baseline;
					float descentline = lineInfo.descender;
					float dottedLineSize = 12f;
					DrawDottedRectangle(m_Transform.TransformPoint(lineInfo.lineExtents.min), m_Transform.TransformPoint(lineInfo.lineExtents.max), Color.green, 4f);
					Vector3 ascentlineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, ascentline, 0f));
					Vector3 ascentlineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, ascentline, 0f));
					Handles.color = Color.yellow;
					Handles.DrawDottedLine(ascentlineStart, ascentlineEnd, dottedLineSize);
					Vector3 baseLineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, baseline, 0f));
					Vector3 baseLineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, baseline, 0f));
					Handles.color = Color.yellow;
					Handles.DrawDottedLine(baseLineStart, baseLineEnd, dottedLineSize);
					Vector3 descentLineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, descentline, 0f));
					Vector3 descentLineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, descentline, 0f));
					Handles.color = Color.yellow;
					Handles.DrawDottedLine(descentLineStart, descentLineEnd, dottedLineSize);
					if (m_HandleSize < 1f)
					{
						GUIStyle style = new GUIStyle();
						style.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
						style.fontSize = 12;
						style.fixedWidth = 200f;
						style.fixedHeight = 20f;
						Vector3 labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, ascentline, 0f));
						style.padding = new RectOffset(0, 10, 0, 5);
						style.alignment = TextAnchor.MiddleRight;
						Handles.Label(labelPosition, "Ascent Line", style);
						labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, baseline, 0f));
						Handles.Label(labelPosition, "Base Line", style);
						labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, descentline, 0f));
						Handles.Label(labelPosition, "Descent Line", style);
					}
				}
			}
		}

		private void DrawBounds()
		{
			Bounds meshBounds = m_TextComponent.bounds;
			Vector3 bottomLeft = m_TextComponent.transform.position + meshBounds.min;
			Vector3 topRight = m_TextComponent.transform.position + meshBounds.max;
			DrawRectangle(bottomLeft, topRight, new Color(1f, 0.5f, 0f));
		}

		private void DrawTextBounds()
		{
			Bounds textBounds = m_TextComponent.textBounds;
			Vector3 bottomLeft = m_TextComponent.transform.position + (textBounds.center - textBounds.extents);
			Vector3 topRight = m_TextComponent.transform.position + (textBounds.center + textBounds.extents);
			DrawRectangle(bottomLeft, topRight, new Color(0f, 0.5f, 0.5f));
		}

		private void DrawRectangle(Vector3 BL, Vector3 TR, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawLine(new Vector3(BL.x, BL.y, 0f), new Vector3(BL.x, TR.y, 0f));
			Gizmos.DrawLine(new Vector3(BL.x, TR.y, 0f), new Vector3(TR.x, TR.y, 0f));
			Gizmos.DrawLine(new Vector3(TR.x, TR.y, 0f), new Vector3(TR.x, BL.y, 0f));
			Gizmos.DrawLine(new Vector3(TR.x, BL.y, 0f), new Vector3(BL.x, BL.y, 0f));
		}

		private void DrawDottedRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5f)
		{
			Handles.color = color;
			Handles.DrawDottedLine(bottomLeft, new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), size);
			Handles.DrawDottedLine(new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), topRight, size);
			Handles.DrawDottedLine(topRight, new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), size);
			Handles.DrawDottedLine(new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), bottomLeft, size);
		}

		private void DrawSolidRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5f)
		{
			Handles.color = color;
			Rect rect = new Rect(bottomLeft, topRight - bottomLeft);
			Handles.DrawSolidRectangleWithOutline(rect, color, Color.black);
		}

		private void DrawSquare(Vector3 position, float size, Color color)
		{
			Handles.color = color;
			Vector3 bottomLeft = new Vector3(position.x - size, position.y - size, position.z);
			Vector3 topLeft = new Vector3(position.x - size, position.y + size, position.z);
			Vector3 topRight = new Vector3(position.x + size, position.y + size, position.z);
			Vector3 bottomRight = new Vector3(position.x + size, position.y - size, position.z);
			Handles.DrawLine(bottomLeft, topLeft);
			Handles.DrawLine(topLeft, topRight);
			Handles.DrawLine(topRight, bottomRight);
			Handles.DrawLine(bottomRight, bottomLeft);
		}

		private void DrawCrosshair(Vector3 position, float size, Color color)
		{
			Handles.color = color;
			Handles.DrawLine(new Vector3(position.x - size, position.y, position.z), new Vector3(position.x + size, position.y, position.z));
			Handles.DrawLine(new Vector3(position.x, position.y - size, position.z), new Vector3(position.x, position.y + size, position.z));
		}

		private void DrawRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawLine(bl, tl);
			Gizmos.DrawLine(tl, tr);
			Gizmos.DrawLine(tr, br);
			Gizmos.DrawLine(br, bl);
		}

		private void DrawDottedRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
		{
			Camera cam = Camera.current;
			float dotSpacing = (cam.WorldToScreenPoint(br).x - cam.WorldToScreenPoint(bl).x) / 75f;
			Handles.color = color;
			Handles.DrawDottedLine(bl, tl, dotSpacing);
			Handles.DrawDottedLine(tl, tr, dotSpacing);
			Handles.DrawDottedLine(tr, br, dotSpacing);
			Handles.DrawDottedLine(br, bl, dotSpacing);
		}
	}
}
