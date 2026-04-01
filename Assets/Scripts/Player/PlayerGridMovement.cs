using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGridMovement : MonoBehaviour
{
    [Header("Combat")]
    public int attackDamage = 2;

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
        GridManager.Instance.Register(gameObject);
    }

    private void Update()
    {
        if (!TurnManager.Instance.IsPlayerTurn())
            return;

        Vector2Int direction = GetCardinalDirection(input);

        if (direction != Vector2Int.zero)
        {
            AttemptMove(direction);
            input = Vector2.zero; // prevent repeat moves
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
        Vector2Int currentGrid = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int targetGrid = currentGrid + direction;

        if (!GridManager.Instance.IsInsideGrid(targetGrid))
            return;

        if (GridManager.Instance.IsTileOccupied(targetGrid))
        {
            GameObject occupant = GridManager.Instance.GetOccupant(targetGrid);

            // Only treat it as an attack if the occupant is an enemy (has EnemyGridMovement)
            EnemyGridMovement enemy = occupant.GetComponent<EnemyGridMovement>();
            Health enemyHealth = occupant.GetComponent<Health>();

            if (enemy != null && enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);

                // Log damage dealt by the player
                if (RunDataLogger.Instance != null)
                    RunDataLogger.Instance.AddDamageDealt(attackDamage);
            }

            TurnManager.Instance.EndPlayerTurn();
            return;
        }

        GridManager.Instance.Move(gameObject, targetGrid);
        TurnManager.Instance.EndPlayerTurn();
    }
}