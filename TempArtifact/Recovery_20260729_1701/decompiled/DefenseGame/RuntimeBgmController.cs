using System.Collections;
using UnityEngine;

namespace DefenseGame;

public class RuntimeBgmController : MonoBehaviour
{
	private const string MainTrackKey = "main";

	private const string BossTrackKey = "boss";

	private DefenseGameController gameController;

	private AudioSource audioSource;

	private AudioClip mainClip;

	private AudioClip bossClip;

	private Coroutine transitionRoutine;

	private string currentTrackKey;

	private bool warnedMissingMain;

	private bool warnedMissingBoss;

	private string mainResourcePath = "Audio/MainBGM";

	private string bossResourcePath = "Audio/BossBGM";

	private float mainVolume = 0.55f;

	private float bossVolume = 0.72f;

	private float fadeDuration = 0.6f;

	public void Configure(DefenseGameController controller, string newMainResourcePath, string newBossResourcePath, float newMainVolume, float newBossVolume, float newFadeDuration)
	{
		Unsubscribe();
		gameController = controller;
		mainResourcePath = (string.IsNullOrWhiteSpace(newMainResourcePath) ? "Audio/MainBGM" : newMainResourcePath);
		bossResourcePath = (string.IsNullOrWhiteSpace(newBossResourcePath) ? "Audio/BossBGM" : newBossResourcePath);
		mainVolume = Mathf.Clamp01(newMainVolume);
		bossVolume = Mathf.Clamp01(newBossVolume);
		fadeDuration = Mathf.Max(0f, newFadeDuration);
		EnsureAudioSource();
		LoadClips();
		Subscribe();
		PlayMain(immediate: true);
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
		if (!((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundStarted -= HandleRoundStarted;
			gameController.OnRoundCompleted -= HandleRoundCompleted;
			gameController.OnGameOver -= HandleGameOver;
			gameController.OnRoundStarted += HandleRoundStarted;
			gameController.OnRoundCompleted += HandleRoundCompleted;
			gameController.OnGameOver += HandleGameOver;
		}
	}

	private void Unsubscribe()
	{
		if (!((Object)(object)gameController == (Object)null))
		{
			gameController.OnRoundStarted -= HandleRoundStarted;
			gameController.OnRoundCompleted -= HandleRoundCompleted;
			gameController.OnGameOver -= HandleGameOver;
		}
	}

	private void EnsureAudioSource()
	{
		if ((Object)(object)audioSource == (Object)null)
		{
			audioSource = ((Component)this).GetComponent<AudioSource>();
		}
		if ((Object)(object)audioSource == (Object)null)
		{
			audioSource = ((Component)this).gameObject.AddComponent<AudioSource>();
		}
		audioSource.loop = true;
		audioSource.playOnAwake = false;
		audioSource.spatialBlend = 0f;
		audioSource.ignoreListenerPause = true;
	}

	private void LoadClips()
	{
		mainClip = Resources.Load<AudioClip>(mainResourcePath);
		bossClip = Resources.Load<AudioClip>(bossResourcePath);
		if ((Object)(object)mainClip == (Object)null && !warnedMissingMain)
		{
			warnedMissingMain = true;
			Debug.LogWarning((object)("Main BGM was not found at Resources/" + mainResourcePath));
		}
		if ((Object)(object)bossClip == (Object)null && !warnedMissingBoss)
		{
			warnedMissingBoss = true;
			Debug.LogWarning((object)("Boss BGM was not found at Resources/" + bossResourcePath));
		}
	}

	private void HandleRoundStarted(int round)
	{
		if (round > 0 && round % 10 == 0)
		{
			PlayBoss();
		}
		else
		{
			PlayMain(immediate: false);
		}
	}

	private void HandleRoundCompleted(int round)
	{
		PlayMain(immediate: false);
	}

	private void HandleGameOver()
	{
		PlayMain(immediate: false);
	}

	private void PlayMain(bool immediate)
	{
		PlayClip(mainClip, mainVolume, "main", immediate);
	}

	private void PlayBoss()
	{
		if ((Object)(object)bossClip == (Object)null)
		{
			if (!warnedMissingBoss)
			{
				warnedMissingBoss = true;
				Debug.LogWarning((object)("Boss BGM was not found at Resources/" + bossResourcePath));
			}
		}
		else
		{
			PlayClip(bossClip, bossVolume, "boss", immediate: false);
		}
	}

	private void PlayClip(AudioClip clip, float volume, string trackKey, bool immediate)
	{
		if ((Object)(object)clip == (Object)null || (Object)(object)audioSource == (Object)null)
		{
			return;
		}
		if (currentTrackKey == trackKey && (Object)(object)audioSource.clip == (Object)(object)clip && audioSource.isPlaying)
		{
			audioSource.volume = volume;
			return;
		}
		if (transitionRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(transitionRoutine);
			transitionRoutine = null;
		}
		if (immediate || fadeDuration <= 0f || !audioSource.isPlaying)
		{
			SetAndPlay(clip, volume, trackKey);
		}
		else
		{
			transitionRoutine = ((MonoBehaviour)this).StartCoroutine(FadeToClip(clip, volume, trackKey));
		}
	}

	private IEnumerator FadeToClip(AudioClip clip, float targetVolume, string trackKey)
	{
		float startVolume = audioSource.volume;
		float fadeOutDuration = fadeDuration * 0.5f;
		float elapsed = 0f;
		while (elapsed < fadeOutDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			audioSource.volume = Mathf.Lerp(startVolume, 0f, (fadeOutDuration <= 0f) ? 1f : (elapsed / fadeOutDuration));
			yield return null;
		}
		SetAndPlay(clip, 0f, trackKey);
		elapsed = 0f;
		float fadeInDuration = fadeDuration * 0.5f;
		while (elapsed < fadeInDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			audioSource.volume = Mathf.Lerp(0f, targetVolume, (fadeInDuration <= 0f) ? 1f : (elapsed / fadeInDuration));
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
