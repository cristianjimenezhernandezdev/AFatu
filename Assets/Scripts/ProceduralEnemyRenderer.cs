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
        "...OBBBB....",
        "..OBEEEEBO..",
        "..OBEDEEBO..",
        ".OBB....BBO.",
        ".OBBBBBBBBO.",
        "..OB.BB.BO..",
        ".OBDBBBBDBO.",
        "..OBBBBBBO..",
        ".OB.B..B.BO.",
        ".BB......BB.",
        "BB..B..B..BB",
        "..D......D.."
    };

    private static readonly string[] SkeletonStrideShape =
    {
        "...OBBBB....",
        "..OBEEEEBO..",
        "..OBEDEEBO..",
        ".OBB....BBO.",
        ".OBBBBBBBBO.",
        "..OB.BB.BO..",
        ".OBDBBBBDBO.",
        "..OBBBBBBO..",
        "OBB......BO.",
        "BB...B..B..B",
        "B......B...B",
        "..D......D.."
    };

    private static readonly string[] BatWingUpShape =
    {
        "WWO....OWW..",
        ".WWWOOWWW...",
        "..WWPPPPWW..",
        ".WWBBBBBBWW.",
        "WWBBEOOEBBWW",
        ".WBBBBBBBBW.",
        "..WBOBBBOW..",
        ".WWBB..BBWW.",
        "WW........WW",
        ".W........W."
    };

    private static readonly string[] BatWingDownShape =
    {
        "...WWWW.....",
        ".WWPPPPWW...",
        "WWBBBBBBWW..",
        "WBBEOOOEBBWW",
        ".WBBBBBBBBW.",
        "..WBOBBBOW..",
        ".WWBBBBWW...",
        "WW......WW..",
        "W........WW.",
        ".WW....WW..."
    };

    private static readonly string[] ZombieIdleShape =
    {
        "...OHHHHO...",
        "..OHFFFFHO..",
        ".OHHFEEFHHO.",
        ".OHCCCCCCHO.",
        ".HCCCOOCCCH.",
        ".HSCCCCCCSH.",
        ".OHCCLLCCHO.",
        ".OHCLLLLCHO.",
        ".OLL....LLO.",
        "LL......LLL.",
        "L..L....L..L",
        "..D......D.."
    };

    private static readonly string[] ZombieStrideShape =
    {
        "...OHHHHO...",
        "..OHFFFFHO..",
        ".OHHFEEFHHO.",
        ".OHCCCCCCHO.",
        ".HCCCOOCCCH.",
        ".HSCCCCCCSH.",
        ".OHCCLLCCHO.",
        ".OHCLLLLCHO.",
        "LLL......LO.",
        "..L.L..L..LL",
        ".L......L...",
        "..D......D.."
    };

    private static readonly string[] GhostPulseShape =
    {
        "...OGGGGO...",
        "..GGEEEOGG..",
        ".GGCCPPCCGG.",
        ".GCCPPPPCCG.",
        "GGCPPPPPPCCG",
        "GGCPPPPPPCCG",
        ".GCCPPPPCCG.",
        ".GGCCPPCCGG.",
        "..GGCCCCGG..",
        "...GG..GG...",
        "..GG....GG..",
        "...G....G..."
    };

    private static readonly string[] GhostFlareShape =
    {
        "..OGGGGGGO..",
        ".GGEEEEEEGG.",
        "GGCCPPPPCCGG",
        "GCCPPPPPPCCG",
        "GCPPPPPPPPCC",
        "GCPPPPPPPPCC",
        "GCCPPPPPPCCG",
        "GGCCPPPPCCGG",
        ".GGCCCCCCGG.",
        "..GGG..GGG..",
        ".GG......GG.",
        "..G......G.."
    };

    [SerializeField] private EnemyVisualType visualType = EnemyVisualType.Skeleton;
    [SerializeField] private float pixelSize = 0.075f;
    [SerializeField] private int sortingOrder = 12;
    [SerializeField] private float animationStepDuration = 0.16f;
    [SerializeField] private float movementThreshold = 0.001f;
    [SerializeField] private float strideBobAmplitude = 0.03f;
    [SerializeField] private float hoverAmplitude = 0.05f;
    [SerializeField] private float leanAngle = 3f;

    private readonly List<GameObject> generatedPixels = new List<GameObject>();
    private readonly List<SpriteRenderer> generatedPixelRenderers = new List<SpriteRenderer>();

    private SpriteRenderer baseSpriteRenderer;
    private Enemy enemy;
    private Transform renderRoot;
    private Vector3 lastObservedPosition;
    private float animationTimer;
    private bool hasObservedPosition;
    private bool stridePhase;
    private int facingDirection = 1;

    public EnemyVisualType VisualType => visualType;

    void Awake()
    {
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        enemy = GetComponent<Enemy>();
        EnsureRenderRoot();
        HideBaseSprite();
    }

    void Start()
    {
        lastObservedPosition = transform.position;
        hasObservedPosition = true;
        RefreshVisual(true);
        ApplyRenderMotion(false);
    }

    void OnValidate()
    {
        pixelSize = Mathf.Max(0.04f, pixelSize);
        animationStepDuration = Mathf.Max(0.06f, animationStepDuration);
        movementThreshold = Mathf.Max(0.0001f, movementThreshold);
        strideBobAmplitude = Mathf.Max(0f, strideBobAmplitude);
        hoverAmplitude = Mathf.Max(0f, hoverAmplitude);
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
        Vector3 worldDelta = transform.position - lastObservedPosition;
        bool facingChanged = false;
        if (Mathf.Abs(worldDelta.x) > movementThreshold)
        {
            int newFacingDirection = worldDelta.x > 0f ? 1 : -1;
            facingChanged = newFacingDirection != facingDirection;
            facingDirection = newFacingDirection;
        }

        bool isMoving = worldDelta.magnitude > movementThreshold;
        bool animateContinuously = visualType == EnemyVisualType.Bat || visualType == EnemyVisualType.GhostElite;
        bool shouldAnimate = isMoving || animateContinuously;
        bool refreshRequired = false;

        if (shouldAnimate)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationStepDuration)
            {
                animationTimer = 0f;
                stridePhase = !stridePhase;
                refreshRequired = true;
            }
        }
        else if (stridePhase)
        {
            animationTimer = 0f;
            stridePhase = false;
            refreshRequired = true;
        }

        if (facingChanged)
            refreshRequired = true;

        if (refreshRequired)
            RefreshVisual();

        ApplyRenderMotion(isMoving);
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
        EnsureRenderRoot();
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
                pixel.transform.localPosition = new Vector3((column - halfWidth) * scaledPixelSize * facingDirection, (halfHeight - row) * scaledPixelSize, 0f);
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
        EnsureRenderRoot();
        while (generatedPixels.Count < requiredCount)
        {
            GameObject pixel = new GameObject($"Proc_EnemyPixel_{generatedPixels.Count}");
            pixel.transform.SetParent(renderRoot, false);
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

    private void EnsureRenderRoot()
    {
        if (renderRoot != null)
            return;

        Transform existingRoot = transform.Find("ProceduralEnemyVisualRoot");
        if (existingRoot != null)
        {
            renderRoot = existingRoot;
            return;
        }

        GameObject root = new GameObject("ProceduralEnemyVisualRoot");
        root.transform.SetParent(transform, false);
        renderRoot = root.transform;
    }

    private float GetScaledPixelSize()
    {
        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        return pixelSize * cellSize;
    }

    private void ApplyRenderMotion(bool isMoving)
    {
        if (renderRoot == null)
            return;

        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        float normalizedCycle = animationStepDuration > Mathf.Epsilon ? animationTimer / animationStepDuration : 0f;
        float bob = 0f;
        float tilt = 0f;

        switch (visualType)
        {
            case EnemyVisualType.Bat:
                bob = Mathf.Sin(Time.time * 8f) * hoverAmplitude * cellSize;
                tilt = Mathf.Sin(Time.time * 6f) * leanAngle;
                break;
            case EnemyVisualType.GhostElite:
                bob = Mathf.Sin(Time.time * 4.8f) * hoverAmplitude * 0.8f * cellSize;
                tilt = Mathf.Sin(Time.time * 3.5f) * leanAngle * 0.5f;
                break;
            default:
                if (isMoving)
                {
                    bob = Mathf.Abs(Mathf.Sin(normalizedCycle * Mathf.PI)) * strideBobAmplitude * cellSize;
                    tilt = -leanAngle * facingDirection;
                }
                break;
        }

        renderRoot.localPosition = new Vector3(0f, bob, 0f);
        renderRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    private bool TryResolvePixelColor(char cell, out Color color)
    {
        switch (visualType)
        {
            case EnemyVisualType.Bat:
                switch (cell)
                {
                    case 'O': color = new Color32(33, 34, 44, 255); return true;
                    case 'W': color = new Color32(67, 51, 94, 255); return true;
                    case 'P': color = new Color32(142, 102, 135, 255); return true;
                    case 'B': color = new Color32(35, 26, 53, 255); return true;
                    case 'E': color = new Color32(230, 84, 124, 255); return true;
                }
                break;
            case EnemyVisualType.Zombie:
                switch (cell)
                {
                    case 'O': color = new Color32(45, 40, 42, 255); return true;
                    case 'H': color = new Color32(116, 154, 86, 255); return true;
                    case 'F': color = new Color32(88, 117, 62, 255); return true;
                    case 'E': color = new Color32(238, 116, 92, 255); return true;
                    case 'C': color = new Color32(94, 81, 109, 255); return true;
                    case 'S': color = new Color32(58, 48, 67, 255); return true;
                    case 'L': color = new Color32(92, 71, 52, 255); return true;
                    case 'D': color = new Color32(60, 52, 48, 255); return true;
                }
                break;
            case EnemyVisualType.GhostElite:
                switch (cell)
                {
                    case 'O': color = new Color32(84, 97, 171, 190); return true;
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
                    case 'O': color = new Color32(52, 52, 61, 255); return true;
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
