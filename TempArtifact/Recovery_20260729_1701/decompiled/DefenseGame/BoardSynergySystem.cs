using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public class BoardSynergySystem : MonoBehaviour
{
	private sealed class SynergyEntry
	{
		public string title;

		public string detail;

		public Color color;
	}

	[SerializeField]
	private DefenseGameController gameController;

	[SerializeField]
	private DefenseBoardManager boardManager;

	[SerializeField]
	private Button summaryButton;

	[SerializeField]
	private Text summaryText;

	[SerializeField]
	private GameObject expandedRoot;

	[SerializeField]
	private Text expandedHeaderText;

	[SerializeField]
	private Button closeButton;

	[SerializeField]
	private Text[] titleTexts;

	[SerializeField]
	private Text[] detailTexts;

	[SerializeField]
	private Image[] accentImages;

	[SerializeField]
	private Image[] iconImages;

	private readonly Dictionary<CharacterRole, int> roleCounts = new Dictionary<CharacterRole, int>();

	private readonly Dictionary<CharacterGrade, int> gradeCounts = new Dictionary<CharacterGrade, int>();

	private readonly Dictionary<CharacterTag, int> tagCounts = new Dictionary<CharacterTag, int>();

	private readonly List<SynergyEntry> activeEntries = new List<SynergyEntry>();

	private bool subscribed;

	private bool isExpanded;

	public void Configure(DefenseGameController controller, DefenseBoardManager board, Button toggleButton, Text summaryLabel, GameObject expandedPanel, Text expandedHeader, Text[] titles, Text[] details, Image[] accents, Image[] icons, Button closePanelButton)
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
		SetExpanded(expanded: false);
		Subscribe();
		RecalculateSynergies();
	}

	public void Configure(DefenseGameController controller, DefenseBoardManager board, GameObject root, Text header, Text[] titles, Text[] details, Image[] accents)
	{
		Configure(controller, board, null, header, root, header, titles, details, accents, null, null);
	}

	public void Configure(DefenseGameController controller, DefenseBoardManager board, Button toggleButton, Text summaryLabel, GameObject expandedPanel, Text expandedHeader, Text[] titles, Text[] details, Image[] accents, Button closePanelButton)
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
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		if ((Object)(object)summaryButton != (Object)null)
		{
			((UnityEvent)summaryButton.onClick).RemoveListener(new UnityAction(ToggleExpanded));
			((UnityEvent)summaryButton.onClick).AddListener(new UnityAction(ToggleExpanded));
		}
		if ((Object)(object)closeButton != (Object)null)
		{
			((UnityEvent)closeButton.onClick).RemoveListener(new UnityAction(CloseExpanded));
			((UnityEvent)closeButton.onClick).AddListener(new UnityAction(CloseExpanded));
		}
	}

	private void Subscribe()
	{
		if (!subscribed)
		{
			if ((Object)(object)gameController != (Object)null)
			{
				gameController.OnStateChanged += HandleBoardStateChanged;
			}
			DefenderUnit.OnDefenderSpawned += HandleDefenderChanged;
			DefenderUnit.OnDefenderRemoved += HandleDefenderChanged;
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed)
		{
			if ((Object)(object)gameController != (Object)null)
			{
				gameController.OnStateChanged -= HandleBoardStateChanged;
			}
			DefenderUnit.OnDefenderSpawned -= HandleDefenderChanged;
			DefenderUnit.OnDefenderRemoved -= HandleDefenderChanged;
			subscribed = false;
		}
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
		SetExpanded(expanded: false);
	}

	private void SetExpanded(bool expanded)
	{
		isExpanded = expanded;
		if ((Object)(object)expandedRoot != (Object)null)
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
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
		roleCounts.Clear();
		gradeCounts.Clear();
		tagCounts.Clear();
		activeEntries.Clear();
		Dictionary<DefenderUnit, UnitSynergyBonus> dictionary = new Dictionary<DefenderUnit, UnitSynergyBonus>();
		foreach (DefenderUnit defenderUnit in array)
		{
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				dictionary[defenderUnit] = default(UnitSynergyBonus);
				Count(roleCounts, defenderUnit.Role);
				Count(gradeCounts, defenderUnit.Grade);
				List<CharacterTag> list = CharacterTagUtility.ResolveTags(defenderUnit.Definition);
				for (int j = 0; j < list.Count; j++)
				{
					Count(tagCounts, list[j]);
				}
			}
		}
		ApplyRoleSynergies(array, dictionary);
		ApplyTagSynergies(array, dictionary);
		ApplySpecialSynergies(array, dictionary);
		foreach (DefenderUnit defenderUnit2 in array)
		{
			if (!((Object)(object)defenderUnit2 == (Object)null))
			{
				defenderUnit2.SetSynergyBonuses(dictionary.TryGetValue(defenderUnit2, out var value) ? value : default(UnitSynergyBonus));
			}
		}
		RefreshUi(array.Length);
		if ((Object)(object)gameController != (Object)null)
		{
			gameController.RecordSynergySnapshot(activeEntries.Count, (activeEntries.Count > 0) ? activeEntries[0].title : "시너지 없음");
		}
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
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Flame, new Color(1f, 0.45f, 0.22f), "화염 회로", 2, new UnitSynergyBonus
		{
			attackPowerBonus = 0.1f
		}, "2 화염: 전체 공격력 +10%", 4, new UnitSynergyBonus
		{
			attackPowerBonus = 0.08f,
			skillPowerBonus = 0.18f
		}, "4 화염: 추가 공격력 +8%, 스킬 위력 +18%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Frost, new Color(0.42f, 0.84f, 1f), "빙결 제어", 2, new UnitSynergyBonus
		{
			rangeBonus = 0.35f,
			damageReductionBonus = 0.04f
		}, "2 냉기: 사거리 +0.35, 받는 피해 -4%", 4, new UnitSynergyBonus
		{
			manaRegenRateBonus = 0.03f
		}, "4 냉기: 추가 초당 마나 +3%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Storm, new Color(0.48f, 1f, 0.86f), "폭풍 탄창", 2, new UnitSynergyBonus
		{
			attackSpeedBonus = 0.08f,
			rangeBonus = 0.25f
		}, "2 폭풍: 공격속도 +8%, 사거리 +0.25", 4, new UnitSynergyBonus
		{
			manaGainPerAttackRateBonus = 0.04f
		}, "4 폭풍: 공격 시 마나 +4%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Nature, new Color(0.45f, 1f, 0.56f), "생명의 뿌리", 2, new UnitSynergyBonus
		{
			maxHealthBonus = 0.1f
		}, "2 자연: 최대 체력 +10%", 4, new UnitSynergyBonus
		{
			damageReductionBonus = 0.07f,
			manaGainWhenHitRateBonus = 0.03f
		}, "4 자연: 받는 피해 -7%, 피격 마나 +3%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Gear, new Color(0.72f, 0.82f, 1f), "기계 공방", 2, new UnitSynergyBonus
		{
			attackSpeedBonus = 0.1f,
			manaGainPerAttackRateBonus = 0.02f
		}, "2 기계: 공격속도 +10%, 공격 마나 +2%", 4, new UnitSynergyBonus
		{
			splashRadiusBonus = 0.75f,
			splashDamageRatioBonus = 0.18f
		}, "4 기계: 평타 폭발 반경 +0.75, 폭발 피해 +18%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Void, new Color(0.7f, 0.38f, 1f), "공허 균열", 2, new UnitSynergyBonus
		{
			bossDamageBonus = 0.15f,
			critChanceBonus = 0.04f
		}, "2 공허: 보스 피해 +15%, 치명타 +4%", 3, new UnitSynergyBonus
		{
			bossDamageBonus = 0.12f,
			skillPowerBonus = 0.12f
		}, "3 공허: 추가 보스 피해 +12%, 스킬 위력 +12%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Light, new Color(1f, 0.86f, 0.38f), "축복 프리즘", 2, new UnitSynergyBonus
		{
			maxHealthBonus = 0.06f,
			manaGainWhenHitRateBonus = 0.03f
		}, "2 빛: 최대 체력 +6%, 피격 마나 +3%", 4, new UnitSynergyBonus
		{
			damageReductionBonus = 0.06f,
			skillPowerBonus = 0.08f
		}, "4 빛: 받는 피해 -6%, 스킬 위력 +8%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Shadow, new Color(1f, 0.38f, 0.6f), "그림자 칼날", 2, new UnitSynergyBonus
		{
			critChanceBonus = 0.06f,
			attackSpeedBonus = 0.06f
		}, "2 그림자: 치명타 +6%, 공격속도 +6%", 4, new UnitSynergyBonus
		{
			criticalDamageBonus = 0.28f
		}, "4 그림자: 치명타 피해 +28%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Spirit, new Color(0.52f, 0.82f, 1f), "정령 공명", 2, new UnitSynergyBonus
		{
			manaRegenRateBonus = 0.02f,
			skillPowerBonus = 0.08f
		}, "2 정령: 초당 마나 +2%, 스킬 위력 +8%", 4, new UnitSynergyBonus
		{
			rangeBonus = 0.4f,
			manaGainPerAttackRateBonus = 0.03f
		}, "4 정령: 사거리 +0.4, 공격 마나 +3%");
		ApplyTagSynergy(defenders, bonuses, CharacterTag.Steel, new Color(0.72f, 0.78f, 0.92f), "강철 방벽", 2, new UnitSynergyBonus
		{
			damageReductionBonus = 0.05f,
			maxHealthBonus = 0.08f
		}, "2 강철: 받는 피해 -5%, 최대 체력 +8%", 4, new UnitSynergyBonus
		{
			attackPowerBonus = 0.08f,
			damageReductionBonus = 0.05f
		}, "4 강철: 추가 공격력 +8%, 받는 피해 -5%");
	}

	private void ApplyTagSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, CharacterTag tag, Color accent, string title, int firstThreshold, UnitSynergyBonus firstBonus, string firstDetail, int secondThreshold, UnitSynergyBonus secondBonus, string secondDetail)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		int count = roleCounts.Count;
		int count2 = gradeCounts.Count;
		int num = GetCount(gradeCounts, CharacterGrade.Legendary) + GetCount(gradeCounts, CharacterGrade.Mythic) + GetCount(gradeCounts, CharacterGrade.Transcendent);
		int count3 = GetCount(roleCounts, CharacterRole.Ranger);
		int count4 = GetCount(roleCounts, CharacterRole.Mage);
		int count5 = GetCount(roleCounts, CharacterRole.Support);
		int count6 = GetCount(roleCounts, CharacterRole.Assassin);
		int count7 = GetCount(roleCounts, CharacterRole.Vanguard);
		if (count >= 4)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				attackPowerBonus = 0.1f,
				manaRegenRateBonus = 0.02f
			});
			AddEntry("프리즘 회로", "4역할: 전체 공격력 +10%, 마나 회복 +2%", new Color(0.55f, 0.95f, 1f));
		}
		if (count >= 6)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				critChanceBonus = 0.08f,
				rangeBonus = 0.5f
			});
			AddEntry("프리즘 회로 MAX", "6역할: 전체 치명타 +8%, 사거리 +0.5", new Color(1f, 0.8f, 0.36f));
		}
		if (count2 >= 3)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				attackSpeedBonus = 0.08f,
				skillPowerBonus = 0.12f
			});
			AddEntry("등급 공명", "3등급 이상: 전체 공속 +8%, 스킬 위력 +12%", new Color(0.56f, 0.86f, 0.56f));
		}
		if (num >= 2)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				attackPowerBonus = 0.08f,
				manaGainPerAttackRateBonus = 0.03f
			});
			AddEntry("왕관 신호", "전설 이상 2기: 전체 공격력 +8%, 공격 마나 +3%", new Color(1f, 0.58f, 0.3f));
		}
		if (count3 >= 2 && count4 >= 2)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				splashRadiusBonus = 0.9f,
				splashDamageRatioBonus = 0.22f,
				skillPowerBonus = 0.1f
			});
			AddEntry("마탄 연구소", "사수2+마법2: 평타 폭발, 스킬 위력 +10%", new Color(0.45f, 0.92f, 1f));
		}
		if (count7 >= 2 && count5 >= 2)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				damageReductionBonus = 0.08f,
				manaGainWhenHitRateBonus = 0.05f
			});
			AddEntry("철벽 배터리", "전위2+지원2: 피해 감소 +8%, 피격 마나 +5%", new Color(0.42f, 1f, 0.64f));
		}
		if (count6 >= 2 && num >= 1)
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
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.33f, 0.72f, 1f);
		if (count >= 2)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Vanguard, new UnitSynergyBonus
			{
				maxHealthBonus = 0.18f,
				manaGainWhenHitRateBonus = 0.06f
			});
			AddEntry("전위 코어", "전위 2기: 전위 체력 +18%, 피격 마나 +6%", color);
		}
		if (count >= 4)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				maxHealthBonus = 0.1f,
				damageReductionBonus = 0.06f
			});
			AddEntry("전위 코어+", "전위 4기: 전체 체력 +10%, 받는 피해 -6%", color);
		}
		if (count >= 6)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Vanguard, new UnitSynergyBonus
			{
				attackPowerBonus = 0.18f,
				damageReductionBonus = 0.14f
			});
			AddEntry("전위 코어 MAX", "전위 6기: 전위 공격력 +18%, 받는 피해 -14%", color);
		}
	}

	private void ApplyRangerSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.36f, 1f, 0.84f);
		if (count >= 2)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Ranger, new UnitSynergyBonus
			{
				attackSpeedBonus = 0.18f
			});
			AddEntry("리코셰 스택", "사수 2기: 사수 공속 +18%", color);
		}
		if (count >= 4)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Ranger, new UnitSynergyBonus
			{
				splashRadiusBonus = 1.2f,
				splashDamageRatioBonus = 0.35f
			});
			AddEntry("리코셰 스택+", "사수 4기: 사수 평타에 폭발 피해 추가", color);
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
			AddEntry("리코셰 스택 MAX", "사수 6기: 전체 사거리 +0.7, 사수 공격력 +12%", color);
		}
	}

	private void ApplyMageSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.92f, 0.54f, 1f);
		if (count >= 2)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Mage, new UnitSynergyBonus
			{
				manaRegenRateBonus = 0.03f,
				skillPowerBonus = 0.18f
			});
			AddEntry("아크 플로우", "마법 2기: 마법 마나 회복 +3%, 스킬 위력 +18%", color);
		}
		if (count >= 3)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				manaGainPerAttackRateBonus = 0.04f
			});
			AddEntry("아크 플로우+", "마법 3기: 전체 공격 마나 +4%", color);
		}
		if (count >= 5)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Mage, new UnitSynergyBonus
			{
				attackPowerBonus = 0.22f,
				skillPowerBonus = 0.3f
			});
			AddEntry("아크 플로우 MAX", "마법 5기: 마법 공격력 +22%, 스킬 위력 +30%", color);
		}
	}

	private void ApplySupportSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(1f, 0.74f, 0.3f);
		if (count >= 2)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				maxHealthBonus = 0.08f,
				critChanceBonus = 0.05f
			});
			AddEntry("하모니 마크", "지원 2기: 전체 체력 +8%, 치명타 +5%", color);
		}
		if (count >= 3)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Support, new UnitSynergyBonus
			{
				attackSpeedBonus = 0.18f,
				manaRegenRateBonus = 0.02f
			});
			AddEntry("하모니 마크+", "지원 3기: 지원 공속 +18%, 마나 회복 +2%", color);
		}
		if (count >= 4)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				damageReductionBonus = 0.08f,
				manaGainWhenHitRateBonus = 0.04f
			});
			AddEntry("하모니 마크 MAX", "지원 4기: 받는 피해 -8%, 피격 마나 +4%", color);
		}
	}

	private void ApplyAssassinSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(1f, 0.42f, 0.52f);
		if (count >= 2)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus
			{
				critChanceBonus = 0.15f
			});
			AddEntry("섀도 템포", "암살 2기: 암살 치명타 +15%", color);
		}
		if (count >= 3)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus
			{
				attackPowerBonus = 0.28f,
				attackSpeedBonus = 0.1f
			});
			AddEntry("섀도 템포+", "암살 3기: 암살 공격력 +28%, 공속 +10%", color);
		}
		if (count >= 5)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Assassin, new UnitSynergyBonus
			{
				rangeBonus = 0.8f,
				critChanceBonus = 0.08f,
				attackSpeedBonus = 0.16f
			});
			AddEntry("섀도 템포 MAX", "암살 5기: 사거리 +0.8, 치명타 +8%, 공속 +16%", color);
		}
	}

	private void ApplySummonerSynergy(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, int count)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.48f, 0.8f, 1f);
		if (count >= 2)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Summoner, new UnitSynergyBonus
			{
				manaRegenRateBonus = 0.04f,
				rangeBonus = 0.8f
			});
			AddEntry("에코 팩토리", "소환 2기: 소환 마나 회복 +4%, 사거리 +0.8", color);
		}
		if (count >= 3)
		{
			ApplyToAll(defenders, bonuses, new UnitSynergyBonus
			{
				attackSpeedBonus = 0.1f
			});
			AddEntry("에코 팩토리+", "소환 3기: 전체 공속 +10%", color);
		}
		if (count >= 5)
		{
			ApplyToRole(defenders, bonuses, CharacterRole.Summoner, new UnitSynergyBonus
			{
				splashRadiusBonus = 1f,
				splashDamageRatioBonus = 0.45f,
				attackPowerBonus = 0.2f
			});
			AddEntry("에코 팩토리 MAX", "소환 5기: 소환 평타 폭발, 공격력 +20%", color);
		}
	}

	private void ApplyToAll(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, UnitSynergyBonus bonus)
	{
		foreach (DefenderUnit defenderUnit in defenders)
		{
			if (!((Object)(object)defenderUnit == (Object)null))
			{
				AddBonus(bonuses, defenderUnit, bonus);
			}
		}
	}

	private void ApplyToRole(DefenderUnit[] defenders, Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, CharacterRole role, UnitSynergyBonus bonus)
	{
		foreach (DefenderUnit defenderUnit in defenders)
		{
			if (!((Object)(object)defenderUnit == (Object)null) && defenderUnit.Role == role)
			{
				AddBonus(bonuses, defenderUnit, bonus);
			}
		}
	}

	private void AddBonus(Dictionary<DefenderUnit, UnitSynergyBonus> bonuses, DefenderUnit defender, UnitSynergyBonus bonus)
	{
		if (!((Object)(object)defender == (Object)null) && bonuses.ContainsKey(defender))
		{
			UnitSynergyBonus value = bonuses[defender];
			value.Add(bonus);
			bonuses[defender] = value;
		}
	}

	private void RefreshUi(int defenderCount)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)summaryText != (Object)null)
		{
			summaryText.text = ((defenderCount > 0) ? ("시너지 " + activeEntries.Count + "개 활성") : "시너지 대기중");
			((Graphic)summaryText).color = (Color)((defenderCount > 0) ? Color.white : new Color(0.86f, 0.92f, 1f));
		}
		if ((Object)(object)expandedHeaderText != (Object)null)
		{
			expandedHeaderText.text = ((activeEntries.Count > 0) ? ("활성 시너지 " + activeEntries.Count) : "시너지 가이드");
		}
		int num = ((titleTexts != null) ? titleTexts.Length : 0);
		int num2 = Mathf.Min(activeEntries.Count, num);
		bool flag = activeEntries.Count > num;
		for (int i = 0; i < num; i++)
		{
			bool flag2 = flag && i == num - 1;
			bool flag3 = i < num2 && !flag2;
			if ((Object)(object)titleTexts[i] != (Object)null)
			{
				titleTexts[i].text = (flag3 ? activeEntries[i].title : (flag2 ? ("+ " + (activeEntries.Count - (num - 1)) + "개 더 있음") : ((i == 0) ? "활성 시너지 없음" : string.Empty)));
				((Graphic)titleTexts[i]).color = (Color)((flag3 || flag2) ? Color.white : new Color(0.75f, 0.8f, 0.9f));
			}
			if (detailTexts != null && i < detailTexts.Length && (Object)(object)detailTexts[i] != (Object)null)
			{
				detailTexts[i].text = (flag3 ? activeEntries[i].detail : (flag2 ? "배치가 더 다양해지면 추가 시너지가 표시됩니다." : ((i == 0) ? "같은 역할 또는 같은 속성 태그를 2명 이상 모아보세요." : string.Empty)));
				((Graphic)detailTexts[i]).color = ((flag3 || flag2) ? new Color(0.84f, 0.91f, 1f) : new Color(0.58f, 0.66f, 0.84f));
			}
			if (accentImages != null && i < accentImages.Length && (Object)(object)accentImages[i] != (Object)null)
			{
				((Graphic)accentImages[i]).color = (Color)(flag3 ? activeEntries[i].color : (flag2 ? new Color(1f, 0.76f, 0.28f, 0.92f) : new Color(0.42f, 0.48f, 0.64f, 0.62f)));
			}
			if (iconImages != null && i < iconImages.Length && (Object)(object)iconImages[i] != (Object)null)
			{
				((Graphic)iconImages[i]).color = (Color)(flag3 ? Color.Lerp(activeEntries[i].color, Color.white, 0.38f) : (flag2 ? new Color(1f, 0.84f, 0.38f, 0.7f) : new Color(0.58f, 0.66f, 0.84f, 0.22f)));
			}
		}
	}

	private void AddEntry(string title, string detail, Color color)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
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
		int value;
		return dictionary.TryGetValue(key, out value) ? value : 0;
	}
}
