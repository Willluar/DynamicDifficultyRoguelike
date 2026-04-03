using System.Collections.Generic;
using UnityEngine;

public class EnemyGridMovement : MonoBehaviour
{
    public int attackDamage = 25;
    public int baseAttackDamage = 25;

    private Transform player;

    private void Start()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.Register(gameObject);

        if (TurnManager.Instance != null)
            TurnManager.Instance.RegisterEnemy(this);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterEnemy(gameObject);

        PlayerSpellController spellController = FindFirstObjectByType<PlayerSpellController>();
        if (spellController != null)
            spellController.RefreshValidTargets();
    }

    public void InitialiseEnemyDamage()
    {
        if (GameManager.Instance == null)
            return;

        attackDamage = Mathf.RoundToInt(baseAttackDamage * GameManager.Instance.currentEnemyDamageMultiplier);
    }

    public void TakeTurn()
    {
        if (player == null || GridManager.Instance == null)
            return;

        Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int playerGrid = GridManager.Instance.WorldToGrid(player.position);

        List<Vector2Int> path = AStarPathfinder.FindPath(enemyGrid, playerGrid);

        if (path == null || path.Count == 0)
            return;

        Vector2Int nextStep = path[0];

        if (GridManager.Instance.IsTileOccupied(nextStep))
        {
            GameObject occupant = GridManager.Instance.GetOccupant(nextStep);
            Health health = occupant != null ? occupant.GetComponent<Health>() : null;

            if (health != null)
                health.TakeDamage(attackDamage);

            return;
        }

        GridManager.Instance.Move(gameObject, nextStep);
    }
}