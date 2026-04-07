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

    [Header("Visibility Mode")]
    [Tooltip("Si está activo, NO desactiva el GameObject del personaje; solo apaga/prende sus Renderers para no reiniciar Animator.")]
    public bool useRendererVisibilityForCharacters = true;

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

    // Estado visual actual para NO repetir SetActive / render toggles cada frame
    private int currentShownIndex = -1;
    private VisualState currentVisualState = VisualState.None;

    private enum VisualState
    {
        None,
        Content,
        Wrong,
        Lost
    }

    private void Awake()
    {
        if (shuffleOnAwake) BuildStorySequence();
        HideEverythingImmediate();
    }

    private void Start()
    {
        if (!shuffleOnAwake) BuildStorySequence();
        HideEverythingImmediate();
        ValidateSetup();
    }

    private void Update()
    {
        int trackedIndex = FindFirstTrackedTargetIndex(out _);
        lastTrackedIndex = trackedIndex;

        int parentedIndex = GetCurrentParentedIndex();
        lastParentedIndex = parentedIndex;

        // 1) No hay tracking
        if (trackedIndex < 0)
        {
            lastDecision = "NO TRACKING => HIDE / LOST";
            lastDecisionTime = Time.time;

            if (validatedTargets.Count > 0 && lostTrackingUI != null)
                ShowLostSafe();
            else
                HideEverythingSafe();

            return;
        }

        // 2) El target detectado no coincide con donde está parentado el personaje principal
        if (trackedIndex != parentedIndex)
        {
            lastDecision = $"TRACKED={trackedIndex}, PARENTED={parentedIndex} => HIDE";
            lastDecisionTime = Time.time;
            HideEverythingSafe();
            return;
        }

        // 3) Si ya fue validado y sigue ahí, vuelve a mostrar su contenido
        if (validatedTargets.Contains(trackedIndex))
        {
            lastDecision = $"TRACKED={trackedIndex}, PARENTED={parentedIndex}, VALIDATED => SHOW CONTENT";
            lastDecisionTime = Time.time;
            ShowContentForIndexSafe(trackedIndex);
            return;
        }

        // 4) Si el personaje sí está ahí, revisamos si es el target esperado
        int expectedTargetIndex = GetExpectedTargetIndex();

        if (trackedIndex != expectedTargetIndex)
        {
            lastDecision = $"TRACKED={trackedIndex}, EXPECTED={expectedTargetIndex}, PARENTED OK => WRONG";
            lastDecisionTime = Time.time;
            ShowWrongSafe();
            return;
        }

        // 5) Correcto: mostrar, validar y avanzar historia
        lastDecision = $"CORRECT TRACKED={trackedIndex}, EXPECTED={expectedTargetIndex} => SHOW + ADVANCE";
        lastDecisionTime = Time.time;

        ShowContentForIndexSafe(trackedIndex);

        if (!validatedTargets.Contains(trackedIndex))
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

    // =========================
    // SAFE VISUAL CONTROL
    // =========================

    private void ShowContentForIndexSafe(int index)
    {
        if (currentVisualState == VisualState.Content && currentShownIndex == index)
            return;

        ApplyCharactersVisibility(index);
        ApplyCluesVisibility(index);

        if (wrongTargetUI != null && wrongTargetUI.activeSelf)
            wrongTargetUI.SetActive(false);

        if (lostTrackingUI != null && lostTrackingUI.activeSelf)
            lostTrackingUI.SetActive(false);

        currentShownIndex = index;
        currentVisualState = VisualState.Content;

        if (debugLogs)
            Debug.Log($"[SHOW CONTENT SAFE] idx={index}");
    }

    private void ShowWrongSafe()
    {
        if (currentVisualState == VisualState.Wrong)
            return;

        HideCharactersAndClues();

        if (lostTrackingUI != null && lostTrackingUI.activeSelf)
            lostTrackingUI.SetActive(false);

        if (wrongTargetUI != null && !wrongTargetUI.activeSelf)
            wrongTargetUI.SetActive(true);

        currentShownIndex = -1;
        currentVisualState = VisualState.Wrong;

        if (debugLogs)
            Debug.Log("[SHOW WRONG SAFE]");
    }

    private void ShowLostSafe()
    {
        if (currentVisualState == VisualState.Lost)
            return;

        HideCharactersAndClues();

        if (wrongTargetUI != null && wrongTargetUI.activeSelf)
            wrongTargetUI.SetActive(false);

        if (lostTrackingUI != null && !lostTrackingUI.activeSelf)
            lostTrackingUI.SetActive(true);

        currentShownIndex = -1;
        currentVisualState = VisualState.Lost;

        if (debugLogs)
            Debug.Log("[SHOW LOST SAFE]");
    }

    private void HideEverythingSafe()
    {
        if (currentVisualState == VisualState.None && currentShownIndex == -1)
            return;

        HideCharactersAndClues();

        if (wrongTargetUI != null && wrongTargetUI.activeSelf)
            wrongTargetUI.SetActive(false);

        if (lostTrackingUI != null && lostTrackingUI.activeSelf)
            lostTrackingUI.SetActive(false);

        currentShownIndex = -1;
        currentVisualState = VisualState.None;

        if (debugLogs)
            Debug.Log("[HIDE EVERYTHING SAFE]");
    }

    private void HideEverythingImmediate()
    {
        HideCharactersAndClues(true);

        if (wrongTargetUI != null) wrongTargetUI.SetActive(false);
        if (lostTrackingUI != null) lostTrackingUI.SetActive(false);

        currentShownIndex = -1;
        currentVisualState = VisualState.None;
    }

    private void ApplyCharactersVisibility(int visibleIndex)
    {
        if (secondaryCharacterByTarget == null) return;

        for (int i = 0; i < secondaryCharacterByTarget.Length; i++)
        {
            GameObject go = secondaryCharacterByTarget[i];
            if (go == null) continue;

            bool visible = (i == visibleIndex);

            if (useRendererVisibilityForCharacters)
                SetCharacterVisibleByRenderer(go, visible);
            else
                SetActiveIfNeeded(go, visible);
        }
    }

    private void ApplyCluesVisibility(int visibleIndex)
    {
        if (clueByTarget == null) return;

        for (int i = 0; i < clueByTarget.Length; i++)
        {
            GameObject go = clueByTarget[i];
            if (go == null) continue;

            bool visible = (i == visibleIndex);
            SetActiveIfNeeded(go, visible);
        }
    }

    private void HideCharactersAndClues(bool force = false)
    {
        if (secondaryCharacterByTarget != null)
        {
            for (int i = 0; i < secondaryCharacterByTarget.Length; i++)
            {
                var go = secondaryCharacterByTarget[i];
                if (go == null) continue;

                if (useRendererVisibilityForCharacters)
                    SetCharacterVisibleByRenderer(go, false);
                else if (force)
                    go.SetActive(false);
                else
                    SetActiveIfNeeded(go, false);
            }
        }

        if (clueByTarget != null)
        {
            for (int i = 0; i < clueByTarget.Length; i++)
            {
                var go = clueByTarget[i];
                if (go == null) continue;

                if (force)
                    go.SetActive(false);
                else
                    SetActiveIfNeeded(go, false);
            }
        }
    }

    private static void SetActiveIfNeeded(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }

    private static void SetCharacterVisibleByRenderer(GameObject root, bool visible)
    {
        if (root == null) return;

        if (!root.activeSelf)
            root.SetActive(true);

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }

        var canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null)
                canvases[i].enabled = visible;
        }
    }

    private void ValidateSetup()
    {
        if (!debugLogs) return;

        Debug.Log($"[CHECK] imageTargets={(imageTargets != null ? imageTargets.Length : 0)}");
        Debug.Log($"[CHECK] story={string.Join(" -> ", storySequence)}");
        Debug.Log($"[CHECK] expected first={GetExpectedTargetIndex()}");
        Debug.Log($"[CHECK] secondary={(secondaryCharacterByTarget != null ? secondaryCharacterByTarget.Length : 0)}");
        Debug.Log($"[CHECK] clues={(clueByTarget != null ? clueByTarget.Length : 0)}");
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
        sb.AppendLine($"VisualState: {currentVisualState}");
        sb.AppendLine($"CurrentShownIndex: {currentShownIndex}");
        sb.AppendLine($"Decision: {lastDecision}");
        sb.AppendLine($"DecisionTime: {lastDecisionTime:0.00}");

        GUI.Box(new Rect(10, 10, 700, 240), sb.ToString());
    }
}