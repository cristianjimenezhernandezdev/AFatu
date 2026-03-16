using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyGridMovement : MonoBehaviour
{
    [SerializeField] private float baseMoveScale = BalanceConfig.GridMoveSpeedScale;
    [SerializeField] private float baseDecisionInterval = 0.8f;

    private Enemy enemy;
    private PlayerGridMovement player;
    private Vector3 targetPosition;
    private bool isMoving;
    private float moveTimer;

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
            transform.position = targetPosition;
        }
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
        targetPosition = WorldGrid.Instance.GridToWorld(newPosition);
        isMoving = true;
    }

    private void MoveToTargetCell()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            baseMoveScale * enemy.Speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            return;

        transform.position = targetPosition;
        isMoving = false;

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
