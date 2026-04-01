using UnityEngine;

public class DynamicDifficultyManager : MonoBehaviour
{
    public static DynamicDifficultyManager Instance;

    public float enemyHealthMultiplier = 1f;
    public float targetWinRate = 0.5f;

    private void Awake()
    {
        Instance = this;
    }

   /* public void ProcessRunData()
    {
        float winRate = RunDataLogger.Instance.GetWinRate();

        if (winRate > targetWinRate + 0.1f)
        {
            enemyHealthMultiplier += 0.1f;
            Debug.Log("Difficulty increased");
        }
        else if (winRate < targetWinRate - 0.1f)
        {
            enemyHealthMultiplier = Mathf.Max(0.5f, enemyHealthMultiplier - 0.1f);
            Debug.Log("Difficulty decreased");
        }
    }*/
}
