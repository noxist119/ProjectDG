using System;
using UnityEngine;

namespace DefenseGame;

[Serializable]
public class CharacterPresentationOverride
{
	public string characterId;

	public GameObject prefab;

	public bool overrideColor;

	public Color accentColor = Color.white;
}
