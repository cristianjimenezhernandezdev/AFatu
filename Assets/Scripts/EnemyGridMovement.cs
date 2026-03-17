using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyGridMovement : MonoBehaviour
{
    [SerializeField] private float baseMoveScale = BalanceConfig.GridMoveSpeedScale;
    [SerializeField] private float baseDecisionInterval = 0.8f;
    [SerializeField] private float stepArcHeight = 0.045f;
    [SerializeField] private float minimumStepDuration = 0.08f;
    [SerializeField] private float arrivalThreshold = 0.01f;

    private Enemy enemy;
    private PlayerGridMovement player;
    private Vector3 targetPosition;
    private Vector3 moveStartPosition;
    private bool isMoving;
    private float moveTimer;
    private float moveProgress;
    private float moveDuration;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void Start()
    {
        player = Object.FindFirstObjectByType<PlayerGridMovement>();
        if (WorldGrid.Instance != null && enemy != null)
        {
            targetPosition = WorldGrid.Instance.GridToWorld(enemy.GridPosition);
            moveStartPosition = targetPosition;
            transform.position = targetPosition;
        }
    }

    void OnValidate()
    {
        stepArcHeight = Mathf.Max(0f, stepArcHeight);
        minimumStepDuration = Mathf.Max(0.02f, minimumStepDuration);
        arrivalThreshold = Mathf.Max(0.001f, arrivalThreshold);
    }

    void Update()
    {
        if (enemy == null || player == null || WorldGrid.Instance == null || RunManager.Instance == null)
            return;

        if (!RunManager.Instance.AllowsActorTurns || !enemy.IsAlive() || player.CurrentHealth <= 0)
            return;

        if (isMoving)
        {
            MoveToTargetCell();
            return;
        }

        moveTimer += Time.deltaTime;
        float interval = Mathf.Max(0.15f, baseDecisionInterval / Mathf.Max(0.2f, enemy.Speed));
        if (moveTimer < interval)
            return;

        moveTimer = 0f;
        TakeAction();
    }

    private void TakeAction()
    {
        int distanceToHero = Manhattan(enemy.GridPosition, player.GridPosition);
        if (distanceToHero <= 1)
        {
            CombatSystem.ResolveMelee(player, enemy);
            return;
        }

        if (enemy.IsRanged && distanceToHero <= enemy.Range && HasLineOfSight(enemy.GridPosition, player.GridPosition))
        {
            CombatSystem.ResolveRangedAttack(enemy, player);
            return;
        }

        bool pathFound = WorldGrid.Instance.TryGetNextPathStep(
            enemy.GridPosition,
            player.GridPosition,
            pos => pos != player.GridPosition && WorldGrid.Instance.HasEnemyAt(pos),
            out Vector2Int nextStep);

        if (!pathFound)
            return;

        TryMove(nextStep);
    }

    private void TryMove(Vector2Int newPosition)
    {
        if (!WorldGrid.Instance.IsWalkable(newPosition))
            return;

        if (newPosition != player.GridPosition && WorldGrid.Instance.HasEnemyAt(newPosition))
            return;

        WorldGrid.Instance.RemoveEnemy(enemy);
        enemy.SetGridPosition(newPosition);
        WorldGrid.Instance.RegisterEnemy(enemy);
        moveStartPosition = transform.position;
        targetPosition = WorldGrid.Instance.GridToWorld(newPosition);
        float moveDistance = Vector3.Distance(moveStartPosition, targetPosition);
        float moveSpeed = Mathf.Max(0.01f, baseMoveScale * Mathf.Max(0.2f, enemy.Speed));
        moveDuration = Mathf.Max(minimumStepDuration, moveDistance / moveSpeed);
        moveProgress = 0f;
        isMoving = true;
    }

    private void MoveToTargetCell()
    {
        if (moveDuration <= Mathf.Epsilon)
        {
            transform.position = targetPosition;
            isMoving = false;
            return;
        }

        moveProgress = Mathf.Clamp01(moveProgress + Time.deltaTime / moveDuration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, moveProgress);
        Vector3 interpolatedPosition = Vector3.Lerp(moveStartPosition, targetPosition, easedProgress);
        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        float arcMultiplier = enemy != null && (enemy.EnemyId == "bat" || enemy.EnemyId == "ghost_elite") ? 0.35f : 1f;
        float arcOffset = Mathf.Sin(moveProgress * Mathf.PI) * stepArcHeight * arcMultiplier * cellSize;
        transform.position = interpolatedPosition + Vector3.up * arcOffset;

        if (moveProgress < 1f && Vector3.Distance(transform.position, targetPosition) > arrivalThreshold)
            return;

        transform.position = targetPosition;
        isMoving = false;
        moveProgress = 0f;
        moveDuration = 0f;

        if (enemy.GridPosition == player.GridPosition)
        {
            CombatSystem.ResolveMelee(player, enemy);
        }
    }

    private bool HasLineOfSight(Vector2Int from, Vector2Int to)
    {
        if (from.x != to.x && from.y != to.y)
            return false;

        Vector2Int direction = new Vector2Int(
            to.x == from.x ? 0 : (to.x > from.x ? 1 : -1),
            to.y == from.y ? 0 : (to.y > from.y ? 1 : -1));

        Vector2Int current = from + direction;
        while (current != to)
        {
            if (WorldGrid.Instance.HasWallAt(current))
                return false;

            current += direction;
        }

        return true;
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
