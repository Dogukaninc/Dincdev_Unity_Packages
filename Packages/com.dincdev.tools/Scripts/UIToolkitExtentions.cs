using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Utils
{
    public readonly struct RevealAnimationStep
    {
        public RevealAnimationStep(
            VisualElement element,
            int durationMs,
            int delayBeforeMs = 0,
            float startScale = 0.94f,
            float overshootScale = 1.035f,
            int settleDurationMs = 110)
        {
            Element = element;
            DurationMs = durationMs;
            DelayBeforeMs = delayBeforeMs;
            StartScale = startScale;
            OvershootScale = overshootScale;
            SettleDurationMs = settleDurationMs;
        }

        public VisualElement Element { get; }
        public int DurationMs { get; }
        public int DelayBeforeMs { get; }
        public float StartScale { get; }
        public float OvershootScale { get; }
        public int SettleDurationMs { get; }
    }

    public static class UIToolkitExtentions
    {
        public static void ChangeClasses(this VisualElement visualElement, string classToAdd, string classToRemove)
        {
            visualElement.RemoveFromClassList(classToRemove);
            visualElement.AddToClassList(classToAdd);
        }

        public static void RemoveClasses(this VisualElement visualElement, params string[] classToRemove)
        {
            if (visualElement == null || classToRemove == null || classToRemove.Length < 1) return;
            classToRemove.ToList().ForEach(x => { visualElement.RemoveFromClassList(x); });
        }


        //Animations
        public static void ScaleUpDownAnimation(this VisualElement target, float scale = 1.1f, int durationMs = 150)
        {
            target.experimental.animation.Scale(scale, durationMs).OnCompleted(() =>
            {
                target.experimental.animation.Scale(1f, durationMs).Ease(Easing.InBack);
            }).Ease(Easing.OutBounce);
        }

        public static async UniTask ScaleUpDownAnimationTask(this VisualElement target, float scale = 1.1f,
            int durationMs = 150)
        {
            bool firstAnimCompleted = false;
            bool secondAnimCompleted = false;

            // İlk animasyon
            target.experimental.animation
                .Scale(scale, durationMs)
                .Ease(Easing.OutBounce)
                .OnCompleted(() => firstAnimCompleted = true);

            await UniTask.WaitUntil(() => firstAnimCompleted);

            // İkinci animasyon
            target.experimental.animation
                .Scale(1f, durationMs)
                .Ease(Easing.InBack)
                .OnCompleted(() => secondAnimCompleted = true);

            await UniTask.WaitUntil(() => secondAnimCompleted);
        }

        public static void ScaleUpDownAnimation(this VisualElement target, bool isLinear, float scale = 1.1f,
            int durationMs = 150)
        {
            target.experimental.animation.Scale(scale, durationMs).OnCompleted(() =>
            {
                target.experimental.animation.Scale(1f, durationMs).Ease(Easing.InBack);
            }).Ease(Easing.OutBounce);
        }

        public static bool CanPlayRevealAnimation(this VisualElement element)
        {
            return element != null && element.style.display != DisplayStyle.None;
        }

        public static void PrepareRevealAnimation(this VisualElement element, float startScale = 0.94f)
        {
            if (!element.CanPlayRevealAnimation())
                return;

            element.style.opacity = 0f;
            element.style.scale = new Scale(new Vector2(startScale, startScale));
        }

        public static async UniTask RevealAnimation(
            this VisualElement element,
            int durationMs,
            int settleDurationMs = 110,
            float overshootScale = 1.035f,
            Func<bool> shouldCancel = null)
        {
            if (!element.CanPlayRevealAnimation() || (shouldCancel?.Invoke() ?? false))
                return;

            element.experimental.animation
                .Start(new StyleValues { opacity = 1f }, durationMs)
                .Ease(Easing.OutCubic);

            element.experimental.animation
                .Scale(overshootScale, durationMs)
                .Ease(Easing.OutCubic)
                .OnCompleted(() =>
                {
                    if (shouldCancel?.Invoke() ?? false)
                        return;

                    element.experimental.animation.Scale(1f, settleDurationMs).Ease(Easing.OutBack);
                });

            await UniTask.Delay(Mathf.Max(60, durationMs - 40));
        }

        public static async UniTask PlayRevealSequence(
            this IReadOnlyList<RevealAnimationStep> steps,
            float speedMultiplier = 1f,
            Func<bool> shouldCancel = null)
        {
            if (steps == null || steps.Count == 0)
                return;

            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].Element.PrepareRevealAnimation(steps[i].StartScale);
            }

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (!step.Element.CanPlayRevealAnimation())
                    continue;

                if (shouldCancel?.Invoke() ?? false)
                    return;

                if (step.DelayBeforeMs > 0)
                {
                    await UniTask.Delay(ApplySpeedMultiplier(step.DelayBeforeMs, speedMultiplier));
                }

                if (shouldCancel?.Invoke() ?? false)
                    return;

                await step.Element.RevealAnimation(
                    ApplySpeedMultiplier(step.DurationMs, speedMultiplier),
                    ApplySpeedMultiplier(step.SettleDurationMs, speedMultiplier),
                    step.OvershootScale,
                    shouldCancel);
            }
        }

        public static int ApplySpeedMultiplier(int durationMs, float speedMultiplier = 1f)
        {
            return Mathf.Max(1, Mathf.RoundToInt(durationMs / Mathf.Max(0.01f, speedMultiplier)));
        }

        /// <summary>
        /// Paneli gösterip scale overshoot + fade animasyonu oynatır.
        /// </summary>
        public static void ShowPanelAnimation(
            this VisualElement panel,
            float startScale      = 0.88f,
            float overshootScale  = 1.04f,
            int   fadeDuration    = 220,
            int   overshootDuration = 180,
            int   settleDuration  = 120)
        {
            panel.style.opacity = 0f;
            panel.style.scale   = new Scale(Vector2.one * startScale);
            panel.style.display = DisplayStyle.Flex;

            panel.experimental.animation
                .Start(new StyleValues { opacity = 1f }, fadeDuration)
                .Ease(Easing.OutCubic);

            panel.experimental.animation
                .Scale(overshootScale, overshootDuration)
                .Ease(Easing.OutCubic)
                .OnCompleted(() =>
                    panel.experimental.animation
                        .Scale(1f, settleDuration)
                        .Ease(Easing.InOutSine));
        }
        public static void ShowPopupOverlay(
            this VisualElement overlay,
            VisualElement content,
            int overlayFadeDurationMs = 160,
            float contentStartScale = 0.92f,
            float contentOvershootScale = 1.02f,
            int contentOvershootDurationMs = 180,
            int contentSettleDurationMs = 120)
        {
            if (overlay == null)
                return;

            overlay.style.display = DisplayStyle.Flex;
            overlay.style.opacity = 0f;
            overlay.BringToFront();
            overlay.experimental.animation
                .Start(new StyleValues { opacity = 1f }, overlayFadeDurationMs)
                .Ease(Easing.OutCubic);

            if (content == null)
                return;

            content.style.opacity = 1f;
            content.style.scale = new Scale(Vector2.one * contentStartScale);
            content.experimental.animation
                .Scale(contentOvershootScale, contentOvershootDurationMs)
                .Ease(Easing.OutCubic)
                .OnCompleted(() =>
                    content.experimental.animation
                        .Scale(1f, contentSettleDurationMs)
                        .Ease(Easing.OutBack));
        }

        public static void HidePopupOverlay(
            this VisualElement overlay,
            VisualElement content,
            Action onHidden = null,
            int overlayFadeDurationMs = 120,
            float contentEndScale = 0.96f,
            int contentDurationMs = 120)
        {
            if (overlay == null)
                return;

            if (content != null)
            {
                content.experimental.animation
                    .Scale(contentEndScale, contentDurationMs)
                    .Ease(Easing.InCubic);
            }

            overlay.experimental.animation
                .Start(new StyleValues { opacity = 0f }, overlayFadeDurationMs)
                .Ease(Easing.InCubic);

            overlay.schedule.Execute(() =>
            {
                overlay.style.display = DisplayStyle.None;
                overlay.style.opacity = 1f;
                if (content != null)
                    content.style.scale = new Scale(Vector2.one);
                onHidden?.Invoke();
            }).StartingIn(Mathf.Max(overlayFadeDurationMs, contentDurationMs));
        }

        /// <summary>
        /// Container'ın direct child'larını sırayla (stagger) fade + scale animasyonuyla gösterir.
        /// </summary>
        public static void ShowChainOverlay(
            this VisualElement host,
            VisualElement content = null,
            VisualElement focusElement = null,
            float hostStartScale = 0.97f,
            int fadeDurationMs = 180,
            int hostScaleDurationMs = 180,
            float contentStartScale = 0.97f,
            float contentOvershootScale = 1.03f,
            int contentOvershootDurationMs = 170,
            int contentSettleDurationMs = 110)
        {
            if (host == null)
                return;

            host.style.opacity = 1f;
            host.style.scale = new Scale(Vector2.one);
            host.style.display = DisplayStyle.Flex;
            host.BringToFront();
            focusElement?.BringToFront();

            if (content == null)
                return;

            content.style.scale = new Scale(Vector2.one * contentStartScale);
            content.experimental.animation
                .Scale(contentOvershootScale, contentOvershootDurationMs)
                .Ease(Easing.OutCubic)
                .OnCompleted(() =>
                    content.experimental.animation
                        .Scale(1f, contentSettleDurationMs)
                        .Ease(Easing.InOutSine));
        }

        public static void HideChainOverlay(
            this VisualElement host,
            VisualElement content = null,
            bool immediate = false,
            Action onHidden = null,
            float endScale = 0.97f,
            int fadeDurationMs = 140,
            int contentScaleDurationMs = 140)
        {
            if (host == null)
                return;

            void ResetAndHide()
            {
                host.style.opacity = 1f;
                host.style.scale = new Scale(Vector2.one);
                if (content != null)
                    content.style.scale = new Scale(Vector2.one);

                host.style.display = DisplayStyle.None;
                onHidden?.Invoke();
            }

            if (immediate)
            {
                ResetAndHide();
                return;
            }

            if (content == null)
            {
                ResetAndHide();
                return;
            }

            content.experimental.animation
                .Scale(endScale, contentScaleDurationMs)
                .Ease(Easing.OutCubic)
                .OnCompleted(ResetAndHide);
        }

        public static void AnimateChildrenStaggered(
            this VisualElement container,
            int   staggerMs       = 70,
            float startScale      = 0.72f,
            float overshootScale  = 1.08f,
            int   fadeDuration    = 200,
            int   overshootDuration = 160,
            int   settleDuration  = 110)
        {
            int index = 0;
            foreach (var child in container.Children())
            {
                child.style.opacity = 0f;
                child.style.scale   = new Scale(Vector2.one * startScale);

                var captured = child;
                int delay    = index * staggerMs;

                captured.schedule.Execute(() =>
                {
                    captured.experimental.animation
                        .Start(new StyleValues { opacity = 1f }, fadeDuration)
                        .Ease(Easing.OutCubic);

                    captured.experimental.animation
                        .Scale(overshootScale, overshootDuration)
                        .Ease(Easing.OutBack)
                        .OnCompleted(() =>
                            captured.experimental.animation
                                .Scale(1f, settleDuration)
                                .Ease(Easing.InOutSine));
                }).StartingIn(delay);

                index++;
            }
        }

        /// <summary>
        /// Elemanı soldan kaydırarak, scale punch + fade ile sahneye sokar. Header kaydırması için.
        /// StyleValues.translate desteklenmediğinden translate manuel tick loop ile animasyona alınır.
        /// </summary>
        public static async UniTask SlideInFromLeft(
            this VisualElement element,
            int slideDurationMs  = 420,
            int settleDurationMs = 130,
            float punchScale     = 1.08f,
            System.Threading.CancellationToken ct = default)
        {
            const float startOffsetPercent = -110f;
            const int   tickMs = 16;

            element.style.opacity   = 0f;
            element.style.scale     = new Scale(Vector2.one * 0.92f);
            element.style.translate = new Translate(new Length(startOffsetPercent, LengthUnit.Percent), new Length(0, LengthUnit.Pixel), 0f);

            // Fade via experimental animation (StyleValues.opacity is supported)
            element.experimental.animation
                .Start(new StyleValues { opacity = 1f }, slideDurationMs)
                .Ease(Easing.OutCubic);

            // Scale punch + settle
            element.experimental.animation
                .Scale(punchScale, slideDurationMs)
                .Ease(Easing.OutCubic)
                .OnCompleted(() =>
                    element.experimental.animation
                        .Scale(1f, settleDurationMs)
                        .Ease(Easing.InOutSine));

            // Translate via manual tick loop (StyleValues.translate not available in this Unity version)
            float elapsed = 0f;
            try
            {
                while (elapsed < slideDurationMs)
                {
                    float t       = Mathf.Clamp01(elapsed / slideDurationMs);
                    float eased   = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
                    float offsetX = Mathf.Lerp(startOffsetPercent, 0f, eased);
                    element.style.translate = new Translate(new Length(offsetX, LengthUnit.Percent), new Length(0, LengthUnit.Pixel), 0f);

                    await UniTask.Delay(tickMs, ignoreTimeScale: true, cancellationToken: ct);
                    elapsed += tickMs;
                }

                element.style.translate = new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel), 0f);
                await UniTask.Delay(settleDurationMs, ignoreTimeScale: true, cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                element.style.opacity   = 1f;
                element.style.scale     = new Scale(Vector2.one);
                element.style.translate = new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel), 0f);
            }
        }

        /// <summary>
        /// Elemanı 0-scale'den başlatıp sert bir punch + settle ile sahneye sokar. Header gibi odak noktaları için.
        /// </summary>
        public static async UniTask JuicyReveal(
            this VisualElement element,
            int punchDurationMs = 380,
            int settleDurationMs = 130,
            float punchScale = 1.18f,
            System.Threading.CancellationToken ct = default)
        {
            element.style.opacity = 0f;
            element.style.scale   = new Scale(Vector2.one * 0.15f);

            bool settled = false;

            element.experimental.animation
                .Start(new StyleValues { opacity = 1f }, punchDurationMs / 2)
                .Ease(Easing.OutCubic);

            element.experimental.animation
                .Scale(punchScale, punchDurationMs)
                .Ease(Easing.OutBack)
                .OnCompleted(() =>
                    element.experimental.animation
                        .Scale(1f, settleDurationMs)
                        .Ease(Easing.InOutSine)
                        .OnCompleted(() => settled = true));

            try
            {
                await UniTask.WaitUntil(() => settled, cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                element.style.opacity = 1f;
                element.style.scale   = new Scale(Vector2.one);
            }
        }

        /// <summary>
        /// Elemanı sağdan kaydırarak, scale punch + fade ile sahneye sokar. ContinueButton gibi sağdan gelen elemanlar için.
        /// StyleValues.translate desteklenmediğinden translate manuel tick loop ile animasyona alınır.
        /// </summary>
        public static async UniTask SlideInFromRight(
            this VisualElement element,
            int slideDurationMs  = 420,
            int settleDurationMs = 130,
            float punchScale     = 1.08f,
            System.Threading.CancellationToken ct = default)
        {
            const float startOffsetPercent = 110f;
            const int   tickMs = 16;

            element.style.opacity   = 0f;
            element.style.scale     = new Scale(Vector2.one * 0.92f);
            element.style.translate = new Translate(new Length(startOffsetPercent, LengthUnit.Percent), new Length(0, LengthUnit.Pixel), 0f);

            element.experimental.animation
                .Start(new StyleValues { opacity = 1f }, slideDurationMs)
                .Ease(Easing.OutCubic);

            element.experimental.animation
                .Scale(punchScale, slideDurationMs)
                .Ease(Easing.OutCubic)
                .OnCompleted(() =>
                    element.experimental.animation
                        .Scale(1f, settleDurationMs)
                        .Ease(Easing.InOutSine));

            float elapsed = 0f;
            try
            {
                while (elapsed < slideDurationMs)
                {
                    float t       = Mathf.Clamp01(elapsed / slideDurationMs);
                    float eased   = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
                    float offsetX = Mathf.Lerp(startOffsetPercent, 0f, eased);
                    element.style.translate = new Translate(new Length(offsetX, LengthUnit.Percent), new Length(0, LengthUnit.Pixel), 0f);

                    await UniTask.Delay(tickMs, ignoreTimeScale: true, cancellationToken: ct);
                    elapsed += tickMs;
                }

                element.style.translate = new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel), 0f);
                await UniTask.Delay(settleDurationMs, ignoreTimeScale: true, cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                element.style.opacity   = 1f;
                element.style.scale     = new Scale(Vector2.one);
                element.style.translate = new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel), 0f);
            }
        }

        /// <summary>
        /// Label'ın text'ini fromValue'dan targetValue'a smooth bir şekilde say-up yapar.
        /// </summary>
        public static async UniTask CountUpLabel(
            this Label label,
            float fromValue,
            float targetValue,
            Func<float, string> formatter,
            int durationMs,
            System.Threading.CancellationToken ct = default)
        {
            const int tickMs = 16;
            float elapsed  = 0f;
            float duration = Mathf.Max(1f, durationMs);

            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested)
                {
                    label.text = formatter(targetValue);
                    return;
                }

                float t     = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
                label.text  = formatter(Mathf.Lerp(fromValue, targetValue, eased));

                try   { await UniTask.Delay(tickMs, ignoreTimeScale: true, cancellationToken: ct); }
                catch (System.OperationCanceledException) { label.text = formatter(targetValue); return; }

                elapsed += tickMs;
            }

            label.text = formatter(targetValue);
        }

        /// <summary>
        /// Label'ın text'ini 0'dan targetValue'a smooth bir şekilde say-up yapar.
        /// </summary>
        public static async UniTask CountUpLabel(
            this Label label,
            float targetValue,
            Func<float, string> formatter,
            int durationMs,
            System.Threading.CancellationToken ct = default)
        {
            const int tickMs = 16;
            float elapsed  = 0f;
            float duration = Mathf.Max(1f, durationMs);

            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested)
                {
                    label.text = formatter(targetValue);
                    return;
                }

                float t     = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
                label.text  = formatter(Mathf.Lerp(0f, targetValue, eased));

                try   { await UniTask.Delay(tickMs, ignoreTimeScale: true, cancellationToken: ct); }
                catch (System.OperationCanceledException) { label.text = formatter(targetValue); return; }

                elapsed += tickMs;
            }

            label.text = formatter(targetValue);
        }

        /// <summary>
        /// Elemanı sonsuz bir scale nefes döngüsüyle pulsa eder (buton vurgusu için).
        /// shouldStop fonksiyonu true döndürdüğünde durur.
        /// </summary>
        public static void StartPulseLoop(
            this VisualElement element,
            float minScale    = 0.97f,
            float maxScale    = 1.05f,
            int   halfPeriodMs = 650,
            Func<bool> shouldStop = null)
        {
            void Step()
            {
                if (shouldStop?.Invoke() ?? false) return;
                if (element?.style.display == DisplayStyle.None) return;

                element.experimental.animation
                    .Scale(maxScale, halfPeriodMs)
                    .Ease(Easing.InOutSine)
                    .OnCompleted(() =>
                    {
                        if (shouldStop?.Invoke() ?? false) return;
                        if (element?.style.display == DisplayStyle.None) return;

                        element.experimental.animation
                            .Scale(minScale, halfPeriodMs)
                            .Ease(Easing.InOutSine)
                            .OnCompleted(Step);
                    });
            }

            Step();
        }

        /// <summary>
        /// Elemanı kademeli olarak artan bir titreme (shake) animasyonuna sokar.
        /// Titreme amplitüdü totalDurationMs boyunca linearStartAmplitude'dan maxAmplitude'a yükselir.
        /// Tamamlandığında eleman orijinal konumuna döner.
        /// </summary>
        public static async UniTask ShakeProgressiveAsync(
            this VisualElement element,
            int   totalDurationMs   = 2000,
            float maxAmplitudePx    = 18f,
            float startAmplitudePx  = 1.5f,
            float maxRotationDeg    = 6f,   // titreme sonunda ulaşılacak max rotasyon açısı
            int   tickMs            = 20,
            System.Threading.CancellationToken ct = default)
        {
            const float freqX = 22f;  // yatay titreme frekansı
            const float freqY = 3.5f; // dikey hop frekansı
            const float freqR = 5f;   // rotasyon frekansı (X'ten yavaş, hafif sallanma hissi)
            float elapsed = 0f;
            float total   = Mathf.Max(1f, totalDurationMs);

            try
            {
                while (elapsed < total)
                {
                    float t         = Mathf.Clamp01(elapsed / total);
                    float amplitude = Mathf.Lerp(startAmplitudePx, maxAmplitudePx, t * t);
                    float rotAmp    = Mathf.Lerp(0f, maxRotationDeg, t * t);

                    float offsetX = Mathf.Sin(elapsed * 0.001f * freqX * Mathf.PI * 2f) * amplitude;
                    float offsetY = Mathf.Sin(elapsed * 0.001f * freqY * Mathf.PI * 2f) * amplitude * 0.2f;
                    float rotDeg  = Mathf.Sin(elapsed * 0.001f * freqR * Mathf.PI * 2f) * rotAmp;

                    element.style.translate = new Translate(
                        new Length(offsetX, LengthUnit.Pixel),
                        new Length(offsetY, LengthUnit.Pixel), 0f);
                    element.style.rotate = new Rotate(rotDeg);

                    await UniTask.Delay(tickMs, ignoreTimeScale: true, cancellationToken: ct);
                    elapsed += tickMs;
                }
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                element.style.translate = new Translate(
                    new Length(0, LengthUnit.Pixel),
                    new Length(0, LengthUnit.Pixel), 0f);
                element.style.rotate = new Rotate(0f);
            }
        }

        /// <summary>
        /// Elemanın arkaplan rengini (background-color overlay) belirli bir sürede hedef renge çeker.
        /// BG fade-to-white gibi kullanım senaryoları için.
        /// </summary>
        public static async UniTask FadeBackgroundColorAsync(
            this VisualElement element,
            Color targetColor,
            int   durationMs = 400,
            int   tickMs     = 16,
            System.Threading.CancellationToken ct = default)
        {
            Color startColor = element.resolvedStyle.backgroundColor;
            float elapsed    = 0f;
            float total      = Mathf.Max(1f, durationMs);

            try
            {
                while (elapsed < total)
                {
                    float t = Mathf.Clamp01(elapsed / total);
                    float eased = 1f - Mathf.Pow(1f - t, 2f); // EaseOutQuad
                    element.style.backgroundColor = Color.Lerp(startColor, targetColor, eased);

                    await UniTask.Delay(tickMs, ignoreTimeScale: true, cancellationToken: ct);
                    elapsed += tickMs;
                }
                element.style.backgroundColor = targetColor;
            }
            catch (System.OperationCanceledException)
            {
                element.style.backgroundColor = targetColor;
            }
        }

        /// <summary>
        /// Elemanı belirli bir süre boyunca sabit genlikte çalkaladır.
        /// Tüm yıldızları birlikte titretmek için onları içeren container elemanına çağır.
        /// </summary>
        /// <param name="rotationAmplitudeDeg">X titreşimiyle senkron ±rotasyon (derece). 0 = kapalı.</param>
        public static async UniTask ShakeAsync(
            this VisualElement element,
            int   totalDurationMs      = 333,
            float amplitudePx          = 30f,
            float freqHz               = 16f,
            float rotationAmplitudeDeg = 0f,
            int   tickMs               = 16,
            System.Threading.CancellationToken ct = default)
        {
            float elapsed = 0f;
            float total   = Mathf.Max(1f, totalDurationMs);
            bool  useRot  = rotationAmplitudeDeg != 0f;

            try
            {
                while (elapsed < total)
                {
                    float t        = Mathf.Clamp01(elapsed / total);
                    float envelope = 1f - t * t; // genlik sona doğru yumuşakça azalır
                    float sine     = Mathf.Sin(elapsed * 0.001f * freqHz * Mathf.PI * 2f);
                    float offsetX  = sine * amplitudePx * envelope;
                    float offsetY  = Mathf.Sin(elapsed * 0.001f * freqHz * 0.65f * Mathf.PI * 2f) * amplitudePx * 0.25f * envelope;
                    element.transform.position = new Vector3(offsetX, offsetY, 0);
                    if (useRot)
                        element.style.rotate = new Rotate(sine * rotationAmplitudeDeg * envelope);
                    await UniTask.Delay(tickMs, cancellationToken: ct);
                    elapsed += tickMs;
                }
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                element.transform.position = Vector3.zero;
                if (useRot) element.style.rotate = new Rotate(0f);
            }
        }

        /// <summary>
        /// Elemanı mevcut rotasyonundan hedef açıya EaseOutCubic ile döndürür (Z ekseni, 2D).
        /// </summary>
        public static async UniTask TiltToAsync(
            this VisualElement element,
            float targetDeg,
            int   durationMs,
            int   tickMs = 16,
            System.Threading.CancellationToken ct = default)
        {
            float startDeg;
            try   { startDeg = element.resolvedStyle.rotate.angle.value; }
            catch { startDeg = 0f; }

            float elapsed = 0f;
            float total   = Mathf.Max(1f, durationMs);

            try
            {
                while (elapsed < total)
                {
                    float t     = Mathf.Clamp01(elapsed / total);
                    float eased = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
                    element.style.rotate = new Rotate(Mathf.Lerp(startDeg, targetDeg, eased));
                    await UniTask.Delay(tickMs, cancellationToken: ct);
                    elapsed += tickMs;
                }
                element.style.rotate = new Rotate(targetDeg);
            }
            catch (System.OperationCanceledException)
            {
                element.style.rotate = new Rotate(targetDeg);
            }
        }

        /// <summary>
        /// Eleman üzerine beyaz bir overlay koyar: opacity 0→1 (rise), kısa bekleme (hold), 1→0 (fall).
        /// Tamamlanınca overlay otomatik kaldırılır.
        /// </summary>
        /// <param name="riseMs">Beyaza geçiş süresi (ms).</param>
        /// <param name="holdMs">Tam beyazda bekleme süresi (ms).</param>
        /// <param name="fallMs">Normale dönüş süresi (ms).</param>
        /// <param name="tickMs">Animasyon adım aralığı (ms).</param>
        public static async UniTask WhiteFlashAsync(
            this VisualElement element,
            int riseMs = 80,
            int holdMs = 40,
            int fallMs = 280,
            int tickMs = 16,
            System.Threading.CancellationToken ct = default)
        {
            var overlay = new VisualElement();
            overlay.style.position        = Position.Absolute;
            overlay.style.left            = 0; overlay.style.right  = 0;
            overlay.style.top             = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new StyleColor(Color.white);
            overlay.style.opacity         = 0f;
            overlay.pickingMode           = PickingMode.Ignore;
            element.Add(overlay);

            try
            {
                // Rise: 0 → 1
                for (int e = 0; e < riseMs; e += tickMs)
                {
                    overlay.style.opacity = (float)e / riseMs;
                    await UniTask.Delay(tickMs, cancellationToken: ct);
                }
                overlay.style.opacity = 1f;

                // Hold
                if (holdMs > 0)
                    await UniTask.Delay(holdMs, cancellationToken: ct);

                // Fall: 1 → 0
                for (int e = 0; e < fallMs; e += tickMs)
                {
                    overlay.style.opacity = 1f - (float)e / fallMs;
                    await UniTask.Delay(tickMs, cancellationToken: ct);
                }
                overlay.style.opacity = 0f;
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                overlay.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// Elemanın yalnızca background-image tint rengini beyaza çekip geri döndürür.
        /// Children etkilenmez — sadece background-image rengi değişir.
        /// </summary>
        public static async UniTask BackgroundFlashAsync(
            this VisualElement element,
            Color?  targetColor = null,
            int     riseMs      = 80,
            int     holdMs      = 40,
            int     fallMs      = 280,
            int     tickMs      = 16,
            System.Threading.CancellationToken ct = default)
        {
            var target   = targetColor ?? Color.white;
            var original = element.resolvedStyle.unityBackgroundImageTintColor;

            try
            {
                // Rise: original → target
                for (int e = 0; e < riseMs; e += tickMs)
                {
                    element.style.unityBackgroundImageTintColor =
                        new StyleColor(Color.Lerp(original, target, (float)e / riseMs));
                    await UniTask.Delay(tickMs, cancellationToken: ct);
                }
                element.style.unityBackgroundImageTintColor = new StyleColor(target);

                // Hold
                if (holdMs > 0)
                    await UniTask.Delay(holdMs, cancellationToken: ct);

                // Fall: target → original
                for (int e = 0; e < fallMs; e += tickMs)
                {
                    element.style.unityBackgroundImageTintColor =
                        new StyleColor(Color.Lerp(target, original, (float)e / fallMs));
                    await UniTask.Delay(tickMs, cancellationToken: ct);
                }
                element.style.unityBackgroundImageTintColor = new StyleColor(original);
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                element.style.unityBackgroundImageTintColor = new StyleColor(original);
            }
        }

        /// <summary>
        /// Bir reward item'ı kaynak pozisyondan (dünya koordinatı değil, panel-relative px offset)
        /// hedef pozisyonuna kıvrık (arc/slerp-like) bir yay çizerek taşır.
        /// </summary>
        public static async UniTask FlyToPositionArc(
            this VisualElement element,
            Vector2 startOffsetPx,
            int     durationMs  = 500,
            float   arcHeightPx = -120f,
            int     tickMs      = 16,
            System.Threading.CancellationToken ct = default)
        {
            float elapsed = 0f;
            float total   = Mathf.Max(1f, durationMs);

            element.style.translate = new Translate(
                new Length(startOffsetPx.x, LengthUnit.Pixel),
                new Length(startOffsetPx.y, LengthUnit.Pixel), 0f);
            element.style.opacity = 0f;
            element.style.scale   = new Scale(Vector2.one * 0.4f);

            try
            {
                while (elapsed < total)
                {
                    float t     = Mathf.Clamp01(elapsed / total);
                    float eased = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic

                    // Yay: quadratic bezier benzeri dikey offset
                    float arcT    = Mathf.Sin(t * Mathf.PI); // 0→1→0 yay
                    float offsetX = Mathf.Lerp(startOffsetPx.x, 0f, eased);
                    float offsetY = Mathf.Lerp(startOffsetPx.y, 0f, eased) + arcHeightPx * arcT * (1f - eased);

                    element.style.translate = new Translate(
                        new Length(offsetX, LengthUnit.Pixel),
                        new Length(offsetY, LengthUnit.Pixel), 0f);
                    element.style.opacity = Mathf.Clamp01(t * 3f);
                    element.style.scale   = new Scale(Vector2.one * Mathf.Lerp(0.4f, 1f, eased));

                    await UniTask.Delay(tickMs, ignoreTimeScale: true, cancellationToken: ct);
                    elapsed += tickMs;
                }

                element.style.translate = new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel), 0f);
                element.style.opacity   = 1f;
                element.style.scale     = new Scale(Vector2.one);
            }
            catch (System.OperationCanceledException)
            {
                element.style.translate = new Translate(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel), 0f);
                element.style.opacity   = 1f;
                element.style.scale     = new Scale(Vector2.one);
            }
        }
    }
}



