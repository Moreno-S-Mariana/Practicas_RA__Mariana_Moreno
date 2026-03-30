using System.Collections;                 
using UnityEngine;                        
using UnityEngine.SceneManagement;        

public sealed class SplashFade : MonoBehaviour
{
    [Header("Assign CanvasGroup from Image_Fade (black overlay)")]
    [SerializeField] private CanvasGroup fadeOverlay;          // CANVASGROUP DEL OVERLAY NEGRO

    [Header("Settings")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 1f;       // DURACION DEL FADE
    [SerializeField, Min(0f)] private float waitBeforeFadeOut = 1.5f;   // ESPERA ANTES DEL FADE OUT

    private Coroutine running;                                 // CORRUTINA ACTIVA (PARA DETENERLA SI HACE FALTA)

    private void Awake()
    {
        if (fadeOverlay == null)                               // SI NO SE ASIGNO EL OVERLAY
        {
            UnityEngine.Debug.LogError("SplashFade: Asigna el CanvasGroup del Image_Fade.", this); // AVISA ERROR
            enabled = false;                                   // DESACTIVA EL SCRIPT
            return;                                            // SALE
        }

        fadeOverlay.alpha = 1f;                                // INICIA EN NEGRO
        fadeOverlay.blocksRaycasts = true;                     // BLOQUEA INPUT
        fadeOverlay.interactable = false;                      // NO INTERACTIVO
    }

    private void Start()
    {
        running = StartCoroutine(SplashSequence());            // INICIA LA SECUENCIA DEL SPLASH
    }

    private IEnumerator SplashSequence()
    {
        yield return FadeTo(0f);                               // FADE IN: SE QUITA EL NEGRO

        if (waitBeforeFadeOut > 0f)                            // SI HAY TIEMPO DE ESPERA
            yield return new WaitForSeconds(waitBeforeFadeOut);// ESPERA

        yield return FadeTo(1f);                               // FADE OUT: REGRESA A NEGRO
    }

    public void TriggerFadeOut()
    {
        if (!enabled) return;                                  // SI ESTA DESACTIVADO, NO HACE NADA

        if (running != null) StopCoroutine(running);           // DETIENE LA CORRUTINA ACTUAL SI EXISTE
        running = StartCoroutine(FadeTo(1f));                  // SOLO HACE FADE OUT (EL MANAGER CARGA ESCENA APARTE)
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = fadeOverlay.alpha;                  // ALPHA INICIAL
        float t = 0f;                                          // TIEMPO ACUMULADO

        fadeOverlay.blocksRaycasts = true;                     // BLOQUEA INPUT DURANTE EL FADE

        while (t < fadeDuration)                               // MIENTRAS NO TERMINE LA DURACION
        {
            t += Time.deltaTime;                               // AVANZA POR FRAME
            float p = Mathf.Clamp01(t / fadeDuration);         // PROGRESO 0..1
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, p); // CAMBIA ALPHA SUAVE
            yield return null;                                 // ESPERA SIGUIENTE FRAME
        }

        fadeOverlay.alpha = targetAlpha;                       // FIJA EL FINAL
        fadeOverlay.blocksRaycasts = targetAlpha > 0.001f;     // BLOQUEA SOLO SI QUEDO OSCURO
    }
}