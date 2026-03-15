using System.Collections.Generic;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2Int gridPosition = new(1, 1);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float decisionInterval = 0.35f;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int attack = 3;

    private int currentHealth;
    private Vector3 targetWorldPosition;
    private bool isMoving;
    private float decisionTimer;
    private bool isPaused;

    public Vector2Int GridPosition => gridPosition;
    public int CurrentHealth => currentHealth;
    public int Attack => attack;

    void Start()
    {
        ResetForRun();

        if (WorldGrid.Instance != null)
        {
            transform.position = WorldGrid.Instance.GridToWorld(gridPosition);
            targetWorldPosition = transform.position;
        }
    }

    void Update()
    {
        if (isPaused || WorldGrid.Instance == null || GoalTile.Instance == null)
            return;

        if (RunManager.Instance != null && !RunManager.Instance.AllowsActorTurns)
            return;

        if (isMoving)
        {
            MoveToTargetCell();
            return;
        }

        decisionTimer += Time.deltaTime;

        if (decisionTimer >= decisionInterval)
        {
            decisionTimer = 0f;
            DecideNextMove();
        }
    }

    void MoveToTargetCell()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            transform.position = targetWorldPosition;
            isMoving = false;

            CheckForEnemy();
            CheckGoalReached();
        }
    }

    void DecideNextMove()
    {
        if (TryMoveAlongPath(true))
            return;

        if (TryMoveAlongPath(false))
            return;

        IReadOnlyList<Vector2Int> prioritizedDirections =
            GridDirectionUtility.BuildPrioritizedDirections(gridPosition, GoalTile.Instance.GridPosition);

        foreach (Vector2Int direction in prioritizedDirections)
        {
            if (TryMove(direction, false))
                return;
        }
    }

    bool TryMoveAlongPath(bool avoidDangerousEnemies)
    {
        bool pathFound = WorldGrid.Instance.TryGetNextPathStep(
            gridPosition,
            GoalTile.Instance.GridPosition,
            pos =>
            {
                if (!avoidDangerousEnemies)
                    return false;

                Enemy enemy = WorldGrid.Instance.GetEnemyAt(pos);
                return enemy != null && IsEnemyDangerous(enemy);
            },
            out Vector2Int nextStep
        );

        if (!pathFound)
            return false;

        return TryMove(nextStep - gridPosition, avoidDangerousEnemies);
    }

    bool TryMove(Vector2Int direction, bool avoidDangerousEnemies)
    {
        if (direction == Vector2Int.zero)
            return false;

        Vector2Int newPos = gridPosition + direction;

        if (!WorldGrid.Instance.IsWalkable(newPos))
            return false;

        Enemy enemy = WorldGrid.Instance.GetEnemyAt(newPos);

        if (enemy != null && avoidDangerousEnemies && IsEnemyDangerous(enemy))
            return false;

        gridPosition = newPos;
        targetWorldPosition = WorldGrid.Instance.GridToWorld(gridPosition);
        isMoving = true;

        return true;
    }

    bool IsEnemyDangerous(Enemy enemy)
    {
        if (enemy == null)
            return false;

        bool lethalSoon = enemy.Attack >= currentHealth;
        bool tooTanky = enemy.CurrentHealth > attack * 2;

        return lethalSoon || tooTanky;
    }

    void CheckForEnemy()
    {
        Enemy enemy = WorldGrid.Instance.GetEnemyAt(gridPosition);

        if (enemy != null)
        {
            StartCombat(enemy);
        }
    }

    void StartCombat(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.TakeDamage(attack);

        if (enemy.IsAlive())
        {
            TakeDamage(enemy.Attack);
        }
    }

    void CheckGoalReached()
    {
        if (GoalTile.Instance != null && gridPosition == GoalTile.Instance.GridPosition)
        {
            PauseHero();
            RunManager.Instance.OnPlayerReachedSegmentGoal();
        }
    }

    public void SetGridPosition(Vector2Int newGridPosition, bool snapToWorld = true)
    {
        gridPosition = newGridPosition;

        if (snapToWorld && WorldGrid.Instance != null)
        {
            targetWorldPosition = WorldGrid.Instance.GridToWorld(gridPosition);
            transform.position = targetWorldPosition;
            isMoving = false;
        }
    }

    public void ResetForRun()
    {
        currentHealth = maxHealth;
        isPaused = false;
        isMoving = false;
        decisionTimer = 0f;
    }

    public void PauseHero()
    {
        isPaused = true;
        isMoving = false;
    }

    public void ResumeHero()
    {
        isPaused = false;
        decisionTimer = 0f;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            RunManager.Instance.FailRun();
        }
    }
}
