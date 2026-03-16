using System;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2Int gridPosition = new Vector2Int(1, 1);

    [Header("Movement")]
    [SerializeField] private float gridMovementScale = BalanceConfig.GridMoveSpeedScale;

    private HeroStatsData baseStats = new HeroStatsData();
    private Vector3 targetWorldPosition;
    private bool isMoving;
    private bool isPaused;

    private int segmentAttackBonus;
    private int segmentDefenseBonus;
    private int segmentMaxHealthBonus;
    private float segmentSpeedMultiplier = 1f;

    private int divineAttackBonus;
    private int divineDefenseBonus;
    private float divineSpeedMultiplier = 1f;

    public event Action<Vector2Int> ArrivedAtCell;
    public event Action<int, int> HealthChanged;
    public event Action Died;

    public Vector2Int GridPosition => gridPosition;
    public bool IsMoving => isMoving;
    public bool IsPaused => isPaused;
    public bool CanTakeTurn => !isPaused && !isMoving;

    public int CurrentHealth { get; private set; } = BalanceConfig.HeroBaseMaxHealth;
    public int MaxHealth => Mathf.Max(1, baseStats.maxHealth + segmentMaxHealthBonus);
    public int Attack => Mathf.Max(1, baseStats.attack + segmentAttackBonus + divineAttackBonus);
    public int Defense => Mathf.Max(0, baseStats.defense + segmentDefenseBonus + divineDefenseBonus);
    public float CombatSpeed => Mathf.Max(0.1f, baseStats.speed * Mathf.Max(0.25f, segmentSpeedMultiplier) * Mathf.Max(0.25f, divineSpeedMultiplier));

    void Start()
    {
        ResetForRun();
        SnapToGridPosition(gridPosition);
    }

    void Update()
    {
        if (!isMoving)
            return;

        MoveToTargetCell();
    }

    public void ConfigureFromRun(HeroStatsData stats)
    {
        baseStats = stats ?? new HeroStatsData();
        CurrentHealth = Mathf.Clamp(baseStats.currentHealth, 0, MaxHealth);
        NotifyHealthChanged();
    }

    public void SetSegmentBonuses(int attackBonus, int defenseBonus, int maxHealthBonus, float speedMultiplier)
    {
        segmentAttackBonus = attackBonus;
        segmentDefenseBonus = defenseBonus;
        segmentMaxHealthBonus = maxHealthBonus;
        segmentSpeedMultiplier = speedMultiplier <= 0f ? 1f : speedMultiplier;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        NotifyHealthChanged();
    }

    public void SetDivinePowerBonuses(int attackBonus, int defenseBonus, float speedMultiplier)
    {
        divineAttackBonus = attackBonus;
        divineDefenseBonus = defenseBonus;
        divineSpeedMultiplier = speedMultiplier <= 0f ? 1f : speedMultiplier;
    }

    public bool TryMove(Vector2Int newGridPosition)
    {
        if (!CanTakeTurn || WorldGrid.Instance == null)
            return false;

        if (!WorldGrid.Instance.IsWalkable(newGridPosition))
            return false;

        gridPosition = newGridPosition;
        targetWorldPosition = WorldGrid.Instance.GridToWorld(gridPosition);
        isMoving = true;
        return true;
    }

    public void SetGridPosition(Vector2Int newGridPosition, bool snapToWorld = true)
    {
        gridPosition = newGridPosition;
        if (snapToWorld)
            SnapToGridPosition(newGridPosition);
    }

    public void SnapToGridPosition(Vector2Int newGridPosition)
    {
        gridPosition = newGridPosition;
        if (WorldGrid.Instance != null)
        {
            targetWorldPosition = WorldGrid.Instance.GridToWorld(gridPosition);
            transform.position = targetWorldPosition;
        }
        isMoving = false;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        NotifyHealthChanged();
    }

    public void ApplyDirectDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0, MaxHealth);
        NotifyHealthChanged();

        if (CurrentHealth <= 0)
        {
            PauseHero();
            Died?.Invoke();
        }
    }

    public void ResetForRun()
    {
        baseStats = new HeroStatsData();
        segmentAttackBonus = 0;
        segmentDefenseBonus = 0;
        segmentMaxHealthBonus = 0;
        segmentSpeedMultiplier = 1f;
        divineAttackBonus = 0;
        divineDefenseBonus = 0;
        divineSpeedMultiplier = 1f;
        CurrentHealth = baseStats.maxHealth;
        isPaused = false;
        isMoving = false;
        NotifyHealthChanged();
    }

    public HeroStatsData BuildSnapshot()
    {
        return new HeroStatsData
        {
            maxHealth = MaxHealth,
            currentHealth = CurrentHealth,
            attack = baseStats.attack,
            defense = baseStats.defense,
            speed = baseStats.speed
        };
    }

    public void PauseHero()
    {
        isPaused = true;
        isMoving = false;
    }

    public void ResumeHero()
    {
        isPaused = false;
    }

    private void MoveToTargetCell()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            gridMovementScale * CombatSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPosition) > 0.01f)
            return;

        transform.position = targetWorldPosition;
        isMoving = false;
        ArrivedAtCell?.Invoke(gridPosition);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
