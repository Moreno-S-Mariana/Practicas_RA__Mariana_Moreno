using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InstructionSlideshowClickUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image targetImage;   // IMAGEN UI DONDE SE MUESTRAN LAS DIAPOSITIVAS
    [SerializeField] private Sprite[] slides;     // LISTA DE SPRITES (ORDEN DE INSTRUCCIONES)
    [SerializeField] private bool loop = true;    // SI ES TRUE, AL FINAL REGRESA AL INICIO

    private int index; // INDICE ACTUAL

    private void Awake()
    {
        // INICIA EN LA PRIMERA DIAPOSITIVA
        index = 0;
        Apply();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // DETECTA CLICK SOBRE EL ELEMENTO UI
        Debug.Log("CLICK DETECTED");
        Next();
    }

    private void Next()
    {
        // VALIDACION BASICA (EVITA NULL / ARREGLO VACIO)
        if (targetImage == null || slides == null || slides.Length == 0) return;

        // AVANZA INDICE (CON LOOP O SIN LOOP)
        index = loop
            ? (index + 1) % slides.Length
            : Mathf.Min(index + 1, slides.Length - 1);

        Apply();
    }

    private void Apply()
    {
        // APLICA EL SPRITE ACTUAL A LA IMAGEN
        if (targetImage == null || slides == null || slides.Length == 0) return;
        targetImage.sprite = slides[index];
    }
}