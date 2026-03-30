//using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string nombreEscena)
    {
        Debug.Log("Intentando cargar escena: " + nombreEscena);
        SceneManager.LoadScene(nombreEscena);
    }
}