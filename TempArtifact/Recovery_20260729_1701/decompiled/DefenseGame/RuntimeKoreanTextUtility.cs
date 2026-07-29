using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

public static class RuntimeKoreanTextUtility
{
	private static readonly Dictionary<string, string> ExactOverrides = new Dictionary<string, string>
	{
		{ "PlayerName", "레드X" },
		{ "LobbyTitle", "로비" },
		{ "LobbySubTitle", "이번 라운드의 추천 조합을 참고하고 전투를 준비하세요." },
		{ "FactoryLabel", "이번 라운드 추천 조합" },
		{ "FactoryHint", "운영 참고용 정보이며 실제 소환 유닛이나 확률에는 영향을 주지 않습니다." },
		{ "LobbyPresetName", "R1 추천 · 안정 성장" },
		{ "LobbyBottomHint", "준비가 끝났다면 전투 시작을 눌러 라운드를 시작하세요." },
		{ "MatchTitle", "전투 준비 중" },
		{ "QueueStatus", "라운드 전장을 준비하는 중..." },
		{ "ResultTitle", "승리" },
		{ "ResultSummary", "라운드 1 클리어" },
		{ "ResultMeta", "연승 보너스 +1" },
		{ "RewardHeader", "보상" },
		{ "LoadoutHeaderText", "이번 라운드 추천 조합" },
		{ "LoadoutSummaryText", "현재 라운드 흐름에 맞춘 운영 참고용 조합입니다." },
		{ "DeckHeader", "추천 핵심 유닛 (참고용)" },
		{ "RosterHeader", "보유 유닛 참고" },
		{ "PresetBadge", "참고" },
		{ "StateText", "준비 단계" },
		{ "MergeResultText", "합성 대기 중" },
		{ "DeckSummaryText", "보유 유닛 0 / 0" },
		{ "CapacityText", "0칸 남음" },
		{ "AugmentHeader", "증강체 선택" },
		{ "AugmentSubtitle", "이번 전투 흐름을 바꿀 보너스 하나를 고르세요." },
		{ "PickLabel", "선택" },
		{ "SummaryText", "시너지 대기중" },
		{ "SummaryHint", "열기" },
		{ "ExpandedHeader", "활성 시너지" },
		{ "ExpandedHint", "같은 역할이나 등급을 모아 강한 조합을 만드세요." },
		{ "SummonButton/Label", "소환" },
		{ "BattleButton/Label", "전투 시작" },
		{ "LobbyButton/Label", "로비" },
		{ "LoadoutButton/Label", "덱" },
		{ "InfoButton/Label", "도감" },
		{ "LobbyCollectionButton/Label", "도감" },
		{ "LobbyBattleButton/Label", "전투 시작" },
		{ "MatchmakingCancelButton/Label", "닫기" },
		{ "ResultLobbyButton/Label", "로비" },
		{ "ResultContinueButton/Label", "계속" },
		{ "LoadoutCloseButton/Label", "닫기" },
		{ "LoadoutCollectionButton/Label", "도감" },
		{ "NormalCard/Title", "일반" },
		{ "RareCard/Title", "레어" },
		{ "EpicCard/Title", "희귀" },
		{ "LegendaryCard/Title", "전설" },
		{ "MythicCard/Title", "초월" },
		{ "CollectionModal/Title", "캐릭터 도감" },
		{ "CollectionCount", "등록 캐릭터 0명" },
		{ "GridHeader", "보유 유닛" },
		{ "SelectedGrade", "일반" },
		{ "SelectedRole", "전위" },
		{ "SkillHeader", "스킬 정보" }
	};

	private static readonly Dictionary<string, string> KnownReplacements = new Dictionary<string, string>
	{
		{ "Gold", "골드" },
		{ "ROUND", "ROUND" },
		{ "ROUND CLEAR", "라운드 클리어" },
		{ "Space Round | S Summon | 1-5 Merge", "Space 라운드 | S 소환 | 1-5 합성" }
	};

	public static string Clean(string value)
	{
		return Clean(null, value);
	}

	public static string Clean(string key, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		string text = TryGetOverride(key);
		if (!string.IsNullOrEmpty(text) && LooksCorrupted(value))
		{
			return text;
		}
		string text2 = ApplyKnownReplacements(value);
		if (!LooksCorrupted(text2))
		{
			return text2;
		}
		string text3 = ApplyKnownReplacements(TryRepairUtf8Mojibake(text2));
		return (SuspiciousScore(text3) <= SuspiciousScore(text2)) ? text3 : text2;
	}

	public static string BuildKey(Text text)
	{
		if ((Object)(object)text == (Object)null)
		{
			return null;
		}
		Transform parent = ((Component)text).transform.parent;
		return ((Object)(object)parent == (Object)null) ? ((Object)text).name : (((Object)parent).name + "/" + ((Object)text).name);
	}

	private static string TryGetOverride(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		if (ExactOverrides.TryGetValue(key, out var value))
		{
			return value;
		}
		int num = key.LastIndexOf('/');
		string key2 = ((num >= 0) ? key.Substring(num + 1) : key);
		return ExactOverrides.TryGetValue(key2, out value) ? value : null;
	}

	private static string ApplyKnownReplacements(string value)
	{
		string text = value;
		foreach (KeyValuePair<string, string> knownReplacement in KnownReplacements)
		{
			text = text.Replace(knownReplacement.Key, knownReplacement.Value);
		}
		return text;
	}

	private static string TryRepairUtf8Mojibake(string value)
	{
		try
		{
			byte[] bytes = Encoding.GetEncoding(949).GetBytes(value);
			return Encoding.UTF8.GetString(bytes);
		}
		catch (Exception)
		{
			return value;
		}
	}

	private static bool LooksCorrupted(string value)
	{
		return SuspiciousScore(value) > 0;
	}

	private static int SuspiciousScore(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return 0;
		}
		int num = 0;
		foreach (char c in value)
		{
			if (c == '\ufffd')
			{
				num += 5;
			}
			else if (c == '?')
			{
				num++;
			}
			else if ((c >= '一' && c <= '鿿') || (c >= '豈' && c <= '\ufaff'))
			{
				num += 2;
			}
		}
		return num;
	}
}
