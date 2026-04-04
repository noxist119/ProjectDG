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
            if (gameController != null) gameController.TryMerge(CharacterGrade.Normal);
        }

        public void OnClickMergeRare()
        {
            if (gameController != null) gameController.TryMerge(CharacterGrade.Rare);
        }

        public void OnClickMergeEpic()
        {
            if (gameController != null) gameController.TryMerge(CharacterGrade.Epic);
        }

        public void OnClickMergeLegendary()
        {
            if (gameController != null) gameController.TryMerge(CharacterGrade.Legendary);
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
