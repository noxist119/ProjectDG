using System.Collections;
using UnityEngine;

namespace DefenseGame
{
    public class RuntimeBgmController : MonoBehaviour
    {
        private const string MainTrackKey = "main";
        private const string BossTrackKey = "boss";
        private static readonly string[] DefaultMainResourcePaths =
        {
            "Audio/MainBGM",
            "Audio/MainBGM_2",
            "Audio/MainBGM_3",
            "Audio/MainBGM_4",
            "Audio/MainBGM_5"
        };

        private DefenseGameController gameController;
        private AudioSource audioSource;
        private AudioClip[] mainClips;
        private AudioClip bossClip;
        private Coroutine transitionRoutine;
        private string currentTrackKey;
        private bool[] warnedMissingMain;
        private bool warnedMissingBoss;

        private string[] mainResourcePaths = DefaultMainResourcePaths;
        private string bossResourcePath = "Audio/BossBGM";
        private float mainVolume = 0.55f;
        private float bossVolume = 0.72f;
        private float fadeDuration = 0.6f;

        public void Configure(
            DefenseGameController controller,
            string newMainResourcePath,
            string newBossResourcePath,
            float newMainVolume,
            float newBossVolume,
            float newFadeDuration)
        {
            Unsubscribe();

            gameController = controller;
            mainResourcePaths = new[]
            {
                string.IsNullOrWhiteSpace(newMainResourcePath) ? DefaultMainResourcePaths[0] : newMainResourcePath,
                DefaultMainResourcePaths[1],
                DefaultMainResourcePaths[2],
                DefaultMainResourcePaths[3],
                DefaultMainResourcePaths[4]
            };
            bossResourcePath = string.IsNullOrWhiteSpace(newBossResourcePath) ? "Audio/BossBGM" : newBossResourcePath;
            mainVolume = Mathf.Clamp01(newMainVolume);
            bossVolume = Mathf.Clamp01(newBossVolume);
            fadeDuration = Mathf.Max(0f, newFadeDuration);

            EnsureAudioSource();
            LoadClips();
            Subscribe();
            PlayMainForRound(1, true);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (gameController == null)
            {
                return;
            }

            gameController.OnRoundStarted -= HandleRoundStarted;
            gameController.OnRoundCompleted -= HandleRoundCompleted;
            gameController.OnGameOver -= HandleGameOver;
            gameController.OnRoundStarted += HandleRoundStarted;
            gameController.OnRoundCompleted += HandleRoundCompleted;
            gameController.OnGameOver += HandleGameOver;
        }

        private void Unsubscribe()
        {
            if (gameController == null)
            {
                return;
            }

            gameController.OnRoundStarted -= HandleRoundStarted;
            gameController.OnRoundCompleted -= HandleRoundCompleted;
            gameController.OnGameOver -= HandleGameOver;
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
        }

        private void LoadClips()
        {
            mainClips = new AudioClip[mainResourcePaths.Length];
            warnedMissingMain = new bool[mainResourcePaths.Length];
            for (int i = 0; i < mainResourcePaths.Length; i++)
            {
                mainClips[i] = Resources.Load<AudioClip>(mainResourcePaths[i]);
                if (mainClips[i] == null && !warnedMissingMain[i])
                {
                    warnedMissingMain[i] = true;
                    Debug.LogWarning("Main BGM was not found at Resources/" + mainResourcePaths[i]);
                }
            }

            bossClip = Resources.Load<AudioClip>(bossResourcePath);

            if (bossClip == null && !warnedMissingBoss)
            {
                warnedMissingBoss = true;
                Debug.LogWarning("Boss BGM was not found at Resources/" + bossResourcePath);
            }
        }

        private void HandleRoundStarted(int round)
        {
            if (IsBossRound(round))
            {
                PlayBoss();
                return;
            }

            PlayMainForRound(round, false);
        }

        private void HandleRoundCompleted(int round)
        {
            PlayMainForRound(IsBossRound(round) ? round + 1 : round, false);
        }

        private void HandleGameOver()
        {
            int round = gameController != null ? gameController.CurrentRound : 1;
            PlayMainForRound(IsBossRound(round) ? round + 1 : round, false);
        }

        public static string GetRegularMainResourcePathForRound(int round)
        {
            int segmentIndex = Mathf.Max(0, (round - 1) / 10) % DefaultMainResourcePaths.Length;
            return DefaultMainResourcePaths[segmentIndex];
        }

        public static bool IsBossRoundForAudio(int round)
        {
            return round > 0 && round % 10 == 0;
        }

        private void PlayMainForRound(int round, bool immediate)
        {
            EnsurePlaybackResources();
            int segmentIndex = Mathf.Max(0, (round - 1) / 10) % mainResourcePaths.Length;
            PlayClip(mainClips[segmentIndex], mainVolume, MainTrackKey, immediate);
        }

        private void PlayBoss()
        {
            EnsurePlaybackResources();
            if (bossClip == null)
            {
                if (!warnedMissingBoss)
                {
                    warnedMissingBoss = true;
                    Debug.LogWarning("Boss BGM was not found at Resources/" + bossResourcePath);
                }

                return;
            }

            PlayClip(bossClip, bossVolume, BossTrackKey, false);
        }
        private void EnsurePlaybackResources()
        {
            EnsureAudioSource();
            if (mainClips == null || mainClips.Length != mainResourcePaths.Length || bossClip == null)
            {
                LoadClips();
            }
        }


        private static bool IsBossRound(int round)
        {
            return IsBossRoundForAudio(round);
        }
        private void PlayClip(AudioClip clip, float volume, string trackKey, bool immediate)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            if (currentTrackKey == trackKey && audioSource.clip == clip && audioSource.isPlaying)
            {
                audioSource.volume = volume;
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (immediate || fadeDuration <= 0f || !audioSource.isPlaying)
            {
                SetAndPlay(clip, volume, trackKey);
                return;
            }

            transitionRoutine = StartCoroutine(FadeToClip(clip, volume, trackKey));
        }

        private IEnumerator FadeToClip(AudioClip clip, float targetVolume, string trackKey)
        {
            float startVolume = audioSource.volume;
            float fadeOutDuration = fadeDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, fadeOutDuration <= 0f ? 1f : elapsed / fadeOutDuration);
                yield return null;
            }

            SetAndPlay(clip, 0f, trackKey);

            elapsed = 0f;
            float fadeInDuration = fadeDuration * 0.5f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(0f, targetVolume, fadeInDuration <= 0f ? 1f : elapsed / fadeInDuration);
                yield return null;
            }

            audioSource.volume = targetVolume;
            transitionRoutine = null;
        }

        private void SetAndPlay(AudioClip clip, float volume, string trackKey)
        {
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.loop = true;
            audioSource.Play();
            currentTrackKey = trackKey;
        }
    }
}
