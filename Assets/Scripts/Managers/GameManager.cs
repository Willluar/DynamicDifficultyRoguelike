using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Run State")]
    public int currentRun = 0;
    public bool runActive = false;
    private bool gameOver = false;

    [Header("References")]
    public GameObject player;
    public Vector2Int playerSpawnGridPos = new Vector2Int(1, 1);

    public GameObject enemyPrefab;
    public List<Vector2Int> enemySpawnGridPositions = new List<Vector2Int>();

    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartRun();
    }

    public void StartRun()
    {
        RunDataLogger.Instance.StartRun(currentRun, "DefaultBuild");
        currentRun++;
        runActive = true;
        gameOver = false;

        Debug.Log("Run Started: " + currentRun);

        // Reset turn system and grid occupancy
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.ClearEnemies();
            TurnManager.Instance.ResetTurns();
        }

        if (GridManager.Instance != null)
            GridManager.Instance.ClearOccupancy();

        // Destroy any leftover enemies from previous run
        CleanupEnemies();

        // Reset player properly via grid
        ResetPlayer();

        // Spawn new enemies
        SpawnEnemies();
    }

    public void EndRun(bool win)
    {
        runActive = false;

        

        Debug.Log("Run Ended. Win: " + win);

        if (win)

            StartCoroutine(StartNextRunDelay());
        else
            Debug.Log("Game Over");
    }

    private IEnumerator StartNextRunDelay()
    {
        yield return new WaitForSeconds(1f);
        StartRun();
    }

    /* ---------------- Enemy Management ---------------- */

    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        activeEnemies.Remove(enemy);

        if (activeEnemies.Count == 0 && runActive)
            EndRun(true);
    }

    private void CleanupEnemies()
    {
        // Destroy tracked enemies
        foreach (var e in activeEnemies)
        {
            if (e != null) Destroy(e);
        }
        activeEnemies.Clear();

        // Extra safety: destroy any untracked enemies in scene
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(enemy);
        }
    }

    private void SpawnEnemies()
    {
        foreach (var gridPos in enemySpawnGridPositions)
        {
            if (!GridManager.Instance.IsInsideGrid(gridPos))
                continue;

            GameObject enemy = Instantiate(enemyPrefab, GridManager.Instance.GridToWorld(gridPos), Quaternion.identity);

            // Ensure tag is set for cleanup
            enemy.tag = "Enemy";

            // Register into systems immediately (don’t wait for Start timing)
            GridManager.Instance.Register(enemy);
            RegisterEnemy(enemy);
        }
    }

    /* ---------------- Player Management ---------------- */

    private void ResetPlayer()
    {
        // Move player using the grid system
        GridManager.Instance.Move(player, playerSpawnGridPos);

        // Reset player health
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.currentHealth = playerHealth.maxHealth;
            // If you have UI updating in Health, call it here if needed
            // playerHealth.ForceRefreshUI(); (only if you add such a method)
        }

        GridManager.Instance.Register(player);
    }



    

    public void PlayerDied()
    {
        if (gameOver) return;

        gameOver = true;
        runActive = false;

        Debug.Log("GAME OVER - player died.");

        // Stop enemy turns
        if (TurnManager.Instance != null)
            TurnManager.Instance.ResetTurns();

#if UNITY_EDITOR
        Debug.Log("Game would quit here (Editor mode).");
#else
    Application.Quit();
#endif
    }

}