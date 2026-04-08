using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class SimpleGameHUD : MonoBehaviour
    {
        [SerializeField] private DefenseGameController gameController;
        [SerializeField] private Text goldText;
        [SerializeField] private Text lifeText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text boardText;
        [SerializeField] private Text contentText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text mergeResultText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text roundBannerText;
        [SerializeField] private string hintMessage = "Space Round | S Summon | 1-4 Merge | C Add Heroes | M Add Monsters";
        [SerializeField] private float mergeBannerTimer;
        [SerializeField] private string mergeBannerMessage = string.Empty;
        [SerializeField] private float roundBannerTimer;

        public void Configure(DefenseGameController controller, Text gold, Text lifeLabel, Text round, Text board, Text content, Text hint, Text mergeResult, Text countdown, Text roundBanner, string overrideHint = null)
        {
            if (gameController != null)
            {
                gameController.OnStateChanged -= Refresh;
                gameController.OnMergeCompleted -= HandleMergeCompleted;
                gameController.OnRoundCountdownChanged -= HandleRoundCountdownChanged;
                gameController.OnBannerRequested -= HandleBannerRequested;
            }

            gameController = controller;
            goldText = gold;
            lifeText = lifeLabel;
            roundText = round;
            boardText = board;
            contentText = content;
            hintText = hint;
            mergeResultText = mergeResult;
            countdownText = countdown;
            roundBannerText = roundBanner;
            if (!string.IsNullOrWhiteSpace(overrideHint))
            {
                hintMessage = overrideHint;
            }

            if (gameController != null)
            {
                gameController.OnStateChanged -= Refresh;
                gameController.OnStateChanged += Refresh;
                gameController.OnMergeCompleted -= HandleMergeCompleted;
                gameController.OnMergeCompleted += HandleMergeCompleted;
                gameController.OnRoundCountdownChanged -= HandleRoundCountdownChanged;
                gameController.OnRoundCountdownChanged += HandleRoundCountdownChanged;
                gameController.OnBannerRequested -= HandleBannerRequested;
                gameController.OnBannerRequested += HandleBannerRequested;
            }

            Refresh();
        }

        private void OnEnable()
        {
            if (gameController != null)
            {
                gameController.OnStateChanged -= Refresh;
                gameController.OnStateChanged += Refresh;
                gameController.OnMergeCompleted -= HandleMergeCompleted;
                gameController.OnMergeCompleted += HandleMergeCompleted;
                gameController.OnRoundCountdownChanged -= HandleRoundCountdownChanged;
                gameController.OnRoundCountdownChanged += HandleRoundCountdownChanged;
                gameController.OnBannerRequested -= HandleBannerRequested;
                gameController.OnBannerRequested += HandleBannerRequested;
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (gameController != null)
            {
                gameController.OnStateChanged -= Refresh;
                gameController.OnMergeCompleted -= HandleMergeCompleted;
                gameController.OnRoundCountdownChanged -= HandleRoundCountdownChanged;
                gameController.OnBannerRequested -= HandleBannerRequested;
            }
        }

        private void Update()
        {
            if (mergeBannerTimer > 0f)
            {
                mergeBannerTimer -= Time.deltaTime;
                if (mergeBannerTextAvailable())
                {
                    Color color = mergeResultText.color;
                    color.a = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(mergeBannerTimer / 3f));
                    mergeResultText.color = color;
                }
            }
            else if (mergeBannerTextAvailable())
            {
                mergeResultText.text = "Merge Result : waiting";
                Color color = mergeResultText.color;
                color.a = 0.65f;
                mergeResultText.color = color;
            }

            if (roundBannerText != null)
            {
                if (roundBannerTimer > 0f)
                {
                    roundBannerTimer -= Time.deltaTime;
                    Color color = roundBannerText.color;
                    color.a = Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(roundBannerTimer / 2.5f));
                    roundBannerText.color = color;
                }
                else if (!string.IsNullOrEmpty(roundBannerText.text))
                {
                    roundBannerText.text = string.Empty;
                }
            }
        }

        public void Refresh()
        {
            if (gameController == null)
            {
                return;
            }

            if (goldText != null) goldText.text = "Gold : " + gameController.Gold;
            if (lifeText != null) lifeText.text = "Life : " + gameController.Life;
            if (roundText != null) roundText.text = "Round : " + gameController.CurrentRound + (gameController.IsBossRound ? " BOSS" : string.Empty);
            if (boardText != null) boardText.text = "Units : " + gameController.BoardUnitCount;
            if (contentText != null) contentText.text = "Characters : " + gameController.CharacterCount + " / Monsters : " + gameController.MonsterCount;
            if (hintText != null) hintText.text = hintMessage;
            if (mergeBannerTextAvailable() && string.IsNullOrWhiteSpace(mergeBannerMessage))
            {
                mergeResultText.text = "Merge Result : waiting";
            }
        }

        private void HandleMergeCompleted(MergeResultInfo result)
        {
            mergeBannerMessage = result.BuildMessage();
            mergeBannerTimer = 3f;

            if (mergeBannerTextAvailable())
            {
                mergeResultText.text = "Merge Result : " + mergeBannerMessage;
                mergeResultText.color = result.resultColor;
            }
        }

        private void HandleRoundCountdownChanged(int countdown)
        {
            if (countdownText == null)
            {
                return;
            }

            countdownText.text = countdown > 0 ? countdown.ToString() : string.Empty;
            Color color = countdownText.color;
            color.a = countdown > 0 ? 1f : 0f;
            countdownText.color = color;
        }

        private void HandleBannerRequested(string message, Color color, float duration)
        {
            if (roundBannerText == null)
            {
                return;
            }

            roundBannerText.text = message;
            roundBannerText.color = color;
            roundBannerTimer = duration;
        }

        private bool mergeBannerTextAvailable()
        {
            return mergeResultText != null;
        }
    }
}
