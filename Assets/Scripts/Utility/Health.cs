using UnityEngine;
using TMPro;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("UI")]
    public TextMeshProUGUI healthText;   // Optional: player UI
    public TextMeshPro healthWorldText;  // Optional: enemy world text

    private TextMeshPro worldText;

    private void Awake()
    {
        Debug.Log("Player HP: " + currentHealth);
        currentHealth = maxHealth;
        worldText = GetComponentInChildren<TextMeshPro>();
        UpdateHealthUI();
    }
    private void Update()
    {
        UpdateHealthUI();
    }
    public void TakeDamage(int damage)
    {

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Log damage taken only for player
        if (CompareTag("Player") && RunDataLogger.Instance != null)
            RunDataLogger.Instance.AddDamageTaken(damage);

       

        if (currentHealth <= 0)
            Die();
    }

    private void UpdateHealthUI()
    {
        string display = currentHealth + "/" + maxHealth;

        if (worldText != null) worldText.text = display;
        if (healthText != null) healthText.text = display;
        if (healthWorldText != null) healthWorldText.text = display;
    }

    private void Die()
    {
        // PLAYER DEATH: end run + end game, do NOT destroy player
        if (CompareTag("Player"))
        {
            if (RunDataLogger.Instance != null)
                RunDataLogger.Instance.EndRunOnDeath();

            if (GridManager.Instance != null)
                GridManager.Instance.Unregister(gameObject);

            if (GameManager.Instance != null)
                GameManager.Instance.PlayerDied();

            // Disable player so nothing else can interact with it
            var move = GetComponent<PlayerGridMovement>();
            if (move != null) move.enabled = false;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            return;
        }

        // ENEMY DEATH: unregister systems + log kill + destroy
        EnemyGridMovement enemy = GetComponent<EnemyGridMovement>();
        if (enemy != null)
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.UnregisterEnemy(enemy);

            if (GameManager.Instance != null)
                GameManager.Instance.UnregisterEnemy(gameObject);

            if (RunDataLogger.Instance != null)
                RunDataLogger.Instance.AddEnemyKill();
        }

        if (GridManager.Instance != null)
            GridManager.Instance.Unregister(gameObject);

        Destroy(gameObject);
    }
}