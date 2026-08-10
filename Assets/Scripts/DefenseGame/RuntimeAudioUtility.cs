using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
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

        // Static Unity object references can survive editor play-session transitions
        // when domain reload is disabled. Each session starts with fresh handles.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            ClipCache.Clear();
            LastPlayedTimes.Clear();
            sfxSource = null;
        }

        public static void PlayButton() => Play(ButtonClip, 0.72f, 0.035f);
        public static void PlayDiceAppear() => Play(DiceAppearClip, 0.82f, 0.06f);
        public static void PlayReroll() => Play(RerollClip, 0.78f, 0.08f);
        public static void PlayMatching() => Play(MatchingClip, 0.64f, 0.45f);
        public static void PlayVictory() => Play(VictoryClip, 0.82f, 0.60f);
        public static void PlayCountdown() => Play(CountdownClip, 0.72f, 0.20f);
        public static void PlayBattleStart() => Play(BattleClip, 0.74f, 0.40f);
        public static void PlayAttack() => Play(AttackClip, 0.55f, 0.045f);
        public static void PlayHit() => Play(HitClip, 0.62f, 0.045f);
        public static void PlayJackpotMinor()
        {
            Play(DiceAppearClip, 0.96f, 0.08f);
            Play(RerollClip, 0.58f, 0.08f);
        }

        public static void PlayJackpotMajor()
        {
            Play(RerollClip, 0.92f, 0.10f);
            Play(MatchingClip, 0.72f, 0.28f);
        }

        public static void PlayMythicSpawn()
        {
            Play(MythicSpawnClip, 0.92f, 0.35f);
        }

        public static void PlayJackpotUltimate()
        {
            Play(TranscendentSpawnClip, 0.95f, 0.45f);
        }

        public static void PlayNamed(string soundName)
        {
            string path = ResolveNamedClip(soundName);
            if (!string.IsNullOrEmpty(path))
            {
                Play(path, 0.72f, 0.04f);
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

            string normalizedPath = NormalizeResourcePath(resourcePath);
            float now = Time.unscaledTime;
            if (minimumInterval > 0f &&
                LastPlayedTimes.TryGetValue(normalizedPath, out float lastPlayed) &&
                now - lastPlayed < minimumInterval)
            {
                return;
            }

            AudioClip clip = LoadClip(normalizedPath);
            AudioSource source = EnsureSfxSource();
            if (clip == null || source == null)
            {
                return;
            }

            LastPlayedTimes[normalizedPath] = now;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static AudioClip LoadClip(string normalizedPath)
        {
            if (ClipCache.TryGetValue(normalizedPath, out AudioClip cached))
            {
                if (cached != null) return cached;
                ClipCache.Remove(normalizedPath);
            }

            AudioClip clip = Resources.Load<AudioClip>(normalizedPath);
            ClipCache[normalizedPath] = clip;
            return clip;
        }

        private static AudioSource EnsureSfxSource()
        {
            if (sfxSource != null)
            {
                return sfxSource;
            }

            GameObject audioObject = GameObject.Find("RuntimeSfxPlayer");
            if (audioObject == null)
            {
                audioObject = new GameObject("RuntimeSfxPlayer");
                Object.DontDestroyOnLoad(audioObject);
            }

            sfxSource = audioObject.GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = audioObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.ignoreListenerPause = true;
            return sfxSource;
        }

        private static string NormalizeResourcePath(string resourcePath)
        {
            string normalized = resourcePath.Replace('\\', '/').Trim();
            if (normalized.StartsWith("Assets/Resources/"))
            {
                normalized = normalized.Substring("Assets/Resources/".Length);
            }

            int extensionIndex = normalized.LastIndexOf('.');
            if (extensionIndex >= 0)
            {
                normalized = normalized.Substring(0, extensionIndex);
            }

            if (!normalized.StartsWith("Audio/"))
            {
                normalized = "Audio/" + normalized;
            }

            return normalized;
        }

        private static string ResolveNamedClip(string soundName)
        {
            if (string.IsNullOrWhiteSpace(soundName))
            {
                return AttackClip;
            }

            string key = soundName.Trim().ToLowerInvariant();
            if (key.Contains("button") || key.Contains("click") || key.Contains("ui"))
            {
                return ButtonClip;
            }

            if (key.Contains("hit") || key.Contains("impact") || key.Contains("damage"))
            {
                return HitClip;
            }

            if (key.Contains("attack") || key.Contains("shoot") || key.Contains("fire"))
            {
                return AttackClip;
            }

            if (key.Contains("spawn") || key.Contains("summon") || key.Contains("appear"))
            {
                return DiceAppearClip;
            }

            if (key.Contains("reroll") || key.Contains("merge") || key.Contains("draw"))
            {
                return RerollClip;
            }

            if (key.Contains("match"))
            {
                return MatchingClip;
            }

            if (key.Contains("victory") || key.Contains("win"))
            {
                return VictoryClip;
            }

            if (key.Contains("count"))
            {
                return CountdownClip;
            }

            if (key.Contains("battle") || key.Contains("round"))
            {
                return BattleClip;
            }

            return NormalizeResourcePath(soundName);
        }
    }
}
