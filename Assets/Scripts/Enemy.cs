using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2Int gridPosition;

    private bool isDead;

    public string EnemyId { get; private set; } = "skeleton";
    public string DisplayName { get; private set; } = "Esquelet";
    public string Rarity { get; private set; } = "common";
    public string MovementPattern { get; private set; } = "chase";
    public long RunSegmentEnemyId { get; private set; }
    public int MaxHealth { get; private set; } = 10;
    public int CurrentHealth { get; private set; } = 10;
    public int Attack { get; private set; } = 4;
    public int Defense { get; private set; }
    public float Speed { get; private set; } = 1f;
    public bool IsRanged { get; private set; }
    public int Range { get; private set; } = 1;
    public int RewardGold { get; private set; } = 2;
    public Vector2Int GridPosition => gridPosition;
    public bool IsElite => EnemyId == "ghost_elite" || Rarity == "rare";

    void Start()
    {
        if (WorldGrid.Instance != null)
        {
            transform.position = WorldGrid.Instance.GridToWorld(gridPosition);
            WorldGrid.Instance.RegisterEnemy(this);
        }
    }

    public void ConfigureFromSpawn(GeneratedEnemySpawnData spawn)
    {
        if (spawn == null || spawn.archetype == null || spawn.segmentEnemy == null)
            return;

        isDead = false;
        EnemyId = spawn.archetype.enemyId;
        DisplayName = spawn.archetype.displayName;
        Rarity = spawn.archetype.rarity;
        MovementPattern = spawn.archetype.movementPattern;
        RunSegmentEnemyId = spawn.segmentEnemy.runSegmentEnemyId;
        MaxHealth = spawn.segmentEnemy.spawnedMaxHealth;
        CurrentHealth = spawn.segmentEnemy.spawnedMaxHealth;
        Attack = spawn.segmentEnemy.spawnedAttack;
        Defense = spawn.segmentEnemy.spawnedDefense;
        Speed = spawn.segmentEnemy.spawnedSpeed;
        IsRanged = spawn.behavior != null && spawn.behavior.ranged;
        Range = Mathf.Max(1, spawn.behavior != null ? spawn.behavior.range : 1);
        RewardGold = spawn.reward != null ? spawn.reward.gold : 0;
        gridPosition = spawn.gridPosition;

        if (WorldGrid.Instance != null)
            transform.position = WorldGrid.Instance.GridToWorld(gridPosition);

        ProceduralEnemyRenderer renderer = GetComponent<ProceduralEnemyRenderer>();
        if (renderer != null)
            renderer.SetVisualByEnemyId(EnemyId, true);
    }

    public bool IsAlive()
    {
        return !isDead && CurrentHealth > 0;
    }

    public void SetGridPosition(Vector2Int newPosition)
    {
        gridPosition = newPosition;
    }

    public void ApplyDirectDamage(int amount)
    {
        if (amount <= 0 || isDead)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        if (CurrentHealth <= 0)
            Die();
    }

    public void DespawnWithoutReward()
    {
        if (isDead)
            return;

        isDead = true;
        if (WorldGrid.Instance != null)
            WorldGrid.Instance.RemoveEnemy(this);
        Destroy(gameObject);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        if (WorldGrid.Instance != null)
            WorldGrid.Instance.RemoveEnemy(this);
        if (RunManager.Instance != null)
            RunManager.Instance.OnEnemyDefeated(this);
        Destroy(gameObject);
    }
}
