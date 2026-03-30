// Assets/Scripts/Move.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class Move : MonoBehaviour
{
    [Header("References")]
    public GameObject model;
    public ObserverBehaviour[] ImageTargets;

    [Header("State")]
    public int currentTarget = 0;

    [Header("Movement")]
    public float speed = 1.0f;
    public float rotationSpeed = 8.0f;

    [Header("Animation")]
    public Animator animator;
    public string movingBool = "Move_IsMoving";

    [Header("Auto-select rules")]
    [Tooltip("Pon 0 para incluir el Target 0.")]
    public int autoMoveMinTargetIndex = 0;

    [Tooltip("Con 2 targets normalmente debe ser FALSE. Si true, bloquea regresar al target anterior.")]
    public bool preventReturningToLastTarget = false;

    [Tooltip("Evita moverse a targets bloqueados (completados/correctos).")]
    public bool forbidLockedTargets = true;

    [Header("Tracking tolerance")]
    [Tooltip("Si true, permite LIMITED como presente (para moverse).")]
    public bool allowLimitedStatus = true;

    [Tooltip("Tiempo mínimo en TRACKED antes de considerarlo estable.")]
    [SerializeField] private float trackedMinSeconds = 0.15f;

    [Header("Startup")]
    public bool fireArrivedEventOnStart = true;

    public event Action<string> OnMoveBlocked;
    public event Action<ObserverBehaviour, int> OnArrivedToTarget;

    private bool isMoving;
    private Transform originalParent;
    private ObserverBehaviour currentParentTarget;

    private readonly HashSet<int> lockedTargets = new HashSet<int>();
    private readonly Dictionary<ObserverBehaviour, float> trackedSince = new Dictionary<ObserverBehaviour, float>();

    private int lastTargetIndex = -1;

    public int LastTargetIndex => lastTargetIndex;
    public ObserverBehaviour CurrentParentTarget => currentParentTarget;

    private void Awake()
    {
        if (model != null) originalParent = model.transform.parent;
    }

    private void Start()
    {
        if (ImageTargets != null && ImageTargets.Length > 0 && ImageTargets[0] != null && model != null)
        {
            ParentToTarget(ImageTargets[0]);
            currentTarget = 0;
            lastTargetIndex = -1;

            if (fireArrivedEventOnStart)
                StartCoroutine(FireArrivedNextFrame(0));
        }
    }

    private IEnumerator FireArrivedNextFrame(int index)
    {
        yield return null;
        if (ImageTargets == null || index < 0 || index >= ImageTargets.Length) yield break;
        OnArrivedToTarget?.Invoke(ImageTargets[index], index);
    }

    /// <summary>

    /// </summary>
    public void MoveToOtherTarget()
    {
        if (isMoving)
        {
            OnMoveBlocked?.Invoke("Movimiento en curso.");
            return;
        }

        if (ImageTargets == null || ImageTargets.Length < 2)
        {
            OnMoveBlocked?.Invoke("Necesitas al menos 2 ImageTargets.");
            return;
        }

        int nextIndex = (currentTarget == 0) ? 1 : 0;

        if (forbidLockedTargets && lockedTargets.Contains(nextIndex))
        {
            OnMoveBlocked?.Invoke($"Target {nextIndex} bloqueado.");
            return;
        }

        var nextTarget = ImageTargets[nextIndex];
        if (!IsPresent(nextTarget))
        {
            OnMoveBlocked?.Invoke($"Target {nextIndex} no presente (tracking).");
            return;
        }

        StartCoroutine(MoveModel(nextTarget));
    }

    /// <summary>
    /// Auto-pick (elige el mejor detectado). Útil si tienes más de 2 targets.
    /// </summary>
    public void MoveToDetectedTarget()
    {
        if (isMoving)
        {
            OnMoveBlocked?.Invoke("Movimiento en curso.");
            return;
        }

        var target = GetBestDetectedTarget(out var reason);
        if (target == null)
        {
            OnMoveBlocked?.Invoke(reason);
            return;
        }

        StartCoroutine(MoveModel(target));
    }

    public void MoveToTargetIndex(int index)
    {
        if (isMoving) return;
        if (ImageTargets == null || index < 0 || index >= ImageTargets.Length) return;


        bool applyPrevBlock = preventReturningToLastTarget && ImageTargets.Length > 2;

        if (applyPrevBlock && index == lastTargetIndex)
        {
            OnMoveBlocked?.Invoke($"Bloqueado: no puedes regresar al target anterior ({lastTargetIndex}).");
            return;
        }

        if (forbidLockedTargets && lockedTargets.Contains(index))
        {
            OnMoveBlocked?.Invoke($"Target {index} bloqueado.");
            return;
        }

        var target = ImageTargets[index];
        if (!IsPresent(target))
        {
            OnMoveBlocked?.Invoke($"Target {index} no presente (tracking).");
            return;
        }

        StartCoroutine(MoveModel(target));
    }

    public void LockTarget(int index)
    {
        if (index < 0) return;
        lockedTargets.Add(index);
    }

    public bool IsLocked(int index) => lockedTargets.Contains(index);

    private IEnumerator MoveModel(ObserverBehaviour target)
    {
        isMoving = true;

        int fromIndex = currentTarget;

        UnparentFromCurrentTarget();

        Vector3 startPosition = model.transform.position;
        Vector3 targetPosition = target.transform.position;

        Vector3 lookDir = targetPosition - model.transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            while (Quaternion.Angle(model.transform.rotation, targetRot) > 1f)
            {
                model.transform.rotation = Quaternion.Slerp(model.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                yield return null;
            }
        }

        if (animator != null && !string.IsNullOrWhiteSpace(movingBool))
            animator.SetBool(movingBool, true);

        float t = 0f;

        while (t < 1f)
        {
            targetPosition = target.transform.position;
            t += Time.deltaTime * speed;

            Vector3 dynamicDir = targetPosition - model.transform.position;
            dynamicDir.y = 0f;
            if (dynamicDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(dynamicDir.normalized, Vector3.up);
                model.transform.rotation = Quaternion.Slerp(model.transform.rotation, rot, Time.deltaTime * rotationSpeed);
            }

            model.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        if (animator != null && !string.IsNullOrWhiteSpace(movingBool))
            animator.SetBool(movingBool, false);

        ParentToTarget(target);

        int arrivedIndex = GetTargetIndex(target);

        if (arrivedIndex >= 0 && arrivedIndex != fromIndex)
        {
            lastTargetIndex = fromIndex;
            currentTarget = arrivedIndex;
        }
        else if (arrivedIndex >= 0)
        {
            currentTarget = arrivedIndex;
        }

        OnArrivedToTarget?.Invoke(target, arrivedIndex);

        isMoving = false;
    }

    private ObserverBehaviour GetBestDetectedTarget(out string reason)
    {
        reason = "No hay targets elegibles detectados.";
        if (ImageTargets == null || ImageTargets.Length == 0)
        {
            reason = "ImageTargets vacío.";
            return null;
        }

        int bestIndex = -1;
        ObserverBehaviour best = null;


        bool applyPrevBlock = preventReturningToLastTarget && ImageTargets.Length > 2;

        for (int i = 0; i < ImageTargets.Length; i++)
        {
            if (i < autoMoveMinTargetIndex) continue;

            if (applyPrevBlock && i == lastTargetIndex) continue;
            if (forbidLockedTargets && lockedTargets.Contains(i)) continue;

            var t = ImageTargets[i];
            if (t == null) continue;
            if (t == currentParentTarget) continue;
            if (!IsPresentForAutoPick(t)) continue;

            if (i > bestIndex)
            {
                bestIndex = i;
                best = t;
            }
        }

        if (best == null)
        {
            reason =
                $"Ningún target elegible. current={currentTarget}, last={lastTargetIndex}, minIndex={autoMoveMinTargetIndex}, " +
                $"preventPrev={preventReturningToLastTarget}, forbidLocked={forbidLockedTargets}.";
        }

        return best;
    }

    private bool IsPresentForAutoPick(ObserverBehaviour t)
    {
        if (t == null) return false;

        var status = t.TargetStatus.Status;

        if (status == Status.TRACKED)
        {
            if (!trackedSince.ContainsKey(t))
                trackedSince[t] = Time.time;

            return (Time.time - trackedSince[t]) >= trackedMinSeconds;
        }

        if (allowLimitedStatus && status == Status.LIMITED)
            return true;

        trackedSince.Remove(t);
        return false;
    }

    private bool IsPresent(ObserverBehaviour t)
    {
        if (t == null) return false;

        var status = t.TargetStatus.Status;

        if (status == Status.TRACKED)
        {
            if (!trackedSince.ContainsKey(t))
                trackedSince[t] = Time.time;

            return (Time.time - trackedSince[t]) >= trackedMinSeconds;
        }

        if (allowLimitedStatus && status == Status.LIMITED)
            return true;

        trackedSince.Remove(t);
        return false;
    }

    private int GetTargetIndex(ObserverBehaviour target)
    {
        if (ImageTargets == null) return -1;
        for (int i = 0; i < ImageTargets.Length; i++)
            if (ImageTargets[i] == target) return i;
        return -1;
    }

    private void ParentToTarget(ObserverBehaviour target)
    {
        if (model == null || target == null) return;
        model.transform.SetParent(target.transform, true);
        currentParentTarget = target;
    }

    private void UnparentFromCurrentTarget()
    {
        if (model == null) return;

        if (model.transform.parent != originalParent)
            model.transform.SetParent(originalParent, true);

        currentParentTarget = null;
    }
}