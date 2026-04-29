using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static helper for common UI animations.
/// </summary>
public static class UIAnimations
{
    public static IEnumerator StaggerShow(
        IEnumerable<GameObject> elements,
        float staggerDelay = 0.3f,
        float fadeDuration = 0.2f,
        Vector3? scaleFrom = null,
        Vector3? scaleTo = null,
        Action onComplete = null)
    {
        Vector3 startScale = scaleFrom ?? Vector3.one * 0.8f;
        Vector3 endScale = scaleTo ?? Vector3.one;

        foreach (GameObject element in elements)
        {
            if (element == null) continue;

            // Ensure it has a CanvasGroup for fading
            CanvasGroup cg = element.GetComponent<CanvasGroup>();
            if (cg == null) cg = element.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Start local scale
            element.transform.localScale = startScale;

            // Animate fade + scale
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                cg.alpha = Mathf.Lerp(0f, 1f, t);
                element.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            cg.alpha = 1f;
            element.transform.localScale = endScale;
            cg.blocksRaycasts = true;
            cg.interactable = true;

            // Optional extra pop (like a tiny bounce)
            yield return new WaitForSecondsRealtime(staggerDelay);
        }

        onComplete?.Invoke();
    }
    public static IEnumerator PopOut(RectTransform target, float duration = 0.2f, float strength = 1.2f, Action onComplete = null)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 originalScale = target.localScale;
        Vector3 peakScale = originalScale * strength;

        float half = duration * 0.5f;
        float elapsed = 0f;

        // Grow to peak
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / half;
            target.localScale = Vector3.Lerp(originalScale, peakScale, t);
            yield return null;
        }

        // Shrink back to original
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / half;
            target.localScale = Vector3.Lerp(peakScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
        onComplete?.Invoke();
    }
    public static IEnumerator PopOut(GameObject go, float duration = 0.2f, float strength = 1.2f, Action onComplete = null) // Convenience overload for PopOut on a GameObject (assumes it has a RectTransform).
    {
        if (go == null)
        {
            onComplete?.Invoke();
            yield break;
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            onComplete?.Invoke();
            yield break;
        }
        yield return PopOut(rt, duration, strength, onComplete);
    }
    public static void PopOutQuick(this MonoBehaviour mb, GameObject go, float strength = 1.2f, Action onComplete = null)
    {
        mb.StartCoroutine(PopOut(go, 0.15f, strength, onComplete));
    }
}