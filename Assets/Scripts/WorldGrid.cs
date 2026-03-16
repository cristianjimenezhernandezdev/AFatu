using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private float cellSize = 1f;

    [Header("Scene References")]
    [SerializeField] private GoalTile goalTile;

    private GameObject[,] cells;
    private readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, Enemy> enemies = new Dictionary<Vector2Int, Enemy>();
    private readonly Dictionary<Vector2Int, Chest> chests = new Dictionary<Vector2Int, Chest>();
    private Sprite cachedCellSprite;
    private Transform segmentRoot;
    private SegmentRuntimeData currentSegment;

    public static WorldGrid Instance { get; private set; }

    public int Width => currentSegment?.segment?.segmentWidth ?? 0;
    public int Height => currentSegment?.segment?.segmentHeight ?? 0;
    public float CellSize => cellSize;
    public SegmentRuntimeData CurrentSegment => currentSegment;
    public Vector2Int EntryPosition => currentSegment == null ? Vector2Int.zero : currentSegment.EntryPosition;
    public Vector2Int ExitPosition => currentSegment == null ? Vector2Int.zero : currentSegment.ExitPosition;
    public IReadOnlyList<Enemy> ActiveEnemies => enemies.Values.Where(enemy => enemy != null && enemy.IsAlive()).ToArray();
    public IReadOnlyList<Chest> ActiveChests => chests.Values.Where(chest => chest != null && !chest.IsOpened).ToArray();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSegmentRoot();
    }

    public void GenerateSegment(SegmentRuntimeData runtime, GameObject enemyTemplate)
    {
        if (runtime == null || runtime.segment == null)
        {
            Debug.LogError("WorldGrid.GenerateSegment ha rebut un segment invalid.");
            return;
        }

        currentSegment = runtime;
        ClearCurrentSegment();

        cells = new GameObject[Width, Height];
        walls.Clear();
        enemies.Clear();
        chests.Clear();

        GenerateCells(runtime);
        ApplyWalls(runtime.wallPositions);
        GenerateEnemies(runtime, enemyTemplate);
        GenerateChests(runtime);
        PlaceGoal(runtime);
        CenterCameraOnSegment();
    }

    private void GenerateCells(SegmentRuntimeData runtime)
    {
        Sprite squareSprite = GetOrCreateCellSprite();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                GameObject cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.SetParent(segmentRoot);
                cell.transform.position = GridToWorld(new Vector2Int(x, y));

                SpriteRenderer spriteRenderer = cell.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = squareSprite;
                spriteRenderer.sortingOrder = 0;

                ProceduralEnvironmentFactory.ApplyCellVisual(cell, spriteRenderer, runtime.card.biomeId, runtime.floorColor, runtime.wallColor, new Vector2Int(x, y), false);
                cells[x, y] = cell;
            }
        }
    }

    private void ApplyWalls(IReadOnlyList<Vector2Int> wallPositions)
    {
        if (wallPositions == null)
            return;

        for (int i = 0; i < wallPositions.Count; i++)
            SetWall(wallPositions[i], true);
    }

    private void GenerateEnemies(SegmentRuntimeData runtime, GameObject enemyTemplate)
    {
        if (runtime.enemySpawns == null || runtime.enemySpawns.Count == 0)
            return;

        for (int i = 0; i < runtime.enemySpawns.Count; i++)
        {
            GeneratedEnemySpawnData spawn = runtime.enemySpawns[i];
            GameObject enemyObject = enemyTemplate != null ? Instantiate(enemyTemplate, segmentRoot) : CreateFallbackEnemyObject();
            enemyObject.name = $"Enemy_{spawn.archetype.enemyId}_{i}";

            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy == null)
            {
                Destroy(enemyObject);
                continue;
            }

            enemy.ConfigureFromSpawn(spawn);
            RegisterEnemy(enemy);
            enemyObject.SetActive(true);
        }
    }

    private void GenerateChests(SegmentRuntimeData runtime)
    {
        if (runtime.chestSpawns == null || runtime.chestSpawns.Count == 0)
            return;

        for (int i = 0; i < runtime.chestSpawns.Count; i++)
        {
            GameObject chestObject = CreateFallbackChestObject();
            chestObject.transform.SetParent(segmentRoot, false);
            chestObject.name = $"Chest_{runtime.chestSpawns[i].reward.chestTier}_{i}";

            Chest chest = chestObject.GetComponent<Chest>();
            chest.Configure(runtime.chestSpawns[i]);
            RegisterChest(chest);
            chestObject.SetActive(true);
        }
    }

    private GameObject CreateFallbackEnemyObject()
    {
        GameObject enemyObject = new GameObject("RuntimeEnemy");
        enemyObject.transform.SetParent(segmentRoot);
        SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
        renderer.sortingOrder = 10;
        enemyObject.AddComponent<Enemy>();
        enemyObject.AddComponent<EnemyGridMovement>();
        enemyObject.AddComponent<ProceduralEnemyRenderer>();
        enemyObject.SetActive(false);
        return enemyObject;
    }

    private GameObject CreateFallbackChestObject()
    {
        GameObject chestObject = new GameObject("RuntimeChest");
        SpriteRenderer renderer = chestObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
        renderer.sortingOrder = 9;
        chestObject.AddComponent<Chest>();
        chestObject.AddComponent<ProceduralChestRenderer>();
        chestObject.SetActive(false);
        return chestObject;
    }

    private void PlaceGoal(SegmentRuntimeData runtime)
    {
        if (goalTile == null)
            goalTile = FindFirstObjectByType<GoalTile>();

        if (goalTile == null)
        {
            Debug.LogError("No hi ha GoalTile a l'escena.");
            return;
        }

        SetWall(runtime.ExitPosition, false);
        goalTile.SetGridPosition(runtime.ExitPosition);
    }


    private void CenterCameraOnSegment()
    {
        Camera camera = Camera.main;
        if (camera == null || Width <= 0 || Height <= 0)
            return;

        Vector3 position = camera.transform.position;
        position.x = (Width - 1) * cellSize * 0.5f;
        position.y = (Height - 1) * cellSize * 0.5f;
        camera.transform.position = position;

        if (!camera.orthographic)
            return;

        float margin = 1.75f;
        float heightHalf = Height * cellSize * 0.5f + margin;
        float widthHalf = Width * cellSize * 0.5f / Mathf.Max(0.1f, camera.aspect) + margin;
        camera.orthographicSize = Mathf.Max(heightHalf, widthHalf);
    }
    private void ClearCurrentSegment()
    {
        EnsureSegmentRoot();

        for (int i = segmentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = segmentRoot.GetChild(i);
            Destroy(child.gameObject);
        }

        cells = null;
        walls.Clear();
        enemies.Clear();
        chests.Clear();
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
    }

    public bool IsWalkable(Vector2Int pos)
    {
        return IsInsideGrid(pos) && !walls.Contains(pos);
    }

    public bool HasWallAt(Vector2Int pos)
    {
        return walls.Contains(pos);
    }

    public bool HasEnemyAt(Vector2Int pos)
    {
        return enemies.TryGetValue(pos, out Enemy enemy) && enemy != null && enemy.IsAlive();
    }

    public Enemy GetEnemyAt(Vector2Int pos)
    {
        return enemies.TryGetValue(pos, out Enemy enemy) && enemy != null && enemy.IsAlive() ? enemy : null;
    }

    public bool HasChestAt(Vector2Int pos)
    {
        return chests.TryGetValue(pos, out Chest chest) && chest != null && !chest.IsOpened;
    }

    public Chest GetChestAt(Vector2Int pos)
    {
        return chests.TryGetValue(pos, out Chest chest) && chest != null && !chest.IsOpened ? chest : null;
    }

    public IReadOnlyList<Enemy> GetEnemiesInRadius(Vector2Int center, float radius)
    {
        List<Enemy> result = new List<Enemy>();
        foreach (Enemy enemy in enemies.Values)
        {
            if (enemy == null || !enemy.IsAlive())
                continue;

            float distance = Mathf.Abs(enemy.GridPosition.x - center.x) + Mathf.Abs(enemy.GridPosition.y - center.y);
            if (distance <= radius)
                result.Add(enemy);
        }

        return result;
    }

    public IReadOnlyList<Chest> GetChestsInRadius(Vector2Int center, float radius)
    {
        List<Chest> result = new List<Chest>();
        foreach (Chest chest in chests.Values)
        {
            if (chest == null || chest.IsOpened)
                continue;

            float distance = Mathf.Abs(chest.GridPosition.x - center.x) + Mathf.Abs(chest.GridPosition.y - center.y);
            if (distance <= radius)
                result.Add(chest);
        }

        return result;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemies[enemy.GridPosition] = enemy;
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (enemies.TryGetValue(enemy.GridPosition, out Enemy current) && current == enemy)
            enemies.Remove(enemy.GridPosition);
    }

    public void RegisterChest(Chest chest)
    {
        if (chest == null)
            return;

        chests[chest.GridPosition] = chest;
    }

    public void RemoveChest(Chest chest)
    {
        if (chest == null)
            return;

        if (chests.TryGetValue(chest.GridPosition, out Chest current) && current == chest)
            chests.Remove(chest.GridPosition);
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int y = Mathf.RoundToInt(worldPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    public bool TryGetNextPathStep(Vector2Int start, Vector2Int target, Func<Vector2Int, bool> extraBlocked, out Vector2Int nextStep)
    {
        return GridPathfinding.TryGetNextStep(this, start, target, extraBlocked, out nextStep);
    }

    public bool SetWall(Vector2Int pos, bool blocked)
    {
        if (!IsInsideGrid(pos))
            return false;

        if (blocked)
            walls.Add(pos);
        else
            walls.Remove(pos);

        UpdateCellVisual(pos, blocked);
        return true;
    }

    public void ToggleWall(Vector2Int pos)
    {
        SetWall(pos, !HasWallAt(pos));
    }

    private void UpdateCellVisual(Vector2Int pos, bool blocked)
    {
        if (cells == null || !IsInsideGrid(pos))
            return;

        SpriteRenderer spriteRenderer = cells[pos.x, pos.y].GetComponent<SpriteRenderer>();
        ProceduralEnvironmentFactory.ApplyCellVisual(cells[pos.x, pos.y], spriteRenderer, currentSegment.card.biomeId, currentSegment.floorColor, currentSegment.wallColor, pos, blocked);
    }

    private void EnsureSegmentRoot()
    {
        if (segmentRoot != null)
            return;

        Transform existingRoot = transform.Find("GeneratedSegment");
        if (existingRoot != null)
        {
            segmentRoot = existingRoot;
            return;
        }

        GameObject rootObject = new GameObject("GeneratedSegment");
        rootObject.transform.SetParent(transform);
        rootObject.transform.localPosition = Vector3.zero;
        segmentRoot = rootObject.transform;
    }

    private Sprite GetOrCreateCellSprite()
    {
        if (cachedCellSprite != null)
            return cachedCellSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        cachedCellSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return cachedCellSprite;
    }
}

public static class GridDirectionUtility
{
    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public static IReadOnlyList<Vector2Int> BuildPrioritizedDirections(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        Vector2Int primaryDirection;
        Vector2Int secondaryDirection;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            primaryDirection = new Vector2Int(delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1), 0);
            secondaryDirection = new Vector2Int(0, delta.y == 0 ? 0 : (delta.y > 0 ? 1 : -1));
        }
        else
        {
            primaryDirection = new Vector2Int(0, delta.y == 0 ? 0 : (delta.y > 0 ? 1 : -1));
            secondaryDirection = new Vector2Int(delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1), 0);
        }

        List<Vector2Int> directions = new List<Vector2Int>(6);
        AddDirectionIfNeeded(directions, primaryDirection);
        AddDirectionIfNeeded(directions, secondaryDirection);

        for (int i = 0; i < CardinalDirections.Length; i++)
            AddDirectionIfNeeded(directions, CardinalDirections[i]);

        return directions;
    }

    private static void AddDirectionIfNeeded(List<Vector2Int> directions, Vector2Int direction)
    {
        if (direction == Vector2Int.zero || directions.Contains(direction))
            return;

        directions.Add(direction);
    }
}

