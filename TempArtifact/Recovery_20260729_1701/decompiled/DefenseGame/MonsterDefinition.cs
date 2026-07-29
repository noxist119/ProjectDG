using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

[Serializable]
public class MonsterDefinition
{
	public string id;

	public string displayName;

	[TextArea]
	public string description;

	public CharacterGrade grade;

	public MonsterRole role;

	public MonsterThreatLevel threatLevel = MonsterThreatLevel.Regular;

	public int minRound = 1;

	public string rosterSourceId;

	public int rosterIndex;

	public int variantIndex;

	public Color accentColor = Color.white;

	public GameObject prefab;

	public bool isBoss;

	public float visualScale = 1f;

	public int rewardGold = 5;

	public CombatStats stats = new CombatStats();

	public AttackBehavior attackBehavior = new AttackBehavior();

	public List<SkillDefinition> skills = new List<SkillDefinition>();

	public bool IsMajorBoss => threatLevel == MonsterThreatLevel.Boss || isBoss;

	public bool IsBossLike => threatLevel == MonsterThreatLevel.MidBoss || IsMajorBoss;
}
