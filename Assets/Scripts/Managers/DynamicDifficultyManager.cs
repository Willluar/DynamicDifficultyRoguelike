using System.Collections.Generic;
using UnityEngine;

public class DynamicDifficultyManager : MonoBehaviour
{
    public static DynamicDifficultyManager Instance;

    // Kept for legacy compatibility with older scripts
    public float enemyHealthMultiplier = 1f;

    public float ddaHealthAdjustment = 0f;
    public float ddaDamageAdjustment = 0f;

    public float fireResistanceAdjustment = 0f;
    public float iceResistanceAdjustment = 0f;
    public float lightningResistanceAdjustment = 0f;
    public float meleeResistanceAdjustment = 0f;

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

        int maxDamage = Mathf.Max(fireDamage, iceDamage, lightningDamage, meleeDamage);

        if (maxDamage > 0)
        {
            if (maxDamage == fireDamage) fireResistanceAdjustment = 0.15f;
            if (maxDamage == iceDamage) iceResistanceAdjustment = 0.15f;
            if (maxDamage == lightningDamage) lightningResistanceAdjustment = 0.15f;
            if (maxDamage == meleeDamage) meleeResistanceAdjustment = 0.15f;
        }

        enemyHealthMultiplier = 1f + ddaHealthAdjustment;
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