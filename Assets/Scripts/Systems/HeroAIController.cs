using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class HeroAIController : MonoBehaviour
{
    [SerializeField] private PlayerGridMovement player;
    [SerializeField] private float decisionInterval = BalanceConfig.HeroDecisionIntervalSeconds;
    [SerializeField] private float chestInterestRadius = 5f;

    private float decisionTimer;

    void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerGridMovement>();
    }

    void Update()
    {
        if (player == null || WorldGrid.Instance == null || GoalTile.Instance == null || RunManager.Instance == null)
            return;
        if (!RunManager.Instance.AllowsActorTurns || !player.CanTakeTurn)
            return;

        decisionTimer += Time.deltaTime;
        if (decisionTimer < decisionInterval)
            return;

        decisionTimer = 0f;
        DecideNextAction();
    }

    public void ResetDecisionTimer()
    {
        decisionTimer = 0f;
    }

    private void DecideNextAction()
    {
        List<Enemy> nearbyEnemies = WorldGrid.Instance.GetEnemiesInRadius(player.GridPosition, BalanceConfig.HeroDangerRadius).ToList();
        string effectiveMode = RunManager.Instance.EffectiveHeroMode;
        Vector2Int target = GetPreferredTarget(nearbyEnemies);

        if (TryResolveBlockingEncounter(target, nearbyEnemies, effectiveMode))
            return;

        bool avoidDanger = effectiveMode != BalanceConfig.HeroModeAggressive;
        if (TryMoveTowardTarget(target, avoidDanger, nearbyEnemies))
            return;

        if (TryRetreat(nearbyEnemies))
            return;

        TryMoveTowardTarget(GoalTile.Instance.GridPosition, false, nearbyEnemies);
    }

    private Vector2Int GetPreferredTarget(IReadOnlyList<Enemy> nearbyEnemies)
    {
        IReadOnlyList<Chest> nearbyChests = WorldGrid.Instance.GetChestsInRadius(player.GridPosition, chestInterestRadius);
        if (nearbyChests != null && nearbyChests.Count > 0 && IsSafeToDetour(nearbyEnemies))
        {
            Chest closestChest = nearbyChests
                .OrderBy(chest => Manhattan(chest.GridPosition, player.GridPosition))
                .ThenByDescending(chest => chest.EmeraldReward)
                .ThenByDescending(chest => chest.GoldReward)
                .FirstOrDefault();

            if (closestChest != null)
                return closestChest.GridPosition;
        }

        return GoalTile.Instance.GridPosition;
    }

    private bool IsSafeToDetour(IReadOnlyList<Enemy> nearbyEnemies)
    {
        int closeEnemies = nearbyEnemies.Count(enemy => enemy != null && enemy.IsAlive() && Manhattan(enemy.GridPosition, player.GridPosition) <= BalanceConfig.HeroPrudentDistance);
        bool eliteNearby = nearbyEnemies.Any(enemy => enemy != null && enemy.IsAlive() && enemy.IsElite && Manhattan(enemy.GridPosition, player.GridPosition) <= BalanceConfig.HeroDangerRadius);
        return closeEnemies <= 1 && !eliteNearby;
    }

    private bool TryResolveBlockingEncounter(Vector2Int target, IReadOnlyList<Enemy> nearbyEnemies, string effectiveMode)
    {
        bool pathFound = WorldGrid.Instance.TryGetNextPathStep(player.GridPosition, target, null, out Vector2Int nextStep);
        if (!pathFound)
            return false;

        Enemy blockingEnemy = WorldGrid.Instance.GetEnemyAt(nextStep);
        if (blockingEnemy == null)
            return false;

        bool aggressive = effectiveMode == BalanceConfig.HeroModeAggressive;
        bool escape = effectiveMode == BalanceConfig.HeroModeEscape;
        bool shouldFight = ShouldFight(blockingEnemy, nearbyEnemies, true, aggressive, escape);
        if (!shouldFight)
            return TryRetreat(nearbyEnemies);

        return player.TryMove(nextStep);
    }

    private bool TryMoveTowardTarget(Vector2Int target, bool avoidDanger, IReadOnlyList<Enemy> nearbyEnemies)
    {
        bool pathFound = WorldGrid.Instance.TryGetNextPathStep(
            player.GridPosition,
            target,
            pos => ShouldAvoidPosition(pos, avoidDanger, nearbyEnemies),
            out Vector2Int nextStep);

        if (!pathFound)
            return false;

        Enemy enemy = WorldGrid.Instance.GetEnemyAt(nextStep);
        if (enemy != null)
        {
            bool aggressive = RunManager.Instance.EffectiveHeroMode == BalanceConfig.HeroModeAggressive;
            bool escape = RunManager.Instance.EffectiveHeroMode == BalanceConfig.HeroModeEscape;
            if (!ShouldFight(enemy, nearbyEnemies, false, aggressive, escape))
                return false;
        }

        return player.TryMove(nextStep);
    }

    private bool ShouldAvoidPosition(Vector2Int position, bool avoidDanger, IReadOnlyList<Enemy> nearbyEnemies)
    {
        if (!avoidDanger)
            return false;

        Enemy enemy = WorldGrid.Instance.GetEnemyAt(position);
        if (enemy == null)
            return false;

        return !ShouldFight(enemy, nearbyEnemies, false, false, false);
    }

    private bool ShouldFight(Enemy enemy, IReadOnlyList<Enemy> nearbyEnemies, bool blocksPath, bool aggressive, bool escape)
    {
        if (enemy == null)
            return false;

        CombatSimulationResult estimate = CombatSystem.Simulate(player, enemy);
        float remainingRatio = player.CurrentHealth <= 0 ? 0f : (float)estimate.estimatedHeroHealth / player.CurrentHealth;
        int closeEnemyCount = nearbyEnemies.Count(item => item != null && item.IsAlive() && Manhattan(item.GridPosition, player.GridPosition) <= BalanceConfig.HeroPrudentDistance);
        bool multipleEnemies = closeEnemyCount >= 2;
        bool eliteThreat = enemy.IsElite && !aggressive;

        if (escape)
            return false;
        if (remainingRatio < BalanceConfig.RetreatHealthThreshold)
            return false;
        if (multipleEnemies && !aggressive)
            return false;
        if (eliteThreat && remainingRatio < 0.65f)
            return false;
        if (aggressive || blocksPath)
            return true;

        return remainingRatio > BalanceConfig.FightHealthThreshold;
    }

    private bool TryRetreat(IReadOnlyList<Enemy> nearbyEnemies)
    {
        if (nearbyEnemies == null || nearbyEnemies.Count == 0)
            return false;

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        Vector2Int bestPosition = player.GridPosition;
        float bestScore = float.MinValue;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int candidate = player.GridPosition + directions[i];
            if (!WorldGrid.Instance.IsWalkable(candidate))
                continue;
            if (WorldGrid.Instance.HasEnemyAt(candidate))
                continue;

            float score = ScoreRetreatPosition(candidate, nearbyEnemies);
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = candidate;
            }
        }

        return bestPosition != player.GridPosition && player.TryMove(bestPosition);
    }

    private float ScoreRetreatPosition(Vector2Int candidate, IReadOnlyList<Enemy> nearbyEnemies)
    {
        float totalDistance = 0f;
        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            if (nearbyEnemies[i] == null || !nearbyEnemies[i].IsAlive())
                continue;
            totalDistance += Manhattan(candidate, nearbyEnemies[i].GridPosition);
        }

        totalDistance -= Manhattan(candidate, GoalTile.Instance.GridPosition) * 0.2f;
        return totalDistance;
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
