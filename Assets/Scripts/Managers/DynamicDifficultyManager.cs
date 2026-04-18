using System.Collections.Generic;
using UnityEngine;

public class DynamicDifficultyManager : MonoBehaviour
{
    public static DynamicDifficultyManager Instance;

    [Header("Legacy Compatibility")]
    public float enemyHealthMultiplier = 1f;

    [Header("Base DDA Stat Adjustments")]
    public float ddaHealthAdjustment = 0f;
    public float ddaDamageAdjustment = 0f;

    [Header("Resistance Pool")]
    [Range(0f, 0.95f)] public float baseTotalResistancePool = 0.45f;
    [Range(0f, 0.95f)] public float baseSingleResistanceCap = 0.30f;

    [Header("DDA Growth Over Time")]
    [Tooltip("How much stronger DDA becomes per completed analysed run.")]
    public float strengthIncreasePerRun = 0.05f;

    [Tooltip("Maximum extra strength multiplier added over time.")]
    public float maxExtraStrength = 0.75f;

    [Tooltip("Also scale health/damage adjustments upward over time.")]
    public bool scaleStatAdjustmentsWithStrength = true;

    [Header("Run Analysis")]
    [Tooltip("How many past runs to analyse each time.")]
    public int runsToAverage = 5;

    [Tooltip("Only use DDA-enabled runs when analysing history. Keep this on for cleaner comparisons.")]
    public bool analyseOnlyDDARuns = true;

    [Header("Current Resistance Outputs")]
    public float fireResistanceAdjustment = 0f;
    public float iceResistanceAdjustment = 0f;
    public float lightningResistanceAdjustment = 0f;
    public float meleeResistanceAdjustment = 0f;

    [Header("Debug")]
    public bool verboseLogging = true;

    private float currentStrengthMultiplier = 1f;
    private float currentTotalResistancePool = 0.45f;
    private float currentSingleResistanceCap = 0.30f;

    private void Awake()
    {
        Instance = this;
    }

    public void LoadAndProcessRunData()
    {
        if (RunDataLogger.Instance == null)
        {
            ResetAdjustments();
            return;
        }

        RunDataCollection data = RunDataLogger.Instance.LoadRunData();
        ProcessRecentRunData(data);
    }

    public void ProcessRecentRunData(RunDataCollection data)
    {
        ResetAdjustments();

        if (data == null || data.runs == null || data.runs.Count == 0)
        {
            if (verboseLogging)
                Debug.Log("DDA: No historical runs found. Using default adjustments.");
            return;
        }

        List<RunData> candidateRuns = new List<RunData>();

        foreach (RunData run in data.runs)
        {
            if (run == null)
                continue;

            if (analyseOnlyDDARuns && !run.ddaEnabled)
                continue;

            candidateRuns.Add(run);
        }

        if (candidateRuns.Count == 0)
        {
            if (verboseLogging)
                Debug.Log("DDA: No valid historical runs found after filtering. Using default adjustments.");
            return;
        }

        // Take the last N valid runs.
        int startIndex = Mathf.Max(0, candidateRuns.Count - runsToAverage);
        List<RunData> relevantRuns = candidateRuns.GetRange(startIndex, candidateRuns.Count - startIndex);

        // Strength grows over time based on how many valid historical runs exist.
        int historicalRunCount = candidateRuns.Count;
        currentStrengthMultiplier = 1f + Mathf.Min(maxExtraStrength, historicalRunCount * strengthIncreasePerRun);

        currentTotalResistancePool = baseTotalResistancePool * currentStrengthMultiplier;
        currentSingleResistanceCap = baseSingleResistanceCap * currentStrengthMultiplier;

        // Never allow the pooled total or single cap to become absurd.
        currentTotalResistancePool = Mathf.Clamp(currentTotalResistancePool, 0f, 0.90f);
        currentSingleResistanceCap = Mathf.Clamp(currentSingleResistanceCap, 0f, 0.75f);

        float averageStages = 0f;
        int fireDamage = 0;
        int iceDamage = 0;
        int lightningDamage = 0;
        int meleeDamage = 0;

        foreach (RunData run in relevantRuns)
        {
            averageStages += run.stagesCleared;
            fireDamage += run.fireDamage;
            iceDamage += run.iceDamage;
            lightningDamage += run.lightningDamage;
            meleeDamage += run.meleeDamage;
        }

        averageStages /= relevantRuns.Count;

        ApplyPerformanceScaling(averageStages);
        ApplySharedResistancePool(fireDamage, iceDamage, lightningDamage, meleeDamage);

        enemyHealthMultiplier = 1f + ddaHealthAdjustment;

        if (verboseLogging)
        {
            Debug.Log(
                $"DDA Processed | Runs Analysed={relevantRuns.Count} | Historical Valid Runs={historicalRunCount} | " +
                $"Strength={currentStrengthMultiplier:F2} | AvgStages={averageStages:F2} | " +
                $"Pool={currentTotalResistancePool:F2} | Cap={currentSingleResistanceCap:F2}"
            );

            Debug.Log(
                $"DDA Resistances | Fire={fireResistanceAdjustment:F2} | Ice={iceResistanceAdjustment:F2} | " +
                $"Lightning={lightningResistanceAdjustment:F2} | Melee={meleeResistanceAdjustment:F2}"
            );

            Debug.Log(
                $"DDA Stat Adjustments | HealthAdj={ddaHealthAdjustment:F2} | DamageAdj={ddaDamageAdjustment:F2}"
            );
        }
    }

