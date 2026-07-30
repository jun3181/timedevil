using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger step that enables the selected behaviours for a configured amount
/// of time, then returns them to the state they had before the step started.
/// </summary>
[DisallowMultipleComponent]
public sealed class TriggerStep_TimedScriptRunner : TriggerStepBase
{
    [Header("Run Settings")]
    [Min(0f)]
    [SerializeField] private float duration = 1f;
    [SerializeField] private bool runOnStart;
    [Tooltip("Use real time so the timer continues while Time.timeScale is 0.")]
    [SerializeField] private bool useUnscaledTime;
    [Tooltip("Allow player input while this step is waiting for the timer.")]
    [SerializeField] private bool allowPlayerInputWhileExecuting;

    [Header("Scripts To Run")]
    [Tooltip("These components are enabled at the start and restored when time runs out.")]
    [SerializeField] private List<MonoBehaviour> scripts = new List<MonoBehaviour>();

    private readonly Dictionary<MonoBehaviour, bool> previousStates =
        new Dictionary<MonoBehaviour, bool>();
    private Coroutine runningCoroutine;

    public override bool AllowPlayerInputWhileExecuting => allowPlayerInputWhileExecuting;
    public bool IsRunning => runningCoroutine != null;
    public float Duration
    {
        get => duration;
        set => duration = Mathf.Max(0f, value);
    }

    private void Start()
    {
        if (runOnStart)
            Run();
    }

    /// <summary>
    /// Called by TriggerRouter. The route waits here until the configured time
    /// has elapsed, then advances to its next TriggerStep.
    /// </summary>
    public override IEnumerator Execute(TriggerContext ctx)
    {
        Run();

        while (IsRunning)
            yield return null;
    }

    /// <summary>Runs the scripts using the duration set in the Inspector.</summary>
    public void Run()
    {
        StartRun(duration);
    }

    /// <summary>Runs the scripts for a duration supplied at runtime.</summary>
    public void RunFor(float seconds)
    {
        StartRun(Mathf.Max(0f, seconds));
    }

    /// <summary>Stops the current run and restores every script immediately.</summary>
    public void Stop()
    {
        if (runningCoroutine == null)
            return;

        StopCoroutine(runningCoroutine);
        runningCoroutine = null;
        RestoreScripts();
    }

    private void OnDisable()
    {
        if (runningCoroutine == null)
            return;

        StopCoroutine(runningCoroutine);
        runningCoroutine = null;
        RestoreScripts();
    }

    private void StartRun(float seconds)
    {
        // Starting again is a restart: first restore the state captured by the
        // previous run, then take a fresh snapshot.
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
            RestoreScripts();
        }

        previousStates.Clear();
        foreach (MonoBehaviour script in scripts)
        {
            if (script == null || script == this || previousStates.ContainsKey(script))
                continue;

            previousStates.Add(script, script.enabled);
            script.enabled = true;

            if (!script.gameObject.activeInHierarchy)
                Debug.LogWarning($"[{nameof(TriggerStep_TimedScriptRunner)}] '{script.name}' is on an inactive GameObject.", script);
        }

        if (seconds <= 0f)
        {
            RestoreScripts();
            return;
        }

        runningCoroutine = StartCoroutine(RunTimer(seconds));
    }

    private IEnumerator RunTimer(float seconds)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);

        RestoreScripts();
        runningCoroutine = null;
    }

    private void RestoreScripts()
    {
        foreach (KeyValuePair<MonoBehaviour, bool> entry in previousStates)
        {
            if (entry.Key != null)
                entry.Key.enabled = entry.Value;
        }

        previousStates.Clear();
    }
}
