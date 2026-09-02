using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class BoardSynergySystem : MonoBehaviour
    {
        [SerializeField] private DefenseGameController gameController;
        [SerializeField] private DefenseBoardManager boardManager;
        [SerializeField] private Button summaryButton;
        [SerializeField] private Text summaryText;
        [SerializeField] private GameObject expandedRoot;
        [SerializeField] private Text expandedHeaderText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text[] titleTexts;
        [SerializeField] private Text[] detailTexts;
        [SerializeField] private Image[] accentImages;
        [SerializeField] private Image[] iconImages;

        private readonly Dictionary<CharacterRole, int> roleCounts = new Dictionary<CharacterRole, int>();
        private readonly Dictionary<CharacterGrade, int> gradeCounts = new Dictionary<CharacterGrade, int>();
        private readonly Dictionary<CharacterTag, int> tagCounts = new Dictionary<CharacterTag, int>();
        private readonly List<SynergyEntry> activeEntries = new List<SynergyEntry>();
        private readonly List<SynergyEntry> nearEntries = new List<SynergyEntry>();
        private bool subscribed;
        private bool isExpanded;

        private sealed class SynergyEntry
        {
            public string title;
            public string detail;
            public Color color;
            public bool locked;
            public int progress;
            public int target;
        }

        public void Configure(
            DefenseGameController controller,
            DefenseBoardManager board,
            Button toggleButton,
            Text summaryLabel,
            GameObject expandedPanel,
            Text expandedHeader,
            Text[] titles,
            Text[] details,
            Image[] accents,
            Image[] icons,
            Button closePanelButton)
        {
            Unsubscribe();
            gameController = controller;
            boardManager = board;
            summaryButton = toggleButton;
            summaryText = summaryLabel;
            expandedRoot = expandedPanel;
            expandedHeaderText = expandedHeader;
            titleTexts = titles;
            detailTexts = details;
            accentImages = accents;
            iconImages = icons;
            closeButton = closePanelButton;

            WireUi();
            SetExpanded(false);
            Subscribe();
            RecalculateSynergies();
        }

        public void Configure(
            DefenseGameController controller,
            DefenseBoardManager board,
            GameObject root,
            Text header,
            Text[] titles,
            Text[] details,
            Image[] accents)
        {
            Configure(controller, board, null, header, root, header, titles, details, accents, null, null);
        }

        public void Configure(
            DefenseGameController controller,
            DefenseBoardManager board,
            Button toggleButton,
            Text summaryLabel,
            GameObject expandedPanel,
            Text expandedHeader,
            Text[] titles,
            Text[] details,
            Image[] accents,
            Button closePanelButton)
        {
            Configure(controller, board, toggleButton, summaryLabel, expandedPanel, expandedHeader, titles, details, accents, null, closePanelButton);
        }

        private void OnEnable()
        {
            WireUi();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void WireUi()
        {
            if (summaryButton != null)
            {
                summaryButton.onClick.RemoveListener(ToggleExpanded);
                summaryButton.onClick.AddListener(ToggleExpanded);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseExpanded);
                closeButton.onClick.AddListener(CloseExpanded);
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (gameController != null)
            {
                gameController.OnStateChanged += HandleBoardStateChanged;
            }

            DefenderUnit.OnDefenderSpawned += HandleDefenderChanged;
            DefenderUnit.OnDefenderRemoved += HandleDefenderChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (gameController != null)
            {
                gameController.OnStateChanged -= HandleBoardStateChanged;
            }

            DefenderUnit.OnDefenderSpawned -= HandleDefenderChanged;
            DefenderUnit.OnDefenderRemoved -= HandleDefenderChanged;
            subscribed = false;
        }

        private void HandleBoardStateChanged()
        {
            RecalculateSynergies();
        }

        private void HandleDefenderChanged(DefenderUnit unit)
        {
            RecalculateSynergies();
        }

        private void ToggleExpanded()
        {
            SetExpanded(!isExpanded);
        }

        private void CloseExpanded()
        {
            SetExpanded(false);
        }

        private void SetExpanded(bool expanded)
        {
            isExpanded = expanded;
            if (expandedRoot != null)
            {
                expandedRoot.SetActive(expanded);
                if (expanded)
                {
                    expandedRoot.transform.SetAsLastSibling();
                }
            }
        }

        private void RecalculateSynergies()
        {
            DefenderUnit[] defenders = boardManager != null ? boardManager.GetAliveDefenders() : new DefenderUnit[0];
            roleCounts.Clear();
            gradeCounts.Clear();
            tagCounts.Clear();
            activeEntries.Clear();
            nearEntries.Clear();

            Dictionary<CharacterRole, HashSet<string>> distinctRoles = new Dictionary<CharacterRole, HashSet<string>>();
            Dictionary<CharacterGrade, HashSet<string>> distinctGrades = new Dictionary<CharacterGrade, HashSet<string>>();
            Dictionary<CharacterTag, HashSet<string>> distinctTags = new Dictionary<CharacterTag, HashSet<string>>();
            HashSet<string> allyHealerIds = new HashSet<string>();
            Dictionary<DefenderUnit, UnitSynergyBonus> bonuses = new Dictionary<DefenderUnit, UnitSynergyBonus>();

            for (int i = 0; i < defenders.Length; i++)
            {
                DefenderUnit defender = defenders[i];
                CharacterDefinition definition = defender != null ? defender.Definition : null;
                if (defender == null || definition == null)
                {
                    continue;
                }

                bonuses[defender] = default;
                string characterId = string.IsNullOrEmpty(definition.id) ? "runtime_" + defender.GetInstanceID() : definition.id;
                AddDistinct(distinctRoles, definition.role, characterId);
                AddDistinct(distinctGrades, definition.grade, characterId);
                List<CharacterTag> tags = CharacterTagUtility.ResolveTags(definition);
                for (int tagIndex = 0; tagIndex < tags.Count; tagIndex++)
                {
                    AddDistinct(distinctTags, tags[tagIndex], characterId);
                }

                if (HasActualAllyHeal(definition))
                {
                    allyHealerIds.Add(characterId);
                }
            }

            CopyDistinctCounts(distinctRoles, roleCounts);
            CopyDistinctCounts(distinctGrades, gradeCounts);
            CopyDistinctCounts(distinctTags, tagCounts);
            ApplyVarietySynergies(defenders, bonuses, allyHealerIds.Count);

            for (int i = 0; i < defenders.Length; i++)
            {
                DefenderUnit defender = defenders[i];
                if (defender != null)
                {
                    defender.SetSynergyBonuses(bonuses.TryGetValue(defender, out UnitSynergyBonus bonus) ? bonus : default);
                }
            }

            nearEntries.Sort((left, right) =>
            {
                float leftRatio = left.target > 0 ? (float)left.progress / left.target : 0f;
                float rightRatio = right.target > 0 ? (float)right.progress / right.target : 0f;
                return rightRatio.CompareTo(leftRatio);
            });
            RefreshUi(defenders.Length);
            if (gameController != null)
            {
                gameController.RecordSynergySnapshot(activeEntries.Count, activeEntries.Count > 0 ? activeEntries[0].title : "\uc2dc\ub108\uc9c0 \uc5c6\uc74c");
            }
        }

        private static void AddDistinct<TKey>(Dictionary<TKey, HashSet<string>> dictionary, TKey key, string characterId)
        {
            if (!dictionary.TryGetValue(key, out HashSet<string> ids))
            {
                ids = new HashSet<string>();
                dictionary[key] = ids;
            }
            ids.Add(characterId);
        }

        private static void CopyDistinctCounts<TKey>(Dictionary<TKey, HashSet<string>> source, Dictionary<TKey, int> destination)
        {
            foreach (KeyValuePair<TKey, HashSet<string>> pair in source)
            {
                destination[pair.Key] = pair.Value.Count;
            }
        }

        private static bool HasActualAllyHeal(CharacterDefinition definition)
        {
            if (definition == null || definition.skills == null)
            {
                return false;
            }

            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                if (skill != null && skill.effectType == SkillEffectType.HealLowestAllies)
                {
                    return true;
                }
            }
            return false;
        }

        private void ApplyVarietySynergies(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int allyHealerCount)
        {
            int vanguards = GetCount(roleCounts, CharacterRole.Vanguard);
            if (vanguards >= 3)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Vanguard, new UnitSynergyBonus { maxHealthBonus = 0.24f, damageReductionBonus = 0.10f });
                AddEntry("\uc0bc\uc911 \ubc29\ubcbd", "\uc11c\ub85c \ub2e4\ub978 \uc804\uc704 3\uc885: \uc804\uc704 \uccb4\ub825 +24%, \ubc1b\ub294 \ud53c\ud574 -10%", new Color(0.46f, 0.74f, 1f));
            }
            else AddNear("\uc0bc\uc911 \ubc29\ubcbd", vanguards, 3, "\uc11c\ub85c \ub2e4\ub978 \uc804\uc704", new Color(0.46f, 0.74f, 1f));

            if (allyHealerCount >= 3)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus { maxHealthBonus = 0.10f, manaRegenRateBonus = 0.025f });
                AddEntry("\uc0dd\uba85 \uc21c\ud658", "\uc2e4\uc81c \uc544\uad70 \ud68c\ubcf5 \uc2a4\ud0ac 3\uc885: \uc804\uccb4 \uccb4\ub825 +10%, \ucd08\ub2f9 \ub9c8\ub098 +2.5%", new Color(0.34f, 1f, 0.58f));
            }
            else AddNear("\uc0dd\uba85 \uc21c\ud658", allyHealerCount, 3, "\uc544\uad70 \ud68c\ubcf5 \uc2a4\ud0ac \ubcf4\uc720\uc790", new Color(0.34f, 1f, 0.58f));

            int lineage = HasGrade(CharacterGrade.Normal) && HasGrade(CharacterGrade.Rare) && HasGrade(CharacterGrade.Epic) && HasGrade(CharacterGrade.Legendary) ? 4 : CountGradeLineage();
            if (lineage >= 4)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus { attackPowerBonus = 0.12f, skillPowerBonus = 0.12f });
                AddEntry("\ub4f1\uae09 \uacc4\ubcf4", "\uc77c\ubc18\u00b7\ub808\uc5b4\u00b7\uc5d0\ud53d\u00b7\uc804\uc124 1\uc885\uc529: \uc804\uccb4 \uacf5\uaca9\ub825, \uc2a4\ud0ac \uc704\ub825 +12%", new Color(1f, 0.78f, 0.30f));
            }
            else AddNear("\ub4f1\uae09 \uacc4\ubcf4", lineage, 4, "\ub2e4\ub978 \ub4f1\uae09", new Color(1f, 0.78f, 0.30f));

            int rangers = GetCount(roleCounts, CharacterRole.Ranger);
            int mages = GetCount(roleCounts, CharacterRole.Mage);
            if (rangers >= 3 && mages >= 2)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Ranger, new UnitSynergyBonus { attackSpeedBonus = 0.12f });
                ApplyToRole(defenders, bonuses, CharacterRole.Mage, new UnitSynergyBonus { skillPowerBonus = 0.18f });
                AddEntry("\uc6d0\uc18c \ud3ec\ud654", "\uc11c\ub85c \ub2e4\ub978 \uc6d0\uac70\ub9ac 3 + \ub9c8\ubc95 2: \uacf5\uc18d +12%, \uc2a4\ud0ac \uc704\ub825 +18%", new Color(0.50f, 0.74f, 1f));
            }
            else AddNear("\uc6d0\uc18c \ud3ec\ud654", Mathf.Min(rangers, 3) + Mathf.Min(mages, 2), 5, "\uc6d0\uac70\ub9ac 3 + \ub9c8\ubc95 2", new Color(0.50f, 0.74f, 1f));

            int assassins = GetCount(roleCounts, CharacterRole.Assassin);
            int highGrades = CountHighGradeLineage();
            if (assassins >= 3 && highGrades >= 1)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus { critChanceBonus = 0.10f, bossDamageBonus = 0.16f });
                AddEntry("\uc5b4\ub460\uc758 \uacfc\uad00", "\uc11c\ub85c \ub2e4\ub978 \uc554\uc0b4 3 + \uc804\uc124+ 1: \uce58\uba85 +10%, \ubcf4\uc2a4 \ud53c\ud574 +16%", new Color(1f, 0.34f, 0.56f));
            }
            else AddNear("\uc5b4\ub460\uc758 \uacfc\uad00", Mathf.Min(assassins, 3) + Mathf.Min(highGrades, 1), 4, "\uc554\uc0b4 3 + \uc804\uc124+ 1", new Color(1f, 0.34f, 0.56f));

            int bestTagCount = 0;
            foreach (KeyValuePair<CharacterTag, int> tag in tagCounts) bestTagCount = Mathf.Max(bestTagCount, tag.Value);
            if (bestTagCount >= 5)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus { attackSpeedBonus = 0.08f, damageReductionBonus = 0.06f });
                AddEntry("\ud0dc\uadf8 \uacf5\uba85 MAX", "\uc11c\ub85c \ub2e4\ub978 \uac19\uc740 \ud0dc\uadf8 5\uc885: \uc804\uccb4 \uacf5\uc18d +8%, \ubc1b\ub294 \ud53c\ud574 -6%", new Color(0.72f, 0.60f, 1f));
            }
            else if (bestTagCount >= 3)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus { attackSpeedBonus = 0.06f });
                AddEntry("\ud0dc\uadf8 \uacf5\uba85", "\uc11c\ub85c \ub2e4\ub978 \uac19\uc740 \ud0dc\uadf8 3\uc885: \uc804\uccb4 \uacf5\uc18d +6%", new Color(0.72f, 0.60f, 1f));
            }
            else AddNear("\ud0dc\uadf8 \uacf5\uba85", bestTagCount, 3, "\uc11c\ub85c \ub2e4\ub978 \uac19\uc740 \ud0dc\uadf8", new Color(0.72f, 0.60f, 1f));

            int roleVariety = roleCounts.Count;
            if (roleVariety >= 5 && lineage >= 4)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus { critChanceBonus = 0.06f, rangeBonus = 0.25f });
                AddEntry("\ud504\ub9ac\uc998 \uc9c4\ud615", "\uc11c\ub85c \ub2e4\ub978 \uc5ed\ud560 5 + \ub4f1\uae09 4: \uc804\uccb4 \uce58\uba85 +6%, \uc0ac\uac70\ub9ac +0.25", new Color(1f, 0.92f, 0.42f));
            }
            else AddNear("\ud504\ub9ac\uc998 \uc9c4\ud615", Mathf.Min(roleVariety, 5) + Mathf.Min(lineage, 4), 9, "\uc5ed\ud560 5 + \ub4f1\uae09 4", new Color(1f, 0.92f, 0.42f));
        }

        private bool HasGrade(CharacterGrade grade) => GetCount(gradeCounts, grade) > 0;

        private int CountGradeLineage()
        {
            int count = 0;
            if (HasGrade(CharacterGrade.Normal)) count++;
            if (HasGrade(CharacterGrade.Rare)) count++;
            if (HasGrade(CharacterGrade.Epic)) count++;
            if (HasGrade(CharacterGrade.Legendary)) count++;
            return count;
        }

        private int CountHighGradeLineage()
        {
            int count = 0;
            if (HasGrade(CharacterGrade.Legendary)) count++;
            if (HasGrade(CharacterGrade.Mythic)) count++;
            if (HasGrade(CharacterGrade.Transcendent)) count++;
            return count;
        }
        private void AddNear(string title, int progress, int target, string requirement, Color color)
        {
            nearEntries.Add(new SynergyEntry
            {
                title = title + " " + progress + "/" + target,
                detail = requirement + " \uc870\ud569\uc744 \ubaa8\uc73c\uba74 \ud65c\uc131\ud654\ub429\ub2c8\ub2e4.",
                color = color,
                locked = true,
                progress = progress,
                target = target
            });
        }

        private void ApplyRoleSynergies(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses)
        {
            ApplyVanguardSynergy(defenders, bonuses, GetCount(roleCounts, CharacterRole.Vanguard));
            ApplyRangerSynergy(defenders, bonuses, GetCount(roleCounts, CharacterRole.Ranger));
            ApplyMageSynergy(defenders, bonuses, GetCount(roleCounts, CharacterRole.Mage));
            ApplySupportSynergy(defenders, bonuses, GetCount(roleCounts, CharacterRole.Support));
            ApplyAssassinSynergy(defenders, bonuses, GetCount(roleCounts, CharacterRole.Assassin));
            ApplySummonerSynergy(defenders, bonuses, GetCount(roleCounts, CharacterRole.Summoner));
        }

        private void ApplyTagSynergies(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses)
        {
            ApplyTagSynergy(defenders, bonuses, CharacterTag.Flame, new Color(1f, 0.45f, 0.22f),
                "화염 회로", 2, new UnitSynergyBonus { attackPowerBonus = 0.10f },
                "2 화염: 전체 공격력 +10%",
                4, new UnitSynergyBonus { attackPowerBonus = 0.08f, skillPowerBonus = 0.18f },
                "4 화염: 추가 공격력 +8%, 스킬 위력 +18%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Frost, new Color(0.42f, 0.84f, 1f),
                "빙결 제어", 2, new UnitSynergyBonus { rangeBonus = 0.35f, damageReductionBonus = 0.04f },
                "2 냉기: 사거리 +0.35, 받는 피해 -4%",
                4, new UnitSynergyBonus { manaRegenRateBonus = 0.03f },
                "4 냉기: 추가 초당 마나 +3%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Storm, new Color(0.48f, 1f, 0.86f),
                "폭풍 탄창", 2, new UnitSynergyBonus { attackSpeedBonus = 0.08f, rangeBonus = 0.25f },
                "2 폭풍: 공격속도 +8%, 사거리 +0.25",
                4, new UnitSynergyBonus { manaGainPerAttackRateBonus = 0.04f },
                "4 폭풍: 공격 시 마나 +4%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Nature, new Color(0.45f, 1f, 0.56f),
                "생명의 뿌리", 2, new UnitSynergyBonus { maxHealthBonus = 0.10f },
                "2 자연: 최대 체력 +10%",
                4, new UnitSynergyBonus { damageReductionBonus = 0.07f, manaGainWhenHitRateBonus = 0.03f },
                "4 자연: 받는 피해 -7%, 피격 마나 +3%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Gear, new Color(0.72f, 0.82f, 1f),
                "기계 공방", 2, new UnitSynergyBonus { attackSpeedBonus = 0.10f, manaGainPerAttackRateBonus = 0.02f },
                "2 기계: 공격속도 +10%, 공격 마나 +2%",
                4, new UnitSynergyBonus { splashRadiusBonus = 0.75f, splashDamageRatioBonus = 0.18f },
                "4 기계: 평타 폭발 반경 +0.75, 폭발 피해 +18%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Void, new Color(0.70f, 0.38f, 1f),
                "공허 균열", 2, new UnitSynergyBonus { bossDamageBonus = 0.15f, critChanceBonus = 0.04f },
                "2 공허: 보스 피해 +15%, 치명타 +4%",
                3, new UnitSynergyBonus { bossDamageBonus = 0.12f, skillPowerBonus = 0.12f },
                "3 공허: 추가 보스 피해 +12%, 스킬 위력 +12%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Light, new Color(1f, 0.86f, 0.38f),
                "축복 프리즘", 2, new UnitSynergyBonus { maxHealthBonus = 0.06f, manaGainWhenHitRateBonus = 0.03f },
                "2 빛: 최대 체력 +6%, 피격 마나 +3%",
                4, new UnitSynergyBonus { damageReductionBonus = 0.06f, skillPowerBonus = 0.08f },
                "4 빛: 받는 피해 -6%, 스킬 위력 +8%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Shadow, new Color(1f, 0.38f, 0.60f),
                "그림자 칼날", 2, new UnitSynergyBonus { critChanceBonus = 0.06f, attackSpeedBonus = 0.06f },
                "2 그림자: 치명타 +6%, 공격속도 +6%",
                4, new UnitSynergyBonus { criticalDamageBonus = 0.28f },
                "4 그림자: 치명타 피해 +28%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Spirit, new Color(0.52f, 0.82f, 1f),
                "정령 공명", 2, new UnitSynergyBonus { manaRegenRateBonus = 0.02f, skillPowerBonus = 0.08f },
                "2 정령: 초당 마나 +2%, 스킬 위력 +8%",
                4, new UnitSynergyBonus { rangeBonus = 0.40f, manaGainPerAttackRateBonus = 0.03f },
                "4 정령: 사거리 +0.4, 공격 마나 +3%");

            ApplyTagSynergy(defenders, bonuses, CharacterTag.Steel, new Color(0.72f, 0.78f, 0.92f),
                "강철 방벽", 2, new UnitSynergyBonus { damageReductionBonus = 0.05f, maxHealthBonus = 0.08f },
                "2 강철: 받는 피해 -5%, 최대 체력 +8%",
                4, new UnitSynergyBonus { attackPowerBonus = 0.08f, damageReductionBonus = 0.05f },
                "4 강철: 추가 공격력 +8%, 받는 피해 -5%");
        }

        private void ApplyTagSynergy(
            DefenderUnit[] defenders,
            Dictionary<DefenderUnit, UnitSynergyBonus> bonuses,
            CharacterTag tag,
            Color accent,
            string title,
            int firstThreshold,
            UnitSynergyBonus firstBonus,
            string firstDetail,
            int secondThreshold,
            UnitSynergyBonus secondBonus,
            string secondDetail)
        {
            int count = GetCount(tagCounts, tag);
            if (count >= firstThreshold)
            {
                ApplyToAll(defenders, bonuses, firstBonus);
                AddEntry(title, firstDetail, accent);
            }

            if (count >= secondThreshold)
            {
                ApplyToAll(defenders, bonuses, secondBonus);
                AddEntry(title + "+", secondDetail, accent);
            }
        }

        private void ApplySpecialSynergies(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses)
        {
            int distinctRoleCount = roleCounts.Count;
            int distinctGradeCount = gradeCounts.Count;
            int highGradeCount = GetCount(gradeCounts, CharacterGrade.Legendary) +
                GetCount(gradeCounts, CharacterGrade.Mythic) +
                GetCount(gradeCounts, CharacterGrade.Transcendent);
            int rangerCount = GetCount(roleCounts, CharacterRole.Ranger);
            int mageCount = GetCount(roleCounts, CharacterRole.Mage);
            int supportCount = GetCount(roleCounts, CharacterRole.Support);
            int assassinCount = GetCount(roleCounts, CharacterRole.Assassin);
            int vanguardCount = GetCount(roleCounts, CharacterRole.Vanguard);

            if (distinctRoleCount >= 4)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    attackPowerBonus = 0.10f,
                    manaRegenRateBonus = 0.02f
                });
                AddEntry("프리즘 회로", "4역할: 전체 공격력 +10%, 마나 회복 +2%", new Color(0.55f, 0.95f, 1f));
            }

            if (distinctRoleCount >= 6)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    critChanceBonus = 0.08f,
                    rangeBonus = 0.5f
                });
                AddEntry("프리즘 회로 MAX", "6역할: 전체 치명타 +8%, 사거리 +0.5", new Color(1f, 0.80f, 0.36f));
            }

            if (distinctGradeCount >= 3)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    attackSpeedBonus = 0.08f,
                    skillPowerBonus = 0.12f
                });
                AddEntry("등급 공명", "3등급 이상: 전체 공속 +8%, 스킬 위력 +12%", new Color(0.56f, 0.86f, 0.56f));
            }

            if (highGradeCount >= 2)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    attackPowerBonus = 0.08f,
                    manaGainPerAttackRateBonus = 0.03f
                });
                AddEntry("왕관 신호", "전설 이상 2기: 전체 공격력 +8%, 공격 마나 +3%", new Color(1f, 0.58f, 0.30f));
            }

            if (rangerCount >= 2 && mageCount >= 2)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    splashRadiusBonus = 0.9f,
                    splashDamageRatioBonus = 0.22f,
                    skillPowerBonus = 0.10f
                });
                AddEntry("마탄 연구소", "사수2+마법2: 평타 폭발, 스킬 위력 +10%", new Color(0.45f, 0.92f, 1f));
            }

            if (vanguardCount >= 2 && supportCount >= 2)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    damageReductionBonus = 0.08f,
                    manaGainWhenHitRateBonus = 0.05f
                });
                AddEntry("철벽 배터리", "전위2+지원2: 피해 감소 +8%, 피격 마나 +5%", new Color(0.42f, 1f, 0.64f));
            }

            if (assassinCount >= 2 && highGradeCount >= 1)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus
                {
                    critChanceBonus = 0.12f,
                    attackSpeedBonus = 0.14f
                });
                AddEntry("그림자 왕관", "암살2+전설 이상: 암살 치명타 +12%, 공속 +14%", new Color(1f, 0.44f, 0.66f));
            }
        }

        private void ApplyVanguardSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
        {
            Color accent = new Color(0.33f, 0.72f, 1f);
            if (count >= 2)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Vanguard, new UnitSynergyBonus
                {
                    maxHealthBonus = 0.18f,
                    manaGainWhenHitRateBonus = 0.06f
                });
                AddEntry("전위 코어", "전위 2기: 전위 체력 +18%, 피격 마나 +6%", accent);
            }

            if (count >= 4)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    maxHealthBonus = 0.10f,
                    damageReductionBonus = 0.06f
                });
                AddEntry("전위 코어+", "전위 4기: 전체 체력 +10%, 받는 피해 -6%", accent);
            }

            if (count >= 6)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Vanguard, new UnitSynergyBonus
                {
                    attackPowerBonus = 0.18f,
                    damageReductionBonus = 0.14f
                });
                AddEntry("전위 코어 MAX", "전위 6기: 전위 공격력 +18%, 받는 피해 -14%", accent);
            }
        }

        private void ApplyRangerSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
        {
            Color accent = new Color(0.36f, 1f, 0.84f);
            if (count >= 2)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Ranger, new UnitSynergyBonus
                {
                    attackSpeedBonus = 0.18f
                });
                AddEntry("리코셰 스택", "사수 2기: 사수 공속 +18%", accent);
            }

            if (count >= 4)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Ranger, new UnitSynergyBonus
                {
                    splashRadiusBonus = 1.2f,
                    splashDamageRatioBonus = 0.35f
                });
                AddEntry("리코셰 스택+", "사수 4기: 사수 평타에 폭발 피해 추가", accent);
            }

            if (count >= 6)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    rangeBonus = 0.7f
                });
                ApplyToRole(defenders, bonuses, CharacterRole.Ranger, new UnitSynergyBonus
                {
                    attackPowerBonus = 0.12f
                });
                AddEntry("리코셰 스택 MAX", "사수 6기: 전체 사거리 +0.7, 사수 공격력 +12%", accent);
            }
        }

        private void ApplyMageSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
        {
            Color accent = new Color(0.92f, 0.54f, 1f);
            if (count >= 2)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Mage, new UnitSynergyBonus
                {
                    manaRegenRateBonus = 0.03f,
                    skillPowerBonus = 0.18f
                });
                AddEntry("아크 플로우", "마법 2기: 마법 마나 회복 +3%, 스킬 위력 +18%", accent);
            }

            if (count >= 3)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    manaGainPerAttackRateBonus = 0.04f
                });
                AddEntry("아크 플로우+", "마법 3기: 전체 공격 마나 +4%", accent);
            }

            if (count >= 5)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Mage, new UnitSynergyBonus
                {
                    attackPowerBonus = 0.22f,
                    skillPowerBonus = 0.30f
                });
                AddEntry("아크 플로우 MAX", "마법 5기: 마법 공격력 +22%, 스킬 위력 +30%", accent);
            }
        }

        private void ApplySupportSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
        {
            Color accent = new Color(1f, 0.74f, 0.30f);
            if (count >= 2)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    maxHealthBonus = 0.08f,
                    critChanceBonus = 0.05f
                });
                AddEntry("하모니 마크", "지원 2기: 전체 체력 +8%, 치명타 +5%", accent);
            }

            if (count >= 3)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Support, new UnitSynergyBonus
                {
                    attackSpeedBonus = 0.18f,
                    manaRegenRateBonus = 0.02f
                });
                AddEntry("하모니 마크+", "지원 3기: 지원 공속 +18%, 마나 회복 +2%", accent);
            }

            if (count >= 4)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    damageReductionBonus = 0.08f,
                    manaGainWhenHitRateBonus = 0.04f
                });
                AddEntry("하모니 마크 MAX", "지원 4기: 받는 피해 -8%, 피격 마나 +4%", accent);
            }
        }

        private void ApplyAssassinSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
        {
            Color accent = new Color(1f, 0.42f, 0.52f);
            if (count >= 2)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus
                {
                    critChanceBonus = 0.15f
                });
                AddEntry("섀도 템포", "암살 2기: 암살 치명타 +15%", accent);
            }

            if (count >= 3)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus
                {
                    attackPowerBonus = 0.28f,
                    attackSpeedBonus = 0.10f
                });
                AddEntry("섀도 템포+", "암살 3기: 암살 공격력 +28%, 공속 +10%", accent);
            }

            if (count >= 5)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus
                {
                    rangeBonus = 0.8f,
                    critChanceBonus = 0.08f,
                    attackSpeedBonus = 0.16f
                });
                AddEntry("섀도 템포 MAX", "암살 5기: 사거리 +0.8, 치명타 +8%, 공속 +16%", accent);
            }
        }

        private void ApplySummonerSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
        {
            Color accent = new Color(0.48f, 0.80f, 1f);
            if (count >= 2)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Summoner, new UnitSynergyBonus
                {
                    manaRegenRateBonus = 0.04f,
                    rangeBonus = 0.8f
                });
                AddEntry("에코 팩토리", "소환 2기: 소환 마나 회복 +4%, 사거리 +0.8", accent);
            }

            if (count >= 3)
            {
                ApplyToAll(defenders, bonuses, new UnitSynergyBonus
                {
                    attackSpeedBonus = 0.10f
                });
                AddEntry("에코 팩토리+", "소환 3기: 전체 공속 +10%", accent);
            }

            if (count >= 5)
            {
                ApplyToRole(defenders, bonuses, CharacterRole.Summoner, new UnitSynergyBonus
                {
                    splashRadiusBonus = 1.0f,
                    splashDamageRatioBonus = 0.45f,
                    attackPowerBonus = 0.20f
                });
                AddEntry("에코 팩토리 MAX", "소환 5기: 소환 평타 폭발, 공격력 +20%", accent);
            }
        }

        private void ApplyToAll(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, UnitSynergyBonus bonus)
        {
            for (int i = 0; i < defenders.Length; i++)
            {
                DefenderUnit defender = defenders[i];
                if (defender == null)
                {
                    continue;
                }

                AddBonus(bonuses, defender, bonus);
            }
        }

        private void ApplyToRole(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, CharacterRole role, UnitSynergyBonus bonus)
        {
            for (int i = 0; i < defenders.Length; i++)
            {
                DefenderUnit defender = defenders[i];
                if (defender == null || defender.Role != role)
                {
                    continue;
                }

                AddBonus(bonuses, defender, bonus);
            }
        }

        private void AddBonus(Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, DefenderUnit defender, UnitSynergyBonus bonus)
        {
            if (defender == null || !bonuses.ContainsKey(defender))
            {
                return;
            }

            UnitSynergyBonus accumulated = bonuses[defender];
            accumulated.Add(bonus);
            bonuses[defender] = accumulated;
        }

        private void RefreshUi(int defenderCount)
        {
            if (summaryText != null)
            {
                summaryText.text = defenderCount > 0
                    ? "\uc2dc\ub108\uc9c0 " + activeEntries.Count + "\uac1c \ud65c\uc131"
                    : "\uc2dc\ub108\uc9c0 \ub300\uae30\uc911";
                summaryText.color = defenderCount > 0 ? Color.white : new Color(0.86f, 0.92f, 1f);
            }

            if (expandedHeaderText != null)
            {
                expandedHeaderText.text = activeEntries.Count > 0
                    ? "\ud65c\uc131 \uc2dc\ub108\uc9c0 " + activeEntries.Count + " | \ub2e4\uc74c \uc870\ud569"
                    : "\ub2e4\uc74c \uc2dc\ub108\uc9c0 \uc870\ud569";
            }

            List<SynergyEntry> displayEntries = new List<SynergyEntry>(activeEntries);
            int nearCount = Mathf.Min(3, nearEntries.Count);
            for (int i = 0; i < nearCount; i++)
            {
                displayEntries.Add(nearEntries[i]);
            }

            int rowCount = titleTexts != null ? titleTexts.Length : 0;
            int displayCount = Mathf.Min(displayEntries.Count, rowCount);
            bool hasOverflow = displayEntries.Count > rowCount;
            for (int i = 0; i < rowCount; i++)
            {
                bool showOverflowRow = hasOverflow && i == rowCount - 1;
                bool hasEntry = i < displayCount && !showOverflowRow;
                SynergyEntry entry = hasEntry ? displayEntries[i] : null;
                if (titleTexts[i] != null)
                {
                    titleTexts[i].text = hasEntry
                        ? (entry.locked ? "\ub2e4\uc74c: " : string.Empty) + entry.title
                        : showOverflowRow ? "+ " + (displayEntries.Count - (rowCount - 1)) + "\uac1c \ub354 \uc788\uc74c"
                        : i == 0 ? "\ud65c\uc131 \uc2dc\ub108\uc9c0 \uc5c6\uc74c" : string.Empty;
                    titleTexts[i].color = hasEntry && entry.locked ? new Color(0.76f, 0.84f, 0.96f) : hasEntry || showOverflowRow ? Color.white : new Color(0.75f, 0.80f, 0.90f);
                }
                if (detailTexts != null && i < detailTexts.Length && detailTexts[i] != null)
                {
                    detailTexts[i].text = hasEntry ? entry.detail
                        : showOverflowRow ? "\ub2e4\uc591\ud55c \uc218\ud638\uc790\ub97c \ub354 \ubc30\uce58\ud558\uba74 \ud45c\uc2dc\ub429\ub2c8\ub2e4."
                        : i == 0 ? "\ubcf5\uc81c \uc720\ub2db\uc740 \uc2dc\ub108\uc9c0 \uc9c4\ud589\ub3c4\uc5d0 \ud3ec\ud568\ub418\uc9c0 \uc54a\uc2b5\ub2c8\ub2e4." : string.Empty;
                    detailTexts[i].color = hasEntry && entry.locked ? new Color(0.60f, 0.70f, 0.88f) : hasEntry || showOverflowRow ? new Color(0.84f, 0.91f, 1f) : new Color(0.58f, 0.66f, 0.84f);
                }
                if (accentImages != null && i < accentImages.Length && accentImages[i] != null)
                {
                    accentImages[i].color = hasEntry ? (entry.locked ? Color.Lerp(entry.color, Color.black, 0.35f) : entry.color)
                        : showOverflowRow ? new Color(1f, 0.76f, 0.28f, 0.92f) : new Color(0.42f, 0.48f, 0.64f, 0.62f);
                }
                if (iconImages != null && i < iconImages.Length && iconImages[i] != null)
                {
                    iconImages[i].color = hasEntry ? Color.Lerp(entry.color, Color.white, entry.locked ? 0.10f : 0.38f)
                        : showOverflowRow ? new Color(1f, 0.84f, 0.38f, 0.70f) : new Color(0.58f, 0.66f, 0.84f, 0.22f);
                }
            }
        }

        private void AddEntry(string title, string detail, Color color)
        {
            activeEntries.Add(new SynergyEntry
            {
                title = title,
                detail = detail,
                color = color
            });
        }

        private void Count<TKey>(Dictionary<TKey, int> dictionary, TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key]++;
            }
            else
            {
                dictionary[key] = 1;
            }
        }

        private int GetCount<TKey>(Dictionary<TKey, int> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out int count) ? count : 0;
        }
    }
}
