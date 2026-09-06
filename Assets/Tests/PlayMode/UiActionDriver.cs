using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenseGame.Tests
{
    internal sealed class UiActionDriver
    {
        private float nextActionRealtime;

        internal bool IsReadyForAction
        {
            get { return UnityEngine.Time.realtimeSinceStartup >= nextActionRealtime; }
        }

        internal bool TryClick(Button button, string action, ValidationRunRecorder recorder, object controller)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable || EventSystem.current == null)
            {
                return false;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            recorder.RecordAction(action, button.name, RuntimeGameView.Int(controller, "CurrentRound"));
            nextActionRealtime = UnityEngine.Time.realtimeSinceStartup + 0.15f;
            return true;
        }
    }
}