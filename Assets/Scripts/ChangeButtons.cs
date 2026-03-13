using System.Collections.Generic;
using UnityEngine;

public class CustomizationSystem : MonoBehaviour
{
    [Header("COLOR")] // SE USA COMO BASE LA VARIABLE COLOR DEL EJERCICIO P02 PARA GUARDAR EL COLOR ACTUAL
    public Color color = Color.white;

    [Header("ACCESORIOS")]// SE USA COMO BASE EL ARRAY DE ACCESORIOS 
    public GameObject[] accessories;
    private int currentAccessory = -1;

    [Header("OUTFITS")]// SE USA COMO BASE EL ARRAY DE OUTFITS
    public GameObject[] outfits;
    private int currentOutfit = -1;

    void Start()
    {
        // Apagar todo al iniciar
        DeactivateAllAccessories();
        DeactivateAllOutfits();
    }

    //FUNCION PARA HACER EL CAMBIO DE COLOR 
     // SE USA COMO BASE LA FUNCION DEL EJERCICIO P02 
     // SE MODIFICA QUE SOLO SE CAMBIE EL ACCESORIO 1 YAS QUE TIENE UNICAMENTE UN SOLO MATERIAL
    public void ChangeColor_BTN()
    {
        // validar que exista el accesorio 1
        if (accessories == null || accessories.Length <= 1 || accessories[1] == null)
            return;

        // apagar todos los outfits
        //EVITAMOS QUE POUTFITS CON OUTFITS SE MEZCLEN Y SOLO SE ACTIVE EL ACCESORIO 1
        DeactivateAllOutfits();
        currentOutfit = -1;

        // apagar todos los accesorios
        //EVITAMOS QUE SE MEZCLEN ACCESORIOS O QUE SE PONGA UN ACCESORIO QUE NO ES 
        DeactivateAllAccessories();

        // activar SOLO el accesorio 1
        currentAccessory = 1;
        accessories[currentAccessory].SetActive(true);

        // generar color random
        color = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        // cambiar color SOLO del accesorio 1
        ApplyColorToObject(accessories[currentAccessory], color);
    }

    // FUNCION PARA HACER EL CAMBIO DE ACCESORIO 
    // SE MODIFICA QUE SOLO SE CAMBIE EL ACCESORIO Y QUE SE APAGUEN LOS OUTFITS PARA EVITAR MEZCLAS
    public void ChangeAccessory_BTN()
    {
        if (accessories == null || accessories.Length == 0) 
            return;

        // apagar todos los outfits
        DeactivateAllOutfits();
        currentOutfit = -1;

        // apagar todos los accesorios
        DeactivateAllAccessories();

        // elegir accesorio nuevo sin repetir el actual
        //EVITAMOS REPETICIONES DE ACCESORIO O QUE QUEDE PERMANENTEMENTE EL MISMO ACCESORIO ACTIVADO
        int newAccessory = GetRandomDifferentIndex(accessories.Length, currentAccessory);

        if (newAccessory < 0 || accessories[newAccessory] == null)
            return;

        currentAccessory = newAccessory;
        accessories[currentAccessory].SetActive(true);

        //FUNCION OPCIONAL PARA CAMBIAR EL COLOR DEL ACCESORIO ACTIVADO
        // cambiar color automáticamente al accesorio activado
        /*color = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        ApplyColorToObject(accessories[currentAccessory], color);*/
    }

    // FUNCION PARA HACER EL CAMBIO DE OUTFIT
    // SE MODIFICA QUE SOLO SE CAMBIE EL OUTFIT Y QUE SE APAGUEN LOS ACCESORIOS PARA EVITAR MEZCLAS
    public void ChangeOutfit_BTN()
    {
        if (outfits == null || outfits.Length == 0)
            return;

        // apagar todos los accesorios
        DeactivateAllAccessories();
        currentAccessory = -1;

        // apagar todos los outfits
        DeactivateAllOutfits();

        // elegir outfit nuevo sin repetir el actual
        int newOutfit = GetRandomDifferentIndex(outfits.Length, currentOutfit);

        if (newOutfit < 0 || outfits[newOutfit] == null)
            return;

        currentOutfit = newOutfit;
        outfits[currentOutfit].SetActive(true);
    }

    /***********************************************************************************/
    // FUNCIONES AUXILIARES PARA APAGAR ACCESORIOS Y OUTFITS, OBTENER UN INDICE ALEATORIO DIFERENTE, Y APLICAR COLOR A UN OBJETO    
    /***********************************************************************************/

    // Función para apagar todos los accesorios
    //evita que se generen confusiones o mezclas de accesorios al activar uno nuevo
    private void DeactivateAllAccessories()
    {
        if (accessories == null) return;

        for (int i = 0; i < accessories.Length; i++)
        {
            if (accessories[i] != null)
                accessories[i].SetActive(false);
        }
    }
    // Función para apagar todos los outfits
    private void DeactivateAllOutfits()
    {
        if (outfits == null) return;

        for (int i = 0; i < outfits.Length; i++)
        {
            if (outfits[i] != null)
                outfits[i].SetActive(false);
        }
    }
    // Función para obtener un índice aleatorio diferente al actual Y EVITAR QUE SE ESTANQUE EN UN SOLO ACCESORIO O VESTUARIO 
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
    //FUNCION PARA RENDEREAR EL COLOR EN LOS ACCESORIOS P02
    private void ApplyColorToObject(GameObject obj, Color newColor)
    {
        if (obj == null) return;

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