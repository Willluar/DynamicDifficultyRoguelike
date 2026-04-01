using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void ResolveCombat(BuildData attacker, EnemyStats enemy)
    {
        enemy.health -= attacker.damage;

        if (enemy.health <= 0)
        {
            Debug.Log("Enemy defeated");
        }
    }
}
