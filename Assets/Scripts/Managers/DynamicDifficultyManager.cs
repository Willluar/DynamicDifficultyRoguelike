using System.Collections.Generic;
using UnityEngine;

public class DynamicDifficultyManager : MonoBehaviour
{
    public static DynamicDifficultyManager Instance;

    [Header("Legacy")]
    public float enemyHealthMultiplier = 1f;

    [Header("DDA Stat Adjustments")]
    public float ddaHealthAdjustment = 0f;
    public float ddaDamageAdjustment = 0f;

    [Header("Damage-Type Resistances")]
    [Range(0f, 0.9f)] public float maxTotalResistancePool = 0.45f;
    [Range(0f, 0.9f)] public float maxSingleResistance = 0.30f;
    public float fireResistanceAdjustment = 0f;
    public float iceResistanceAdjustment = 0f;
    public float lightningResistanceAdjustment = 0f;
    public float meleeResistanceAdjustment = 0f;

    [Header("Data Settings")]
    public int runsToAverage = 5;

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
        Debug.Log("DDA manager loading run data");
    }

    public void ProcessRecentRunData(RunDataCollection data)
    {
        ResetAdjustments();

        if (data == null || data.runs == null || data.runs.Count == 0)
            return;

        List<RunData> relevantRuns = new List<RunData>();

        for (int i = data.runs.Count - 1; i >= 0; i--)
        {
            if (data.runs[i] != null && data.runs[i].ddaEnabled)
                relevantRuns.Add(data.runs[i]);

            if (relevantRuns.Count >= runsToAverage)
                break;
        }

        if (relevantRuns.Count == 0)
            return;

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

        if (averageStages >= 4f)
        {
            ddaHealthAdjustment += 0.1f;
            ddaDamageAdjustment += 0.05f;
        }
        else if (averageStages <= 1.5f)
        {
            ddaHealthAdjustment -= 0.05f;
            ddaDamageAdjustment -= 0.02f;
        }

        ApplySharedResistancePool(fireDamage, iceDamage, lightningDamage, meleeDamage);

        enemyHealthMultiplier = 1f + ddaHealthAdjustment;
        Debug.Log(
    $"Processed DDA. Fire={fireResistanceAdjustment:F2}, Ice={iceResistanceAdjustment:F2}, " +
    $"Lightning={lightningResistanceAdjustment:F2}, Melee={meleeResistanceAdjustment:F2}"
);
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
        float pooledResistance = usageRatio * maxTotalResistancePool;

        return Mathf.Min(pooledResistance, maxSingleResistance);
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

    private void ResetAdjustments()
    {
        ddaHealthAdjustment = 0f;
        ddaDamageAdjustment = 0f;

        fireResistanceAdjustment = 0f;
        iceResistanceAdjustment = 0f;
        lightningResistanceAdjustment = 0f;
        meleeResistanceAdjustment = 0f;

        enemyHealthMultiplier = 1f;
    }
}