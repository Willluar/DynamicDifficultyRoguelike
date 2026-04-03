using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatedPlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerGridMovement movement;
    public PlayerSpellController spellController;

    [Header("Decision Settings")]
    [Tooltip("If spell resistance is above this and it only hits one target, the simulator becomes more willing to avoid that spell.")]
    public float resistanceAvoidThreshold = 0.25f;

    [Tooltip("If two or more enemies are adjacent and the current attack is weak, try to reposition.")]
    public int adjacentThreatThreshold = 2;

    [Tooltip("Only move away under pressure if the best current attack is below this score.")]
    public float weakAttackScoreThreshold = 120f;

    [Tooltip("Allow melee if it is genuinely competitive with the best spell option.")]
    public bool allowMeleeWhenAdjacent = true;

    [Tooltip("Melee is chosen if it reaches this fraction of the best spell score.")]
    [Range(0.5f, 1.5f)]
    public float meleeScorePreferenceRatio = 0.9f;

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

        Vector2Int adjacentEnemyDirection = Vector2Int.zero;
        bool hasAdjacentEnemy = false;

        if (movement != null)
            hasAdjacentEnemy = movement.HasAdjacentEnemy(out adjacentEnemyDirection);

        int adjacentEnemyCount = CountAdjacentEnemies();
        SpellDecision bestSpell = ChooseBestSpell();
        float meleeScore = hasAdjacentEnemy ? CalculateMeleeScore() : 0f;

        bool actionTaken = false;

        // 1. Use a spell if it is clearly the best available option.
        if (bestSpell.isValid && bestSpell.score > 0f)
        {
            bool spellClearlyBeatsMelee = !hasAdjacentEnemy || !allowMeleeWhenAdjacent || bestSpell.score > meleeScore * (1f / Mathf.Max(0.01f, meleeScorePreferenceRatio));
            bool bestSpellIsWorthUsing = bestSpell.score >= weakAttackScoreThreshold || !hasAdjacentEnemy;

            if (spellClearlyBeatsMelee || bestSpellIsWorthUsing)
            {
                spellController.SetCurrentSpell(bestSpell.spellType);

                if (spellController.HasValidTargetForSpell(bestSpell.spellType))
                {
                    spellController.CastCurrentSpell();
                    actionTaken = true;
                }
            }
        }

        // 2. If a good spell was not taken, use melee only when it is actually competitive.
        if (!actionTaken && hasAdjacentEnemy && allowMeleeWhenAdjacent)
        {
            bool meleeIsGoodChoice =
                meleeScore > 0f &&
                (
                    !bestSpell.isValid ||
                    meleeScore >= bestSpell.score * meleeScorePreferenceRatio ||
                    (bestSpell.resistance >= resistanceAvoidThreshold && bestSpell.hitCount <= 1)
                );

            if (meleeIsGoodChoice && movement != null)
            {
                movement.PerformSimulatedAction(adjacentEnemyDirection);
                actionTaken = true;
            }
        }

        // 3. If under real pressure and current attacks are weak, reposition to a safer tile.
        if (!actionTaken)
        {
            bool underPressure = adjacentEnemyCount >= adjacentThreatThreshold;
            bool attacksAreWeak = !bestSpell.isValid || bestSpell.score < weakAttackScoreThreshold;

            if (underPressure && attacksAreWeak)
            {
                Vector2Int retreatDirection = GetBestRetreatDirection();

                if (retreatDirection != Vector2Int.zero && movement != null)
                {
                    movement.PerformSimulatedAction(retreatDirection);
                    actionTaken = true;
                }
            }
        }

        // 4. If there is still no worthwhile action, move towards the nearest enemy.
        if (!actionTaken && movement != null)
        {
            Vector2Int moveDirection = GetMoveDirectionTowardsNearestEnemy();

            if (moveDirection != Vector2Int.zero)
            {
                movement.PerformSimulatedAction(moveDirection);
                actionTaken = true;
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
        if (spellController == null)
            return SpellDecision.Invalid(spellType);

        if (!spellController.HasValidTargetForSpell(spellType))
            return SpellDecision.Invalid(spellType);

        int hitCount = spellController.GetBestHitCountForSpell(spellType);
        if (hitCount <= 0)
            return SpellDecision.Invalid(spellType);

        float resistance = spellController.GetResistanceForSpell(spellType);
        int effectiveDamage = GetExpectedDamageForSpell(spellType);

        float score = effectiveDamage * hitCount;

        // Reward multi-hit opportunities a bit more for AoE/chain spells.
        if (spellType == SpellType.LightningBolt && hitCount > 1)
            score += 20f * (hitCount - 1);

        if (spellType == SpellType.Fireball && hitCount > 1)
            score += 18f * (hitCount - 1);

        // Penalise highly resisted single-target usage.
        if (resistance >= resistanceAvoidThreshold && hitCount <= 1)
            score *= 0.35f;

        return new SpellDecision(spellType, true, score, hitCount, resistance, effectiveDamage);
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

        // Small adjacency bonus so melee is considered when already in position,
        // but it still has to be competitive.
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

        int adjacentThreat = 0;
        int nearThreat = 0;
        int nearestDistance = int.MaxValue;

        foreach (EnemyGridMovement enemy in enemies)
        {
            if (enemy == null)
                continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - tile.x) + Mathf.Abs(enemyGrid.y - tile.y);

            if (distance == 1)
                adjacentThreat++;

            if (distance <= 2)
                nearThreat++;

            if (distance < nearestDistance)
                nearestDistance = distance;
        }

        // Prefer fewer adjacent enemies, then fewer nearby enemies,
        // and only slightly prefer being not too far from combat.
        return
            (-adjacentThreat * 100f) +
            (-nearThreat * 20f) +
            (-nearestDistance * 2f);
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

        Vector2Int nextStep = bestPath[0];
        return nextStep - playerGrid;
    }

    private struct SpellDecision
    {
        public SpellType spellType;
        public bool isValid;
        public float score;
        public int hitCount;
        public float resistance;
        public int expectedDamage;

        public SpellDecision(SpellType spellType, bool isValid, float score, int hitCount, float resistance, int expectedDamage)
        {
            this.spellType = spellType;
            this.isValid = isValid;
            this.score = score;
            this.hitCount = hitCount;
            this.resistance = resistance;
            this.expectedDamage = expectedDamage;
        }

        public static SpellDecision Invalid(SpellType spellType)
        {
            return new SpellDecision(spellType, false, 0f, 0, 0f, 0);
        }
    }
}