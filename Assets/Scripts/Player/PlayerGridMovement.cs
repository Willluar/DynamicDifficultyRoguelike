using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGridMovement : MonoBehaviour
{
    [Header("Combat")]
    public int attackDamage = 50;

    private PlayerInputActions inputActions;
    private Vector2 input;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += ctx => input = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => input = Vector2.zero;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.Register(gameObject);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn())
            return;

        Vector2Int direction = GetCardinalDirection(input);

        if (direction != Vector2Int.zero)
        {
            AttemptMove(direction);
            input = Vector2.zero;
        }
    }

    private Vector2Int GetCardinalDirection(Vector2 rawInput)
    {
        if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
            return rawInput.x > 0 ? Vector2Int.right : Vector2Int.left;
        else if (Mathf.Abs(rawInput.y) > 0)
            return rawInput.y > 0 ? Vector2Int.up : Vector2Int.down;

        return Vector2Int.zero;
    }

    private void AttemptMove(Vector2Int direction)
    {
        if (GridManager.Instance == null)
            return;

        Vector2Int currentGrid = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int targetGrid = currentGrid + direction;

        if (!GridManager.Instance.IsInsideGrid(targetGrid))
            return;

        if (GridManager.Instance.IsTileOccupied(targetGrid))
        {
            GameObject occupant = GridManager.Instance.GetOccupant(targetGrid);

            EnemyGridMovement enemy = occupant != null ? occupant.GetComponent<EnemyGridMovement>() : null;
            Health enemyHealth = occupant != null ? occupant.GetComponent<Health>() : null;

            if (enemy != null && enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);

                if (RunDataLogger.Instance != null)
                {
                    RunDataLogger.Instance.RecordSpellCast(DamageType.Melee);
                    RunDataLogger.Instance.RecordDamageByType(DamageType.Melee, attackDamage);
                    RunDataLogger.Instance.AddDamageDealt(attackDamage);
                }

                if (GameManager.Instance == null || !GameManager.Instance.ConsumeSkipPlayerEndTurnFlag())
                {
                    if (TurnManager.Instance != null)
                        TurnManager.Instance.EndPlayerTurn();
                }
            }

            // Occupied by wall or other blocker = do nothing, do not spend turn
            return;
        }

        GridManager.Instance.Move(gameObject, targetGrid);

        if (TurnManager.Instance != null)
            TurnManager.Instance.EndPlayerTurn();
    }
}