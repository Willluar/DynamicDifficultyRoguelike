using UnityEngine;
using TMPro;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("UI")]
    public TextMeshProUGUI healthText;
    public TextMeshPro healthWorldText;

    private TextMeshPro worldText;

    [Header("Enemy Scaling")]
    public bool isEnemy = false;
    public int baseMaxHealth = 100;

    private void Awake()
    {
        worldText = GetComponentInChildren<TextMeshPro>();
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void InitialiseEnemyHealth()
    {
        if (!isEnemy || GameManager.Instance == null)
            return;

        maxHealth = Mathf.RoundToInt(baseMaxHealth * GameManager.Instance.currentEnemyHealthMultiplier);
        currentHealth = maxHealth;

        UpdateHealthUI();
    }

    public void ResetForNewRun()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (CompareTag("Player") && RunDataLogger.Instance != null)
            RunDataLogger.Instance.AddDamageTaken(damage);

        UpdateHealthUI();

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
        if (CompareTag("Player"))
        {
            if (RunDataLogger.Instance != null)
                RunDataLogger.Instance.EndRunOnDeath();

            if (GridManager.Instance != null)
                GridManager.Instance.Unregister(gameObject);

            if (GameManager.Instance != null)
                GameManager.Instance.PlayerDied();

            PlayerGridMovement move = GetComponent<PlayerGridMovement>();
            if (move != null) move.enabled = false;

            PlayerSpellController spell = GetComponent<PlayerSpellController>();
            if (spell != null) spell.enabled = false;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            return;
        }

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

        PlayerSpellController spellController = FindFirstObjectByType<PlayerSpellController>();
        if (spellController != null)
            spellController.RefreshValidTargets();

        Destroy(gameObject);
    }
}