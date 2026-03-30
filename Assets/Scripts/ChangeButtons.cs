using UnityEngine;

public class CustomizationSystem : MonoBehaviour
{
    [Header("COLOR")] // SE USA COMO BASE LA VARIABLE COLOR DEL EJERCICIO P02 PARA GUARDAR EL COLOR ACTUAL
    public Color color = Color.white;

    [Header("ACCESORIOS")] // SE USA COMO BASE EL ARRAY DE ACCESORIOS
    public GameObject[] accessories;
    private int currentAccessory = -1;

    [Header("OUTFITS")] // SE USA COMO BASE EL ARRAY DE OUTFITS
    public GameObject[] outfits;
    private int currentOutfit = -1;

    [Header("ANIMATOR ACCESORIOS")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string accessoryIdParam = "AccesoryId";

    [Header("CONTROL ANIMACION ACCESORIOS")]
    [SerializeField] private string accessoryChangeTrigger = "AccesoryChange";
    [SerializeField] private bool waitForButtonToAssignId = true;
    private bool accessoryButtonPressed = false;

    [Header("ANIMATOR OUTFITS")]
    [SerializeField] private string outfitIdParam = "OutfitId";

    [Header("CONTROL ANIMACION OUTFITS")]
    [SerializeField] private string outfitChangeTrigger = "OutfitChange";
    [SerializeField] private bool waitForButtonToAssignOutfitId = true;
    private bool outfitButtonPressed = false;

    [Header("ANIMATOR IDDLE")]
    [SerializeField] private string idleStateName = "PerryIddle";

    void Start()
    {
        // INICIO: SI HAY PERSONALIZACION GUARDADA, SE CARGA Y SE APLICA
        // SI NO HAY DATOS, SE RESETEA (APAGA ACCESORIOS/OUTFITS) COMO EN EL CODIGO ORIGINAL

        if (CustomizationStorage.HasSaved())
        {
            LoadAndApplySavedCustomization();
        }
        else
        {
            DeactivateAllAccessories();
            DeactivateAllOutfits();
        }
    }

    // CAMBIO DE COLOR (BOTON)
    // BASE: P02
    // MOD: SOLO CAMBIA COLOR DEL ACCESORIO ACTIVO (NO FUERZA UN ACCESORIO ESPECIFICO)
    public void ChangeColor_BTN()
    {
        // OPCIONAL: SI QUIERES BLOQUEAR OUTFITS AL CAMBIAR COLOR, DESCOMENTA
        //DeactivateAllOutfits();
        //currentOutfit = -1;

        int activeAccessoryIndex = GetActiveAccessoryIndex();
        if (activeAccessoryIndex < 0) return;

        currentAccessory = activeAccessoryIndex;

        // COLOR RANDOM
        color = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        // APLICA COLOR SOLO AL ACCESORIO ACTIVO
        ApplyColorToObject(accessories[currentAccessory], color);

        // REGRESA A IDLE PARA EVITAR ESTADOS RAROS EN LA ANIMACION
        ForceIdleAnimation();
    }

    // CAMBIO DE ACCESORIO (BOTON)
    // REGLA: APAGA OUTFITS PARA EVITAR MEZCLAS
    public void ChangeAccessory_BTN()
    {
        accessoryButtonPressed = true;

        if (accessories == null || accessories.Length == 0)
            return;

        // NO MEZCLAR: APAGA OUTFITS
        DeactivateAllOutfits();
        currentOutfit = -1;

        // APAGA ACCESORIOS ANTES DE ACTIVAR EL NUEVO
        DeactivateAllAccessories();

        // SELECCIONA ACCESORIO DISTINTO AL ACTUAL (EVITA REPETICION)
        int newAccessory = GetRandomDifferentIndex(accessories.Length, currentAccessory);

        if (newAccessory < 0 || accessories[newAccessory] == null)
            return;

        currentAccessory = newAccessory;
        accessories[currentAccessory].SetActive(true);

        // DISPARA ANIMACION SEGUN ID
        ApplyAccessoryAnimation(currentAccessory);

        // HEREDA EL COLOR ACTUAL AL NUEVO ACCESORIO
        ApplyColorToObject(accessories[currentAccessory], color);

        // OPCIONAL: SI QUIERES QUE CADA CAMBIO GENERE UN NUEVO COLOR, USA ESTE BLOQUE
        /*
        color = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        ApplyColorToObject(accessories[currentAccessory], color);
        */
    }

    // CAMBIO DE OUTFIT (BOTON)
    // REGLA: APAGA ACCESORIOS PARA EVITAR MEZCLAS
    public void ChangeOutfit_BTN()
    {
        outfitButtonPressed = true;

        if (outfits == null || outfits.Length == 0)
            return;

        // NO MEZCLAR: APAGA ACCESORIOS
        DeactivateAllAccessories();
        currentAccessory = -1;

        // APAGA OUTFITS ANTES DE ACTIVAR EL NUEVO
        DeactivateAllOutfits();

        // SELECCIONA OUTFIT DISTINTO AL ACTUAL (EVITA REPETICION)
        int newOutfit = GetRandomDifferentIndex(outfits.Length, currentOutfit);

        if (newOutfit < 0 || outfits[newOutfit] == null)
            return;

        currentOutfit = newOutfit;
        outfits[currentOutfit].SetActive(true);

        // DISPARA ANIMACION SEGUN ID
        ApplyOutfitAnimation(currentOutfit);
    }

    /***********************************************************************************/
    // GUARDAR / CARGAR PERSONALIZACION ENTRE ESCENAS
    /***********************************************************************************/

    // GUARDAR PERSONALIZACION (BOTON)
    // USO: LLAMAR ANTES DE CAMBIAR DE ESCENA (CONFIRMAR / MENU / PLAY)
    public void SaveCustomization_BTN()
    {
        // DETECTA QUE ESTA ACTIVO
        int accessoryIndex = GetActiveAccessoryIndex();
        int outfitIndex = GetActiveOutfitIndex();

        // CREA DATA
        CustomizationData data = new CustomizationData
        {
            accessoryIndex = accessoryIndex,
            outfitIndex = outfitIndex,
            color = color
        };

        // GUARDA EN PLAYERPREFS (JSON)
        CustomizationStorage.Save(data);
    }

    // CARGAR Y APLICAR PERSONALIZACION GUARDADA
    // USO: Start() O CUANDO ENTRES A UNA ESCENA NUEVA
    public void LoadAndApplySavedCustomization()
    {
        CustomizationData data = CustomizationStorage.LoadOrDefault();
        ApplyCustomizationData(data);
    }

    // APLICA LOS DATOS GUARDADOS AL PERSONAJE
    // REGLA: OUTFIT TIENE PRIORIDAD PARA MANTENER "NO MEZCLAR"
    private void ApplyCustomizationData(CustomizationData data)
    {
        // LIMPIA ESTADO
        DeactivateAllAccessories();
        DeactivateAllOutfits();

        currentAccessory = -1;
        currentOutfit = -1;

        // RESTAURA COLOR
        color = data.color;

        // PRIORIDAD: OUTFIT
        if (data.outfitIndex >= 0 &&
            outfits != null &&
            data.outfitIndex < outfits.Length &&
            outfits[data.outfitIndex] != null)
        {
            currentOutfit = data.outfitIndex;
            outfits[currentOutfit].SetActive(true);

            // PERMITE ANIMACION AUNQUE NO VENGA DE BOTON
            outfitButtonPressed = true;
            ApplyOutfitAnimation(currentOutfit);

            return;
        }

        // SI NO HAY OUTFIT, APLICA ACCESORIO
        if (data.accessoryIndex >= 0 &&
            accessories != null &&
            data.accessoryIndex < accessories.Length &&
            accessories[data.accessoryIndex] != null)
        {
            currentAccessory = data.accessoryIndex;
            accessories[currentAccessory].SetActive(true);

            // PERMITE ANIMACION AUNQUE NO VENGA DE BOTON
            accessoryButtonPressed = true;
            ApplyAccessoryAnimation(currentAccessory);

            // APLICA COLOR GUARDADO
            ApplyColorToObject(accessories[currentAccessory], color);
        }
    }

    /***********************************************************************************/
    // AUXILIARES (ESTADO ACTIVO / APAGAR TODO / RANDOM DIFERENTE / COLOR)
    /***********************************************************************************/

    // OBTIENE INDICE DEL ACCESORIO ACTIVO
    private int GetActiveAccessoryIndex()
    {
        if (accessories == null) return -1;

        // PRIORIZA currentAccessory SI SIGUE ACTIVO
        if (currentAccessory >= 0 &&
            currentAccessory < accessories.Length &&
            accessories[currentAccessory] != null &&
            accessories[currentAccessory].activeInHierarchy)
        {
            return currentAccessory;
        }

        // SI NO, BUSCA EL PRIMERO ACTIVO
        for (int i = 0; i < accessories.Length; i++)
        {
            if (accessories[i] != null && accessories[i].activeInHierarchy)
                return i;
        }

        return -1;
    }

    // OBTIENE INDICE DEL OUTFIT ACTIVO
    private int GetActiveOutfitIndex()
    {
        if (outfits == null) return -1;

        // PRIORIZA currentOutfit SI SIGUE ACTIVO
        if (currentOutfit >= 0 &&
            currentOutfit < outfits.Length &&
            outfits[currentOutfit] != null &&
            outfits[currentOutfit].activeInHierarchy)
        {
            return currentOutfit;
        }

        // SI NO, BUSCA EL PRIMERO ACTIVO
        for (int i = 0; i < outfits.Length; i++)
        {
            if (outfits[i] != null && outfits[i].activeInHierarchy)
                return i;
        }

        return -1;
    }

    // APLICA ANIMACION DE ACCESORIO
    private void ApplyAccessoryAnimation(int accessoryIndex)
    {
        if (characterAnimator == null) return;
        if (accessoryIndex < 0) return;

        // BLOQUEO OPCIONAL: SOLO PERMITIR CAMBIO SI VIENE DE BOTON
        if (waitForButtonToAssignId && !accessoryButtonPressed) return;

        characterAnimator.SetInteger(accessoryIdParam, accessoryIndex);
        characterAnimator.SetTrigger(accessoryChangeTrigger);
    }

    // APLICA ANIMACION DE OUTFIT
    private void ApplyOutfitAnimation(int outfitIndex)
    {
        if (characterAnimator == null) return;
        if (outfitIndex < 0) return;

        // BLOQUEO OPCIONAL: SOLO PERMITIR CAMBIO SI VIENE DE BOTON
        if (waitForButtonToAssignOutfitId && !outfitButtonPressed) return;

        characterAnimator.SetInteger(outfitIdParam, outfitIndex);
        characterAnimator.SetTrigger(outfitChangeTrigger);
    }

    // FORZAR IDLE (RESET DE TRIGGERS)
    private void ForceIdleAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.ResetTrigger(accessoryChangeTrigger);
        characterAnimator.ResetTrigger(outfitChangeTrigger);

        if (!string.IsNullOrEmpty(idleStateName))
            characterAnimator.Play(idleStateName, 0, 0f);
    }

