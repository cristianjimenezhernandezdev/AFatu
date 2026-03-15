using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private float cellSize = 1f;

    [Header("Scene References")]
    [SerializeField] private GoalTile goalTile;

    private GameObject[,] cells;
    private readonly HashSet<Vector2Int> walls = new();
    private readonly Dictionary<Vector2Int, Enemy> enemies = new();
    private Sprite cachedCellSprite;
    private Transform segmentRoot;

    private int width;
    private int height;
    private MapCardData currentCard;

    public static WorldGrid Instance { get; private set; }

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public MapCardData CurrentCard => currentCard;
    public Vector2Int EntryPosition => currentCard == null ? Vector2Int.zero : new Vector2Int(currentCard.entryX, height / 2);
    public Vector2Int ExitPosition => currentCard == null ? Vector2Int.zero : new Vector2Int(currentCard.exitX, height / 2);

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

    public void GenerateSegment(MapCardData card)
    {
        if (card == null)
        {
            Debug.LogError("WorldGrid.GenerateSegment ha rebut una carta null.");
            return;
        }

        currentCard = card;
        width = card.segmentWidth;
        height = card.segmentHeight;

        ClearCurrentSegment();

        cells = new GameObject[width, height];
        walls.Clear();
        enemies.Clear();

        GenerateCells(card);
        GenerateWalls(card);
        GenerateEnemies(card);
        PlaceGoal(card);
    }

    void GenerateCells(MapCardData card)
    {
        Sprite squareSprite = GetOrCreateCellSprite();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cell = new($"Cell_{x}_{y}");
                cell.transform.SetParent(segmentRoot);
                cell.transform.position = GridToWorld(new Vector2Int(x, y));

                SpriteRenderer spriteRenderer = cell.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = squareSprite;
                spriteRenderer.color = card.floorColor;
                spriteRenderer.sortingOrder = 0;

                cells[x, y] = cell;
            }
        }
    }

    void GenerateWalls(MapCardData card)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new(x, y);

                bool border =
                    x == 0 || y == 0 ||
                    x == width - 1 || y == height - 1;

                if (border)
                {
                    AddWall(pos);
                    continue;
                }

                bool reservedLane =
                    (x == card.entryX && y == height / 2) ||
                    (x == card.exitX && y == height / 2);

                if (reservedLane)
                    continue;

                if (UnityEngine.Random.value < card.obstacleChance)
                {
                    AddWall(pos);
                }
            }
        }
    }

    void GenerateEnemies(MapCardData card)
    {
        if (card.possibleEnemyPrefabs == null || card.possibleEnemyPrefabs.Length == 0)
            return;

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                Vector2Int pos = new(x, y);

                if (!IsWalkable(pos))
                    continue;

                if (UnityEngine.Random.value > card.enemyChance)
                    continue;

                GameObject prefab = card.possibleEnemyPrefabs[UnityEngine.Random.Range(0, card.possibleEnemyPrefabs.Length)];
                if (prefab == null)
                    continue;

                GameObject enemyObject = Instantiate(prefab, segmentRoot);
                Enemy enemy = enemyObject.GetComponent<Enemy>();

                if (enemy == null)
                {
                    Debug.LogError("El prefab d'enemic no te component Enemy.");
                    Destroy(enemyObject);
                    continue;
                }

                enemy.gridPosition = pos;
                enemyObject.transform.position = GridToWorld(pos);
                RegisterEnemy(enemy);
            }
        }
    }

    void PlaceGoal(MapCardData card)
    {
        if (goalTile == null)
        {
            goalTile = FindFirstObjectByType<GoalTile>();
        }

        if (goalTile == null)
        {
            Debug.LogError("No hi ha GoalTile a l'escena.");
            return;
        }

        SetWall(ExitPosition, false);
        goalTile.SetGridPosition(new Vector2Int(card.exitX, height / 2));
    }

    void ClearCurrentSegment()
    {
        EnsureSegmentRoot();

        for (int i = segmentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = segmentRoot.GetChild(i);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        Enemy[] existingEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in existingEnemies)
        {
            if (enemy == null || enemy.transform.IsChildOf(segmentRoot))
                continue;

            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }

        cells = null;
        walls.Clear();
        enemies.Clear();
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
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
        return enemies.TryGetValue(pos, out Enemy enemy) && enemy != null;
    }

    public Enemy GetEnemyAt(Vector2Int pos)
    {
        return enemies.TryGetValue(pos, out Enemy enemy) ? enemy : null;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemies[enemy.gridPosition] = enemy;
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (enemies.TryGetValue(enemy.gridPosition, out Enemy current) && current == enemy)
        {
            enemies.Remove(enemy.gridPosition);
        }
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

    public void ToggleWall(Vector2Int pos)
    {
        SetWall(pos, !HasWallAt(pos));
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

    void AddWall(Vector2Int pos)
    {
        SetWall(pos, true);
    }

    void UpdateCellVisual(Vector2Int pos, bool blocked)
    {
        if (cells == null || !IsInsideGrid(pos))
            return;

        SpriteRenderer spriteRenderer = cells[pos.x, pos.y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = blocked ? currentCard.wallColor : currentCard.floorColor;
    }

    void EnsureSegmentRoot()
    {
        if (segmentRoot != null)
            return;

        Transform existingRoot = transform.Find("GeneratedSegment");
        if (existingRoot != null)
        {
            segmentRoot = existingRoot;
            return;
        }

        GameObject rootObject = new("GeneratedSegment");
        rootObject.transform.SetParent(transform);
        rootObject.transform.localPosition = Vector3.zero;
        segmentRoot = rootObject.transform;
    }

    Sprite GetOrCreateCellSprite()
    {
        if (cachedCellSprite != null)
            return cachedCellSprite;

        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        cachedCellSprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

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

        List<Vector2Int> directions = new(6);
        AddDirectionIfNeeded(directions, primaryDirection);
        AddDirectionIfNeeded(directions, secondaryDirection);

        foreach (Vector2Int direction in CardinalDirections)
        {
            AddDirectionIfNeeded(directions, direction);
        }

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

        List<Vector2Int> openSet = new() { start };
        HashSet<Vector2Int> closedSet = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, int> gScore = new() { [start] = 0 };
        Dictionary<Vector2Int, int> fScore = new() { [start] = Heuristic(start, target) };

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
            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighbor = current + direction;

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
                {
                    openSet.Add(neighbor);
                }
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
        {
            step = previous;
        }

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
