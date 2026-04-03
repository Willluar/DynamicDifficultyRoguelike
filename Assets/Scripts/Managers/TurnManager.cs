using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public enum TurnState { PlayerTurn, EnemyTurn }
    public TurnState currentTurn = TurnState.PlayerTurn;

    private readonly List<EnemyGridMovement> enemies = new List<EnemyGridMovement>();
    private Coroutine enemyTurnRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterEnemy(EnemyGridMovement enemy)
    {
        if (enemy != null && !enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyGridMovement enemy)
    {
        if (enemy != null && enemies.Contains(enemy))
            enemies.Remove(enemy);
    }

    public void ClearEnemies()
    {
        enemies.Clear();
    }

    public bool IsPlayerTurn()
    {
        return currentTurn == TurnState.PlayerTurn;
    }

    public void EndPlayerTurn()
    {
        if (enemyTurnRoutine != null)
            StopCoroutine(enemyTurnRoutine);

        currentTurn = TurnState.EnemyTurn;
        enemyTurnRoutine = StartCoroutine(EnemyTurnRoutine());
    }

    public void ResetTurns()
    {
        if (enemyTurnRoutine != null)
            StopCoroutine(enemyTurnRoutine);

        enemyTurnRoutine = null;
        currentTurn = TurnState.PlayerTurn;
    }

    private IEnumerator EnemyTurnRoutine()
    {
        float delay = 0.2f;

        if (SimulationManager.Instance != null)
            delay = SimulationManager.Instance.GetEnemyTurnDelay();

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            enemies[i].TakeTurn();

            if (delay > 0f)
                yield return new WaitForSeconds(delay);
        }

        if (RunDataLogger.Instance != null)
            RunDataLogger.Instance.RecordTurn();

        enemyTurnRoutine = null;
        currentTurn = TurnState.PlayerTurn;
    }
}