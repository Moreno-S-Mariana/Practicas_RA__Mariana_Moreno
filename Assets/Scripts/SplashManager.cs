using UnityEngine; 
using UnityEngine.SceneManagement; 

public class SplashManager : MonoBehaviour 
{
    public float tiempo = 3f; // TIEMPO DE ESPERA ANTES DE CAMBIAR ESCENA

    void Start() // SE EJECUTA AL INICIAR EL OBJETO
    {
        Invoke("IrAlMenu", tiempo); // LLAMA AL METODO DESPUES DEL TIEMPO INDICADO
    }

    void IrAlMenu() // METODO QUE CAMBIA DE ESCENA
    {
        SceneManager.LoadScene("MainMenu"); // CARGA LA ESCENA LLAMADA MAINMENU
    }
}