using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Vuforia;

public class TarguetGate : MonoBehaviour
{
    [Header("References")]
    public Move mover;
    public ObserverBehaviour[] imageTargets;

    [Header("Story order")]
    public int fixedStart0 = 0;
    public int fixedStart1 = 1;
    public int randomMidA = 2;
    public int randomMidB = 3;
    public int randomMidC = 4;
    public int fixedEnd = 5;

    [Tooltip("Genera la secuencia al iniciar.")]
    public bool shuffleOnAwake = true;

    [Header("Per-target content")]
    [Tooltip("Villano o personaje secundario por índice de target.")]
    public GameObject[] secondaryCharacterByTarget;

    [Tooltip("Canvas / mensaje / pista por índice de target.")]
    public GameObject[] clueByTarget;

    [Header("UI Panels")]
    [Tooltip("Panel que dice que ese target no es el correcto.")]
    public GameObject wrongTargetUI;

    [Tooltip("Panel opcional cuando se pierde tracking.")]
    public GameObject lostTrackingUI;

    [Header("Tracking")]
    public bool allowLimitedWhileVisible = false;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugOverlay = true;

    private readonly HashSet<int> validatedTargets = new HashSet<int>();
    private int[] storySequence = Array.Empty<int>();
    private int expectedStoryPos = 0;

    private int lastTrackedIndex = -1;
    private int lastParentedIndex = -1;
    private string lastDecision = "";
    private float lastDecisionTime = 0f;

    private void Awake()
    {
        if (shuffleOnAwake) BuildStorySequence();
        HideEverything();
    }

    private void Start()
    {
        if (!shuffleOnAwake) BuildStorySequence();
        HideEverything();
        ValidateSetup();
    }

    private void Update()
    {
        int trackedIndex = FindFirstTrackedTargetIndex(out _);
        lastTrackedIndex = trackedIndex;

        int parentedIndex = GetCurrentParentedIndex();
        lastParentedIndex = parentedIndex;

        // 1) Si no hay ningún target detectado, no mostramos contenido.
        if (trackedIndex < 0)
        {
            lastDecision = "NO TRACKING => HIDE / LOST";
            lastDecisionTime = Time.time;

            if (validatedTargets.Count > 0 && lostTrackingUI != null)
                ShowLost();
            else
                HideEverything();

            return;
        }

        // 2) Si el target detectado no es donde está parado el personaje principal,
        //    NO se muestra nada en esa tarjeta.
        if (trackedIndex != parentedIndex)
        {
            lastDecision = $"TRACKED={trackedIndex}, PARENTED={parentedIndex} => HIDE";
            lastDecisionTime = Time.time;
            HideEverything();
            return;
        }

        // 3) Si ya fue validado y el personaje sigue parado ahí,
        //    sí puede volver a verse su contenido.
        if (validatedTargets.Contains(trackedIndex))
        {
            lastDecision = $"TRACKED={trackedIndex}, PARENTED={parentedIndex}, VALIDATED => SHOW CONTENT";
            lastDecisionTime = Time.time;
            ShowContentForIndex(trackedIndex);
            return;
        }

        // 4) Si el personaje sí está parado ahí, revisamos si es el siguiente correcto.
        int expectedTargetIndex = GetExpectedTargetIndex();

        if (trackedIndex != expectedTargetIndex)
        {
            lastDecision = $"TRACKED={trackedIndex}, EXPECTED={expectedTargetIndex}, PARENTED OK => WRONG";
            lastDecisionTime = Time.time;
            ShowWrong();
            return;
        }

        // 5) Si sí es el correcto, renderizamos villano + canvas, validamos y avanzamos historia.
        lastDecision = $"CORRECT TRACKED={trackedIndex}, EXPECTED={expectedTargetIndex} => SHOW + ADVANCE";
        lastDecisionTime = Time.time;

        ShowContentForIndex(trackedIndex);

        validatedTargets.Add(trackedIndex);

        if (expectedStoryPos < storySequence.Length - 1)
            expectedStoryPos++;
    }

    public int GetExpectedTargetPublic()
    {
        return GetExpectedTargetIndex();
    }

    public bool IsTargetExpected(int index)
    {
        return index == GetExpectedTargetIndex();
    }

    public bool IsTargetValidated(int index)
    {
        return validatedTargets.Contains(index);
    }

    private int GetCurrentParentedIndex()
    {
        if (mover == null || mover.CurrentParentTarget == null || imageTargets == null)
            return -1;

        for (int i = 0; i < imageTargets.Length; i++)
        {
            if (imageTargets[i] == mover.CurrentParentTarget)
                return i;
        }

        return -1;
    }

    private int GetExpectedTargetIndex()
    {
        if (storySequence == null || storySequence.Length == 0)
            return fixedStart0;

        expectedStoryPos = Mathf.Clamp(expectedStoryPos, 0, storySequence.Length - 1);
        return storySequence[expectedStoryPos];
    }

