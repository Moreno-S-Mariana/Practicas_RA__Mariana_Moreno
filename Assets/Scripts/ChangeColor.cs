using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    public GameObject model; // declaramos el objeto que queremos cambiar de color
    public Color color; // declaramos el color que queremos asignar al objeto
    public Material colorMaterial; // declaramos el material que queremos asignar al objeto 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //ETODO INDEPENDIENTE QUE SE EJECUTA UNICAMENTE CUANDO OPRIMO EL BOTON
    public void ChangeColor_BTN()
    {
        color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

        // cambiar color del modelo
        model.GetComponent<Renderer>().material.color = color;

        // cambiar color del material
        if (colorMaterial != null)
            colorMaterial.color = color;
    }
}
