using System.Collections;
using UnityEngine;

public class MultiRunBatchController : MonoBehaviour
{
    public static MultiRunBatchController Instance;

    [Header("Batch Settings")]
    public bool enableBatchRuns = false;
    public int totalRunsToExecute = 10;

    [Header("Restart Timing")]
    public float manualRestartDelay = 1.0f;
    public float simulationRestartDelay = 0.05f;

    [Header("Runtime Info")]
    public int runsStarted = 0;
    public int runsCompleted = 0;

    private bool batchActive = false;
    private bool waitingToStartNextRun = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void NotifyRunStarted(int runID)
    {
        runsStarted++;

        if (enableBatchRuns)
            batchActive = true;
    }

    public bool HandleRunEnded()
    {
        runsCompleted++;

        bool simActive = SimulationManager.Instance != null && SimulationManager.Instance.IsUsingSimulatedPlayer();

        if (!enableBatchRuns || !simActive)
        {
            batchActive = false;
            return false;
        }

        if (runsCompleted >= totalRunsToExecute)
        {
            batchActive = false;
            return false;
        }

        if (!waitingToStartNextRun)
            StartCoroutine(StartNextRunRoutine());

        return true;
    }

    private IEnumerator StartNextRunRoutine()
    {
        waitingToStartNextRun = true;

        float delay = manualRestartDelay;

        if (SimulationManager.Instance != null && SimulationManager.Instance.IsUsingSimulatedPlayer())
            delay = simulationRestartDelay;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        if (GameManager.Instance != null)
            GameManager.Instance.StartRun();

        waitingToStartNextRun = false;
    }

    public void ResetBatchProgress()
    {
        runsStarted = 0;
        runsCompleted = 0;
        batchActive = false;
        waitingToStartNextRun = false;
    }

    public bool IsBatchActive()
    {
        return batchActive;
    }

    public bool IsBatchComplete()
    {
        return enableBatchRuns && runsCompleted >= totalRunsToExecute;
    }
}