    private void ApplyPerformanceScaling(float averageStages)
    {
        float healthStepUp = 0.10f;
        float damageStepUp = 0.05f;
        float healthStepDown = 0.05f;
        float damageStepDown = 0.02f;

        if (scaleStatAdjustmentsWithStrength)
        {
            healthStepUp *= currentStrengthMultiplier;
            damageStepUp *= currentStrengthMultiplier;
            healthStepDown *= currentStrengthMultiplier;
            damageStepDown *= currentStrengthMultiplier;
        }

        // Stronger player performance -> stronger future enemies.
        if (averageStages >= 12f)
        {
            ddaHealthAdjustment += healthStepUp * 1.75f;
            ddaDamageAdjustment += damageStepUp * 1.75f;
        }
        else if (averageStages >= 8f)
        {
            ddaHealthAdjustment += healthStepUp * 1.25f;
            ddaDamageAdjustment += damageStepUp * 1.25f;
        }
        else if (averageStages >= 4f)
        {
            ddaHealthAdjustment += healthStepUp;
            ddaDamageAdjustment += damageStepUp;
        }
        else if (averageStages <= 1.5f)
        {
            ddaHealthAdjustment -= healthStepDown;
            ddaDamageAdjustment -= damageStepDown;
        }

        // Clamp to avoid extreme runaway scaling.
        ddaHealthAdjustment = Mathf.Clamp(ddaHealthAdjustment, -0.20f, 1.50f);
        ddaDamageAdjustment = Mathf.Clamp(ddaDamageAdjustment, -0.10f, 1.00f);
    }

    private void ApplySharedResistancePool(int fireDamage, int iceDamage, int lightningDamage, int meleeDamage)
    {
        int totalTrackedDamage = fireDamage + iceDamage + lightningDamage + meleeDamage;

        if (totalTrackedDamage <= 0)
            return;

        fireResistanceAdjustment = CalculatePooledResistance(fireDamage, totalTrackedDamage);
        iceResistanceAdjustment = CalculatePooledResistance(iceDamage, totalTrackedDamage);
        lightningResistanceAdjustment = CalculatePooledResistance(lightningDamage, totalTrackedDamage);
        meleeResistanceAdjustment = CalculatePooledResistance(meleeDamage, totalTrackedDamage);
    }

    private float CalculatePooledResistance(int typeDamage, int totalTrackedDamage)
    {
        if (totalTrackedDamage <= 0 || typeDamage <= 0)
            return 0f;

        float usageRatio = (float)typeDamage / totalTrackedDamage;
        float resistance = usageRatio * currentTotalResistancePool;

        return Mathf.Min(resistance, currentSingleResistanceCap);
    }

    public float GetResistanceForDamageType(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Fire:
                return fireResistanceAdjustment;

            case DamageType.Ice:
                return iceResistanceAdjustment;

            case DamageType.Lightning:
                return lightningResistanceAdjustment;

            case DamageType.Melee:
                return meleeResistanceAdjustment;

            default:
                return 0f;
        }
    }

    public int ApplyResistanceToDamage(DamageType damageType, int baseDamage)
    {
        float resistance = GetResistanceForDamageType(damageType);
        float multiplier = 1f - resistance;

        int adjustedDamage = Mathf.RoundToInt(baseDamage * multiplier);

        return Mathf.Max(1, adjustedDamage);
    }

    public float GetCurrentStrengthMultiplier()
    {
        return currentStrengthMultiplier;
    }

    public float GetCurrentTotalResistancePool()
    {
        return currentTotalResistancePool;
    }

    public float GetCurrentSingleResistanceCap()
    {
        return currentSingleResistanceCap;
    }

    private void ResetAdjustments()
    {
        ddaHealthAdjustment = 0f;
        ddaDamageAdjustment = 0f;

        fireResistanceAdjustment = 0f;
        iceResistanceAdjustment = 0f;
        lightningResistanceAdjustment = 0f;
        meleeResistanceAdjustment = 0f;

        currentStrengthMultiplier = 1f;
        currentTotalResistancePool = baseTotalResistancePool;
        currentSingleResistanceCap = baseSingleResistanceCap;

        enemyHealthMultiplier = 1f;
    }
}