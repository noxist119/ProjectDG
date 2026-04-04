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

        private void OnEnable()
        {
            if (gameController != null)
            {
                gameController.OnStateChanged += Refresh;
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
            }
        }

        public void Refresh()
        {
            if (gameController == null)
            {
                return;
            }

            if (goldText != null) goldText.text = $"Gold : {gameController.Gold}";
            if (lifeText != null) lifeText.text = $"Life : {gameController.Life}";
            if (roundText != null) roundText.text = $"Round : {gameController.CurrentRound} {(gameController.IsBossRound ? "BOSS" : string.Empty)}";
            if (boardText != null) boardText.text = $"Units : {gameController.BoardUnitCount}";
            if (contentText != null) contentText.text = $"Characters : {gameController.CharacterCount} / Monsters : {gameController.MonsterCount}";
        }
    }
}

