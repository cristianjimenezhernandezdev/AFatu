using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyGridMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float moveInterval = 0.8f;

    private Enemy enemy;
    private PlayerGridMovement player;

    private Vector3 targetPosition;
    private bool isMoving;
    private float moveTimer;
    private bool isDeadOrDisabled;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        player = Object.FindFirstObjectByType<PlayerGridMovement>();

        if (enemy == null)
        {
            Debug.LogError("EnemyGridMovement necessita un component Enemy.");
            enabled = false;
            return;
        }

        if (WorldGrid.Instance != null)
        {
            targetPosition = WorldGrid.Instance.GridToWorld(enemy.gridPosition);
            transform.position = targetPosition;
        }
    }

    void Update()
    {
        if (isDeadOrDisabled)
            return;

        if (player == null || WorldGrid.Instance == null || enemy == null)
            return;

        if (RunManager.Instance != null && !RunManager.Instance.AllowsActorTurns)
            return;

        if (!enemy.IsAlive())
        {
            isDeadOrDisabled = true;
            return;
        }

        if (isMoving)
        {
            MoveToTargetCell();
            return;
        }

        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            TryChasePlayer();
        }
    }

    void MoveToTargetCell()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
            CheckPlayerCollision();
        }
    }

    void TryChasePlayer()
    {
        if (player == null)
            return;

        bool pathFound = WorldGrid.Instance.TryGetNextPathStep(
            enemy.gridPosition,
            player.GridPosition,
            pos => pos != player.GridPosition && WorldGrid.Instance.HasEnemyAt(pos),
            out Vector2Int nextStep
        );

        if (pathFound && TryMove(nextStep - enemy.gridPosition))
            return;

        IReadOnlyList<Vector2Int> prioritizedDirections =
            GridDirectionUtility.BuildPrioritizedDirections(enemy.gridPosition, player.GridPosition);

        foreach (Vector2Int direction in prioritizedDirections)
        {
            if (TryMove(direction))
                return;
        }
    }

    bool TryMove(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return false;

        Vector2Int newPosition = enemy.gridPosition + direction;

        if (!WorldGrid.Instance.IsWalkable(newPosition))
            return false;

        if (newPosition != player.GridPosition && WorldGrid.Instance.HasEnemyAt(newPosition))
            return false;

        WorldGrid.Instance.RemoveEnemy(enemy);

        enemy.gridPosition = newPosition;
        WorldGrid.Instance.RegisterEnemy(enemy);

        targetPosition = WorldGrid.Instance.GridToWorld(enemy.gridPosition);
        isMoving = true;

        return true;
    }

    void CheckPlayerCollision()
    {
        if (player == null || enemy == null || !enemy.IsAlive())
            return;

        if (enemy.gridPosition == player.GridPosition)
        {
            Debug.Log("L'enemic ha atrapat l'heroi");

            player.TakeDamage(enemy.Attack);

            if (player.CurrentHealth > 0 && enemy.IsAlive())
            {
                enemy.TakeDamage(player.Attack);
            }

            if (!enemy.IsAlive())
            {
                isDeadOrDisabled = true;
            }
        }
    }
}
