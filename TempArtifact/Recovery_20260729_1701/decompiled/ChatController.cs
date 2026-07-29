using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChatController : MonoBehaviour
{
	public TMP_InputField ChatInputField;

	public TMP_Text ChatDisplayOutput;

	public Scrollbar ChatScrollbar;

	private void OnEnable()
	{
		((UnityEvent<string>)(object)ChatInputField.onSubmit).AddListener((UnityAction<string>)AddToChatOutput);
	}

	private void OnDisable()
	{
		((UnityEvent<string>)(object)ChatInputField.onSubmit).RemoveListener((UnityAction<string>)AddToChatOutput);
	}

	private void AddToChatOutput(string newText)
	{
		ChatInputField.text = string.Empty;
		DateTime now = DateTime.Now;
		string text = "[<#FFFF80>" + now.Hour.ToString("d2") + ":" + now.Minute.ToString("d2") + ":" + now.Second.ToString("d2") + "</color>] " + newText;
		if ((Object)(object)ChatDisplayOutput != (Object)null)
		{
			if (ChatDisplayOutput.text == string.Empty)
			{
				ChatDisplayOutput.text = text;
			}
			else
			{
				TMP_Text chatDisplayOutput = ChatDisplayOutput;
				chatDisplayOutput.text = chatDisplayOutput.text + "\n" + text;
			}
		}
		ChatInputField.ActivateInputField();
		ChatScrollbar.value = 0f;
	}
}
