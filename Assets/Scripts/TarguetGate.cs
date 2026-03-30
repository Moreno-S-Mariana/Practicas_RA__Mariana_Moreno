// Assets/Scripts/StoryTargetGateUI_NoMain.cs
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Vuforia;

public class StoryTargetGateUI_NoMain : MonoBehaviour
{
    [Header("Vuforia")]
    public ObserverBehaviour[] imageTargets;

    [Header("Story (fijo+aleatorio)")]
    public int fixedStart0 = 0;
    public int fixedStart1 = 1;

    [Tooltip("Estos 3 se aleatorizan (por defecto 2,3,4).")]
    public int randomMidA = 2;
    public int randomMidB = 3;
    public int randomMidC = 4;

    [Tooltip("Final fijo (por defecto 5).")]
    public int fixedEnd = 5;

    [Tooltip("Si true, se genera la secuencia al iniciar.")]
    public bool shuffleOnAwake = true;

    [Header("Per-target content (match imageTargets indices)")]
    public GameObject[] secondaryCharacterByTarget;
    public GameObject[] clueByTarget;

    [Header("UI Panels")]
    public GameObject wrongTargetUI;  // "este no es"
    public GameObject lostTrackingUI; // "me perdiste"

    [Header("Tracking")]
    public bool allowLimitedWhileVisible = false;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugOverlay = true;

    private readonly HashSet<int> validatedTargets = new HashSet<int>();
    private int[] storySequence = Array.Empty<int>();
    private int expectedStoryPos = 0;
    private bool hasShownAnyPanel = false;

    // debug
    private int lastTrackedIndex = -1;
    private string lastDecision = "";
    private float lastDecisionTime = 0f;

    private void Awake()
    {
        if (shuffleOnAwake) BuildStorySequence();
        HideEverything();
        LogSequence("Awake");
    }

    private void Start()
    {
        if (!shuffleOnAwake) BuildStorySequence();
        HideEverything();
        LogSequence("Start");
        ValidateSetup();
    }

    private void Update()
    {
        int trackedIndex = FindFirstTrackedTargetIndex(out _);
        lastTrackedIndex = trackedIndex;

        // Siempre apagamos lo anterior cuando cambias de estado/carta
        // (esto garantiza que al moverte, el canvas anterior se va).
        if (trackedIndex < 0)
        {
            if (hasShownAnyPanel && expectedStoryPos > 0)
            {
                lastDecision = "NO TRACKING => LOST";
                lastDecisionTime = Time.time;
                ShowLost();
            }
            else
            {
                lastDecision = "NO TRACKING => HIDE";
                lastDecisionTime = Time.time;
                HideEverything();
            }
            return;
        }

        // Si el target trackeado NO está en la historia => no muestra nada.
        if (!IsInStory(trackedIndex))
        {
            lastDecision = $"TRACKED idx={trackedIndex} NOT IN STORY => HIDE";
            lastDecisionTime = Time.time;
            HideEverything();
            return;
        }

        ProcessTrackedTarget(trackedIndex);
    }

    private void ProcessTrackedTarget(int trackedIndex)
    {
        // Apaga todo antes de decidir (evita que quede UI "pegada" al moverte)
        HidePanelsOnly();

        // Si ya validaste este target, puede volver a mostrar al apuntarlo (pero se apaga al salir).
        if (validatedTargets.Contains(trackedIndex))
        {
            lastDecision = $"VALIDATED idx={trackedIndex} => SHOW CONTENT";
            lastDecisionTime = Time.time;
            ShowContentForIndex(trackedIndex);
            return;
        }

        int expectedTargetIndex = GetExpectedTargetIndex();
        bool isCorrect = trackedIndex == expectedTargetIndex;

        if (!isCorrect)
        {
            lastDecision = $"WRONG idx={trackedIndex} expected={expectedTargetIndex} => SHOW WRONG";
            lastDecisionTime = Time.time;
            ShowWrong();
            return;
        }

        lastDecision = $"CORRECT idx={trackedIndex} (step {expectedStoryPos}/{storySequence.Length - 1}) => SHOW + ADVANCE";
        lastDecisionTime = Time.time;

        ShowContentForIndex(trackedIndex);
        validatedTargets.Add(trackedIndex);
        hasShownAnyPanel = true;

        expectedStoryPos = Mathf.Min(expectedStoryPos + 1, storySequence.Length - 1);
    }

    private bool IsInStory(int idx)
    {
        if (storySequence == null) return false;
        for (int i = 0; i < storySequence.Length; i++)
            if (storySequence[i] == idx) return true;
        return false;
    }

    private int GetExpectedTargetIndex()
    {
        if (storySequence == null || storySequence.Length == 0) return fixedStart0;
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
            hasShownAnyPanel = false;
            return;
        }

