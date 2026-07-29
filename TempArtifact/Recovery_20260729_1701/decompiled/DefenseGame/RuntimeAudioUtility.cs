using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

public static class RuntimeAudioUtility
{
	private const string ButtonClip = "Audio/sfx_common_button";

	private const string DiceAppearClip = "Audio/sfx_ingame_dice_appear";

	private const string RerollClip = "Audio/sfx_ingame_reroll";

	private const string MatchingClip = "Audio/mfx_ingame_matching";

	private const string VictoryClip = "Audio/BGM_Ingame_victory";

	private const string CountdownClip = "Audio/ingame_countdown";

	private const string BattleClip = "Audio/ingame_battle";

	private const string AttackClip = "Audio/ingame_commondice_attack";

	private const string HitClip = "Audio/ingame_commondice_hit";

	private const string MythicSpawnClip = "Audio/sfx_spawn_Mythic";

	private const string TranscendentSpawnClip = "Audio/sfx_spawn_Transcendent";

	private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

	private static readonly Dictionary<string, float> LastPlayedTimes = new Dictionary<string, float>();

	private static AudioSource sfxSource;

	public static void PlayButton()
	{
		Play("Audio/sfx_common_button", 0.72f, 0.035f);
	}

	public static void PlayDiceAppear()
	{
		Play("Audio/sfx_ingame_dice_appear", 0.82f, 0.06f);
	}

	public static void PlayReroll()
	{
		Play("Audio/sfx_ingame_reroll", 0.78f, 0.08f);
	}

	public static void PlayMatching()
	{
		Play("Audio/mfx_ingame_matching", 0.64f, 0.45f);
	}

	public static void PlayVictory()
	{
		Play("Audio/BGM_Ingame_victory", 0.82f, 0.6f);
	}

	public static void PlayCountdown()
	{
		Play("Audio/ingame_countdown", 0.72f, 0.2f);
	}

	public static void PlayBattleStart()
	{
		Play("Audio/ingame_battle", 0.74f, 0.4f);
	}

	public static void PlayAttack()
	{
		Play("Audio/ingame_commondice_attack", 0.55f, 0.045f);
	}

	public static void PlayHit()
	{
		Play("Audio/ingame_commondice_hit", 0.62f, 0.045f);
	}

	public static void PlayJackpotMinor()
	{
		Play("Audio/sfx_ingame_dice_appear", 0.96f, 0.08f);
		Play("Audio/sfx_ingame_reroll", 0.58f, 0.08f);
	}

	public static void PlayJackpotMajor()
	{
		Play("Audio/sfx_ingame_reroll", 0.92f, 0.1f);
		Play("Audio/mfx_ingame_matching", 0.72f, 0.28f);
	}

	public static void PlayMythicSpawn()
	{
		Play("Audio/sfx_spawn_Mythic", 0.92f, 0.35f);
	}

	public static void PlayJackpotUltimate()
	{
		Play("Audio/sfx_spawn_Transcendent", 0.95f, 0.45f);
	}

	public static void PlayNamed(string soundName)
	{
		string text = ResolveNamedClip(soundName);
		if (!string.IsNullOrEmpty(text))
		{
			Play(text, 0.72f, 0.04f);
		}
	}

	public static void PlayIndexed(int soundIndex)
	{
		switch (soundIndex)
		{
		case 0:
			PlayAttack();
			break;
		case 1:
			PlayHit();
			break;
		case 2:
			PlayDiceAppear();
			break;
		case 3:
			PlayReroll();
			break;
		case 4:
			PlayBattleStart();
			break;
		default:
			PlayButton();
			break;
		}
	}

	public static void Play(string resourcePath, float volume = 1f, float minimumInterval = 0f)
	{
		if (!Application.isPlaying || string.IsNullOrWhiteSpace(resourcePath))
		{
			return;
		}
		string text = NormalizeResourcePath(resourcePath);
		float unscaledTime = Time.unscaledTime;
		if (!(minimumInterval > 0f) || !LastPlayedTimes.TryGetValue(text, out var value) || !(unscaledTime - value < minimumInterval))
		{
			AudioClip val = LoadClip(text);
			AudioSource val2 = EnsureSfxSource();
			if (!((Object)(object)val == (Object)null) && !((Object)(object)val2 == (Object)null))
			{
				LastPlayedTimes[text] = unscaledTime;
				val2.PlayOneShot(val, Mathf.Clamp01(volume));
			}
		}
	}

	private static AudioClip LoadClip(string normalizedPath)
	{
		if (ClipCache.TryGetValue(normalizedPath, out var value))
		{
			return value;
		}
		AudioClip val = Resources.Load<AudioClip>(normalizedPath);
		ClipCache[normalizedPath] = val;
		return val;
	}

	private static AudioSource EnsureSfxSource()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		if ((Object)(object)sfxSource != (Object)null)
		{
			return sfxSource;
		}
		GameObject val = GameObject.Find("RuntimeSfxPlayer");
		if ((Object)(object)val == (Object)null)
		{
			val = new GameObject("RuntimeSfxPlayer");
			Object.DontDestroyOnLoad((Object)(object)val);
		}
		sfxSource = val.GetComponent<AudioSource>();
		if ((Object)(object)sfxSource == (Object)null)
		{
			sfxSource = val.AddComponent<AudioSource>();
		}
		sfxSource.playOnAwake = false;
		sfxSource.loop = false;
		sfxSource.spatialBlend = 0f;
		sfxSource.ignoreListenerPause = true;
		return sfxSource;
	}

	private static string NormalizeResourcePath(string resourcePath)
	{
		string text = resourcePath.Replace('\\', '/').Trim();
		if (text.StartsWith("Assets/Resources/"))
		{
			text = text.Substring("Assets/Resources/".Length);
		}
		int num = text.LastIndexOf('.');
		if (num >= 0)
		{
			text = text.Substring(0, num);
		}
		if (!text.StartsWith("Audio/"))
		{
			text = "Audio/" + text;
		}
		return text;
	}

	private static string ResolveNamedClip(string soundName)
	{
		if (string.IsNullOrWhiteSpace(soundName))
		{
			return "Audio/ingame_commondice_attack";
		}
		string text = soundName.Trim().ToLowerInvariant();
		if (text.Contains("button") || text.Contains("click") || text.Contains("ui"))
		{
			return "Audio/sfx_common_button";
		}
		if (text.Contains("hit") || text.Contains("impact") || text.Contains("damage"))
		{
			return "Audio/ingame_commondice_hit";
		}
		if (text.Contains("attack") || text.Contains("shoot") || text.Contains("fire"))
		{
			return "Audio/ingame_commondice_attack";
		}
		if (text.Contains("spawn") || text.Contains("summon") || text.Contains("appear"))
		{
			return "Audio/sfx_ingame_dice_appear";
		}
		if (text.Contains("reroll") || text.Contains("merge") || text.Contains("draw"))
		{
			return "Audio/sfx_ingame_reroll";
		}
		if (text.Contains("match"))
		{
			return "Audio/mfx_ingame_matching";
		}
		if (text.Contains("victory") || text.Contains("win"))
		{
			return "Audio/BGM_Ingame_victory";
		}
		if (text.Contains("count"))
		{
			return "Audio/ingame_countdown";
		}
		if (text.Contains("battle") || text.Contains("round"))
		{
			return "Audio/ingame_battle";
		}
		return NormalizeResourcePath(soundName);
	}
}
