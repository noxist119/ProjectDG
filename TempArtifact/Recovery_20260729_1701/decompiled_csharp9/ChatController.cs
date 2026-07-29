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
		DateTime timeNow = DateTime.Now;
		string formattedInput = "[<#FFFF80>" + timeNow.Hour.ToString("d2") + ":" + timeNow.Minute.ToString("d2") + ":" + timeNow.Second.ToString("d2") + "</color>] " + newText;
		if ((UnityEngine.Object)(object)ChatDisplayOutput != null)
		{
			if (ChatDisplayOutput.text == string.Empty)
			{
				ChatDisplayOutput.text = formattedInput;
			}
			else
			{
				TMP_Text chatDisplayOutput = ChatDisplayOutput;
				chatDisplayOutput.text = chatDisplayOutput.text + "\n" + formattedInput;
			}
		}
		ChatInputField.ActivateInputField();
		ChatScrollbar.value = 0f;
	}
}
