using UnityEngine;

namespace DefenseGame
{
    public class DemoInputController : MonoBehaviour
    {
        [SerializeField] private DefenseGameController gameController;

        public void Configure(DefenseGameController controller)
        {
            gameController = controller;
        }

        private void Update()
        {
            if (gameController == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space)) gameController.StartRound();
            if (Input.GetKeyDown(KeyCode.S)) gameController.TrySummon();
            if (Input.GetKeyDown(KeyCode.Alpha1)) gameController.TryMerge(CharacterGrade.Normal);
            if (Input.GetKeyDown(KeyCode.Alpha2)) gameController.TryMerge(CharacterGrade.Rare);
            if (Input.GetKeyDown(KeyCode.Alpha3)) gameController.TryMerge(CharacterGrade.Epic);
            if (Input.GetKeyDown(KeyCode.Alpha4)) gameController.TryMerge(CharacterGrade.Legendary);
            if (Input.GetKeyDown(KeyCode.C)) gameController.AddCharacterContent(5);
            if (Input.GetKeyDown(KeyCode.M)) gameController.AddMonsterContent(3);
        }
    }
}
