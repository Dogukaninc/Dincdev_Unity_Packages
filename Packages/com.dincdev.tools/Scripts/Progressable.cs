using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Main.Project.Scripts.Utils
{
    // Plain C# 0..1 progress value. Embed as a field in any class that needs to
    // track a progress fraction (health, stamina, cooldowns, loading bars, ...).
    // Supports either a per-frame auto-increment (Tick) or a lerp-to-target
    // animation (AnimateTo) driven by an external UniTask loop.
    public class Progressable
    {
        public float Value { get; private set; }
        public float Speed { get; set; }

        private CancellationTokenSource _animationCts;

        public Progressable(float initialValue = 0f, float speed = 1f)
        {
            Value = Mathf.Clamp01(initialValue);
            Speed = speed;
        }

        public void Tick(float deltaTime)
        {
            SetImmediate(Value + Speed * deltaTime);
        }

        public void SetImmediate(float value)
        {
            _animationCts?.Cancel();
            Value = Mathf.Clamp01(value);
        }

        public async UniTask AnimateTo(float target, float duration, Action<float> onValueChanged, float delay = 0f, CancellationToken externalToken = default)
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            CancellationToken token = _animationCts.Token;

            target = Mathf.Clamp01(target);

            if (delay > 0f)
            {
                bool delayCancelled = await UniTask.Delay((int)(delay * 1000f), cancellationToken: token).SuppressCancellationThrow();
                if (delayCancelled)
                {
                    return;
                }
            }

            float start = Value;
            float elapsed = 0f;

            while (elapsed < duration && !token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Value = Mathf.Lerp(start, target, t);
                onValueChanged?.Invoke(Value);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (!token.IsCancellationRequested)
            {
                Value = target;
                onValueChanged?.Invoke(Value);
            }
        }

        public void CancelAnimation()
        {
            _animationCts?.Cancel();
        }

        public void Dispose()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
        }
    }
}
