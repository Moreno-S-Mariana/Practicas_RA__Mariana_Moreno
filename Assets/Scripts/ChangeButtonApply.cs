using UnityEngine;

public class CustomizationApplier : MonoBehaviour
{
    [SerializeField] private CustomizationSystem customizationSystem;

    private void Start()
    {
        // SI NO LO ASIGNO EN INSPECTOR, INTENTA ENCONTRARLO EN EL MISMO GAMEOBJECT
        if (customizationSystem == null)
            customizationSystem = GetComponent<CustomizationSystem>();

        // SI EXISTE, CARGA Y APLICA LA PERSONALIZACION
        if (customizationSystem != null)
            customizationSystem.LoadAndApplySavedCustomization();
    }
}