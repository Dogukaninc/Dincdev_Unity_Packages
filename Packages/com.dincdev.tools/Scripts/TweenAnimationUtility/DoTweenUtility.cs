using DG.Tweening;
using UnityEngine;

namespace _Main.Project.Scripts.Utils
{
    public static class DoTweenUtility
    {
        public static Tween ScaleUpBounce(
            Transform transform,
            Ease scaleUpEase,
            Ease scaleBackEase,
            float duration = 0.2f,
            float scaleFactor = 1.2f)
        {
            var originalScale = transform.localScale;
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(originalScale * scaleFactor, duration).SetEase(Ease.OutBack).From(0));
            sequence.Append(transform.DOScale(originalScale, duration).SetEase(Ease.InBack));
            return sequence;
        }
        
        public static Tween ScaleDownInBounce(
            Transform transform,
            Ease scaleUpEase,
            Ease scaleBackEase,
            float duration = 0.2f,
            float scaleFactor = 1.2f)
        {
            var originalScale = transform.localScale;
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(originalScale * scaleFactor, duration).SetEase(Ease.OutBack));
            sequence.Append(transform.DOScale(0, duration).SetEase(Ease.InBack));
            return sequence;
        }


        public static Tween RevealWithSmoke(
            Transform transform,
            ParticleSystem smokeVfxPrefab,
            float smokeDuration,
            float scaleDuration = 0.2f,
            float scaleFactor = 1.2f)
        {
            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

            var sequence = DOTween.Sequence();

            if (smokeVfxPrefab != null)
            {
                sequence.AppendCallback(() =>
                {
                    var smoke = Object.Instantiate(smokeVfxPrefab, transform.position, transform.rotation);
                    smoke.Play();
                    Object.Destroy(smoke.gameObject, smoke.main.duration + smoke.main.startLifetime.constantMax);
                });
            }

            sequence.AppendInterval(smokeDuration);
            sequence.AppendCallback(() =>
            {
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }
            });
            sequence.Append(ScaleUpBounce(transform, Ease.OutBack, Ease.InBack, scaleDuration, scaleFactor));

            return sequence;
        }

        public static Tween MoveUpInBounce(
            Transform transform,
            Ease moveUpEase,
            Ease moveDownEase,
            float duration = 0.2f,
            float positionFactor = 1.2f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y - positionFactor, transform.position.z);
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOMoveY(transform.position.y + positionFactor, duration).SetEase(moveUpEase));
            sequence.Append(transform.DOMoveY(transform.position.y, duration).SetEase(moveDownEase));
            return sequence;
        }
        
    }
}