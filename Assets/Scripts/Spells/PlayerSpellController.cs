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

    private List<EnemyGridMovement> validTargets = new List<EnemyGridMovement>();
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
            StartCoroutine(CastCurrentSpellRoutine());
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

        if (validTargets.Count == 0 || currentTargetIndex < 0 || currentTargetIndex >= validTargets.Count)
            yield break;

        EnemyGridMovement target = validTargets[currentTargetIndex];

        if (target == null)
        {
            RefreshValidTargets();
            yield break;
        }

        DamageType spellDamageType = GetDamageTypeForSpell(currentSpell);

        if (RunDataLogger.Instance != null)
            RunDataLogger.Instance.RecordSpellCast(spellDamageType);

        isCasting = true;
        ClearAllHighlights();

        bool hitResolved = false;

        if (currentSpell.projectilePrefab != null)
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

        Vector3 impactPos = target.transform.position;

        if (currentSpell.impactEffectPrefab != null)
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
                CastLightningBolt(target);
                break;

            case SpellType.Fireball:
                CastFireball(target);
                break;
        }
    }

    private DamageType GetDamageTypeForSpell(SpellData spell)
    {
        if (spell == null)
            return DamageType.Melee;

        switch (spell.spellType)
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

    private void ApplySpellDamage(Health health, int baseAmount)
    {
        if (health == null || currentSpell == null)
            return;

        DamageType spellDamageType = GetDamageTypeForSpell(currentSpell);

        int finalDamage = baseAmount;

        Debug.Log("----- SPELL DAMAGE DEBUG -----");
        Debug.Log("DDA Enabled: " + GameManager.Instance.useDDA);
        Debug.Log("DDA Instance: " + (DynamicDifficultyManager.Instance == null ? "NULL" : "PRESENT"));
        Debug.Log("Damage Type: " + spellDamageType);
        Debug.Log("Base Damage: " + baseAmount);

        if (health.isEnemy && DynamicDifficultyManager.Instance != null && GameManager.Instance != null && GameManager.Instance.useDDA)
        {
            float resistance = DynamicDifficultyManager.Instance.GetResistanceForDamageType(spellDamageType);
            Debug.Log("Resistance Applied: " + resistance);

            finalDamage = DynamicDifficultyManager.Instance.ApplyResistanceToDamage(spellDamageType, baseAmount);
        }
        else
        {
            Debug.Log("Resistance NOT applied (failed condition)");
        }

        Debug.Log("Final Damage: " + finalDamage);

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

    private void CastLightningBolt(EnemyGridMovement primaryTarget)
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

                    if (currentSpell.secondaryEffectPrefab != null)
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

    private void CastFireball(EnemyGridMovement target)
    {
        if (target == null || currentSpell == null || GridManager.Instance == null)
            return;

        Vector2Int targetGrid = GridManager.Instance.WorldToGrid(target.transform.position);
        EnemyGridMovement[] snapshot = FindObjectsByType<EnemyGridMovement>(FindObjectsSortMode.None);

        if (currentSpell.secondaryEffectPrefab != null)
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

    public string GetCurrentSpellName()
    {
        return currentSpell != null ? currentSpell.spellName : "None";
    }
}