using System.Collections;
//using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [Header("Assign CanvasGroup from Image_Fade (black overlay)")]
    [SerializeField] private CanvasGroup fadeOverlay;

    [Header("Settings")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.8f;
    [SerializeField] private bool ignoreTimeScale = false;

    private Coroutine running;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeOverlay == null)
        {
            fadeOverlay = GetComponentInChildren<CanvasGroup>(true);
        }

        if (fadeOverlay == null)
        {
            Debug.LogError($"{nameof(FadeManager)}: No se encontró un CanvasGroup para el fade.", this);
            enabled = false;
            return;
        }

        fadeOverlay.alpha = 1f;
        fadeOverlay.interactable = false;
        fadeOverlay.blocksRaycasts = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        StartFadeIn();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeOverlay == null)
        {
            fadeOverlay = GetComponentInChildren<CanvasGroup>(true);
        }

        if (fadeOverlay == null)
        {
            Debug.LogWarning("FadeManager: fadeOverlay no existe en esta escena.");
            return;
        }

        StartFadeIn();
    }

    private void StartFadeIn()
    {
        if (!enabled || fadeOverlay == null) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeTo(0f, false));
    }

    public void FadeOutAndLoad(string sceneName)
    {
        if (!enabled || fadeOverlay == null) return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("FadeOutAndLoad: sceneName está vacío.", this);
            return;
        }

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeOutLoad(sceneName));
    }

    private IEnumerator FadeOutLoad(string sceneName)
    {
        yield return FadeTo(1f, true);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeTo(float targetAlpha, bool blockRaycasts)
    {
        if (fadeOverlay == null) yield break;

        float startAlpha = fadeOverlay.alpha;
        float t = 0f;

        fadeOverlay.blocksRaycasts = blockRaycasts;

        while (t < fadeDuration)
        {
            if (fadeOverlay == null) yield break;

            t += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, p);
            yield return null;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = targetAlpha;

            if (targetAlpha <= 0.001f)
                fadeOverlay.blocksRaycasts = false;
        }
    }
}