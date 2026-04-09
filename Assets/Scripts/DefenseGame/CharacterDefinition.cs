using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    [Serializable]
    public class CharacterDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public CharacterGrade grade;
        public CharacterRole role;
        public Color accentColor = Color.white;
        public GameObject prefab;
        public CombatStats stats = new CombatStats();
        public AttackBehavior attackBehavior = new AttackBehavior();
        public List<SkillDefinition> skills = new List<SkillDefinition>();
        public int mergeValue = 1;
    }
}
