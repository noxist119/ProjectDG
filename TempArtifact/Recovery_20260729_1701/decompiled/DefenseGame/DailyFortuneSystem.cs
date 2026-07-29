using System;
using UnityEngine;

namespace DefenseGame;

public static class DailyFortuneSystem
{
	private static DailyFortuneRule cachedRule;

	private static string cachedDateKey;

	private static readonly DailyFortuneRule[] Rules = new DailyFortuneRule[5]
	{
		new DailyFortuneRule
		{
			title = "합성 예감",
			summary = "Epic 소환률 최대 +5%(초반 제한), 보스 체력 +8%",
			epicSummonChanceBonus = 0.05f,
			bossHealthBonus = 0.08f
		},
		new DailyFortuneRule
		{
			title = "보급 장날",
			summary = "전투 상점 가격 -15%, 보스 체력 +5%",
			shopDiscountRate = 0.15f,
			bossHealthBonus = 0.05f
		},
		new DailyFortuneRule
		{
			title = "초반 순풍",
			summary = "시작 골드 +8, 보스 체력 +6%",
			startGoldBonus = 8,
			bossHealthBonus = 0.06f
		},
		new DailyFortuneRule
		{
			title = "회복의 날",
			summary = "회복 상품 생명력 +1, 보스 체력 +4%",
			lifeRecoveryBonus = 1,
			bossHealthBonus = 0.04f
		},
		new DailyFortuneRule
		{
			title = "대박 기류",
			summary = "Epic 소환률 최대 +3%(초반 제한), 상점 가격 -8%",
			epicSummonChanceBonus = 0.03f,
			shopDiscountRate = 0.08f
		}
	};

	public static DailyFortuneRule Today
	{
		get
		{
			string text = DateTime.Now.ToString("yyyyMMdd");
			if (cachedRule == null || cachedDateKey != text)
			{
				cachedDateKey = text;
				cachedRule = ResolveRule(DateTime.Now);
			}
			return cachedRule;
		}
	}

	public static string TodaySummary
	{
		get
		{
			DailyFortuneRule today = Today;
			return (today != null) ? ("오늘의 운세: " + today.title + " / " + today.summary) : "오늘의 운세 준비 중";
		}
	}

	private static DailyFortuneRule ResolveRule(DateTime date)
	{
		if (Rules == null || Rules.Length == 0)
		{
			return new DailyFortuneRule
			{
				title = "기본 운세",
				summary = "특별 규칙 없음"
			};
		}
		int num = date.Year * 10000 + date.Month * 100 + date.Day;
		int num2 = Mathf.Abs(num * 31 + date.DayOfYear * 17) % Rules.Length;
		return Rules[num2];
	}
}
