using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using UnityEngine;
using Vuforia;

public class Move : MonoBehaviour
{
    public GameObject model;
    public ObserverBehaviour[] ImageTargets;
    public int currentTarget;
    public float speed = 1.0f;

    public float rotationSpeed = 8.0f; // velocidad de rotacion hacia la direccion del movimiento
    public Animator animator; // animator del personaje
    public string movingBool = "Move_IsMoving"; // bool en animator para caminar/correr 

    private bool isMoving = false; // como no me interesa que aparezca en el inspector lo dejamos como privado0

    public void MoveToNextmARKER()
    {
        if (!isMoving)
        {
            StartCoroutine(MoveModel());
        }
    }
    //SE CREA UNA CORRUTINA QUE SE USA EN UNITY PARA PODER REALIZAR TAREAS FUERA DEL MOMENTO DE RENDERIZADO TRADICIONAL
    private IEnumerator MoveModel()
    {
        isMoving = true;
        ObserverBehaviour target = GetNextDetectedTarget();
        if (target == null)
        {
            isMoving = false;
            yield break;
        }
        Vector3 startPosition = model.transform.position;
        Vector3 targetPosition = target.transform.position;

        // rotamos hacia donde se va a mover antes de iniciar el movimiento
        Vector3 lookDir = targetPosition - model.transform.position;
        lookDir.y = 0f; // evitamos que se incline arriba/abajo
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            while (Quaternion.Angle(model.transform.rotation, targetRot) > 1f)
            {
                model.transform.rotation = Quaternion.Slerp(model.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                yield return null; // espera hasta el siguiente frame
            }
        }

        // activamos animacion de movimiento
        if (animator != null && !string.IsNullOrWhiteSpace(movingBool))
        {
            animator.SetBool(movingBool, true);
        }

        float journey = 0f;

        while (journey < 1f)
        {
            journey += Time.deltaTime * speed;//estandarizamos la vwelocidad del movimiento con el tiempo transcurrido entre frames

            // seguimos rotando hacia la direccion por si cambia un poco 
            Vector3 dynamicDir = target.transform.position - model.transform.position;
            dynamicDir.y = 0f;
            if (dynamicDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(dynamicDir.normalized, Vector3.up);
                model.transform.rotation = Quaternion.Slerp(model.transform.rotation, rot, Time.deltaTime * rotationSpeed);
            }

            model.transform.position = Vector3.Lerp(startPosition, targetPosition, journey);
            yield return null; // espera hasta el siguiente frame
        }

        // desactivamos animacion al llegar
        if (animator != null && !string.IsNullOrWhiteSpace(movingBool))
        {
            animator.SetBool(movingBool, false);
        }

        // actualiza el índice del objetivo actual (al que llego de verdad)
        for (int i = 0; i < ImageTargets.Length; i++)
        {
            if (ImageTargets[i] == target)
            {
                currentTarget = i;
                break;
            }
        }

        isMoving = false;

    }

    private ObserverBehaviour GetNextDetectedTarget()
    {
        // busca SIEMPRE el siguiente target (no el que ya es currentTarget)
        if (ImageTargets == null || ImageTargets.Length == 0) return null;

        for (int i = 1; i <= ImageTargets.Length; i++)
        {
            int idx = (currentTarget + i) % ImageTargets.Length;
            ObserverBehaviour target = ImageTargets[idx];

            if (target != null && (target.TargetStatus.Status == Status.TRACKED || target.TargetStatus.Status == Status.EXTENDED_TRACKED))
            {
                return target;
            }
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        // NO SE VA A AVER EL MOVIMIENTO LO HARA EN UN SOLO FRAME POR ESO SE HACE EN UN METODO EXTERNO
    }
}