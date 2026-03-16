using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class SegmentGenerator
{
    private readonly IContentRepository contentRepository;
    private readonly IRunRepository runRepository;

    public SegmentGenerator(IContentRepository contentRepository, IRunRepository runRepository)
    {
        this.contentRepository = contentRepository;
        this.runRepository = runRepository;
    }

    public SegmentRuntimeData GenerateSegment(RunSessionData run, string cardId)
    {
        CardSeedData card = contentRepository.GetCard(cardId);
        if (card == null)
            return null;

        BiomeSeedData biome = contentRepository.GetBiome(card.biomeId);
        float difficultyMultiplier = BalanceConfig.GetDifficultyMultiplier(run.currentSegmentIndex, run.targetSegmentCount);
        RunSegmentData segment = runRepository.CreateSegment(run, card, difficultyMultiplier);
        segment.heroHealthOnEnter = run.heroCurrentHealth;

        SegmentRuntimeData runtime = new SegmentRuntimeData
        {
            segment = segment,
            card = card,
            biome = biome,
            floorColor = ParseColor(card.floorColorHex, Color.gray),
            wallColor = ParseColor(card.wallColorHex, Color.black)
        };

        ApplyModifiers(card, runtime);
        runtime.wallPositions = GenerateWalls(runtime);
        runtime.enemySpawns = GenerateEnemySpawns(run, runtime);
        runtime.chestSpawns = GenerateChestSpawns(runtime);

        runRepository.AddEvent(run.runId, segment.runSegmentId, "segment_generated",
            $"{{\"segmentIndex\":{segment.segmentIndex},\"cardId\":\"{card.cardId}\",\"difficulty\":{difficultyMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"chests\":{runtime.chestSpawns.Count}}}");

        return runtime;
    }

    private void ApplyModifiers(CardSeedData card, SegmentRuntimeData runtime)
    {
        IReadOnlyList<CardModifierPoolSeedData> modifierPool = contentRepository.GetCardModifierPool(card.cardId);
        List<CardModifierPoolSeedData> optionalPool = new List<CardModifierPoolSeedData>();

        for (int i = 0; i < modifierPool.Count; i++)
        {
            CardModifierPoolSeedData entry = modifierPool[i];
            WorldModifierSeedData modifier = contentRepository.GetModifier(entry.modifierId);
            if (modifier == null || !modifier.isActive)
                continue;

            if (entry.guaranteed)
                AddModifier(runtime, modifier);
            else
                optionalPool.Add(entry);
        }

        if (optionalPool.Count > 0 && Random.value > 0.35f)
        {
            CardModifierPoolSeedData picked = WeightedSelectionUtility.PickWeighted(optionalPool, item => item.weight);
            WorldModifierSeedData modifier = contentRepository.GetModifier(picked.modifierId);
            AddModifier(runtime, modifier);
        }
    }

    private void AddModifier(SegmentRuntimeData runtime, WorldModifierSeedData modifier)
    {
        if (modifier == null || runtime.appliedModifiers.Any(item => item.modifierId == modifier.modifierId))
            return;

        runtime.appliedModifiers.Add(modifier);
        runtime.segment.appliedModifierIds.Add(modifier.modifierId);

        ModifierEffectConfigData effect = JsonSeedParser.ParseModifierEffect(modifier.effectConfigJson);
        runtime.modifierRuntime.heroSpeedMultiplier *= Mathf.Max(0.25f, effect.heroSpeedMultiplier <= 0f ? 1f : effect.heroSpeedMultiplier);
        runtime.modifierRuntime.heroHealOnEnter += effect.healOnEnter;
        runtime.modifierRuntime.enemyAttackBonus += effect.enemyAttackBonus;
        runtime.modifierRuntime.eliteChanceBonus += effect.eliteChanceBonus;
        runtime.segment.obstacleChance = Mathf.Clamp(runtime.segment.obstacleChance + effect.extraObstacleChance, 0.04f, 0.33f);
    }

    private List<Vector2Int> GenerateWalls(SegmentRuntimeData runtime)
    {
        int width = runtime.segment.segmentWidth;
        int height = runtime.segment.segmentHeight;
        Vector2Int entry = runtime.EntryPosition;
        Vector2Int exit = runtime.ExitPosition;

        for (int attempt = 0; attempt < 18; attempt++)
        {
            HashSet<Vector2Int> walls = new HashSet<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    if (border)
                    {
                        if (pos != entry && pos != exit)
                            walls.Add(pos);
                        continue;
                    }

                    if (pos == entry || pos == exit)
                        continue;

                    if (Mathf.Abs(pos.y - entry.y) <= 1 && (pos.x == entry.x + 1 || pos.x == exit.x - 1))
                        continue;

                    float laneBias = Mathf.Abs(pos.y - entry.y) <= 1 ? 0.55f : 1f;
                    if (Random.value < runtime.segment.obstacleChance * laneBias)
                        walls.Add(pos);
                }
            }

            if (HasPath(width, height, entry, exit, walls))
                return walls.ToList();
        }

        return new List<Vector2Int>();
    }

    private List<GeneratedEnemySpawnData> GenerateEnemySpawns(RunSessionData run, SegmentRuntimeData runtime)
    {
        List<GeneratedEnemySpawnData> spawns = new List<GeneratedEnemySpawnData>();
        List<Vector2Int> candidatePositions = BuildEnemyCandidatePositions(runtime);
        IReadOnlyList<CardEnemyPoolSeedData> pool = contentRepository.GetCardEnemyPool(runtime.card.cardId);

        if (candidatePositions.Count == 0 || pool.Count == 0)
            return spawns;

        Dictionary<string, int> countsByEnemyId = new Dictionary<string, int>();
        int targetCount = Mathf.Min(candidatePositions.Count, BalanceConfig.GetBaseEnemyBudget(runtime.segment.segmentIndex, runtime.card.cardType));
        int guaranteedCount = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            CardEnemyPoolSeedData entry = pool[i];
            countsByEnemyId[entry.enemyId] = 0;
            guaranteedCount += entry.minCount;
        }

        targetCount = Mathf.Max(targetCount, guaranteedCount);

        for (int i = 0; i < pool.Count; i++)
        {
            CardEnemyPoolSeedData entry = pool[i];
            for (int count = 0; count < entry.minCount && spawns.Count < candidatePositions.Count; count++)
            {
                CreateSpawn(runtime, entry.enemyId, countsByEnemyId, candidatePositions, spawns);
            }
        }

        while (spawns.Count < targetCount && candidatePositions.Count > 0)
        {
            List<CardEnemyPoolSeedData> available = pool
                .Where(entry => countsByEnemyId.TryGetValue(entry.enemyId, out int current) && current < entry.maxCount)
                .ToList();

            if (available.Count == 0)
                break;

            CardEnemyPoolSeedData picked = WeightedSelectionUtility.PickWeighted(available, entry => GetEnemyWeight(entry, runtime));
            CreateSpawn(runtime, picked.enemyId, countsByEnemyId, candidatePositions, spawns);
        }

        return spawns;
    }

    private List<GeneratedChestSpawnData> GenerateChestSpawns(SegmentRuntimeData runtime)
    {
        List<GeneratedChestSpawnData> chests = new List<GeneratedChestSpawnData>();
        List<Vector2Int> candidatePositions = BuildChestCandidatePositions(runtime);
        int chestCount = Mathf.Min(candidatePositions.Count, BalanceConfig.GetChestCount(runtime.segment.segmentIndex, runtime.card.cardType));

        for (int i = 0; i < chestCount && candidatePositions.Count > 0; i++)
        {
            int positionIndex = Random.Range(0, candidatePositions.Count);
            Vector2Int position = candidatePositions[positionIndex];
            candidatePositions.RemoveAt(positionIndex);

            bool givesEmerald = Random.value < BalanceConfig.GetChestEmeraldChance(runtime.segment.segmentIndex, runtime.card.cardType);
            int goldAmount = BalanceConfig.GetChestGoldAmount(runtime.segment.segmentIndex, runtime.card.cardType);
            int emeraldAmount = givesEmerald ? 1 : 0;
            string chestTier = goldAmount >= 9 || emeraldAmount > 0 ? "rare" : (goldAmount >= 6 ? "medium" : "small");

            chests.Add(new GeneratedChestSpawnData
            {
                gridPosition = position,
                reward = new ChestRewardRuntimeData
                {
                    gold = goldAmount,
                    emeralds = emeraldAmount,
                    chestTier = chestTier
                }
            });
        }

        return chests;
    }

    private int GetEnemyWeight(CardEnemyPoolSeedData entry, SegmentRuntimeData runtime)
    {
        int weight = Mathf.Max(1, entry.weight);
        if (entry.enemyId == "ghost_elite")
        {
            weight += Mathf.RoundToInt(runtime.modifierRuntime.eliteChanceBonus * 20f);
            if (runtime.card.cardType == "risky")
                weight += 2;
        }

        return weight;
    }

    private void CreateSpawn(SegmentRuntimeData runtime, string enemyId, IDictionary<string, int> countsByEnemyId, IList<Vector2Int> candidatePositions, IList<GeneratedEnemySpawnData> spawns)
    {
        if (candidatePositions.Count == 0)
            return;

        EnemyArchetypeSeedData archetype = contentRepository.GetEnemy(enemyId);
        if (archetype == null || !archetype.isActive)
            return;

        int positionIndex = Random.Range(0, candidatePositions.Count);
        Vector2Int position = candidatePositions[positionIndex];
        candidatePositions.RemoveAt(positionIndex);

        EnemyBehaviorConfigData behavior = JsonSeedParser.ParseEnemyBehavior(archetype.behaviorConfigJson);
        EnemyRewardConfigData reward = JsonSeedParser.ParseEnemyReward(archetype.rewardConfigJson);

        int spawnedHealth = Mathf.Max(1, Mathf.RoundToInt(archetype.maxHealth * runtime.segment.difficultyMultiplier));
        int spawnedAttack = Mathf.Max(1, Mathf.RoundToInt(archetype.attack * runtime.segment.difficultyMultiplier)) + runtime.modifierRuntime.enemyAttackBonus;
        int spawnedDefense = archetype.defense;
        float spawnedSpeed = archetype.speed;
        string metadataJson =
            $"{{\"movementPattern\":\"{archetype.movementPattern}\",\"ranged\":{behavior.ranged.ToString().ToLowerInvariant()},\"range\":{behavior.range},\"rewardGold\":{reward.gold}}}";

        RunSegmentEnemyData segmentEnemy = runRepository.AddSegmentEnemy(
            runtime.segment.runSegmentId,
            enemyId,
            position.x,
            position.y,
            spawnedHealth,
            spawnedAttack,
            spawnedDefense,
            spawnedSpeed,
            metadataJson);

        countsByEnemyId[enemyId] = countsByEnemyId.TryGetValue(enemyId, out int current) ? current + 1 : 1;
        spawns.Add(new GeneratedEnemySpawnData
        {
            archetype = archetype,
            segmentEnemy = segmentEnemy,
            gridPosition = position,
            behavior = behavior,
            reward = reward
        });
    }

    private List<Vector2Int> BuildEnemyCandidatePositions(SegmentRuntimeData runtime)
    {
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(runtime.wallPositions);
        List<Vector2Int> candidates = new List<Vector2Int>();
        int width = runtime.segment.segmentWidth;
        int height = runtime.segment.segmentHeight;

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (wallSet.Contains(pos))
                    continue;
                if (pos == runtime.EntryPosition || pos == runtime.ExitPosition)
                    continue;
                if (Mathf.Abs(pos.x - runtime.EntryPosition.x) + Mathf.Abs(pos.y - runtime.EntryPosition.y) <= 2)
                    continue;
                if (Mathf.Abs(pos.x - runtime.ExitPosition.x) + Mathf.Abs(pos.y - runtime.ExitPosition.y) <= 1)
                    continue;

                candidates.Add(pos);
            }
        }

        return candidates;
    }

    private List<Vector2Int> BuildChestCandidatePositions(SegmentRuntimeData runtime)
    {
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>(runtime.wallPositions);
        HashSet<Vector2Int> enemySet = new HashSet<Vector2Int>(runtime.enemySpawns.Select(item => item.gridPosition));
        List<Vector2Int> candidates = new List<Vector2Int>();
        int width = runtime.segment.segmentWidth;
        int height = runtime.segment.segmentHeight;

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (wallSet.Contains(pos) || enemySet.Contains(pos))
                    continue;
                if (pos == runtime.EntryPosition || pos == runtime.ExitPosition)
                    continue;
                if (Mathf.Abs(pos.x - runtime.EntryPosition.x) + Mathf.Abs(pos.y - runtime.EntryPosition.y) <= 1)
                    continue;
                if (Mathf.Abs(pos.x - runtime.ExitPosition.x) + Mathf.Abs(pos.y - runtime.ExitPosition.y) <= 1)
                    continue;

                candidates.Add(pos);
            }
        }

        return candidates;
    }

    private static bool HasPath(int width, int height, Vector2Int start, Vector2Int target, HashSet<Vector2Int> walls)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start };
        queue.Enqueue(start);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == target)
                return true;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                    continue;
                if (visited.Contains(next) || walls.Contains(next))
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static Color ParseColor(string htmlColor, Color fallback)
    {
        return BalanceConfig.TryParseHtmlColor(htmlColor, out Color parsed) ? parsed : fallback;
    }
}
