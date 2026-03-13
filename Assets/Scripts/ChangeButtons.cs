using System.Collections.Generic;
using UnityEngine;

public class CustomizationSystem : MonoBehaviour
{
    [Header("COLOR")]
    public Color color = Color.white;

    [Header("ACCESORIOS")]
    public GameObject[] accessories;
    private int currentAccessory = -1;

    [Header("OUTFITS")]
    public GameObject[] outfits;
    private int currentOutfit = -1;

    void Start()
    {
        // Apagar todo al iniciar
        DeactivateAllAccessories();
        DeactivateAllOutfits();
    }

    // =========================
    // BOTON COLOR
    // Activa SOLO accessories[1] y le cambia el color solo a ese
    // =========================
    public void ChangeColor_BTN()
    {
        // validar que exista el accesorio 1
        if (accessories == null || accessories.Length <= 1 || accessories[1] == null)
            return;

        // apagar todos los outfits
        DeactivateAllOutfits();
        currentOutfit = -1;

        // apagar todos los accesorios
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

    // =========================
    // BOTON ACCESORIO
    // =========================
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
        int newAccessory = GetRandomDifferentIndex(accessories.Length, currentAccessory);

        if (newAccessory < 0 || accessories[newAccessory] == null)
            return;

        currentAccessory = newAccessory;
        accessories[currentAccessory].SetActive(true);

        // cambiar color automáticamente al accesorio activado
        color = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        ApplyColorToObject(accessories[currentAccessory], color);
    }

    // =========================
    // BOTON OUTFIT
    // =========================
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

    // =========================
    // METODOS AUXILIARES
    // =========================
    private void DeactivateAllAccessories()
    {
        if (accessories == null) return;

        for (int i = 0; i < accessories.Length; i++)
        {
            if (accessories[i] != null)
                accessories[i].SetActive(false);
        }
    }

    private void DeactivateAllOutfits()
    {
        if (outfits == null) return;

        for (int i = 0; i < outfits.Length; i++)
        {
            if (outfits[i] != null)
                outfits[i].SetActive(false);
        }
    }

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