    // APAGA TODOS LOS ACCESORIOS
    // EVITA MEZCLAS / DUPLICADOS VISUALES
    private void DeactivateAllAccessories()
    {
        if (accessories == null) return;

        for (int i = 0; i < accessories.Length; i++)
        {
            if (accessories[i] != null)
                accessories[i].SetActive(false);
        }
    }

    // APAGA TODOS LOS OUTFITS
    private void DeactivateAllOutfits()
    {
        if (outfits == null) return;

        for (int i = 0; i < outfits.Length; i++)
        {
            if (outfits[i] != null)
                outfits[i].SetActive(false);
        }
    }

    // RANDOM DIFERENTE AL ACTUAL (EVITA ESTANCARSE EN UNO SOLO)
    private int GetRandomDifferentIndex(int length, int currentIndex)
    {
        if (length <= 0)
            return -1;

        if (length == 1)
            return 0;

        int newIndex = UnityEngine.Random.Range(0, length);

        while (newIndex == currentIndex)
        {
            newIndex = UnityEngine.Random.Range(0, length);
        }

        return newIndex;
    }

    // APLICA COLOR A TODO EL OBJETO (Y SUS HIJOS) QUE TENGAN RENDERER
    private void ApplyColorToObject(GameObject obj, Color newColor)
    {
        if (obj == null)
            return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = newColor;
            }
        }
    }
}