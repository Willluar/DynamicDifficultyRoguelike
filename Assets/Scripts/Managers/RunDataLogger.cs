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

    public string GetSavePath()
    {
        return filePath;
    }

    public void StartRun(int runID, string buildID)
    {
        currentRun = new RunData
        {
            runID = runID,
            timestampStart = System.DateTime.UtcNow.ToString("o"),
            buildID = buildID
        };

        
    }

    public void EndRunOnDeath()
    {
        if (currentRun == null) return;

        FinaliseCurrentStage();
        currentRun.timestampEnd = System.DateTime.UtcNow.ToString("o");
        SaveRunToJson(currentRun);

        Debug.Log("Run saved to: " + filePath);

        currentRun = null;
        currentStage = null;
    }

    public void StartStage(int stageIndex)
    {
        if (currentRun == null) return;

        currentStage = new StageData
        {
            stageIndex = stageIndex
        };
    }

    public void SetCurrentStageDifficulty(float healthMult, float damageMult, int enemyCount)
    {
        if (currentStage == null) return;

        currentStage.enemyHealthMultiplier = healthMult;
        currentStage.enemyDamageMultiplier = damageMult;
        currentStage.enemyCount = enemyCount;
    }

    public void CompleteStage()
    {
        if (currentRun == null || currentStage == null) return;

        currentRun.stagesCleared++;
        currentRun.stages.Add(currentStage);
    }

    private void FinaliseCurrentStage()
    {
        if (currentRun == null || currentStage == null) return;

        bool alreadyAdded = currentRun.stages.Exists(s => s.stageIndex == currentStage.stageIndex);
        if (!alreadyAdded)
            currentRun.stages.Add(currentStage);
    }

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

    public void RecordSpellCast(DamageType damageType)
    {
        if (currentRun == null || currentStage == null) return;

        switch (damageType)
        {
            case DamageType.Melee:
                currentRun.meleeCasts++;
                currentStage.meleeCasts++;
                break;
            case DamageType.Fire:
                currentRun.fireCasts++;
                currentStage.fireCasts++;
                break;
            case DamageType.Ice:
                currentRun.iceCasts++;
                currentStage.iceCasts++;
                break;
            case DamageType.Lightning:
                currentRun.lightningCasts++;
                currentStage.lightningCasts++;
                break;
        }
    }

    public void SetDDAEnabled(bool enabled)
    {
        if (currentRun != null)
            currentRun.ddaEnabled = enabled;
    }

    public void RecordDamageByType(DamageType damageType, int amount)
    {
        if (currentRun == null || currentStage == null) return;

        switch (damageType)
        {
            case DamageType.Melee:
                currentRun.meleeDamage += amount;
                currentStage.meleeDamage += amount;
                break;
            case DamageType.Fire:
                currentRun.fireDamage += amount;
                currentStage.fireDamage += amount;
                break;
            case DamageType.Ice:
                currentRun.iceDamage += amount;
                currentStage.iceDamage += amount;
                break;
            case DamageType.Lightning:
                currentRun.lightningDamage += amount;
                currentStage.lightningDamage += amount;
                break;
        }
    }

    public RunDataCollection LoadRunData()
    {
        if (!File.Exists(filePath))
            return new RunDataCollection();

        string json = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new RunDataCollection();

        RunDataCollection collection = JsonUtility.FromJson<RunDataCollection>(json);
        return collection ?? new RunDataCollection();
    }

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