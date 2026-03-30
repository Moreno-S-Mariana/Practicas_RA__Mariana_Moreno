// Assets/Scripts/Customization/CustomizationData.cs
using System;
using UnityEngine;

// CLASE DE DATOS PARA GUARDAR LA PERSONALIZACION
// GUARDAMOS SOLO VALORES (INDICES + COLOR) PARA PODER RECONSTRUIR EL PERSONAJE EN OTRA ESCENA
[Serializable]
public class CustomizationData
{
    // INDICE DEL ACCESORIO ACTIVO (-1 SI NO HAY)
    public int accessoryIndex = -1;

    // INDICE DEL OUTFIT ACTIVO (-1 SI NO HAY)
    public int outfitIndex = -1;

    // COLOR ACTUAL (SE USA PARA TINTAR EL ACCESORIO ACTIVO)
    public Color color = Color.white;
}

// CLASE ESTATICA PARA GUARDAR Y CARGAR EN PLAYERPREFS
// SE USA JSON PORQUE ES FACIL Y SE GUARDA TODO EN UNA SOLA CLAVE
public static class CustomizationStorage
{
    // CLAVE UNICA DONDE SE GUARDA EL JSON
    private const string Key = "CUSTOMIZATION_DATA_JSON";

    // FUNCION PARA GUARDAR LA INFO ACTUAL
    public static void Save(CustomizationData data)
    {
        // CONVERTIMOS A JSON PARA QUE PLAYERPREFS LO GUARDE COMO STRING
        string json = JsonUtility.ToJson(data);

        PlayerPrefs.SetString(Key, json);

        // MUY IMPORTANTE: Save() PARA QUE SE ESCRIBA EN DISCO
        PlayerPrefs.Save();
    }

    // FUNCION PARA CARGAR LA INFO
    // SI NO HAY DATA GUARDADA, REGRESA VALORES DEFAULT
    public static CustomizationData LoadOrDefault()
    {
        if (!PlayerPrefs.HasKey(Key))
            return new CustomizationData();

        string json = PlayerPrefs.GetString(Key);

        if (string.IsNullOrWhiteSpace(json))
            return new CustomizationData();

        // TRY/CATCH POR SI EL JSON SE ROMPE (EVITA CRASHEOS)
        try
        {
            CustomizationData loaded = JsonUtility.FromJson<CustomizationData>(json);
            return loaded != null ? loaded : new CustomizationData();
        }
        catch
        {
            return new CustomizationData();
        }
    }

    // FUNCION PARA SABER SI EXISTE DATA GUARDADA
    public static bool HasSaved()
    {
        return PlayerPrefs.HasKey(Key);
    }

    // FUNCION PARA BORRAR LA PERSONALIZACION GUARDADA (OPCIONAL)
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}