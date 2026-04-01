using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public enum TurnState { PlayerTurn, EnemyTurn }
    public TurnState currentTurn = TurnState.PlayerTurn;

    private List<EnemyGridMovement> enemies = new List<EnemyGridMovement>();
    private Coroutine enemyTurnRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /* ---------------- Enemy Registration ---------------- */

    public void RegisterEnemy(EnemyGridMovement enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyGridMovement enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);
    }

    public void ClearEnemies()
    {
        enemies.Clear();
    }

    /* ---------------- Turn Flow ---------------- */

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

        currentTurn = TurnState.PlayerTurn;
    }

    private IEnumerator EnemyTurnRoutine()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            enemies[i].TakeTurn();
            yield return new WaitForSeconds(0.2f);
        }
        RunDataLogger.Instance.RecordTurn();
        currentTurn = TurnState.PlayerTurn;
    }
}