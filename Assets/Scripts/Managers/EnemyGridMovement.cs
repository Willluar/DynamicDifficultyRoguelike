using System.Collections.Generic;
using UnityEngine;

public class EnemyGridMovement : MonoBehaviour
{
    public int attackDamage = 1;

    private Transform player;

    private void Start()
    {
        GridManager.Instance.Register(gameObject);
        TurnManager.Instance.RegisterEnemy(this);

        player = GameObject.FindGameObjectWithTag("Player").transform;
        GameManager.Instance.RegisterEnemy(gameObject);

        PlayerSpellController spellController = FindFirstObjectByType<PlayerSpellController>();
        if (spellController != null)
            spellController.RefreshValidTargets();
    }

    public void TakeTurn()
    {
        Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int playerGrid = GridManager.Instance.WorldToGrid(player.position);

        List<Vector2Int> path = AStarPathfinder.FindPath(enemyGrid, playerGrid);

        if (path == null || path.Count == 0)
            return;

        Vector2Int nextStep = path[0];

        if (GridManager.Instance.IsTileOccupied(nextStep))
        {
            GameObject occupant = GridManager.Instance.GetOccupant(nextStep);
            Health health = occupant.GetComponent<Health>();

            if (health != null)
                health.TakeDamage(attackDamage);

            return;
        }

        GridManager.Instance.Move(gameObject, nextStep);
    }
}