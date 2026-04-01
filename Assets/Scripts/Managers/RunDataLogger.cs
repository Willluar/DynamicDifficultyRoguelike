using UnityEngine;
using System.IO;

public class RunDataLogger : MonoBehaviour
{
    public static RunDataLogger Instance;

    private RunData currentRun;
    private StageData currentStage;

    private string filePath;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, "runData.json");
        Debug.Log("RunDataLogger saving to: " + filePath);
    }

    /* ---------------- Run Lifecycle ---------------- */

    public void StartRun(int runID, string buildID)
    {
        currentRun = new RunData
        {
            runID = runID,
            timestampStart = System.DateTime.UtcNow.ToString("o"),
            buildID = buildID
        };

        StartStage(1);
    }

    public void EndRunOnDeath()
    {
        if (currentRun == null) return;

        // close out the current stage as "failed stage" stats are still useful
        FinaliseCurrentStage();

        currentRun.timestampEnd = System.DateTime.UtcNow.ToString("o");
        SaveRunToJson(currentRun);

        Debug.Log("Run saved to: " + filePath);
        currentRun = null;
        currentStage = null;
    }

    /* ---------------- Stage Lifecycle ---------------- */

    public void StartStage(int stageIndex)
    {
        if (currentRun == null) return;

        currentStage = new StageData
        {
            stageIndex = stageIndex
        };
    }

    // Call when all enemies are defeated
    public void CompleteStageAndAdvance(DifficultySnapshot difficultyForStage)
    {
        if (currentRun == null || currentStage == null) return;

        currentRun.stagesCleared++;

        currentStage.difficulty = difficultyForStage;
        currentRun.stages.Add(currentStage);

        // Start next stage
        StartStage(currentRun.stagesCleared + 1);
    }

    private void FinaliseCurrentStage()
    {
        // Only add it if it's not already in the list
        // (If you die mid-stage, we still want the partial stats)
        if (currentRun == null || currentStage == null) return;

        // Don’t double-add if it already exists (safety)
        bool alreadyAdded = currentRun.stages.Exists(s => s.stageIndex == currentStage.stageIndex);
        if (!alreadyAdded)
            currentRun.stages.Add(currentStage);
    }

    /* ---------------- Metrics ---------------- */

    public void RecordTurn()
    {
        if (currentRun == null || currentStage == null) return;
        currentRun.totalTurns++;
        currentStage.stageTurns++;
    }

    public void AddDamageDealt(int amount)
    {
        if (currentRun == null || currentStage == null) return;
        currentRun.totalDamageDealt += amount;
        currentStage.stageDamageDealt += amount;
    }

    public void AddDamageTaken(int amount)
    {
        if (currentRun == null || currentStage == null) return;
        currentRun.totalDamageTaken += amount;
        currentStage.stageDamageTaken += amount;
    }

    public void AddEnemyKill()
    {
        if (currentRun == null || currentStage == null) return;
        currentRun.totalEnemiesKilled++;
        currentStage.stageEnemiesKilled++;
    }

    /* ---------------- JSON IO ---------------- */

    private void SaveRunToJson(RunData run)
    {
        RunDataCollection collection = new RunDataCollection();

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            if (!string.IsNullOrWhiteSpace(json))
            {
                collection = JsonUtility.FromJson<RunDataCollection>(json);
                if (collection == null) collection = new RunDataCollection();
            }
        }

        collection.runs.Add(run);

        string updatedJson = JsonUtility.ToJson(collection, true);
        File.WriteAllText(filePath, updatedJson);
    }
}