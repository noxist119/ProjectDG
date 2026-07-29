using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace TMPro
{
	public class TMP_TextEventHandler : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		[Serializable]
		public class CharacterSelectionEvent : UnityEvent<char, int>
		{
		}

		[Serializable]
		public class SpriteSelectionEvent : UnityEvent<char, int>
		{
		}

		[Serializable]
		public class WordSelectionEvent : UnityEvent<string, int, int>
		{
		}

		[Serializable]
		public class LineSelectionEvent : UnityEvent<string, int, int>
		{
		}

		[Serializable]
		public class LinkSelectionEvent : UnityEvent<string, string, int>
		{
		}

		[SerializeField]
		private CharacterSelectionEvent m_OnCharacterSelection = new CharacterSelectionEvent();

		[SerializeField]
		private SpriteSelectionEvent m_OnSpriteSelection = new SpriteSelectionEvent();

		[SerializeField]
		private WordSelectionEvent m_OnWordSelection = new WordSelectionEvent();

		[SerializeField]
		private LineSelectionEvent m_OnLineSelection = new LineSelectionEvent();

		[SerializeField]
		private LinkSelectionEvent m_OnLinkSelection = new LinkSelectionEvent();

		private TMP_Text m_TextComponent;

		private Camera m_Camera;

		private Canvas m_Canvas;

		private int m_selectedLink = -1;

		private int m_lastCharIndex = -1;

		private int m_lastWordIndex = -1;

		private int m_lastLineIndex = -1;

		public CharacterSelectionEvent onCharacterSelection
		{
			get
			{
				return m_OnCharacterSelection;
			}
			set
			{
				m_OnCharacterSelection = value;
			}
		}

		public SpriteSelectionEvent onSpriteSelection
		{
			get
			{
				return m_OnSpriteSelection;
			}
			set
			{
				m_OnSpriteSelection = value;
			}
		}

		public WordSelectionEvent onWordSelection
		{
			get
			{
				return m_OnWordSelection;
			}
			set
			{
				m_OnWordSelection = value;
			}
		}

		public LineSelectionEvent onLineSelection
		{
			get
			{
				return m_OnLineSelection;
			}
			set
			{
				m_OnLineSelection = value;
			}
		}

		public LinkSelectionEvent onLinkSelection
		{
			get
			{
				return m_OnLinkSelection;
			}
			set
			{
				m_OnLinkSelection = value;
			}
		}

		private void Awake()
		{
			m_TextComponent = base.gameObject.GetComponent<TMP_Text>();
			if (((object)m_TextComponent).GetType() == typeof(TextMeshProUGUI))
			{
				m_Canvas = base.gameObject.GetComponentInParent<Canvas>();
				if (m_Canvas != null)
				{
					if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
					{
						m_Camera = null;
					}
					else
					{
						m_Camera = m_Canvas.worldCamera;
					}
				}
			}
			else
			{
				m_Camera = Camera.main;
			}
		}

		private void LateUpdate()
		{
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Invalid comparison between Unknown and I4
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Invalid comparison between Unknown and I4
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0216: Unknown result type (might be due to invalid IL or missing references)
			//IL_021d: Unknown result type (might be due to invalid IL or missing references)
			if (TMP_TextUtilities.IsIntersectingRectTransform(m_TextComponent.rectTransform, Input.mousePosition, m_Camera))
			{
				int charIndex = TMP_TextUtilities.FindIntersectingCharacter(m_TextComponent, Input.mousePosition, m_Camera, true);
				if (charIndex != -1 && charIndex != m_lastCharIndex)
				{
					m_lastCharIndex = charIndex;
					TMP_TextElementType elementType = m_TextComponent.textInfo.characterInfo[charIndex].elementType;
					if ((int)elementType == 0)
					{
						SendOnCharacterSelection(m_TextComponent.textInfo.characterInfo[charIndex].character, charIndex);
					}
					else if ((int)elementType == 1)
					{
						SendOnSpriteSelection(m_TextComponent.textInfo.characterInfo[charIndex].character, charIndex);
					}
				}
				int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextComponent, Input.mousePosition, m_Camera);
				if (wordIndex != -1 && wordIndex != m_lastWordIndex)
				{
					m_lastWordIndex = wordIndex;
					TMP_WordInfo wInfo = m_TextComponent.textInfo.wordInfo[wordIndex];
					SendOnWordSelection(((TMP_WordInfo)(ref wInfo)).GetWord(), wInfo.firstCharacterIndex, wInfo.characterCount);
				}
				int lineIndex = TMP_TextUtilities.FindIntersectingLine(m_TextComponent, Input.mousePosition, m_Camera);
				if (lineIndex != -1 && lineIndex != m_lastLineIndex)
				{
					m_lastLineIndex = lineIndex;
					TMP_LineInfo lineInfo = m_TextComponent.textInfo.lineInfo[lineIndex];
					char[] buffer = new char[lineInfo.characterCount];
					for (int i = 0; i < lineInfo.characterCount && i < m_TextComponent.textInfo.characterInfo.Length; i++)
					{
						buffer[i] = m_TextComponent.textInfo.characterInfo[i + lineInfo.firstCharacterIndex].character;
					}
					string lineText = new string(buffer);
					SendOnLineSelection(lineText, lineInfo.firstCharacterIndex, lineInfo.characterCount);
				}
				int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextComponent, Input.mousePosition, m_Camera);
				if (linkIndex != -1 && linkIndex != m_selectedLink)
				{
					m_selectedLink = linkIndex;
					TMP_LinkInfo linkInfo = m_TextComponent.textInfo.linkInfo[linkIndex];
					SendOnLinkSelection(((TMP_LinkInfo)(ref linkInfo)).GetLinkID(), ((TMP_LinkInfo)(ref linkInfo)).GetLinkText(), linkIndex);
				}
			}
			else
			{
				m_selectedLink = -1;
				m_lastCharIndex = -1;
				m_lastWordIndex = -1;
				m_lastLineIndex = -1;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
		}

		public void OnPointerExit(PointerEventData eventData)
		{
		}

		private void SendOnCharacterSelection(char character, int characterIndex)
		{
			if (onCharacterSelection != null)
			{
				onCharacterSelection.Invoke(character, characterIndex);
			}
		}

		private void SendOnSpriteSelection(char character, int characterIndex)
		{
			if (onSpriteSelection != null)
			{
				onSpriteSelection.Invoke(character, characterIndex);
			}
		}

		private void SendOnWordSelection(string word, int charIndex, int length)
		{
			if (onWordSelection != null)
			{
				onWordSelection.Invoke(word, charIndex, length);
			}
		}

		private void SendOnLineSelection(string line, int charIndex, int length)
		{
			if (onLineSelection != null)
			{
				onLineSelection.Invoke(line, charIndex, length);
			}
		}

		private void SendOnLinkSelection(string linkID, string linkText, int linkIndex)
		{
			if (onLinkSelection != null)
			{
				onLinkSelection.Invoke(linkID, linkText, linkIndex);
			}
		}
	}
}
