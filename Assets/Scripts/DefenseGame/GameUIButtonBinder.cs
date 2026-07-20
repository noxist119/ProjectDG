using UnityEngine;

namespace DefenseGame
{
    public class GameUIButtonBinder : MonoBehaviour
    {
        [SerializeField] private DefenseGameController gameController;

        public void Configure(DefenseGameController controller)
        {
            gameController = controller;
        }

        public void OnClickStartRound()
        {
            if (gameController != null) gameController.StartRound();
        }

        public void OnClickSummon()
        {
            if (gameController != null) gameController.TrySummon();
        }

        public void OnClickMergeNormal()
        {
            TryMergeWithFeedback(CharacterGrade.Normal);
        }

        public void OnClickMergeRare()
        {
            TryMergeWithFeedback(CharacterGrade.Rare);
        }

        public void OnClickMergeEpic()
        {
            TryMergeWithFeedback(CharacterGrade.Epic);
        }

        public void OnClickMergeLegendary()
        {
            TryMergeWithFeedback(CharacterGrade.Legendary);
        }

        public void OnClickMergeMythic()
        {
            TryMergeWithFeedback(CharacterGrade.Mythic);
        }

        private void TryMergeWithFeedback(CharacterGrade grade)
        {
            if (gameController == null)
            {
                Debug.LogWarning("Merge input ignored because DefenseGameController is not configured.");
                return;
            }

            string gradeName = CharacterGradeUtility.GetDisplayName(grade);
            if (gameController.IsCombatInteractionLocked)
            {
                gameController.RequestBanner(
                    "\uC804\uD22C \uC911\uC5D0\uB294 \uD569\uC131\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4. \uC6B4\uBA85 \uCE74\uB4DC\uC758 \uC804\uD22C \uD3B8\uC9D1 \uD6A8\uACFC\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4.",
                    new Color(1f, 0.66f, 0.24f),
                    2.0f);
                return;
            }

            int ownedCount = gameController.CountUnitsOfGrade(grade);
            if (grade != CharacterGrade.Mythic && ownedCount < 3)
            {
                gameController.RequestBanner(
                    gradeName + " \uD569\uC131 \uC7AC\uB8CC \uBD80\uC871  " + ownedCount + "/3",
                    new Color(1f, 0.66f, 0.24f),
                    1.8f);
                return;
            }

            if (!gameController.TryMerge(grade))
            {
                string reason = string.IsNullOrWhiteSpace(gameController.LastMergeFailureReason)
                    ? gradeName + " \uD569\uC131\uC744 \uC644\uB8CC\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4."
                    : gameController.LastMergeFailureReason;
                gameController.RequestBanner(
                    reason,
                    new Color(1f, 0.42f, 0.30f),
                    2.0f);
            }
        }

        public void OnClickAddCharacters()
        {
            if (gameController != null) gameController.AddCharacterContent(5);
        }

        public void OnClickAddMonsters()
        {
            if (gameController != null) gameController.AddMonsterContent(3);
        }
    }
}
