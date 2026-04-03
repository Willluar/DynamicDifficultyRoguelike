using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpellController : MonoBehaviour
{
    [Header("Spell Loadout")]
    public SpellData iceBolt;
    public SpellData lightningBolt;
    public SpellData fireball;

    [Header("UI")]
    public SpellUIController spellUI;

    private SpellData currentSpell;

    private readonly List<EnemyGridMovement> validTargets = new List<EnemyGridMovement>();
    private int currentTargetIndex = -1;

    private bool isCasting = false;

    private void Start()
    {
        currentSpell = iceBolt;

        if (spellUI != null && currentSpell != null)
            spellUI.SetSelectedSpell(currentSpell.spellType);

        RefreshValidTargets();
    }

    private void Update()
    {
        if (SimulationManager.Instance != null && SimulationManager.Instance.IsUsingSimulatedPlayer())
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn())
            return;

        if (isCasting)
            return;

        HandleSpellSwitchInput();
        HandleTargetCycleInput();
        HandleCastInput();
    }

    private void HandleSpellSwitchInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool spellChanged = false;

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            currentSpell = iceBolt;
            spellChanged = true;
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            currentSpell = lightningBolt;
            spellChanged = true;
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            currentSpell = fireball;
            spellChanged = true;
        }

        if (spellChanged && currentSpell != null)
        {
            if (spellUI != null)
                spellUI.SetSelectedSpell(currentSpell.spellType);

            RefreshValidTargets();
        }
    }

    private void HandleTargetCycleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.tabKey.wasPressedThisFrame)
            CycleTarget();
    }

    private void HandleCastInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame)
            CastCurrentSpell();
    }

    public void CastCurrentSpell()
    {
        if (isCasting)
            return;

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn())
            return;

        StartCoroutine(CastCurrentSpellRoutine());
    }

    public bool IsCasting()
    {
        return isCasting;
    }

    public void SetCurrentSpell(SpellType spellType)
    {
        SpellData spell = GetSpellData(spellType);
        if (spell == null)
            return;

        currentSpell = spell;

        if (spellUI != null)
            spellUI.SetSelectedSpell(currentSpell.spellType);

        RefreshValidTargets();
    }

    public string GetCurrentSpellName()
    {
        return currentSpell != null ? currentSpell.spellName : "None";
    }

    public SpellType GetCurrentSpellType()
    {
        return currentSpell != null ? currentSpell.spellType : SpellType.IceBolt;
    }

    public DamageType GetDamageTypeForSpell(SpellType spellType)
    {
        switch (spellType)
        {
            case SpellType.IceBolt:
                return DamageType.Ice;
            case SpellType.LightningBolt:
                return DamageType.Lightning;
            case SpellType.Fireball:
                return DamageType.Fire;
            default:
                return DamageType.Melee;
        }
    }

    public float GetResistanceForSpell(SpellType spellType)
    {
        if (DynamicDifficultyManager.Instance == null || GameManager.Instance == null || !GameManager.Instance.useDDA)
            return 0f;

        return DynamicDifficultyManager.Instance.GetResistanceForDamageType(GetDamageTypeForSpell(spellType));
    }

    public bool HasValidTargetForSpell(SpellType spellType)
    {
        return GetBestHitCountForSpell(spellType) > 0;
    }

    public int GetBestHitCountForSpell(SpellType spellType)
    {
        SpellData spell = GetSpellData(spellType);

        if (spell == null || GridManager.Instance == null)
            return 0;

        EnemyGridMovement[] allEnemies = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);
        Vector2Int playerGrid = GridManager.Instance.WorldToGrid(transform.position);

        int bestHitCount = 0;

        foreach (EnemyGridMovement primaryTarget in allEnemies)
        {
            if (primaryTarget == null)
                continue;

            Vector2Int targetGrid = GridManager.Instance.WorldToGrid(primaryTarget.transform.position);
            int distance = Mathf.Abs(targetGrid.x - playerGrid.x) + Mathf.Abs(targetGrid.y - playerGrid.y);

            if (distance > spell.range)
                continue;

            if (!LineOfSightUtility.HasLineOfSight(playerGrid, targetGrid))
                continue;

            int hitCount = 1;

            if (spell.spellType == SpellType.Fireball)
            {
                hitCount = CountFireballHits(targetGrid, spell.splashRadius, allEnemies);
            }
            else if (spell.spellType == SpellType.LightningBolt)
            {
                hitCount = CountLightningHits(primaryTarget, spell.chainRange, spell.chainCount, allEnemies);
            }

            if (hitCount > bestHitCount)
                bestHitCount = hitCount;
        }

        return bestHitCount;
    }

    public void RefreshValidTargets()
    {
        ClearAllHighlights();
        validTargets.Clear();
        currentTargetIndex = -1;

        if (currentSpell == null || GridManager.Instance == null)
            return;

        EnemyGridMovement[] allEnemies = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);
        Vector2Int playerGrid = GridManager.Instance.WorldToGrid(transform.position);

        foreach (EnemyGridMovement enemy in allEnemies)
        {
            if (enemy == null) continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - playerGrid.x) + Mathf.Abs(enemyGrid.y - playerGrid.y);

            if (distance > currentSpell.range)
                continue;

            if (!LineOfSightUtility.HasLineOfSight(playerGrid, enemyGrid))
                continue;

            validTargets.Add(enemy);
        }

        if (validTargets.Count > 0)
        {
            currentTargetIndex = 0;
            HighlightCurrentTarget();
        }
    }

    private SpellData GetSpellData(SpellType spellType)
    {
        switch (spellType)
        {
            case SpellType.IceBolt:
                return iceBolt;
            case SpellType.LightningBolt:
                return lightningBolt;
            case SpellType.Fireball:
                return fireball;
            default:
                return null;
        }
    }

    private int CountFireballHits(Vector2Int targetGrid, int splashRadius, EnemyGridMovement[] allEnemies)
    {
        int hitCount = 0;

        foreach (EnemyGridMovement enemy in allEnemies)
        {
            if (enemy == null)
                continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - targetGrid.x) + Mathf.Abs(enemyGrid.y - targetGrid.y);

            if (distance <= splashRadius)
                hitCount++;
        }

        return hitCount;
    }

    private int CountLightningHits(EnemyGridMovement primaryTarget, int chainRange, int chainCount, EnemyGridMovement[] allEnemies)
    {
        if (primaryTarget == null)
            return 0;

        int hitCount = 1;
        int chainsDone = 0;

        Vector2Int primaryGrid = GridManager.Instance.WorldToGrid(primaryTarget.transform.position);

        foreach (EnemyGridMovement enemy in allEnemies)
        {
            if (enemy == null || enemy == primaryTarget)
                continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - primaryGrid.x) + Mathf.Abs(enemyGrid.y - primaryGrid.y);

            if (distance <= chainRange)
            {
                hitCount++;
                chainsDone++;
            }

            if (chainsDone >= chainCount)
                break;
        }

        return hitCount;
    }

    private void CycleTarget()
    {
        if (validTargets.Count == 0)
        {
            RefreshValidTargets();
            return;
        }

        ClearAllHighlights();

        currentTargetIndex++;
        if (currentTargetIndex >= validTargets.Count)
            currentTargetIndex = 0;

        HighlightCurrentTarget();
    }

    private void HighlightCurrentTarget()
    {
        if (currentTargetIndex < 0 || currentTargetIndex >= validTargets.Count)
            return;

        EnemyGridMovement enemy = validTargets[currentTargetIndex];
        if (enemy == null) return;

        TargetHighlighter highlighter = enemy.GetComponent<TargetHighlighter>();
        if (highlighter != null)
            highlighter.SetHighlighted(true);
    }

    private void ClearAllHighlights()
    {
        EnemyGridMovement[] allEnemies = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);

        foreach (EnemyGridMovement enemy in allEnemies)
        {
            if (enemy == null) continue;

            TargetHighlighter highlighter = enemy.GetComponent<TargetHighlighter>();
            if (highlighter != null)
                highlighter.SetHighlighted(false);
        }
    }

    private IEnumerator CastCurrentSpellRoutine()
    {
        if (currentSpell == null)
            yield break;

        RefreshValidTargets();

        if (validTargets.Count == 0 || currentTargetIndex < 0 || currentTargetIndex >= validTargets.Count)
            yield break;

        EnemyGridMovement target = validTargets[currentTargetIndex];

        if (target == null)
        {
            RefreshValidTargets();
            yield break;
        }

        DamageType spellDamageType = GetDamageTypeForSpell(currentSpell.spellType);

        if (RunDataLogger.Instance != null)
            RunDataLogger.Instance.RecordSpellCast(spellDamageType);

        isCasting = true;
        ClearAllHighlights();

        bool hitResolved = false;
        bool skipVisuals = SimulationManager.Instance != null && SimulationManager.Instance.ShouldSkipSpellVisuals();

        if (skipVisuals)
        {
            ResolveSpellEffect(target);
            hitResolved = true;
        }
        else if (currentSpell.projectilePrefab != null)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = target.transform.position;

            GameObject projectileObj = Instantiate(currentSpell.projectilePrefab, startPos, Quaternion.identity);
            SpellProjectileVisual projectile = projectileObj.GetComponent<SpellProjectileVisual>();

            if (projectile != null)
            {
                projectile.Launch(startPos, endPos, currentSpell.projectileTravelTime, () =>
                {
                    ResolveSpellEffect(target);
                    hitResolved = true;
                });
            }
            else
            {
                ResolveSpellEffect(target);
                hitResolved = true;
            }
        }
        else
        {
            ResolveSpellEffect(target);
            hitResolved = true;
        }

        while (!hitResolved)
            yield return null;

        if (GameManager.Instance != null)
            GameManager.Instance.ResolvePendingStageClear();

        bool skipTurn = GameManager.Instance != null && GameManager.Instance.ConsumeSkipPlayerEndTurnFlag();

        if (!skipTurn && TurnManager.Instance != null)
            TurnManager.Instance.EndPlayerTurn();

        RefreshValidTargets();
        isCasting = false;
    }

    private void ResolveSpellEffect(EnemyGridMovement target)
    {
        if (target == null || currentSpell == null)
            return;

        bool skipVisuals = SimulationManager.Instance != null && SimulationManager.Instance.ShouldSkipSpellVisuals();
        Vector3 impactPos = target.transform.position;

        if (!skipVisuals && currentSpell.impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(currentSpell.impactEffectPrefab, impactPos, Quaternion.identity);
            SpellEffectVisual impactVisual = impact.GetComponent<SpellEffectVisual>();

            if (impactVisual != null)
                impactVisual.PlayAtPoint(impactPos, 1f);
        }

        switch (currentSpell.spellType)
        {
            case SpellType.IceBolt:
                CastIceBolt(target);
                break;

            case SpellType.LightningBolt:
                CastLightningBolt(target, skipVisuals);
                break;

            case SpellType.Fireball:
                CastFireball(target, skipVisuals);
                break;
        }
    }

    private void ApplySpellDamage(Health health, int baseAmount)
    {
        if (health == null || currentSpell == null)
            return;

        DamageType spellDamageType = GetDamageTypeForSpell(currentSpell.spellType);
        int finalDamage = baseAmount;

        if (health.isEnemy &&
            DynamicDifficultyManager.Instance != null &&
            GameManager.Instance != null &&
            GameManager.Instance.useDDA)
        {
            finalDamage = DynamicDifficultyManager.Instance.ApplyResistanceToDamage(spellDamageType, baseAmount);
        }

        if (RunDataLogger.Instance != null)
        {
            RunDataLogger.Instance.AddDamageDealt(finalDamage);
            RunDataLogger.Instance.RecordDamageByType(spellDamageType, finalDamage);
        }

        health.TakeDamage(finalDamage);
    }

    private void CastIceBolt(EnemyGridMovement target)
    {
        if (target == null || currentSpell == null)
            return;

        Health health = target.GetComponent<Health>();
        if (health != null)
            ApplySpellDamage(health, currentSpell.damage);
    }

    private void CastLightningBolt(EnemyGridMovement primaryTarget, bool skipVisuals)
    {
        if (primaryTarget == null || currentSpell == null || GridManager.Instance == null)
            return;

        EnemyGridMovement[] snapshot = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);
        List<EnemyGridMovement> hitTargets = new List<EnemyGridMovement>();

        Health primaryHealth = primaryTarget.GetComponent<Health>();
        if (primaryHealth != null)
        {
            ApplySpellDamage(primaryHealth, currentSpell.damage);
            hitTargets.Add(primaryTarget);
        }

        int chainsDone = 0;
        Vector2Int primaryGrid = GridManager.Instance.WorldToGrid(primaryTarget.transform.position);

        foreach (EnemyGridMovement enemy in snapshot)
        {
            if (enemy == null || hitTargets.Contains(enemy))
                continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - primaryGrid.x) + Mathf.Abs(enemyGrid.y - primaryGrid.y);

            if (distance <= currentSpell.chainRange)
            {
                Health health = enemy.GetComponent<Health>();
                if (health != null)
                {
                    ApplySpellDamage(health, currentSpell.damage);

                    if (!skipVisuals && currentSpell.secondaryEffectPrefab != null)
                    {
                        GameObject chainEffect = Instantiate(currentSpell.secondaryEffectPrefab);
                        SpellEffectVisual visual = chainEffect.GetComponent<SpellEffectVisual>();

                        if (visual != null)
                            visual.PlayBetweenPoints(primaryTarget.transform.position, enemy.transform.position);
                    }

                    hitTargets.Add(enemy);
                    chainsDone++;
                }
            }

            if (chainsDone >= currentSpell.chainCount)
                break;
        }
    }

    private void CastFireball(EnemyGridMovement target, bool skipVisuals)
    {
        if (target == null || currentSpell == null || GridManager.Instance == null)
            return;

        Vector2Int targetGrid = GridManager.Instance.WorldToGrid(target.transform.position);
        EnemyGridMovement[] snapshot = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);

        if (!skipVisuals && currentSpell.secondaryEffectPrefab != null)
        {
            GameObject explosion = Instantiate(currentSpell.secondaryEffectPrefab);
            SpellEffectVisual visual = explosion.GetComponent<SpellEffectVisual>();

            if (visual != null)
            {
                float scaleMultiplier = 1f + (currentSpell.splashRadius * 0.5f);
                visual.PlayAtPoint(target.transform.position, scaleMultiplier);
            }
        }

        foreach (EnemyGridMovement enemy in snapshot)
        {
            if (enemy == null) continue;

            Vector2Int enemyGrid = GridManager.Instance.WorldToGrid(enemy.transform.position);
            int distance = Mathf.Abs(enemyGrid.x - targetGrid.x) + Mathf.Abs(enemyGrid.y - targetGrid.y);

            if (distance <= currentSpell.splashRadius)
            {
                Health health = enemy.GetComponent<Health>();
                if (health != null)
                    ApplySpellDamage(health, currentSpell.damage);
            }
        }
    }
}