public static class GridPathfinding
{
    public static bool TryGetNextStep(WorldGrid worldGrid, Vector2Int start, Vector2Int target, Func<Vector2Int, bool> extraBlocked, out Vector2Int nextStep)
    {
        nextStep = start;
        if (worldGrid == null || start == target)
            return false;

        List<Vector2Int> openSet = new List<Vector2Int> { start };
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        Dictionary<Vector2Int, int> fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, target) };

        while (openSet.Count > 0)
        {
            Vector2Int current = GetBestNode(openSet, fScore, target);
            if (current == target)
            {
                nextStep = ReconstructFirstStep(cameFrom, start, current);
                return nextStep != start;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            IReadOnlyList<Vector2Int> directions = GridDirectionUtility.BuildPrioritizedDirections(current, target);
            for (int i = 0; i < directions.Count; i++)
            {
                Vector2Int neighbor = current + directions[i];
                if (closedSet.Contains(neighbor))
                    continue;

                bool canStandOnNeighbor = neighbor == target || worldGrid.IsWalkable(neighbor);
                if (!canStandOnNeighbor)
                    continue;
                if (neighbor != target && extraBlocked != null && extraBlocked(neighbor))
                    continue;

                int tentativeGScore = gScore[current] + 1;
                bool isBetterPath = !gScore.TryGetValue(neighbor, out int knownGScore) || tentativeGScore < knownGScore;
                if (!isBetterPath)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + Heuristic(neighbor, target);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        return false;
    }

    private static Vector2Int GetBestNode(List<Vector2Int> openSet, Dictionary<Vector2Int, int> fScore, Vector2Int target)
    {
        Vector2Int bestNode = openSet[0];
        int bestScore = GetScore(fScore, bestNode);
        int bestHeuristic = Heuristic(bestNode, target);

        for (int i = 1; i < openSet.Count; i++)
        {
            Vector2Int candidate = openSet[i];
            int candidateScore = GetScore(fScore, candidate);
            int candidateHeuristic = Heuristic(candidate, target);
            if (candidateScore < bestScore || (candidateScore == bestScore && candidateHeuristic < bestHeuristic))
            {
                bestNode = candidate;
                bestScore = candidateScore;
                bestHeuristic = candidateHeuristic;
            }
        }

        return bestNode;
    }

    private static Vector2Int ReconstructFirstStep(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int current)
    {
        Vector2Int step = current;
        while (cameFrom.TryGetValue(step, out Vector2Int previous) && previous != start)
            step = previous;

        return step;
    }

    private static int GetScore(Dictionary<Vector2Int, int> scores, Vector2Int node)
    {
        return scores.TryGetValue(node, out int score) ? score : int.MaxValue;
    }

    private static int Heuristic(Vector2Int from, Vector2Int to)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }
}

