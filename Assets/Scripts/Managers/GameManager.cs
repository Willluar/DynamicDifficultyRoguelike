using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Mode")]
    public bool useDDA = false;

    [Header("Run State")]
    public int currentRun = 0;
    public bool runActive = false;
    private bool gameOver = false;
    private bool runStartedFromMenu = false;

    [Header("References")]
    public GameObject player;
    public Vector2Int playerSpawnGridPos = new Vector2Int(1, 1);

    public GameObject enemyPrefab;
    public List<Vector2Int> enemySpawnGridPositions = new List<Vector2Int>();

    [Header("Stage Progression")]
    public int currentStage = 1;

    [Header("Base Enemy Scaling")]
    public float baseEnemyHealthMultiplier = 1f;
    public float baseEnemyDamageMultiplier = 1f;
    public int baseEnemyCount = 3;

    [Header("Per-Stage Scaling")]
    public float healthIncreasePerStage = 0.1f;
    public float damageIncreasePerStage = 0.05f;
    public int extraEnemyEveryXStages = 2;

    [HideInInspector] public float currentEnemyHealthMultiplier = 1f;
    [HideInInspector] public float currentEnemyDamageMultiplier = 1f;
    [HideInInspector] public int currentEnemyCount = 3;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    private bool skipPlayerEndTurnOnce = false;
    private bool pendingStageClear = false;

    public bool IsGameOver => gameOver;
    public bool HasRunStartedFromMenu => runStartedFromMenu;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartRun()
    {
        runStartedFromMenu = true;

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.ApplySimulationSettings();

        currentRun = GetNextRunID();
        currentStage = 1;
        runActive = true;
        gameOver = false;
        skipPlayerEndTurnOnce = false;
        pendingStageClear = false;

        if (useDDA && DynamicDifficultyManager.Instance != null)
            DynamicDifficultyManager.Instance.LoadAndProcessRunData();

        UpdateStageScaling();

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.ClearEnemies();
            TurnManager.Instance.ResetTurns();
        }

        if (GridManager.Instance != null)
            GridManager.Instance.ClearOccupancy();

        CleanupEnemies();
        PreparePlayerForNewRun();

        if (RunDataLogger.Instance != null)
        {
            RunDataLogger.Instance.StartRun(currentRun, "DefaultBuild");
            RunDataLogger.Instance.SetDDAEnabled(useDDA);
            RunDataLogger.Instance.StartStage(currentStage);
            RunDataLogger.Instance.SetCurrentStageDifficulty(
                currentEnemyHealthMultiplier,
                currentEnemyDamageMultiplier,
                currentEnemyCount
            );
        }

        SpawnEnemies();

        PlayerSpellController spellController = FindFirstObjectByType<PlayerSpellController>();
        if (spellController != null)
            spellController.RefreshValidTargets();

        if (MultiRunBatchController.Instance != null)
            MultiRunBatchController.Instance.NotifyRunStarted(currentRun);

        Debug.Log("Run Started: " + currentRun);
    }

    private int GetNextRunID()
    {
        if (RunDataLogger.Instance == null)
            return currentRun + 1;

        RunDataCollection data = RunDataLogger.Instance.LoadRunData();

        if (data == null || data.runs == null || data.runs.Count == 0)
            return 1;

        int highestRunID = 0;

        foreach (RunData run in data.runs)
        {
            if (run != null && run.runID > highestRunID)
                highestRunID = run.runID;
        }

        return highestRunID + 1;
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (enemy != null)
            activeEnemies.Remove(enemy);

        if (activeEnemies.Count == 0 && runActive && !gameOver)
            pendingStageClear = true;
    }

    public void ResolvePendingStageClear()
    {
        if (!pendingStageClear || !runActive || gameOver)
            return;

        pendingStageClear = false;
        HandleStageClear();
    }

    private void HandleStageClear()
    {
        skipPlayerEndTurnOnce = true;

        if (RunDataLogger.Instance != null)
            RunDataLogger.Instance.CompleteStage();

        currentStage++;
        UpdateStageScaling();

        if (TurnManager.Instance != null)
            TurnManager.Instance.ResetTurns();

        MovePlayerToStageStart();

        if (RunDataLogger.Instance != null)
        {
            RunDataLogger.Instance.StartStage(currentStage);
            RunDataLogger.Instance.SetCurrentStageDifficulty(
                currentEnemyHealthMultiplier,
                currentEnemyDamageMultiplier,
                currentEnemyCount
            );
        }

        SpawnEnemies();

        PlayerSpellController spellController = FindFirstObjectByType<PlayerSpellController>();
        if (spellController != null)
            spellController.RefreshValidTargets();
    }

    private void CleanupEnemies()
    {
        foreach (GameObject e in activeEnemies)
        {
            if (e != null) Destroy(e);
        }

        activeEnemies.Clear();

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(enemy);
        }
    }

    private void SpawnEnemies()
    {
        if (GridManager.Instance == null || enemyPrefab == null)
            return;

        int enemiesToSpawn = Mathf.Min(currentEnemyCount, enemySpawnGridPositions.Count);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector2Int gridPos = enemySpawnGridPositions[i];

            if (!GridManager.Instance.IsInsideGrid(gridPos))
                continue;

            GameObject enemy = Instantiate(
                enemyPrefab,
                GridManager.Instance.GridToWorld(gridPos),
                Quaternion.identity
            );

            enemy.tag = "Enemy";

            Health health = enemy.GetComponent<Health>();
            if (health != null)
                health.InitialiseEnemyHealth();

            EnemyGridMovement enemyMove = enemy.GetComponent<EnemyGridMovement>();
            if (enemyMove != null)
                enemyMove.InitialiseEnemyDamage();

            GridManager.Instance.Register(enemy);
            RegisterEnemy(enemy);
        }
    }

    private void PreparePlayerForNewRun()
    {
        if (player == null || GridManager.Instance == null)
            return;

        GridManager.Instance.Move(player, playerSpawnGridPos);
        GridManager.Instance.Register(player);

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
            playerHealth.ResetForNewRun();

        PlayerGridMovement move = player.GetComponent<PlayerGridMovement>();
        if (move != null) move.enabled = true;

        PlayerSpellController spell = player.GetComponent<PlayerSpellController>();
        if (spell != null) spell.enabled = true;

        SimulatedPlayerController simulator = player.GetComponent<SimulatedPlayerController>();
        if (simulator != null) simulator.enabled = true;

        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        ApplyPlayerControlMode();
    }

    private void ApplyPlayerControlMode()
    {
        if (player == null)
            return;

        bool useSim = SimulationManager.Instance != null && SimulationManager.Instance.IsUsingSimulatedPlayer();

        PlayerGridMovement move = player.GetComponent<PlayerGridMovement>();
        if (move != null)
            move.enabled = !useSim;

        SimulatedPlayerController simulator = player.GetComponent<SimulatedPlayerController>();
        if (simulator != null)
            simulator.enabled = useSim;

        PlayerSpellController spell = player.GetComponent<PlayerSpellController>();
        if (spell != null)
            spell.enabled = true;
    }

    private void MovePlayerToStageStart()
    {
        if (player == null || GridManager.Instance == null)
            return;

        GridManager.Instance.Move(player, playerSpawnGridPos);
        GridManager.Instance.Register(player);

        ApplyPlayerControlMode();
    }

    public void UpdateStageScaling()
    {
        currentEnemyHealthMultiplier = baseEnemyHealthMultiplier + ((currentStage - 1) * healthIncreasePerStage);
        currentEnemyDamageMultiplier = baseEnemyDamageMultiplier + ((currentStage - 1) * damageIncreasePerStage);
        currentEnemyCount = baseEnemyCount + ((currentStage - 1) / extraEnemyEveryXStages);

        if (useDDA && DynamicDifficultyManager.Instance != null)
        {
            currentEnemyHealthMultiplier += DynamicDifficultyManager.Instance.ddaHealthAdjustment;
            currentEnemyDamageMultiplier += DynamicDifficultyManager.Instance.ddaDamageAdjustment;
        }
    }

    public bool ConsumeSkipPlayerEndTurnFlag()
    {
        bool value = skipPlayerEndTurnOnce;
        skipPlayerEndTurnOnce = false;
        return value;
    }

    public void PlayerDied()
    {
        if (gameOver)
            return;

        gameOver = true;
        runActive = false;

        Debug.Log("GAME OVER - player died.");

        if (TurnManager.Instance != null)
            TurnManager.Instance.ResetTurns();

        bool batchHandled = false;

        if (MultiRunBatchController.Instance != null)
            batchHandled = MultiRunBatchController.Instance.HandleRunEnded();

        if (batchHandled)
        {
            Debug.Log("Batch controller will start the next run.");
            return;
        }

#if UNITY_EDITOR
        Debug.Log("Run ended. No batch continuation active.");
#else
        Application.Quit();
#endif
    }
}