        // clamp safe
        fixedStart0 = Mathf.Clamp(fixedStart0, 0, imageTargets.Length - 1);
        fixedStart1 = Mathf.Clamp(fixedStart1, 0, imageTargets.Length - 1);
        randomMidA = Mathf.Clamp(randomMidA, 0, imageTargets.Length - 1);
        randomMidB = Mathf.Clamp(randomMidB, 0, imageTargets.Length - 1);
        randomMidC = Mathf.Clamp(randomMidC, 0, imageTargets.Length - 1);
        fixedEnd = Mathf.Clamp(fixedEnd, 0, imageTargets.Length - 1);

        // randomize mids (2,3,4)
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
        hasShownAnyPanel = false;

        if (debugLogs)
            Debug.Log($"[STORY] Sequence built: {string.Join(" -> ", storySequence)}");
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
            if (t == null) continue;

            if (IsVisibleByTracking(t))
            {
                target = t;
                if (debugLogs) Debug.Log($"[TRACKED] idx={i} name={t.name} status={t.TargetStatus.Status}");
                return i;
            }
        }

        return -1;
    }

    private bool IsVisibleByTracking(ObserverBehaviour t)
    {
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

        hasShownAnyPanel = true;

        if (debugLogs)
            Debug.Log($"[SHOW] idx={index} secondary={ObjName(secondaryCharacterByTarget, index)} clue={ObjName(clueByTarget, index)}");
    }

    private void ShowWrong()
    {
        SetAllSecondaryAndCluesOff();
        if (lostTrackingUI != null) lostTrackingUI.SetActive(false);
        if (wrongTargetUI != null) wrongTargetUI.SetActive(true);
        hasShownAnyPanel = true;
    }

    private void ShowLost()
    {
        SetAllSecondaryAndCluesOff();
        if (wrongTargetUI != null) wrongTargetUI.SetActive(false);
        if (lostTrackingUI != null) lostTrackingUI.SetActive(true);
    }

    private void HideEverything()
    {
        SetAllSecondaryAndCluesOff();
        if (wrongTargetUI != null) wrongTargetUI.SetActive(false);
        if (lostTrackingUI != null) lostTrackingUI.SetActive(false);
    }

    private void HidePanelsOnly()
    {
        // Igual que HideEverything en este caso (solo UI secundaria/paneles)
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
            if (arr[i] != null) arr[i].SetActive(active);
    }

    private static void EnableForIndex(GameObject[] arr, int index, bool active)
    {
        if (arr == null) return;
        if (index < 0 || index >= arr.Length) return;
        if (arr[index] != null) arr[index].SetActive(active);
    }

    private static string ObjName(GameObject[] arr, int index)
    {
        if (arr == null) return "arr=null";
        if (index < 0 || index >= arr.Length) return $"out-of-range({index}/{arr.Length})";
        return arr[index] ? arr[index].name : "NULL";
    }

    private void ValidateSetup()
    {
        if (!debugLogs) return;

        Debug.Log($"[CHECK] imageTargets.Length={(imageTargets != null ? imageTargets.Length : 0)}");
        Debug.Log($"[CHECK] story={string.Join(" -> ", storySequence)} expected={GetExpectedTargetIndex()}");

        Debug.Log($"[CHECK] secondary[{fixedStart0}]={ObjName(secondaryCharacterByTarget, fixedStart0)}");
        Debug.Log($"[CHECK] secondary[{fixedStart1}]={ObjName(secondaryCharacterByTarget, fixedStart1)}");
        Debug.Log($"[CHECK] secondary[{randomMidA}]={ObjName(secondaryCharacterByTarget, randomMidA)}");
        Debug.Log($"[CHECK] secondary[{randomMidB}]={ObjName(secondaryCharacterByTarget, randomMidB)}");
        Debug.Log($"[CHECK] secondary[{randomMidC}]={ObjName(secondaryCharacterByTarget, randomMidC)}");
        Debug.Log($"[CHECK] secondary[{fixedEnd}]={ObjName(secondaryCharacterByTarget, fixedEnd)}");
    }

    private void LogSequence(string where)
    {
        if (!debugLogs) return;
        Debug.Log($"[SEQ:{where}] {string.Join(" -> ", storySequence)} expectedTarget={GetExpectedTargetIndex()}");
    }

    private void OnGUI()
    {
        if (!debugOverlay) return;

        var sb = new StringBuilder(1024);
        sb.AppendLine("=== DEBUG StoryTargetGateUI_NoMain (0->1, 2/3/4 RAND, 5 FINAL) ===");
        sb.AppendLine($"TrackedIndex: {lastTrackedIndex}");
        sb.AppendLine($"ExpectedPos: {expectedStoryPos}/{(storySequence.Length > 0 ? storySequence.Length - 1 : 0)}");
        sb.AppendLine($"ExpectedTargetIndex: {GetExpectedTargetIndex()}");
        sb.AppendLine($"Decision: {lastDecision} (t={lastDecisionTime:0.00})");
        sb.AppendLine($"HasShownAnyPanel: {hasShownAnyPanel}");
        sb.AppendLine($"Validated: [{string.Join(",", validatedTargets)}]");
        sb.AppendLine($"Story: {string.Join(" -> ", storySequence)}");

        GUI.Box(new Rect(10, 10, 620, 240), sb.ToString());
    }
}