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
        "....BBBB....",
        "...BEEEEB...",
        "...BEEEEB...",
        "..BB....BB..",
        "..BBBBBBBB..",
        "...B.BB.B...",
        "..BDBBBBDB..",
        "...B.BB.B...",
        "..BB....BB..",
        ".B..B..B..B."
    };

    private static readonly string[] SkeletonStrideShape =
    {
        "....BBBB....",
        "...BEEEEB...",
        "...BEEEEB...",
        "..BB....BB..",
        "..BBBBBBBB..",
        "...B.BB.B...",
        "..BDBBBBDB..",
        "...B.BB.B...",
        ".BB......B..",
        "B...B..B...B"
    };

    private static readonly string[] BatWingUpShape =
    {
        "WW........WW",
        ".WWW....WWW.",
        "..WWPPPPWW..",
        ".WWBBBBBBWW.",
        "WWBBEEEEBBWW",
        ".WBBBBBBBBW.",
        "..WBBBBBBW..",
        ".WW......WW.",
        "WW........WW",
        "..W......W.."
    };

    private static readonly string[] BatWingDownShape =
    {
        "...WWWW.....",
        ".WWPPPPWW...",
        "WWBBBBBBWW..",
        "WBBEEEEBBWW.",
        ".WBBBBBBBBW.",
        "..WBBBBBBW..",
        ".WW....WW...",
        "WW......WW..",
        "W........WW.",
        ".WW....WW..."
    };

    private static readonly string[] ZombieIdleShape =
    {
        "....HHHH....",
        "...HFFFFH...",
        "..HHFEEFHH..",
        ".HHCCCCCCHH.",
        ".HCCCCCCCCH.",
        ".HSCCCCCCSH.",
        "..HCCLLCCH..",
        "..HCLLLLCH..",
        ".LL......LL.",
        "L..L....L..L"
    };

    private static readonly string[] ZombieStrideShape =
    {
        "....HHHH....",
        "...HFFFFH...",
        "..HHFEEFHH..",
        ".HHCCCCCCHH.",
        ".HCCCCCCCCH.",
        ".HSCCCCCCSH.",
        "..HCCLLCCH..",
        "..HCLLLLCH..",
        "LL.......L..",
        "..L.L..L...L"
    };

    private static readonly string[] GhostPulseShape =
    {
        "....GGGG....",
        "...GGEEGG...",
        "..GGCCCCGG..",
        ".GGCCPPCCGG.",
        ".GCCPPPPCCG.",
        ".GCCPPPPCCG.",
        ".GGCCPPCCGG.",
        "..GGCCCCGG..",
        "...GG..GG...",
        "..G......G.."
    };

    private static readonly string[] GhostFlareShape =
    {
        "...GGGGGG...",
        "..GGEEEEGG..",
        ".GGCCCCCCGG.",
        "GGCCPPPPCCGG",
        "GCCPPPPPPCCG",
        "GCCPPPPPPCCG",
        "GGCCPPPPCCGG",
        ".GGCCCCCCGG.",
        "..GGG..GGG..",
        ".GG......GG."
    };

    [SerializeField] private EnemyVisualType visualType = EnemyVisualType.Skeleton;
    [SerializeField] private float pixelSize = 0.075f;
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
            GameObject pixel = new GameObject($"Proc_EnemyPixel_{generatedPixels.Count}");
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
            if (generatedPixels[i] != null)
                generatedPixels[i].SetActive(false);
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
                    case 'W': color = new Color32(67, 51, 94, 255); return true;
                    case 'P': color = new Color32(142, 102, 135, 255); return true;
                    case 'B': color = new Color32(35, 26, 53, 255); return true;
                    case 'E': color = new Color32(230, 84, 124, 255); return true;
                }
                break;
            case EnemyVisualType.Zombie:
                switch (cell)
                {
                    case 'H': color = new Color32(116, 154, 86, 255); return true;
                    case 'F': color = new Color32(88, 117, 62, 255); return true;
                    case 'E': color = new Color32(238, 116, 92, 255); return true;
                    case 'C': color = new Color32(94, 81, 109, 255); return true;
                    case 'S': color = new Color32(58, 48, 67, 255); return true;
                    case 'L': color = new Color32(92, 71, 52, 255); return true;
                }
                break;
            case EnemyVisualType.GhostElite:
                switch (cell)
                {
                    case 'G': color = new Color32(163, 196, 245, 220); return true;
                    case 'E': color = new Color32(255, 240, 165, 255); return true;
                    case 'C': color = new Color32(106, 129, 215, 220); return true;
                    case 'P': color = new Color32(191, 228, 255, 240); return true;
                }
                break;
            case EnemyVisualType.Skeleton:
            default:
                switch (cell)
                {
                    case 'B': color = new Color32(226, 222, 208, 255); return true;
                    case 'D': color = new Color32(122, 114, 104, 255); return true;
                    case 'E': color = new Color32(127, 211, 245, 255); return true;
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
            if (shape[i].Length != width)
                return false;

        return true;
    }
}
