using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefenseGame
{
    [DisallowMultipleComponent]
    public sealed class DOTweenUiPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.90f, 1f)] private float pressedScale = 0.96f;

        private Vector3 restingScale = Vector3.one;
        private Tween scaleTween;

        public void OnPointerDown(PointerEventData eventData)
        {
            restingScale = transform.localScale;
            PlayScale(restingScale * pressedScale, 0.06f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            scaleTween?.Kill();
            scaleTween = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .Append(transform.DOScale(restingScale * 1.02f, 0.07f).SetEase(Ease.OutCubic))
                .Append(transform.DOScale(restingScale, 0.09f).SetEase(Ease.OutCubic));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayScale(restingScale, 0.08f, Ease.OutCubic);
        }

        private void OnDisable()
        {
            scaleTween?.Kill();
            transform.localScale = restingScale;
        }

        private void OnDestroy()
        {
            scaleTween?.Kill();
        }

        private void PlayScale(Vector3 targetScale, float duration, Ease ease)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(targetScale, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetTarget(this);
        }
    }
}