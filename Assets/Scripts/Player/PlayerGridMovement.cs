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
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCancelled;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCancelled;
        inputActions.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        input = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCancelled(InputAction.CallbackContext ctx)
    {
        input = Vector2.zero;
    }

    private void Start()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.Register(gameObject);
    }

    private void Update()
    {
        if (SimulationManager.Instance != null && SimulationManager.Instance.IsUsingSimulatedPlayer())
            return;

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

    public Vector2Int GetCardinalDirection(Vector2 rawInput)
    {
        if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
            return rawInput.x > 0 ? Vector2Int.right : Vector2Int.left;
        else if (Mathf.Abs(rawInput.y) > 0)
            return rawInput.y > 0 ? Vector2Int.up : Vector2Int.down;

        return Vector2Int.zero;
    }

    public bool HasAdjacentEnemy(out Vector2Int directionToEnemy)
    {
        directionToEnemy = Vector2Int.zero;

        if (GridManager.Instance == null)
            return false;

        Vector2Int currentGrid = GridManager.Instance.WorldToGrid(transform.position);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int checkGrid = currentGrid + direction;

            if (!GridManager.Instance.IsInsideGrid(checkGrid))
                continue;

            if (!GridManager.Instance.IsTileOccupied(checkGrid))
                continue;

            GameObject occupant = GridManager.Instance.GetOccupant(checkGrid);
            if (occupant == null)
                continue;

            if (occupant.GetComponent<EnemyGridMovement>() != null)
            {
                directionToEnemy = direction;
                return true;
            }
        }

        return false;
    }

    public bool PerformSimulatedAction(Vector2Int direction)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return false;

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn())
            return false;

        return AttemptMove(direction);
    }

    private bool AttemptMove(Vector2Int direction)
    {
        if (GridManager.Instance == null)
            return false;

        Vector2Int currentGrid = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int targetGrid = currentGrid + direction;

        if (!GridManager.Instance.IsInsideGrid(targetGrid))
            return false;

        if (GridManager.Instance.IsTileOccupied(targetGrid))
        {
            GameObject occupant = GridManager.Instance.GetOccupant(targetGrid);

            EnemyGridMovement enemy = occupant != null ? occupant.GetComponent<EnemyGridMovement>() : null;
            Health enemyHealth = occupant != null ? occupant.GetComponent<Health>() : null;

            if (enemy != null && enemyHealth != null)
            {
                int finalDamage = attackDamage;

                if (enemyHealth.isEnemy &&
                    DynamicDifficultyManager.Instance != null &&
                    GameManager.Instance != null &&
                    GameManager.Instance.useDDA)
                {
                    finalDamage = DynamicDifficultyManager.Instance.ApplyResistanceToDamage(DamageType.Melee, attackDamage);
                }

                if (RunDataLogger.Instance != null)
                {
                    RunDataLogger.Instance.RecordSpellCast(DamageType.Melee);
                    RunDataLogger.Instance.RecordDamageByType(DamageType.Melee, finalDamage);
                    RunDataLogger.Instance.AddDamageDealt(finalDamage);
                }

                enemyHealth.TakeDamage(finalDamage);

                if (GameManager.Instance != null)
                    GameManager.Instance.ResolvePendingStageClear();

                bool skipTurn = GameManager.Instance != null && GameManager.Instance.ConsumeSkipPlayerEndTurnFlag();

                if (!skipTurn && TurnManager.Instance != null)
                    TurnManager.Instance.EndPlayerTurn();

                return true;
            }

            return false;
        }

        GridManager.Instance.Move(gameObject, targetGrid);

        if (TurnManager.Instance != null)
            TurnManager.Instance.EndPlayerTurn();

        return true;
    }
}