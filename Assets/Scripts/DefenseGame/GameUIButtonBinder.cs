using UnityEngine;

namespace DefenseGame
{
    public class GameUIButtonBinder : MonoBehaviour
    {
        [SerializeField] private DefenseGameController gameController;

        public void OnClickStartRound()
        {
            gameController.StartRound();
        }

        public void OnClickSummon()
        {
            gameController.TrySummon();
        }

        public void OnClickMergeNormal()
        {
            gameController.TryMerge(CharacterGrade.Normal);
        }

        public void OnClickMergeRare()
        {
            gameController.TryMerge(CharacterGrade.Rare);
        }

        public void OnClickMergeEpic()
        {
            gameController.TryMerge(CharacterGrade.Epic);
        }

        public void OnClickMergeLegendary()
        {
            gameController.TryMerge(CharacterGrade.Legendary);
        }

        public void OnClickAddCharacters()
        {
            gameController.AddCharacterContent(5);
        }

        public void OnClickAddMonsters()
        {
            gameController.AddMonsterContent(3);
        }
    }
}

