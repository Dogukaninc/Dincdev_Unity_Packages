using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Utils
{
    public static class ColorBlendUtility
    {
        public static async UniTask FloatTweenAsync(
            float startValue,
            float targetValue,
            float duration,
            Action<float> onUpdate,
            CancellationToken cancellationToken = default)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float value = Mathf.Lerp(startValue, targetValue, t);

                onUpdate?.Invoke(value);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            onUpdate?.Invoke(targetValue);
        }
    }
}
