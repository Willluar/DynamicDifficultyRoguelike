using System;
using System.Collections.Generic;

[Serializable]
public class RunData
{
    public int runID;
    public string timestampStart;
    public string timestampEnd;

    public int stagesCleared;

    public int totalTurns;
    public int totalDamageDealt;
    public int totalDamageTaken;
    public int totalEnemiesKilled;

    public string buildID;

    public List<StageData> stages = new List<StageData>();
}

[Serializable]
public class StageData
{
    public int stageIndex;

    public int stageTurns;
    public int stageDamageDealt;
    public int stageDamageTaken;
    public int stageEnemiesKilled;

    public DifficultySnapshot difficulty;
}

[Serializable]
public class DifficultySnapshot
{
    public float enemyHealthMultiplier;
    public float enemyDamageMultiplier;
    public int enemySpawnCount;
}