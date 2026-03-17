using System;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2Int gridPosition = new Vector2Int(1, 1);

    [Header("Movement")]
    [SerializeField] private float gridMovementScale = BalanceConfig.GridMoveSpeedScale;
    [SerializeField] private float stepArcHeight = 0.08f;
    [SerializeField] private float minimumStepDuration = 0.07f;
    [SerializeField] private float arrivalThreshold = 0.01f;

    private HeroStatsData baseStats = new HeroStatsData();
    private Vector3 targetWorldPosition;
    private Vector3 moveStartWorldPosition;
    private bool isMoving;
    private bool isPaused;
    private float moveDuration;
    private float moveProgress;

    private int segmentAttackBonus;
    private int segmentDefenseBonus;
    private int segmentMaxHealthBonus;
    private float segmentSpeedMultiplier = 1f;

    private int divineAttackBonus;
    private int divineDefenseBonus;
    private float divineSpeedMultiplier = 1f;

    private float lastCombatTimestamp = -999f;
    private int lastCombatDamageDealt;
    private int lastCombatDamageTaken;

    public event Action<Vector2Int> ArrivedAtCell;
    public event Action<int, int> HealthChanged;
    public event Action Died;

    public Vector2Int GridPosition => gridPosition;
    public bool IsMoving => isMoving;
    public bool IsPaused => isPaused;
    public bool CanTakeTurn => !isPaused && !isMoving;
    public bool HasRecentCombat => Time.time - lastCombatTimestamp <= 1.35f;
    public float RecentCombatPulse => HasRecentCombat ? 1f - Mathf.Clamp01((Time.time - lastCombatTimestamp) / 1.35f) : 0f;
    public int LastCombatDamageDealt => lastCombatDamageDealt;
    public int LastCombatDamageTaken => lastCombatDamageTaken;
    public Vector2 MovementDirection { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.right;
    public float MovementProgress => isMoving ? Mathf.Clamp01(moveProgress) : 0f;

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

    void OnValidate()
    {
        stepArcHeight = Mathf.Max(0f, stepArcHeight);
        minimumStepDuration = Mathf.Max(0.02f, minimumStepDuration);
        arrivalThreshold = Mathf.Max(0.001f, arrivalThreshold);
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

        Vector3 nextWorldPosition = WorldGrid.Instance.GridToWorld(newGridPosition);
        Vector3 moveDelta = nextWorldPosition - transform.position;
        if (moveDelta.sqrMagnitude <= Mathf.Epsilon)
            return false;

        gridPosition = newGridPosition;
        targetWorldPosition = nextWorldPosition;
        moveStartWorldPosition = transform.position;
        moveProgress = 0f;
        float worldDistance = moveDelta.magnitude;
        float moveSpeed = Mathf.Max(0.01f, gridMovementScale * CombatSpeed);
        moveDuration = Mathf.Max(minimumStepDuration, worldDistance / moveSpeed);
        MovementDirection = new Vector2(moveDelta.x, moveDelta.y).normalized;

        if (Mathf.Abs(MovementDirection.x) > 0.01f)
            FacingDirection = new Vector2(Mathf.Sign(MovementDirection.x), 0f);

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
            moveStartWorldPosition = targetWorldPosition;
            transform.position = targetWorldPosition;
        }

        moveProgress = 0f;
        moveDuration = 0f;
        MovementDirection = Vector2.zero;
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

    public void RegisterCombatExchange(int damageDealt, int damageTaken)
    {
        lastCombatTimestamp = Time.time;
        lastCombatDamageDealt = Mathf.Max(0, damageDealt);
        lastCombatDamageTaken = Mathf.Max(0, damageTaken);
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
        lastCombatTimestamp = -999f;
        lastCombatDamageDealt = 0;
        lastCombatDamageTaken = 0;
        CurrentHealth = baseStats.maxHealth;
        MovementDirection = Vector2.zero;
        FacingDirection = Vector2.right;
        moveProgress = 0f;
        moveDuration = 0f;
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
        MovementDirection = Vector2.zero;
    }

    public void ResumeHero()
    {
        isPaused = false;
    }

    private void MoveToTargetCell()
    {
        if (moveDuration <= Mathf.Epsilon)
        {
            transform.position = targetWorldPosition;
            isMoving = false;
            MovementDirection = Vector2.zero;
            ArrivedAtCell?.Invoke(gridPosition);
            return;
        }

        moveProgress = Mathf.Clamp01(moveProgress + Time.deltaTime / moveDuration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, moveProgress);
        Vector3 interpolatedPosition = Vector3.Lerp(moveStartWorldPosition, targetWorldPosition, easedProgress);
        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        float arcOffset = Mathf.Sin(moveProgress * Mathf.PI) * stepArcHeight * cellSize;
        transform.position = interpolatedPosition + Vector3.up * arcOffset;

        if (moveProgress < 1f && Vector3.Distance(transform.position, targetWorldPosition) > arrivalThreshold)
            return;

        transform.position = targetWorldPosition;
        isMoving = false;
        moveProgress = 0f;
        moveDuration = 0f;
        MovementDirection = Vector2.zero;
        ArrivedAtCell?.Invoke(gridPosition);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
