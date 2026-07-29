using System;
using System.Collections;
using UnityEngine;

namespace TMPro.Examples;

public class TextConsoleSimulator : MonoBehaviour
{
	private TMP_Text m_TextComponent;

	private bool hasTextChanged;

	private void Awake()
	{
		m_TextComponent = ((Component)this).gameObject.GetComponent<TMP_Text>();
	}

	private void Start()
	{
		((MonoBehaviour)this).StartCoroutine(RevealCharacters(m_TextComponent));
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
		hasTextChanged = true;
	}

	private IEnumerator RevealCharacters(TMP_Text textComponent)
	{
		textComponent.ForceMeshUpdate(false, false);
		TMP_TextInfo textInfo = textComponent.textInfo;
		int totalVisibleCharacters = textInfo.characterCount;
		int visibleCount = 0;
		while (true)
		{
			if (hasTextChanged)
			{
				totalVisibleCharacters = textInfo.characterCount;
				hasTextChanged = false;
			}
			if (visibleCount > totalVisibleCharacters)
			{
				yield return (object)new WaitForSeconds(1f);
				visibleCount = 0;
			}
			textComponent.maxVisibleCharacters = visibleCount;
			visibleCount++;
			yield return null;
		}
	}

	private IEnumerator RevealWords(TMP_Text textComponent)
	{
		textComponent.ForceMeshUpdate(false, false);
		int totalWordCount = textComponent.textInfo.wordCount;
		int totalVisibleCharacters = textComponent.textInfo.characterCount;
		int counter = 0;
		int visibleCount = 0;
		while (true)
		{
			int currentWord = counter % (totalWordCount + 1);
			if (currentWord == 0)
			{
				visibleCount = 0;
			}
			else if (currentWord < totalWordCount)
			{
				visibleCount = textComponent.textInfo.wordInfo[currentWord - 1].lastCharacterIndex + 1;
			}
			else if (currentWord == totalWordCount)
			{
				visibleCount = totalVisibleCharacters;
			}
			textComponent.maxVisibleCharacters = visibleCount;
			if (visibleCount >= totalVisibleCharacters)
			{
				yield return (object)new WaitForSeconds(1f);
			}
			counter++;
			yield return (object)new WaitForSeconds(0.1f);
		}
	}
}
