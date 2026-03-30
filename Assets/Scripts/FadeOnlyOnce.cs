// Assets/Scripts/UI/FadeInOnlyManager.cs
using System.Collections;
using UnityEngine;

public sealed class FadeInOnlyManager : MonoBehaviour
{
    [Header("Assign CanvasGroup from Image_Fade (black overlay)")]
    [SerializeField] private CanvasGroup fadeOverlay;

    [Header("IMPORTANT: Assign the exact GameObject you want to disable (Image_Fade).")]
    [SerializeField] private GameObject overlayRootToDisable;

    [Header("Settings")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 1f;
    [SerializeField] private bool ignoreTimeScale = true;
    [SerializeField] private bool disableOverlayAfterFade = true;

    private void Awake()
    {
        if (fadeOverlay == null)
        {
            UnityEngine.Debug.LogError($"{nameof(FadeInOnlyManager)}: Asigna el CanvasGroup del Image_Fade.", this);
            enabled = false;
            return;
        }

        if (overlayRootToDisable == null)
        {
            // Fallback seguro: desactiva el MISMO objeto del CanvasGroup (siempre que sea el Image_Fade)
            overlayRootToDisable = fadeOverlay.gameObject;
        }

        fadeOverlay.alpha = 1f;             // negro al inicio
        fadeOverlay.interactable = false;
        fadeOverlay.blocksRaycasts = false; // NO toca botones
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);
            fadeOverlay.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        fadeOverlay.alpha = 0f;

        if (disableOverlayAfterFade && overlayRootToDisable != null)
        {
            // Apaga SOLO el overlay negro (Image_Fade), NO el Canvas completo.
            overlayRootToDisable.SetActive(false);
        }
    }
}