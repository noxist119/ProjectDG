using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public class TacticalMissionSystem : MonoBehaviour
{
	private enum MissionKind
	{
		GoldReserve,
		PerfectDefense,
		MergeRush,
		RoleCollector,
		LeanDefense,
		BossPreparation,
		SummonSprint,
		EmptySlotDiscipline,
		RareUpgrade,
		LegendaryHunt,
		MonsterHunter,
		BossSlayer,
		NoSummonHold,
		KillStreak,
		HighGradeForge,
		SpendDownGambit,
		UltimateRecipeChase,
		GradeRainbow
	}

	private sealed class MissionInstance
	{
		public MissionKind kind;

		public int tier;

		public string title;

		public string description;

		public string rewardText;

		public int target;

		public int secondaryTarget;

		public int targetRound;

		public int goldReward;

		public int roundGoldBonus;

		public int rouletteGoldMin;

		public int rouletteGoldMax;

		public int jackpotGold;

		public float jackpotChance;

		public float summonDiscount;

		public bool expiresOnRoundStart;

		public int earliestCompleteRound;

		public int completedRound;

		public Color color;

		public Color accentColor;

		public int startRound;

		public int startLife;

		public int startSummons;

		public int startMerges;

		public int startRarePlusMerges;

		public int startEpicPlusMerges;

		public int startLegendaryPlusMerges;

		public int startFinalMerges;

		public int startKills;

		public int startBossKills;

		public string Key => kind.ToString() + ":" + tier;
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
	private GameObject panelRoot;

	[SerializeField]
	private Text panelHeaderText;

	[SerializeField]
	private GameObject activeCardRoot;

	[SerializeField]
	private Text activeTitleText;

	[SerializeField]
	private Text activeDescriptionText;

	[SerializeField]
	private Text activeProgressText;

	[SerializeField]
	private Button[] optionButtons;

	[SerializeField]
	private Text[] optionTitleTexts;

	[SerializeField]
	private Text[] optionDescriptionTexts;

	[SerializeField]
	private Text[] optionRewardTexts;

	[SerializeField]
	private Image[] optionAccentImages;

	[SerializeField]
	private Button closeButton;

	[SerializeField]
	private GameObject completionToastRoot;

	[SerializeField]
	private CanvasGroup completionToastGroup;

	[SerializeField]
	private Text completionToastTitleText;

	[SerializeField]
	private Text completionToastRewardText;

	private const int MaxActiveMissions = 3;

	private const float CompletionToastDuration = 2.4f;

	private readonly List<MissionInstance> activeMissions = new List<MissionInstance>();

	private readonly Dictionary<MissionKind, int> completedFamilyLevels = new Dictionary<MissionKind, int>();

	private readonly HashSet<string> completedMissionKeys = new HashSet<string>();

	private readonly Dictionary<string, int> recentlyExpiredKeys = new Dictionary<string, int>();

	private readonly List<string> recentCompletionFeed = new List<string>();

	private readonly List<MissionInstance> pendingRewardMissions = new List<MissionInstance>();

	private int totalSummons;

	private int totalMerges;

	private int totalRarePlusMerges;

	private int totalEpicPlusMerges;

	private int totalLegendaryPlusMerges;

	private int totalFinalMerges;

	private int totalKills;

	private int totalBossKills;

	private int missionCursor;

	private int completedMissionCount;

	private float toastTimer;

	private bool subscribed;

	private bool resolvingMission;

	public void Configure(DefenseGameController controller, DefenseBoardManager board, Button missionSummaryButton, Text missionSummaryText, GameObject missionPanelRoot, Text missionPanelHeader, GameObject activeMissionCard, Text activeTitle, Text activeDescription, Text activeProgress, Button[] missionOptionButtons, Text[] missionOptionTitles, Text[] missionOptionDescriptions, Text[] missionOptionRewards, Image[] missionOptionAccents, Button missionCloseButton, GameObject missionCompletionToastRoot = null, CanvasGroup missionCompletionToastGroup = null, Text missionCompletionToastTitle = null, Text missionCompletionToastReward = null)
	{
		Unsubscribe();
		gameController = controller;
		boardManager = board;
		summaryButton = missionSummaryButton;
		summaryText = missionSummaryText;
		panelRoot = missionPanelRoot;
		panelHeaderText = missionPanelHeader;
		activeCardRoot = activeMissionCard;
		activeTitleText = activeTitle;
		activeDescriptionText = activeDescription;
		activeProgressText = activeProgress;
		optionButtons = missionOptionButtons;
		optionTitleTexts = missionOptionTitles;
		optionDescriptionTexts = missionOptionDescriptions;
		optionRewardTexts = missionOptionRewards;
		optionAccentImages = missionOptionAccents;
		closeButton = missionCloseButton;
		completionToastRoot = missionCompletionToastRoot;
		completionToastGroup = missionCompletionToastGroup;
		completionToastTitleText = missionCompletionToastTitle;
		completionToastRewardText = missionCompletionToastReward;
		ResetRunState();
		WireUi();
		Subscribe();
		RefillMissions();
		SetPanelOpen(open: false);
		HideCompletionToast();
		RefreshUi();
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

	private void Update()
	{
		UpdateCompletionToast();
	}

	private void ResetRunState()
	{
		activeMissions.Clear();
		pendingRewardMissions.Clear();
		completedFamilyLevels.Clear();
		completedMissionKeys.Clear();
		recentlyExpiredKeys.Clear();
		recentCompletionFeed.Clear();
		totalSummons = 0;
		totalMerges = 0;
		totalRarePlusMerges = 0;
		totalEpicPlusMerges = 0;
		totalLegendaryPlusMerges = 0;
		totalFinalMerges = 0;
		totalKills = 0;
		totalBossKills = 0;
		missionCursor = 0;
		completedMissionCount = 0;
		toastTimer = 0f;
		resolvingMission = false;
	}

	private void WireUi()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		if ((Object)(object)summaryButton != (Object)null)
		{
			((UnityEvent)summaryButton.onClick).RemoveListener(new UnityAction(TogglePanel));
			((UnityEvent)summaryButton.onClick).AddListener(new UnityAction(TogglePanel));
			SetChildText(((Component)summaryButton).transform, "MissionOpenHint", "보기");
		}
		if ((Object)(object)closeButton != (Object)null)
		{
			((UnityEvent)closeButton.onClick).RemoveListener(new UnityAction(ClosePanel));
			((UnityEvent)closeButton.onClick).AddListener(new UnityAction(ClosePanel));
		}
		if (optionButtons == null)
		{
			return;
		}
		for (int i = 0; i < optionButtons.Length; i++)
		{
			if (!((Object)(object)optionButtons[i] == (Object)null))
			{
				((UnityEventBase)optionButtons[i].onClick).RemoveAllListeners();
				((Selectable)optionButtons[i]).interactable = true;
				SetChildText(((Component)optionButtons[i]).transform, "PickLabel", "진행");
			}
		}
	}

	private void Subscribe()
	{
		if (!subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnStateChanged += HandleStateChanged;
			gameController.OnMergeCompleted += HandleMergeCompleted;
			gameController.OnRoundStarted += HandleRoundStarted;
			gameController.OnRoundMissionSettlement += HandleRoundMissionSettlement;
			gameController.OnGameOver += HandleGameOver;
			gameController.OnUnitSummoned += HandleUnitSummoned;
			MonsterUnit.OnMonsterKilled += HandleMonsterKilled;
			MonsterUnit.OnMonsterEscaped += HandleMonsterEscaped;
			subscribed = true;
		}
	}

	private void Unsubscribe()
	{
		if (subscribed && !((Object)(object)gameController == (Object)null))
		{
			gameController.OnStateChanged -= HandleStateChanged;
			gameController.OnMergeCompleted -= HandleMergeCompleted;
			gameController.OnRoundStarted -= HandleRoundStarted;
			gameController.OnRoundMissionSettlement -= HandleRoundMissionSettlement;
			gameController.OnGameOver -= HandleGameOver;
			gameController.OnUnitSummoned -= HandleUnitSummoned;
			MonsterUnit.OnMonsterKilled -= HandleMonsterKilled;
			MonsterUnit.OnMonsterEscaped -= HandleMonsterEscaped;
			subscribed = false;
		}
	}

	private void RefillMissions()
	{
		if ((Object)(object)gameController == (Object)null)
		{
			return;
		}
		ClearExpiredCooldowns();
		int num = 0;
		while (activeMissions.Count < 3 && num < 40)
		{
			MissionInstance missionInstance = CreateNextMissionCandidate(num);
			num++;
			if (missionInstance != null && !IsMissionActive(missionInstance.Key) && !completedMissionKeys.Contains(missionInstance.Key) && !IsRecentlyExpired(missionInstance.Key))
			{
				activeMissions.Add(missionInstance);
			}
		}
	}

	private MissionInstance CreateNextMissionCandidate(int attempt)
	{
		MissionKind[] array = BuildCandidateOrder();
		if (array.Length == 0)
		{
			return null;
		}
		int num = Mathf.Abs(missionCursor + attempt) % array.Length;
		MissionKind kind = array[num];
		int num2 = GetNextTier(kind);
		while (completedMissionKeys.Contains(kind.ToString() + ":" + num2))
		{
			num2++;
		}
		missionCursor++;
		return CreateMission(kind, num2);
	}

	private MissionKind[] BuildCandidateOrder()
	{
		int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0);
		if (num <= 0)
		{
			return new MissionKind[5]
			{
				MissionKind.SummonSprint,
				MissionKind.PerfectDefense,
				MissionKind.MonsterHunter,
				MissionKind.MergeRush,
				MissionKind.RoleCollector
			};
		}
		List<MissionKind> list = new List<MissionKind>
		{
			MissionKind.GoldReserve,
			MissionKind.SummonSprint,
			MissionKind.MergeRush,
			MissionKind.PerfectDefense,
			MissionKind.RoleCollector,
			MissionKind.MonsterHunter
		};
		if (num >= 2)
		{
			list.Add(MissionKind.EmptySlotDiscipline);
			list.Add(MissionKind.RareUpgrade);
			list.Add(MissionKind.KillStreak);
			list.Add(MissionKind.SpendDownGambit);
		}
		if (num >= 4)
		{
			list.Add(MissionKind.LeanDefense);
			list.Add(MissionKind.NoSummonHold);
			list.Add(MissionKind.GradeRainbow);
		}
		if (num >= 5)
		{
			list.Add(MissionKind.BossPreparation);
			list.Add(MissionKind.HighGradeForge);
		}
		if (num >= 6)
		{
			list.Add(MissionKind.LegendaryHunt);
		}
		if (num >= 8 || GetRoundsUntilNextBoss() <= 2)
		{
			list.Add(MissionKind.BossSlayer);
		}
		if (num >= 9)
		{
			list.Add(MissionKind.UltimateRecipeChase);
		}
		return list.ToArray();
	}

	private MissionInstance CreateMission(MissionKind kind, int tier)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0578: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0604: Unknown result type (might be due to invalid IL or missing references)
		//IL_0685: Unknown result type (might be due to invalid IL or missing references)
		//IL_068a: Unknown result type (might be due to invalid IL or missing references)
		//IL_071d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0722: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0883: Unknown result type (might be due to invalid IL or missing references)
		//IL_0888: Unknown result type (might be due to invalid IL or missing references)
		//IL_0921: Unknown result type (might be due to invalid IL or missing references)
		//IL_0926: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_09cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c21: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ccb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cdc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ceb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b70: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b75: Unknown result type (might be due to invalid IL or missing references)
		int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0);
		int value = tier + 1;
		MissionInstance missionInstance = new MissionInstance
		{
			kind = kind,
			tier = tier,
			startRound = num,
			startLife = (((Object)(object)gameController != (Object)null) ? gameController.Life : 0),
			startSummons = totalSummons,
			startMerges = totalMerges,
			startRarePlusMerges = totalRarePlusMerges,
			startEpicPlusMerges = totalEpicPlusMerges,
			startLegendaryPlusMerges = totalLegendaryPlusMerges,
			startFinalMerges = totalFinalMerges,
			startKills = totalKills,
			startBossKills = totalBossKills,
			earliestCompleteRound = num + 1,
			color = new Color(1f, 0.78f, 0.3f),
			accentColor = new Color(1f, 0.92f, 0.58f),
			goldReward = 26 + tier * 9
		};
		switch (kind)
		{
		case MissionKind.GoldReserve:
			missionInstance.target = 95 + tier * 40 + num * 5;
			missionInstance.targetRound = num + 3 + Mathf.Min(2, tier / 2);
			missionInstance.goldReward = 42 + tier * 14;
			missionInstance.title = "골드 창고 " + ToRoman(value);
			missionInstance.description = "라운드가 오기 전까지 목표 골드를 보유하세요. 소환을 참을수록 보상이 커집니다.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드";
			missionInstance.color = new Color(1f, 0.76f, 0.22f);
			missionInstance.expiresOnRoundStart = true;
			break;
		case MissionKind.PerfectDefense:
			missionInstance.targetRound = num + 1;
			missionInstance.goldReward = 34 + tier * 10;
			missionInstance.roundGoldBonus = 1 + tier / 2;
			missionInstance.title = "무결 방어 " + ToRoman(value);
			missionInstance.description = "다음 라운드를 체력 손실 없이 막아내세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드, 라운드 보너스 +" + missionInstance.roundGoldBonus;
			missionInstance.color = new Color(0.42f, 1f, 0.72f);
			break;
		case MissionKind.MergeRush:
			missionInstance.target = 1 + Mathf.Min(4, tier + num / 7);
			missionInstance.targetRound = num + 2;
			missionInstance.earliestCompleteRound = num;
			missionInstance.goldReward = 36 + tier * 12;
			missionInstance.title = "합성 러시 " + ToRoman(value);
			missionInstance.description = "제한 라운드 안에 합성을 성공시켜 성장 속도를 끌어올리세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드";
			missionInstance.color = new Color(0.86f, 0.48f, 1f);
			break;
		case MissionKind.RoleCollector:
			missionInstance.target = Mathf.Clamp(4 + tier, 4, 6);
			missionInstance.targetRound = num + 4;
			missionInstance.goldReward = 30 + tier * 10;
			missionInstance.summonDiscount = Mathf.Min(0.03f + (float)tier * 0.01f, 0.08f);
			missionInstance.title = "역할 컬렉터 " + ToRoman(value);
			missionInstance.description = "서로 다른 역할의 유닛을 모아 시너지 선택지를 넓히세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드, 소환비 할인";
			missionInstance.color = new Color(0.46f, 0.86f, 1f);
			break;
		case MissionKind.LeanDefense:
			missionInstance.target = Mathf.Max(4, 6 - Mathf.Min(2, tier));
			missionInstance.secondaryTarget = 2;
			missionInstance.targetRound = num + 1;
			missionInstance.goldReward = 48 + tier * 13;
			missionInstance.title = "소수 정예 " + ToRoman(value);
			missionInstance.description = "적은 유닛으로 다음 라운드를 버텨내면 높은 보상을 받습니다.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드";
			missionInstance.color = new Color(1f, 0.52f, 0.34f);
			break;
		case MissionKind.BossPreparation:
			missionInstance.target = 1 + Mathf.Min(3, tier);
			missionInstance.targetRound = GetNextBossRound(num);
			missionInstance.goldReward = 40 + tier * 14;
			missionInstance.roundGoldBonus = 2 + tier;
			missionInstance.title = "보스 브레이커 " + ToRoman(value);
			missionInstance.description = "다음 보스가 오기 전까지 전설 이상 유닛을 준비하세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드, 라운드 보너스 +" + missionInstance.roundGoldBonus;
			missionInstance.color = new Color(1f, 0.36f, 0.46f);
			missionInstance.expiresOnRoundStart = true;
			break;
		case MissionKind.SummonSprint:
			missionInstance.target = 3 + Mathf.Min(5, tier + num / 5);
			missionInstance.targetRound = num + 2;
			missionInstance.earliestCompleteRound = num;
			missionInstance.goldReward = 26 + tier * 9;
			missionInstance.title = "소환 스퍼트 " + ToRoman(value);
			missionInstance.description = "빠르게 전장을 채워 초반 화력을 확보하세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드";
			missionInstance.color = new Color(0.3f, 0.76f, 1f);
			break;
		case MissionKind.EmptySlotDiscipline:
			missionInstance.target = Mathf.Clamp(2 + tier, 2, 4);
			missionInstance.targetRound = num + 1;
			missionInstance.goldReward = 38 + tier * 11;
			missionInstance.title = "빈칸 운영 " + ToRoman(value);
			missionInstance.description = "다음 라운드 종료 시 빈 슬롯을 남겨 합성 여지를 유지하세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드";
			missionInstance.color = new Color(0.66f, 0.92f, 1f);
			break;
		case MissionKind.RareUpgrade:
			missionInstance.target = 2 + Mathf.Min(4, tier);
			missionInstance.targetRound = num + 4;
			missionInstance.goldReward = 32 + tier * 11;
			missionInstance.title = "레어 라인업 " + ToRoman(value);
			missionInstance.description = "레어 이상 유닛을 확보해 전투 안정성을 올리세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드";
			missionInstance.color = new Color(0.25f, 0.62f, 1f);
			break;
		case MissionKind.LegendaryHunt:
			missionInstance.target = 1 + tier / 2;
			missionInstance.targetRound = num + 5;
			missionInstance.goldReward = 58 + tier * 18;
			missionInstance.roundGoldBonus = 1 + tier / 2;
			missionInstance.title = "전설 탐색 " + ToRoman(value);
			missionInstance.description = "전설 이상 유닛을 만들어 판을 뒤집을 힘을 모으세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드, 라운드 보너스 +" + missionInstance.roundGoldBonus;
			missionInstance.color = new Color(1f, 0.68f, 0.2f);
			break;
		case MissionKind.MonsterHunter:
			missionInstance.target = 12 + num * 3 + tier * 7;
			missionInstance.targetRound = num + 2;
			missionInstance.earliestCompleteRound = num;
			missionInstance.goldReward = 34 + tier * 10;
			missionInstance.title = "몬스터 사냥 " + ToRoman(value);
			missionInstance.description = "제한 라운드 안에 몬스터를 처치해 추가 골드를 받으세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드";
			missionInstance.color = new Color(0.52f, 1f, 0.58f);
			break;
		case MissionKind.BossSlayer:
			missionInstance.target = 1;
			missionInstance.targetRound = GetNextBossRound(num);
			missionInstance.goldReward = 80 + tier * 22;
			missionInstance.roundGoldBonus = 3 + tier;
			missionInstance.rouletteGoldMin = 8 + tier * 4;
			missionInstance.rouletteGoldMax = 28 + tier * 8;
			missionInstance.jackpotChance = Mathf.Min(0.18f + (float)tier * 0.025f, 0.32f);
			missionInstance.jackpotGold = 45 + tier * 15;
			missionInstance.title = "보스 처단 " + ToRoman(value);
			missionInstance.description = "다음 보스 라운드에서 보스를 쓰러뜨리세요.";
			missionInstance.rewardText = "+" + missionInstance.goldReward + "골드, 라운드 보너스 +" + missionInstance.roundGoldBonus;
			missionInstance.color = new Color(1f, 0.24f, 0.26f);
			break;
		case MissionKind.NoSummonHold:
			missionInstance.targetRound = num + 1;
			missionInstance.secondaryTarget = Mathf.Min(1 + tier / 2, 2);
			missionInstance.goldReward = 24 + tier * 9;
			missionInstance.rouletteGoldMin = 10 + tier * 4;
			missionInstance.rouletteGoldMax = 30 + tier * 7;
			missionInstance.summonDiscount = Mathf.Min(0.04f + (float)tier * 0.01f, 0.1f);
			missionInstance.title = "봉인된 지갑 " + ToRoman(value);
			missionInstance.description = "다음 라운드 동안 소환하지 않고 버텨보세요. 이미 만든 덱을 믿는 고위험 계약입니다.";
			missionInstance.color = new Color(0.38f, 1f, 0.92f);
			break;
		case MissionKind.KillStreak:
			missionInstance.target = 10 + num * 3 + tier * 6;
			missionInstance.targetRound = num + 2;
			missionInstance.goldReward = 28 + tier * 10;
			missionInstance.rouletteGoldMin = 6 + tier * 3;
			missionInstance.rouletteGoldMax = 22 + tier * 7;
			missionInstance.jackpotChance = Mathf.Min(0.12f + (float)tier * 0.02f, 0.26f);
			missionInstance.jackpotGold = 28 + tier * 12;
			missionInstance.title = "처치 콤보 " + ToRoman(value);
			missionInstance.description = "체력을 잃지 않은 채 제한 라운드 안에 몬스터를 몰아 잡으세요. 끊기지 않으면 잭팟이 붙습니다.";
			missionInstance.color = new Color(0.52f, 1f, 0.48f);
			break;
		case MissionKind.HighGradeForge:
			missionInstance.target = 1;
			missionInstance.secondaryTarget = ((tier >= 4) ? 3 : ((tier < 2) ? 1 : 2));
			missionInstance.targetRound = num + 4;
			missionInstance.earliestCompleteRound = num;
			missionInstance.goldReward = 36 + tier * 12;
			missionInstance.rouletteGoldMin = 12 + tier * 5;
			missionInstance.rouletteGoldMax = 42 + tier * 10;
			missionInstance.jackpotChance = Mathf.Min(0.15f + (float)tier * 0.025f, 0.34f);
			missionInstance.jackpotGold = 35 + tier * 14;
			missionInstance.title = "고등급 도박 " + ToRoman(value);
			missionInstance.description = "제한 시간 안에 합성으로 " + CharacterGradeUtility.GetDisplayName((CharacterGrade)missionInstance.secondaryTarget) + " 이상 결과를 뽑으세요. 성공하면 정산 룰렛이 크게 돌아갑니다.";
			missionInstance.color = new Color(1f, 0.48f, 0.92f);
			break;
		case MissionKind.SpendDownGambit:
			missionInstance.target = Mathf.Max(6, Mathf.RoundToInt((float)(((Object)(object)gameController != (Object)null) ? gameController.SummonCost : 10) * 0.65f) + tier * 2);
			missionInstance.targetRound = num + 1;
			missionInstance.goldReward = 18 + tier * 7;
			missionInstance.rouletteGoldMin = 14 + tier * 5;
			missionInstance.rouletteGoldMax = 44 + tier * 11;
			missionInstance.jackpotChance = Mathf.Min(0.1f + (float)tier * 0.018f, 0.24f);
			missionInstance.jackpotGold = 32 + tier * 10;
			missionInstance.title = "올인 운영 " + ToRoman(value);
			missionInstance.description = "다음 라운드 종료 시 골드를 거의 남기지 마세요. 전부 쏟아붓고 살아남으면 큰 보상이 따라옵니다.";
			missionInstance.color = new Color(1f, 0.42f, 0.3f);
			break;
		case MissionKind.UltimateRecipeChase:
			missionInstance.target = 1;
			missionInstance.targetRound = GetNextBossRound(num);
			missionInstance.goldReward = 64 + tier * 20;
			missionInstance.roundGoldBonus = 2 + tier / 2;
			missionInstance.rouletteGoldMin = 18 + tier * 7;
			missionInstance.rouletteGoldMax = 60 + tier * 14;
			missionInstance.jackpotChance = Mathf.Min(0.18f + (float)tier * 0.03f, 0.38f);
			missionInstance.jackpotGold = 60 + tier * 18;
			missionInstance.title = "레시피 추적 " + ToRoman(value);
			missionInstance.description = "다음 보스 전까지 초월 합성 재료를 완성하거나 초월 합성을 성공시키세요. 한 판의 목표를 크게 바꿉니다.";
			missionInstance.color = new Color(0.92f, 0.54f, 1f);
			missionInstance.expiresOnRoundStart = true;
			break;
		case MissionKind.GradeRainbow:
			missionInstance.target = Mathf.Clamp(3 + tier / 2, 3, 5);
			missionInstance.targetRound = num + 3;
			missionInstance.goldReward = 30 + tier * 10;
			missionInstance.summonDiscount = Mathf.Min(0.03f + (float)tier * 0.012f, 0.09f);
			missionInstance.rouletteGoldMin = 8 + tier * 3;
			missionInstance.rouletteGoldMax = 26 + tier * 7;
			missionInstance.title = "등급 무지개 " + ToRoman(value);
			missionInstance.description = "서로 다른 등급의 유닛을 동시에 보유하세요. 마구 합치기보다 판을 설계하는 미션입니다.";
			missionInstance.color = new Color(0.42f, 0.72f, 1f);
			break;
		}
		ApplyRewardPacing(missionInstance);
		missionInstance.accentColor = Color.Lerp(missionInstance.color, Color.white, 0.24f);
		return missionInstance;
	}

	private void ApplyRewardPacing(MissionInstance mission)
	{
		if (mission != null)
		{
			int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0);
			float num2 = 1f;
			if (num <= 2)
			{
				num2 = 0.25f;
			}
			else if (num <= 5)
			{
				num2 = 0.42f;
			}
			else if (num <= 9)
			{
				num2 = 0.62f;
			}
			else if (num <= 14)
			{
				num2 = 0.82f;
			}
			int num3 = ((num <= 2) ? 5 : ((num <= 5) ? 7 : 10));
			mission.goldReward = Mathf.Max(num3, Mathf.RoundToInt((float)mission.goldReward * num2));
			if (mission.rouletteGoldMax > 0)
			{
				mission.rouletteGoldMin = Mathf.Max(1, Mathf.RoundToInt((float)mission.rouletteGoldMin * num2));
				mission.rouletteGoldMax = Mathf.Max(mission.rouletteGoldMin, Mathf.RoundToInt((float)mission.rouletteGoldMax * num2));
			}
			if (mission.jackpotGold > 0)
			{
				mission.jackpotGold = Mathf.Max(num3, Mathf.RoundToInt((float)mission.jackpotGold * num2));
				mission.jackpotChance = Mathf.Clamp01(mission.jackpotChance * Mathf.Lerp(0.72f, 1f, num2));
			}
			if (num <= 5)
			{
				mission.roundGoldBonus = 0;
				mission.summonDiscount = Mathf.Min(mission.summonDiscount, 0.015f);
			}
			else if (num <= 9)
			{
				mission.roundGoldBonus = Mathf.Min(mission.roundGoldBonus, 1);
				mission.summonDiscount = Mathf.Min(mission.summonDiscount, 0.03f);
			}
			else if (num <= 14)
			{
				mission.roundGoldBonus = Mathf.Min(mission.roundGoldBonus, 2);
				mission.summonDiscount = Mathf.Min(mission.summonDiscount, 0.05f);
			}
			mission.rewardText = BuildRewardText(mission);
		}
	}

	private string BuildRewardText(MissionInstance mission)
	{
		if (mission == null)
		{
			return string.Empty;
		}
		string text = "+" + mission.goldReward + "골드";
		if (mission.roundGoldBonus > 0)
		{
			text = text + ", 라운드 보너스 +" + mission.roundGoldBonus;
		}
		if (mission.summonDiscount > 0f)
		{
			text += ", 소환비 할인";
		}
		if (mission.rouletteGoldMax > 0)
		{
			text = text + ", 룰렛 " + mission.rouletteGoldMin + "~" + mission.rouletteGoldMax + "G";
		}
		if (mission.jackpotGold > 0 && mission.jackpotChance > 0f)
		{
			text = text + ", 잭팟 " + Mathf.RoundToInt(mission.jackpotChance * 100f) + "%";
		}
		return text;
	}

	private string BuildRewardSummary(int goldReward, int roundGoldBonus, float summonDiscount)
	{
		string text = ((goldReward > 0) ? ("+" + goldReward + "골드") : "보상 없음");
		if (roundGoldBonus > 0)
		{
			text = text + ", 라운드 보너스 +" + roundGoldBonus;
		}
		if (summonDiscount > 0f)
		{
			text = text + ", 소환비 -" + Mathf.RoundToInt(summonDiscount * 100f) + "%";
		}
		return text;
	}

	private void EvaluateMissions(bool roundCompleted = false, int completedRound = 0)
	{
		if (resolvingMission || (Object)(object)gameController == (Object)null)
		{
			return;
		}
		for (int num = activeMissions.Count - 1; num >= 0; num--)
		{
			MissionInstance mission = activeMissions[num];
			if (IsMissionComplete(mission, roundCompleted, completedRound))
			{
				QueueMissionReward(num, roundCompleted ? completedRound : gameController.CurrentRound);
			}
			else if (IsMissionExpired(mission, roundCompleted, completedRound))
			{
				ExpireMission(num);
			}
		}
		RefillMissions();
		RefreshUi();
	}

	private bool IsMissionComplete(MissionInstance mission, bool roundCompleted, int completedRound)
	{
		if (mission == null || (Object)(object)gameController == (Object)null)
		{
			return false;
		}
		int num = (roundCompleted ? completedRound : gameController.CurrentRound);
		if (num < mission.earliestCompleteRound)
		{
			return false;
		}
		switch (mission.kind)
		{
		case MissionKind.GoldReserve:
			return gameController.Gold >= mission.target;
		case MissionKind.PerfectDefense:
			return roundCompleted && completedRound == mission.targetRound && gameController.Life >= mission.startLife;
		case MissionKind.MergeRush:
			return totalMerges - mission.startMerges >= mission.target;
		case MissionKind.RoleCollector:
			return CountDistinctRoles() >= mission.target;
		case MissionKind.LeanDefense:
			return roundCompleted && completedRound == mission.targetRound && gameController.BoardUnitCount <= mission.target && Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
		case MissionKind.BossPreparation:
			return CountUnitsAtLeast(CharacterGrade.Legendary) >= mission.target;
		case MissionKind.SummonSprint:
			return totalSummons - mission.startSummons >= mission.target;
		case MissionKind.EmptySlotDiscipline:
			return roundCompleted && completedRound == mission.targetRound && gameController.EmptySlotCount >= mission.target;
		case MissionKind.RareUpgrade:
			return CountUnitsAtLeast(CharacterGrade.Rare) >= mission.target;
		case MissionKind.LegendaryHunt:
			return CountUnitsAtLeast(CharacterGrade.Legendary) >= mission.target;
		case MissionKind.MonsterHunter:
			return totalKills - mission.startKills >= mission.target;
		case MissionKind.BossSlayer:
			return totalBossKills - mission.startBossKills >= mission.target;
		case MissionKind.NoSummonHold:
			return roundCompleted && completedRound == mission.targetRound && totalSummons == mission.startSummons && Mathf.Max(0, mission.startLife - gameController.Life) <= mission.secondaryTarget;
		case MissionKind.KillStreak:
			return totalKills - mission.startKills >= mission.target && gameController.Life >= mission.startLife;
		case MissionKind.HighGradeForge:
		{
			CharacterGrade grade = (CharacterGrade)Mathf.Clamp(mission.secondaryTarget, 0, 5);
			return GetMergeResultsAtLeast(grade) - GetStartMergeResultsAtLeast(mission, grade) >= mission.target;
		}
		case MissionKind.SpendDownGambit:
		{
			int num2 = Mathf.Max(0, gameController.Gold - gameController.LastRoundClearGoldReward);
			return roundCompleted && completedRound == mission.targetRound && num2 <= mission.target;
		}
		case MissionKind.UltimateRecipeChase:
			return gameController.CanMergeUltimate() || totalFinalMerges - mission.startFinalMerges >= mission.target;
		case MissionKind.GradeRainbow:
			return CountDistinctGrades() >= mission.target;
		default:
			return false;
		}
	}

	private bool IsMissionExpired(MissionInstance mission, bool roundCompleted, int completedRound)
	{
		if (mission == null || mission.targetRound <= 0 || (Object)(object)gameController == (Object)null)
		{
			return false;
		}
		if (mission.expiresOnRoundStart)
		{
			return gameController.IsRoundRunning && gameController.CurrentRound >= mission.targetRound;
		}
		return roundCompleted && completedRound >= mission.targetRound;
	}

	private void QueueMissionReward(int index, int completedRound)
	{
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		if (index >= 0 && index < activeMissions.Count && !((Object)(object)gameController == (Object)null))
		{
			MissionInstance missionInstance = activeMissions[index];
			activeMissions.RemoveAt(index);
			completedMissionKeys.Add(missionInstance.Key);
			completedFamilyLevels[missionInstance.kind] = GetCompletedFamilyLevel(missionInstance.kind) + 1;
			completedMissionCount++;
			missionInstance.completedRound = Mathf.Max(0, completedRound);
			pendingRewardMissions.Add(missionInstance);
			AddCompletionFeed(missionInstance.title + " 완료 대기  " + missionInstance.rewardText);
			ShowCompletionToast(missionInstance);
			gameController.RequestBanner("미션 완료 대기!  " + missionInstance.title + "  라운드 클리어 시 지급", missionInstance.color, 2.5f);
			RuntimeCameraShake.Request(0.045f, 0.16f);
		}
	}

	private void PayPendingRewards(int completedRound)
	{
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		if (pendingRewardMissions.Count == 0 || (Object)(object)gameController == (Object)null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		float num3 = 0f;
		List<string> list = null;
		for (int i = 0; i < pendingRewardMissions.Count; i++)
		{
			MissionInstance missionInstance = pendingRewardMissions[i];
			if (missionInstance == null)
			{
				continue;
			}
			num += Mathf.Max(0, missionInstance.goldReward);
			num2 += Mathf.Max(0, missionInstance.roundGoldBonus);
			num3 += Mathf.Max(0f, missionInstance.summonDiscount);
			if (missionInstance.rouletteGoldMax > 0)
			{
				int num4 = Mathf.Max(0, missionInstance.rouletteGoldMin);
				int num5 = Mathf.Max(num4, missionInstance.rouletteGoldMax);
				int num6 = Random.Range(num4, num5 + 1);
				num += num6;
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(missionInstance.title + " 룰렛 +" + num6 + "G");
			}
			if (missionInstance.jackpotGold > 0 && missionInstance.jackpotChance > 0f && Random.value <= missionInstance.jackpotChance)
			{
				int num7 = Mathf.Max(1, missionInstance.jackpotGold);
				num += num7;
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add("JACKPOT! " + missionInstance.title + " +" + num7 + "G");
			}
		}
		resolvingMission = true;
		try
		{
			if (num > 0)
			{
				gameController.AddGold(num);
			}
			if (num2 > 0)
			{
				gameController.AddRoundGoldBonus(num2);
			}
			if (num3 > 0f)
			{
				gameController.AddSummonCostDiscount(num3);
			}
		}
		finally
		{
			resolvingMission = false;
		}
		string text = BuildRewardSummary(num, num2, num3);
		pendingRewardMissions.Clear();
		AddCompletionFeed("ROUND " + completedRound + " 미션 정산  " + text);
		if (list != null)
		{
			for (int j = 0; j < list.Count; j++)
			{
				AddCompletionFeed(list[j]);
			}
		}
		ShowSettlementToast(text);
		string text2 = ((list != null && list.Count > 0) ? "미션 룰렛 정산!  " : "미션 보상 정산!  ");
		gameController.RequestBanner(text2 + text, new Color(0.48f, 1f, 0.72f), 2.8f);
		RuntimeCameraShake.Request(0.055f, 0.18f);
	}

	private void ExpireMission(int index)
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (index >= 0 && index < activeMissions.Count)
		{
			MissionInstance missionInstance = activeMissions[index];
			activeMissions.RemoveAt(index);
			recentlyExpiredKeys[missionInstance.Key] = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0) + 3;
			if ((Object)(object)gameController != (Object)null)
			{
				gameController.RequestBanner("미션 갱신  " + missionInstance.title + " 조건이 바뀌었어요", new Color(0.75f, 0.86f, 1f), 1.8f);
			}
		}
	}

	private void HandleStateChanged()
	{
		EvaluateMissions();
	}

	private void HandleUnitSummoned(CharacterDefinition definition)
	{
		totalSummons++;
		EvaluateMissions();
	}

	private void HandleMergeCompleted(MergeResultInfo result)
	{
		totalMerges++;
		if (result.resultGrade >= CharacterGrade.Rare)
		{
			totalRarePlusMerges++;
		}
		if (result.resultGrade >= CharacterGrade.Epic)
		{
			totalEpicPlusMerges++;
		}
		if (result.resultGrade >= CharacterGrade.Legendary)
		{
			totalLegendaryPlusMerges++;
		}
		if (result.isFinalMerge)
		{
			totalFinalMerges++;
		}
		EvaluateMissions();
	}

	private void HandleRoundStarted(int round)
	{
		for (int i = 0; i < activeMissions.Count; i++)
		{
			MissionInstance missionInstance = activeMissions[i];
			if (missionInstance.targetRound == round)
			{
				missionInstance.startLife = (((Object)(object)gameController != (Object)null) ? gameController.Life : missionInstance.startLife);
			}
		}
		EvaluateMissions();
	}

	private void HandleRoundMissionSettlement(int round)
	{
		EvaluateMissions(roundCompleted: true, round);
		PayPendingRewards(round);
		RefillMissions();
		RefreshUi();
	}

	private void HandleMonsterKilled(MonsterUnit monster)
	{
		totalKills++;
		if ((Object)(object)monster != (Object)null && monster.IsBoss)
		{
			totalBossKills++;
		}
		EvaluateMissions();
	}

	private void HandleMonsterEscaped(MonsterUnit monster)
	{
		EvaluateMissions();
	}

	private void HandleGameOver()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (pendingRewardMissions.Count > 0 && (Object)(object)gameController != (Object)null)
		{
			gameController.RequestBanner("방어 실패!  대기 중인 미션 보상은 사라졌어요", new Color(1f, 0.42f, 0.42f), 2.4f);
		}
		activeMissions.Clear();
		pendingRewardMissions.Clear();
		HideCompletionToast();
		RefreshUi();
	}

	private void TogglePanel()
	{
		SetPanelOpen((Object)(object)panelRoot == (Object)null || !panelRoot.activeSelf);
	}

	private void ClosePanel()
	{
		SetPanelOpen(open: false);
	}

	private void SetPanelOpen(bool open)
	{
		if ((Object)(object)panelRoot != (Object)null)
		{
			panelRoot.SetActive(open);
		}
	}

	private void RefreshUi()
	{
		RefreshSummary();
		RefreshPanel();
	}

	private void RefreshSummary()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)summaryText == (Object)null))
		{
			if (pendingRewardMissions.Count > 0)
			{
				summaryText.text = "정산 " + pendingRewardMissions.Count + "개";
				((Graphic)summaryText).color = new Color(1f, 0.92f, 0.58f);
				return;
			}
			summaryText.text = "미션 " + activeMissions.Count + "/" + 3 + "  완료 " + completedMissionCount;
			((Graphic)summaryText).color = ((activeMissions.Count > 0) ? activeMissions[0].accentColor : Color.white);
		}
	}

	private void RefreshPanel()
	{
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)panelHeaderText != (Object)null)
		{
			panelHeaderText.text = "자동 미션 보드";
		}
		if ((Object)(object)activeCardRoot != (Object)null)
		{
			bool flag = activeMissions.Count == 0;
			activeCardRoot.SetActive(flag);
			if (flag)
			{
				SetText(activeTitleText, (pendingRewardMissions.Count > 0) ? "라운드 정산 대기" : "최근 완료");
				string value = ((recentCompletionFeed.Count > 0) ? string.Join("\n", recentCompletionFeed.ToArray()) : "전투 중 조건을 만족하면 완료 대기 상태가 되고, 라운드 클리어 시 보상이 정산됩니다.");
				SetText(activeDescriptionText, value);
				SetText(activeProgressText, "대기 " + pendingRewardMissions.Count + "개  |  완료 " + completedMissionCount + "개  |  진행 " + activeMissions.Count + "개");
			}
		}
		int num = ((optionButtons != null) ? optionButtons.Length : 0);
		for (int i = 0; i < num; i++)
		{
			bool flag2 = i < activeMissions.Count;
			if ((Object)(object)optionButtons[i] != (Object)null)
			{
				((Component)optionButtons[i]).gameObject.SetActive(flag2);
				((Selectable)optionButtons[i]).interactable = true;
				SetChildText(((Component)optionButtons[i]).transform, "PickLabel", "진행");
			}
			if (flag2)
			{
				MissionInstance missionInstance = activeMissions[i];
				SetText(GetText(optionTitleTexts, i), missionInstance.title);
				SetText(GetText(optionDescriptionTexts, i), missionInstance.description + "\n" + GetProgressText(missionInstance));
				SetText(GetText(optionRewardTexts, i), "클리어 정산: " + missionInstance.rewardText);
				Image image = GetImage(optionAccentImages, i);
				if ((Object)(object)image != (Object)null)
				{
					((Graphic)image).color = missionInstance.color;
				}
			}
		}
	}

	private string GetProgressText(MissionInstance mission)
	{
		if (mission == null || (Object)(object)gameController == (Object)null)
		{
			return string.Empty;
		}
		string text = ((mission.targetRound > 0) ? ("  |  ROUND " + mission.targetRound + "까지") : string.Empty);
		switch (mission.kind)
		{
		case MissionKind.GoldReserve:
			return Mathf.Min(gameController.Gold, mission.target) + " / " + mission.target + "G" + text;
		case MissionKind.PerfectDefense:
			return "체력 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / 0" + text;
		case MissionKind.MergeRush:
			return Mathf.Min(totalMerges - mission.startMerges, mission.target) + " / " + mission.target + " 합성" + text;
		case MissionKind.RoleCollector:
			return CountDistinctRoles() + " / " + mission.target + " 역할" + text;
		case MissionKind.LeanDefense:
			return "유닛 " + gameController.BoardUnitCount + " / " + mission.target + ", 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / " + mission.secondaryTarget + text;
		case MissionKind.BossPreparation:
			return CountUnitsAtLeast(CharacterGrade.Legendary) + " / " + mission.target + " 전설+" + text;
		case MissionKind.SummonSprint:
			return Mathf.Min(totalSummons - mission.startSummons, mission.target) + " / " + mission.target + " 소환" + text;
		case MissionKind.EmptySlotDiscipline:
			return gameController.EmptySlotCount + " / " + mission.target + " 빈칸" + text;
		case MissionKind.RareUpgrade:
			return CountUnitsAtLeast(CharacterGrade.Rare) + " / " + mission.target + " 레어+" + text;
		case MissionKind.LegendaryHunt:
			return CountUnitsAtLeast(CharacterGrade.Legendary) + " / " + mission.target + " 전설+" + text;
		case MissionKind.MonsterHunter:
			return Mathf.Min(totalKills - mission.startKills, mission.target) + " / " + mission.target + " 처치" + text;
		case MissionKind.BossSlayer:
			return Mathf.Min(totalBossKills - mission.startBossKills, mission.target) + " / " + mission.target + " 보스 처치" + text;
		case MissionKind.NoSummonHold:
			return "소환 " + (totalSummons - mission.startSummons) + " / 0, 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / " + mission.secondaryTarget + text;
		case MissionKind.KillStreak:
			return Mathf.Min(totalKills - mission.startKills, mission.target) + " / " + mission.target + " 처치, 체력 손실 " + Mathf.Max(0, mission.startLife - gameController.Life) + " / 0" + text;
		case MissionKind.HighGradeForge:
		{
			CharacterGrade grade = (CharacterGrade)Mathf.Clamp(mission.secondaryTarget, 0, 5);
			int num = GetMergeResultsAtLeast(grade) - GetStartMergeResultsAtLeast(mission, grade);
			return Mathf.Min(num, mission.target) + " / " + mission.target + " " + CharacterGradeUtility.GetDisplayName(grade) + "+ 합성" + text;
		}
		case MissionKind.SpendDownGambit:
			return gameController.Gold + " / " + mission.target + "G 이하로 종료" + text;
		case MissionKind.UltimateRecipeChase:
			return gameController.GetUltimateMergeStatus() + "  |  초월 합성 " + Mathf.Min(totalFinalMerges - mission.startFinalMerges, mission.target) + " / " + mission.target + text;
		case MissionKind.GradeRainbow:
			return CountDistinctGrades() + " / " + mission.target + " 등급" + text;
		default:
			return string.Empty;
		}
	}

	private int CountDistinctRoles()
	{
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
		HashSet<CharacterRole> hashSet = new HashSet<CharacterRole>();
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null)
			{
				hashSet.Add(array[i].Role);
			}
		}
		return hashSet.Count;
	}

	private int CountUnitsAtLeast(CharacterGrade grade)
	{
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null && array[i].Grade >= grade)
			{
				num++;
			}
		}
		return num;
	}

	private int CountDistinctGrades()
	{
		DefenderUnit[] array = (((Object)(object)boardManager != (Object)null) ? boardManager.GetAliveDefenders() : new DefenderUnit[0]);
		HashSet<CharacterGrade> hashSet = new HashSet<CharacterGrade>();
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null)
			{
				hashSet.Add(array[i].Grade);
			}
		}
		return hashSet.Count;
	}

	private int GetMergeResultsAtLeast(CharacterGrade grade)
	{
		if (grade <= CharacterGrade.Rare)
		{
			return totalRarePlusMerges;
		}
		if (grade <= CharacterGrade.Epic)
		{
			return totalEpicPlusMerges;
		}
		if (grade <= CharacterGrade.Legendary)
		{
			return totalLegendaryPlusMerges;
		}
		return totalFinalMerges;
	}

	private int GetStartMergeResultsAtLeast(MissionInstance mission, CharacterGrade grade)
	{
		if (mission == null)
		{
			return 0;
		}
		if (grade <= CharacterGrade.Rare)
		{
			return mission.startRarePlusMerges;
		}
		if (grade <= CharacterGrade.Epic)
		{
			return mission.startEpicPlusMerges;
		}
		if (grade <= CharacterGrade.Legendary)
		{
			return mission.startLegendaryPlusMerges;
		}
		return mission.startFinalMerges;
	}

	private int GetNextTier(MissionKind kind)
	{
		return GetCompletedFamilyLevel(kind);
	}

	private int GetCompletedFamilyLevel(MissionKind kind)
	{
		int value;
		return completedFamilyLevels.TryGetValue(kind, out value) ? value : 0;
	}

	private bool IsMissionActive(string key)
	{
		for (int i = 0; i < activeMissions.Count; i++)
		{
			if (activeMissions[i] != null && activeMissions[i].Key == key)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsRecentlyExpired(string key)
	{
		int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0);
		int value;
		return recentlyExpiredKeys.TryGetValue(key, out value) && value > num;
	}

	private void ClearExpiredCooldowns()
	{
		int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0);
		List<string> list = null;
		foreach (KeyValuePair<string, int> recentlyExpiredKey in recentlyExpiredKeys)
		{
			if (recentlyExpiredKey.Value <= num)
			{
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(recentlyExpiredKey.Key);
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				recentlyExpiredKeys.Remove(list[i]);
			}
		}
	}

	private int GetNextBossRound(int round)
	{
		int num = Mathf.Max(10, (round / 10 + 1) * 10);
		return (num <= round) ? (round + 10) : num;
	}

	private int GetRoundsUntilNextBoss()
	{
		int num = (((Object)(object)gameController != (Object)null) ? gameController.CurrentRound : 0);
		return Mathf.Max(0, GetNextBossRound(num) - num);
	}

	private void AddCompletionFeed(string message)
	{
		recentCompletionFeed.Insert(0, message);
		while (recentCompletionFeed.Count > 3)
		{
			recentCompletionFeed.RemoveAt(recentCompletionFeed.Count - 1);
		}
	}

	private void ShowCompletionToast(MissionInstance mission)
	{
		if (!((Object)(object)completionToastRoot == (Object)null) && mission != null)
		{
			ShowToast("미션 완료 대기!", mission.title + "  라운드 종료 시 " + mission.rewardText);
		}
	}

	private void ShowSettlementToast(string rewardSummary)
	{
		if (!((Object)(object)completionToastRoot == (Object)null))
		{
			ShowToast("미션 보상 정산!", rewardSummary);
		}
	}

	private void ShowToast(string title, string reward)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		SetText(completionToastTitleText, title);
		SetText(completionToastRewardText, reward);
		completionToastRoot.SetActive(true);
		toastTimer = 2.4f;
		if ((Object)(object)completionToastGroup != (Object)null)
		{
			completionToastGroup.alpha = 1f;
		}
		RectTransform component = completionToastRoot.GetComponent<RectTransform>();
		if ((Object)(object)component != (Object)null)
		{
			((Transform)component).localScale = Vector3.one;
		}
	}

	private void UpdateCompletionToast()
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)completionToastRoot == (Object)null) && completionToastRoot.activeSelf)
		{
			toastTimer -= Time.unscaledDeltaTime;
			float num = Mathf.Clamp01(toastTimer / 2.4f);
			float alpha = Mathf.Min(Mathf.Clamp01((2.4f - toastTimer) / 0.18f), Mathf.Clamp01(num / 0.2f));
			if ((Object)(object)completionToastGroup != (Object)null)
			{
				completionToastGroup.alpha = alpha;
			}
			RectTransform component = completionToastRoot.GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null)
			{
				float num2 = Mathf.Sin((1f - num) * MathF.PI);
				((Transform)component).localScale = Vector3.one * Mathf.Lerp(0.98f, 1.08f, num2);
			}
			if (toastTimer <= 0f)
			{
				HideCompletionToast();
			}
		}
	}

	private void HideCompletionToast()
	{
		toastTimer = 0f;
		if ((Object)(object)completionToastGroup != (Object)null)
		{
			completionToastGroup.alpha = 0f;
		}
		if ((Object)(object)completionToastRoot != (Object)null)
		{
			completionToastRoot.SetActive(false);
		}
	}

	private string ToRoman(int value)
	{
		if (value <= 1)
		{
			return "I";
		}
		return value switch
		{
			2 => "II", 
			3 => "III", 
			4 => "IV", 
			5 => "V", 
			_ => value.ToString(), 
		};
	}

	private Text GetText(Text[] texts, int index)
	{
		return (texts != null && index >= 0 && index < texts.Length) ? texts[index] : null;
	}

	private Image GetImage(Image[] images, int index)
	{
		return (images != null && index >= 0 && index < images.Length) ? images[index] : null;
	}

	private void SetText(Text target, string value)
	{
		if ((Object)(object)target != (Object)null && target.text != value)
		{
			target.text = value;
		}
	}

	private void SetChildText(Transform root, string childName, string value)
	{
		if (!((Object)(object)root == (Object)null))
		{
			Transform val = root.Find(childName);
			Text target = (((Object)(object)val != (Object)null) ? ((Component)val).GetComponent<Text>() : null);
			SetText(target, value);
		}
	}
}
