using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public int baseHealth = 10;
    public int health;

    private void Start()
    {
        health = Mathf.RoundToInt(baseHealth * DynamicDifficultyManager.Instance.enemyHealthMultiplier);
    }
}
