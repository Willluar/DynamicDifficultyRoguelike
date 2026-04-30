using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatedPlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerGridMovement movement;
    public PlayerSpellController spellController;

    [Header("Decision Settings")]
    public float resistanceAvoidThreshold = 0.25f;
    public int adjacentThreatThreshold = 2;
    public float weakAttackScoreThreshold = 120f;
    public bool allowMeleeWhenAdjacent = true;
    [Range(0.5f, 1.5f)] public float meleeScorePreferenceRatio = 0.9f;

    private bool acting = false;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerGridMovement>();

        if (spellController == null)
            spellController = GetComponent<PlayerSpellController>();
    }

    private void Update()
    {
        if (SimulationManager.Instance == null || !SimulationManager.Instance.IsUsingSimulatedPlayer())
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn())
            return;

        if (acting)
            return;

        StartCoroutine(TakeSimulatedTurnRoutine());
    }

    private IEnumerator TakeSimulatedTurnRoutine()
    {
        acting = true;

        if (spellController != null)
            spellController.RefreshValidTargets();

        Vector2Int adjacentDir = Vector2Int.zero;
        bool adjacentEnemy = false;

        if (movement != null)
            adjacentEnemy = movement.HasAdjacentEnemy(out adjacentDir);

        int adjacentCount = CountAdjacentEnemies();
        SpellDecision spell = ChooseBestSpell();
        float meleeScore = adjacentEnemy ? CalculateMeleeScore() : 0f;

        bool done = false;

        if (spell.isValid && spell.score > 0f)
        {
            bool spellBeatsMelee = !adjacentEnemy || !allowMeleeWhenAdjacent || spell.score > meleeScore * (1f / Mathf.Max(0.01f, meleeScorePreferenceRatio));
            bool spellWorth = spell.score >= weakAttackScoreThreshold || !adjacentEnemy;

            if (spellBeatsMelee || spellWorth)
            {
                spellController.SetCurrentSpell(spell.spellType);

                if (spellController.HasValidTargetForSpell(spell.spellType))
                {
                    spellController.CastCurrentSpell();
                    done = true;
                }
            }
        }

        if (!done && adjacentEnemy && allowMeleeWhenAdjacent)
        {
            bool meleeOk =
                meleeScore > 0f &&
                (
                    !spell.isValid ||
                    meleeScore >= spell.score * meleeScorePreferenceRatio ||
                    (spell.resistance >= resistanceAvoidThreshold && spell.hitCount <= 1)
                );

            if (meleeOk && movement != null)
            {
                movement.PerformSimulatedAction(adjacentDir);
                done = true;
            }
        }

        if (!done)
        {
            bool pressured = adjacentCount >= adjacentThreatThreshold;
            bool weak = !spell.isValid || spell.score < weakAttackScoreThreshold;

            if (pressured && weak)
            {
                Vector2Int retreat = GetBestRetreatDirection();

                if (retreat != Vector2Int.zero && movement != null)
                {
                    movement.PerformSimulatedAction(retreat);
                    done = true;
                }
            }
        }

        if (!done && movement != null)
        {
            Vector2Int dir = GetMoveDirectionTowardsNearestEnemy();

            if (dir != Vector2Int.zero)
            {
                movement.PerformSimulatedAction(dir);
                done = true;
            }
        }

        yield return null;
        acting = false;
    }

    private SpellDecision ChooseBestSpell()
    {
        SpellDecision ice = BuildSpellDecision(SpellType.IceBolt);
        SpellDecision lightning = BuildSpellDecision(SpellType.LightningBolt);
        SpellDecision fire = BuildSpellDecision(SpellType.Fireball);

        SpellDecision best = ice;

        if (lightning.score > best.score)
            best = lightning;

        if (fire.score > best.score)
            best = fire;

        return best;
    }

    private SpellDecision BuildSpellDecision(SpellType spellType)
    {
        SpellDecision result = new SpellDecision();
        result.spellType = spellType;

        if (spellController == null)
            return result;

        if (!spellController.HasValidTargetForSpell(spellType))
            return result;

        int hitCount = spellController.GetBestHitCountForSpell(spellType);
        if (hitCount <= 0)
            return result;

        float resistance = spellController.GetResistanceForSpell(spellType);
        int effectiveDamage = GetExpectedDamageForSpell(spellType);

        float score = effectiveDamage * hitCount;

        if (spellType == SpellType.LightningBolt && hitCount > 1)
            score += 20f * (hitCount - 1);

        if (spellType == SpellType.Fireball && hitCount > 1)
            score += 18f * (hitCount - 1);

        if (resistance >= resistanceAvoidThreshold && hitCount <= 1)
            score *= 0.35f;

        result.isValid = true;
        result.score = score;
        result.hitCount = hitCount;
        result.resistance = resistance;
        result.expectedDamage = effectiveDamage;

        return result;
    }

    private int GetExpectedDamageForSpell(SpellType spellType)
    {
        if (spellController == null)
            return 0;

        SpellData spellData = null;

        switch (spellType)
        {
            case SpellType.IceBolt:
                spellData = spellController.iceBolt;
                break;

            case SpellType.LightningBolt:
                spellData = spellController.lightningBolt;
                break;

            case SpellType.Fireball:
                spellData = spellController.fireball;
                break;
        }

        if (spellData == null)
            return 0;

        int baseDamage = spellData.damage;

        if (GameManager.Instance != null &&
            GameManager.Instance.useDDA &&
            DynamicDifficultyManager.Instance != null)
        {
            DamageType damageType = spellController.GetDamageTypeForSpell(spellType);
            return DynamicDifficultyManager.Instance.ApplyResistanceToDamage(damageType, baseDamage);
        }

        return baseDamage;
    }

    private float CalculateMeleeScore()
    {
        if (movement == null)
            return 0f;

        int damage = movement.attackDamage;

        if (GameManager.Instance != null &&
            GameManager.Instance.useDDA &&
            DynamicDifficultyManager.Instance != null)
        {
            damage = DynamicDifficultyManager.Instance.ApplyResistanceToDamage(DamageType.Melee, damage);
        }

        return damage + 10f;
    }

    private int CountAdjacentEnemies()
    {
        if (GridManager.Instance == null)
            return 0;

        EnemyGridMovement[] enemies = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);
        Vector2Int playerGrid = GridManager.Instance.WorldToGrid(transform.position);

        int count = 0;

        foreach (EnemyGridMovement enemy in enemies)
        {
            if (enemy == null)
                continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - playerGrid.x) + Mathf.Abs(enemyGrid.y - playerGrid.y);

            if (distance == 1)
                count++;
        }

        return count;
    }

    private Vector2Int GetBestRetreatDirection()
    {
        if (GridManager.Instance == null)
            return Vector2Int.zero;

        Vector2Int playerGrid = GridManager.Instance.WorldToGrid(transform.position);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        Vector2Int bestDirection = Vector2Int.zero;
        float bestScore = float.NegativeInfinity;

        foreach (Vector2Int dir in directions)
        {
            Vector2Int target = playerGrid + dir;

            if (!GridManager.Instance.IsInsideGrid(target))
                continue;

            if (GridManager.Instance.IsTileOccupied(target))
                continue;

            float score = ScoreTileSafety(target);

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        return bestDirection;
    }

    private float ScoreTileSafety(Vector2Int tile)
    {
        if (GridManager.Instance == null)
            return float.NegativeInfinity;

        EnemyGridMovement[] enemies = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);

        int adjacent = 0;
        int near = 0;
        int nearest = int.MaxValue;

        foreach (EnemyGridMovement enemy in enemies)
        {
            if (enemy == null)
                continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - tile.x) + Mathf.Abs(enemyGrid.y - tile.y);

            if (distance == 1) adjacent++;
            if (distance <= 2) near++;
            if (distance < nearest) nearest = distance;
        }

        return (-adjacent * 100f) + (-near * 20f) + (-nearest * 2f);
    }

    private Vector2Int GetMoveDirectionTowardsNearestEnemy()
    {
        if (GridManager.Instance == null)
            return Vector2Int.zero;

        EnemyGridMovement[] enemies = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);
        if (enemies == null || enemies.Length == 0)
            return Vector2Int.zero;

        Vector2Int playerGrid = GridManager.Instance.WorldToGrid(transform.position);
        List<Vector2Int> bestPath = null;

        foreach (EnemyGridMovement enemy in enemies)
        {
            if (enemy == null)
                continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            List<Vector2Int> path = AStarPathfinder.FindPath(playerGrid, enemyGrid);

            if (path == null || path.Count == 0)
                continue;

            if (bestPath == null || path.Count < bestPath.Count)
                bestPath = path;
        }

        if (bestPath == null || bestPath.Count == 0)
            return Vector2Int.zero;

        return bestPath[0] - playerGrid;
    }

    private struct SpellDecision
    {
        public SpellType spellType;
        public bool isValid;
        public float score;
        public int hitCount;
        public float resistance;
        public int expectedDamage;
    }
}
