using DG.Tweening;
using UnityEngine;

namespace _Main.Project.Scripts.Utils
{
    public static class TweenAnimationSequences
    {
        public static Tween PlayEntranceAnimation(
            this Transform[] transforms,
            ParticleSystem smokeVfxPrefab,
            float smokeDuration,
            float scaleDuration,
            float delayBetween = 0.15f)
        {
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                sequence.Insert(i * delayBetween,
                    DoTweenUtility.RevealWithSmoke(transform, smokeVfxPrefab, smokeDuration, scaleDuration));
            }

            return sequence;
        }
    }
}