    private void BuildStorySequence()
    {
        if (imageTargets == null || imageTargets.Length == 0)
        {
            storySequence = new[] { fixedStart0, fixedStart1, randomMidA, randomMidB, randomMidC, fixedEnd };
            expectedStoryPos = 0;
            validatedTargets.Clear();
            return;
        }

        fixedStart0 = Mathf.Clamp(fixedStart0, 0, imageTargets.Length - 1);
        fixedStart1 = Mathf.Clamp(fixedStart1, 0, imageTargets.Length - 1);
        randomMidA = Mathf.Clamp(randomMidA, 0, imageTargets.Length - 1);
        randomMidB = Mathf.Clamp(randomMidB, 0, imageTargets.Length - 1);
        randomMidC = Mathf.Clamp(randomMidC, 0, imageTargets.Length - 1);
        fixedEnd = Mathf.Clamp(fixedEnd, 0, imageTargets.Length - 1);

        var mids = new List<int> { randomMidA, randomMidB, randomMidC };
        Shuffle(mids);

        storySequence = new[]
        {
            fixedStart0,
            fixedStart1,
            mids[0],
            mids[1],
            mids[2],
            fixedEnd
        };

        expectedStoryPos = 0;
        validatedTargets.Clear();

        if (debugLogs)
            Debug.Log("[STORY] Sequence: " + string.Join(" -> ", storySequence));
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private int FindFirstTrackedTargetIndex(out ObserverBehaviour target)
    {
        target = null;
        if (imageTargets == null) return -1;

        for (int i = 0; i < imageTargets.Length; i++)
        {
            var t = imageTargets[i];
            if (IsVisibleByTracking(t))
            {
                target = t;
                return i;
            }
        }

        return -1;
    }

    private bool IsVisibleByTracking(ObserverBehaviour t)
    {
        if (t == null) return false;

        var status = t.TargetStatus.Status;
        if (status == Status.TRACKED) return true;
        if (allowLimitedWhileVisible && status == Status.LIMITED) return true;

        return false;
    }

    private void ShowContentForIndex(int index)
    {
        SetAllSecondaryAndCluesOff();

        EnableForIndex(secondaryCharacterByTarget, index, true);
        EnableForIndex(clueByTarget, index, true);

        if (wrongTargetUI != null) wrongTargetUI.SetActive(false);
        if (lostTrackingUI != null) lostTrackingUI.SetActive(false);

        if (debugLogs)
            Debug.Log($"[SHOW CONTENT] idx={index}");
    }

    private void ShowWrong()
    {
        SetAllSecondaryAndCluesOff();

        if (lostTrackingUI != null) lostTrackingUI.SetActive(false);
        if (wrongTargetUI != null) wrongTargetUI.SetActive(true);

        if (debugLogs)
            Debug.Log("[SHOW WRONG]");
    }

    private void ShowLost()
    {
        SetAllSecondaryAndCluesOff();

        if (wrongTargetUI != null) wrongTargetUI.SetActive(false);
        if (lostTrackingUI != null) lostTrackingUI.SetActive(true);

        if (debugLogs)
            Debug.Log("[SHOW LOST]");
    }

    private void HideEverything()
    {
        SetAllSecondaryAndCluesOff();

        if (wrongTargetUI != null) wrongTargetUI.SetActive(false);
        if (lostTrackingUI != null) lostTrackingUI.SetActive(false);
    }

    private void SetAllSecondaryAndCluesOff()
    {
        SetArrayActive(secondaryCharacterByTarget, false);
        SetArrayActive(clueByTarget, false);
    }

    private static void SetArrayActive(GameObject[] arr, bool active)
    {
        if (arr == null) return;

        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null)
                arr[i].SetActive(active);
        }
    }

    private static void EnableForIndex(GameObject[] arr, int index, bool active)
    {
        if (arr == null) return;
        if (index < 0 || index >= arr.Length) return;

        if (arr[index] != null)
            arr[index].SetActive(active);
    }

    private void ValidateSetup()
    {
        if (!debugLogs) return;

        Debug.Log($"[CHECK] imageTargets={(imageTargets != null ? imageTargets.Length : 0)}");
        Debug.Log($"[CHECK] story={string.Join(" -> ", storySequence)}");
        Debug.Log($"[CHECK] expected first={GetExpectedTargetIndex()}");
    }

    private void OnGUI()
    {
        if (!debugOverlay) return;

        var sb = new StringBuilder(1024);
        sb.AppendLine("=== DEBUG TarguetGate ===");
        sb.AppendLine($"Story: {string.Join(" -> ", storySequence)}");
        sb.AppendLine($"ExpectedPos: {expectedStoryPos}");
        sb.AppendLine($"ExpectedTarget: {GetExpectedTargetIndex()}");
        sb.AppendLine($"TrackedIndex: {lastTrackedIndex}");
        sb.AppendLine($"ParentedIndex: {lastParentedIndex}");
        sb.AppendLine($"Validated: [{string.Join(",", validatedTargets)}]");
        sb.AppendLine($"Decision: {lastDecision}");
        sb.AppendLine($"DecisionTime: {lastDecisionTime:0.00}");

        GUI.Box(new Rect(10, 10, 650, 220), sb.ToString());
    }
}