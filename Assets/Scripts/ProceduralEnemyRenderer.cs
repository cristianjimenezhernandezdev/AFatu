using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class ProceduralEnemyRenderer : MonoBehaviour
{
    public enum EnemyVisualType
    {
        Skeleton,
        Bat,
        Zombie,
        GhostElite
    }

    private static readonly string[] SkeletonIdleShape =
    {
        "..BBBB..",
        ".BEEEEB.",
        ".BBBBBB.",
        "..B..B..",
        ".DBB.BD.",
        "..B..B..",
        ".B....B.",
        "..B..B.."
    };

    private static readonly string[] SkeletonStrideShape =
    {
        "..BBBB..",
        ".BEEEEB.",
        ".BBBBBB.",
        "..B..B..",
        ".DBB.BD.",
        "..B..B..",
        "BB....B.",
        "B..B...."
    };

    private static readonly string[] BatWingUpShape =
    {
        "W......W",
        ".WW..WW.",
        "..WPPW..",
        ".WBBBBW.",
        "WBBEEBBW",
        ".WBBBBW.",
        "..W..W..",
        "...WW..."
    };

    private static readonly string[] BatWingDownShape =
    {
        "...WW...",
        "..WPPW..",
        ".WBBBBW.",
        "WBBEEBBW",
        ".WBBBBW.",
        "..WBBW..",
        ".W....W.",
        "W......W"
    };

    private static readonly string[] ZombieIdleShape =
    {
        "..HHHH..",
        ".HFFFFH.",
        ".HHEEHH.",
        "..CCCC..",
        ".SCCCCS.",
        "..C..C..",
        ".L....L.",
        "L......L"
    };

    private static readonly string[] ZombieStrideShape =
    {
        "..HHHH..",
        ".HFFFFH.",
        ".HHEEHH.",
        "..CCCC..",
        ".SCCCCS.",
        "..C..C..",
        "LL....L.",
        "..L..L.."
    };

    private static readonly string[] GhostPulseShape =
    {
        "...GG...",
        "..GEEG..",
        ".GGCCGG.",
        ".GCCCCG.",
        ".GCCCCG.",
        ".GGCCGG.",
        "..G..G..",
        ".G....G."
    };

    private static readonly string[] GhostFlareShape =
    {
        "..GGGG..",
        ".GGEEGG.",
        "GGCCCCGG",
        ".GCCCCG.",
        ".GCCCCG.",
        "GGCCCCGG",
        ".GG..GG.",
        "G......G"
    };

    [SerializeField] private EnemyVisualType visualType = EnemyVisualType.Skeleton;
    [SerializeField] private float pixelSize = 0.1f;
    [SerializeField] private int sortingOrder = 12;
    [SerializeField] private float animationStepDuration = 0.16f;

    private readonly List<GameObject> generatedPixels = new List<GameObject>();
    private readonly List<SpriteRenderer> generatedPixelRenderers = new List<SpriteRenderer>();

    private SpriteRenderer baseSpriteRenderer;
    private Enemy enemy;
    private Vector3 lastObservedPosition;
    private float animationTimer;
    private bool hasObservedPosition;
    private bool stridePhase;

    public EnemyVisualType VisualType => visualType;

    void Awake()
    {
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        enemy = GetComponent<Enemy>();
        HideBaseSprite();
    }

    void Start()
    {
        lastObservedPosition = transform.position;
        hasObservedPosition = true;
        RefreshVisual(true);
    }

    void OnValidate()
    {
        pixelSize = Mathf.Max(0.04f, pixelSize);
        animationStepDuration = Mathf.Max(0.06f, animationStepDuration);
    }

    void LateUpdate()
    {
        if (enemy == null || !enemy.IsAlive())
            return;

        if (!hasObservedPosition)
        {
            lastObservedPosition = transform.position;
            hasObservedPosition = true;
        }

        UpdateAnimation();
        lastObservedPosition = transform.position;
    }

    public void SetVisualType(EnemyVisualType newVisualType, bool forceRefresh = false)
    {
        if (!forceRefresh && visualType == newVisualType)
            return;

        visualType = newVisualType;
        RefreshVisual(true);
    }

    public void SetVisualByEnemyId(string enemyId, bool forceRefresh = false)
    {
        EnemyVisualType newType = enemyId switch
        {
            "bat" => EnemyVisualType.Bat,
            "zombie" => EnemyVisualType.Zombie,
            "ghost_elite" => EnemyVisualType.GhostElite,
            _ => EnemyVisualType.Skeleton
        };

        SetVisualType(newType, forceRefresh);
    }

    private void UpdateAnimation()
    {
        float distance = Vector3.Distance(transform.position, lastObservedPosition);
        bool isMoving = distance > 0.001f;

        animationTimer += Time.deltaTime;
        if (animationTimer >= animationStepDuration)
        {
            animationTimer = 0f;
            stridePhase = !stridePhase;
            RefreshVisual();
            return;
        }

        if (!isMoving && (visualType == EnemyVisualType.Skeleton || visualType == EnemyVisualType.Zombie) && stridePhase)
        {
            stridePhase = false;
            RefreshVisual();
        }
    }

    private void RefreshVisual(bool forceRefresh = false)
    {
        HideBaseSprite();
        ApplyShape(GetShape(forceRefresh));
    }

    private string[] GetShape(bool forceRefresh)
    {
        switch (visualType)
        {
            case EnemyVisualType.Bat:
                return stridePhase || forceRefresh ? BatWingUpShape : BatWingDownShape;
            case EnemyVisualType.Zombie:
                return stridePhase && !forceRefresh ? ZombieStrideShape : ZombieIdleShape;
            case EnemyVisualType.GhostElite:
                return stridePhase && !forceRefresh ? GhostFlareShape : GhostPulseShape;
            case EnemyVisualType.Skeleton:
            default:
                return stridePhase && !forceRefresh ? SkeletonStrideShape : SkeletonIdleShape;
        }
    }

    private void ApplyShape(string[] shape)
    {
        if (!TryGetShapeSize(shape, out int width, out int height))
        {
            SetInactiveFrom(0);
            return;
        }

        float scaledPixelSize = GetScaledPixelSize();
        float halfWidth = (width - 1) * 0.5f;
        float halfHeight = (height - 1) * 0.5f;
        int pixelIndex = 0;

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                char cell = shape[row][column];
                if (!TryResolvePixelColor(cell, out Color color))
                    continue;

                EnsurePixelPoolSize(pixelIndex + 1);

                GameObject pixel = generatedPixels[pixelIndex];
                SpriteRenderer spriteRenderer = generatedPixelRenderers[pixelIndex];
                pixel.SetActive(true);
                pixel.transform.localPosition = new Vector3((column - halfWidth) * scaledPixelSize, (halfHeight - row) * scaledPixelSize, 0f);
                pixel.transform.localScale = Vector3.one * scaledPixelSize;
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder = sortingOrder;
                pixelIndex++;
            }
        }

        SetInactiveFrom(pixelIndex);
    }

    private void EnsurePixelPoolSize(int requiredCount)
    {
        while (generatedPixels.Count < requiredCount)
        {
            int index = generatedPixels.Count;
            GameObject pixel = new GameObject($"Proc_EnemyPixel_{index}");
            pixel.transform.SetParent(transform, false);
            pixel.SetActive(false);

            SpriteRenderer spriteRenderer = pixel.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
            spriteRenderer.sortingOrder = sortingOrder;

            generatedPixels.Add(pixel);
            generatedPixelRenderers.Add(spriteRenderer);
        }
    }

    private void SetInactiveFrom(int startIndex)
    {
        for (int i = startIndex; i < generatedPixels.Count; i++)
        {
            if (generatedPixels[i] != null)
                generatedPixels[i].SetActive(false);
        }
    }

    private void HideBaseSprite()
    {
        if (baseSpriteRenderer != null)
            baseSpriteRenderer.enabled = false;
    }

    private float GetScaledPixelSize()
    {
        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        return pixelSize * cellSize;
    }

    private bool TryResolvePixelColor(char cell, out Color color)
    {
        switch (visualType)
        {
            case EnemyVisualType.Bat:
                switch (cell)
                {
                    case 'W': color = new Color32(72, 55, 104, 255); return true;
                    case 'P': color = new Color32(137, 87, 126, 255); return true;
                    case 'B': color = new Color32(36, 24, 52, 255); return true;
                    case 'E': color = new Color32(226, 83, 124, 255); return true;
                }
                break;
            case EnemyVisualType.Zombie:
                switch (cell)
                {
                    case 'H': color = new Color32(121, 155, 92, 255); return true;
                    case 'F': color = new Color32(92, 121, 69, 255); return true;
                    case 'E': color = new Color32(236, 110, 88, 255); return true;
                    case 'C': color = new Color32(92, 78, 108, 255); return true;
                    case 'S': color = new Color32(68, 57, 78, 255); return true;
                    case 'L': color = new Color32(88, 69, 52, 255); return true;
                }
                break;
            case EnemyVisualType.GhostElite:
                switch (cell)
                {
                    case 'G': color = new Color32(154, 188, 236, 220); return true;
                    case 'E': color = new Color32(255, 238, 157, 255); return true;
                    case 'C': color = new Color32(110, 132, 214, 210); return true;
                }
                break;
            case EnemyVisualType.Skeleton:
            default:
                switch (cell)
                {
                    case 'B': color = new Color32(224, 220, 206, 255); return true;
                    case 'D': color = new Color32(118, 111, 102, 255); return true;
                    case 'E': color = new Color32(125, 208, 239, 255); return true;
                }
                break;
        }

        color = default;
        return false;
    }

    private static bool TryGetShapeSize(string[] shape, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (shape == null || shape.Length == 0)
            return false;

        width = shape[0].Length;
        height = shape.Length;
        if (width == 0)
            return false;

        for (int i = 1; i < shape.Length; i++)
        {
            if (shape[i].Length != width)
                return false;
        }

        return true;
    }
}
