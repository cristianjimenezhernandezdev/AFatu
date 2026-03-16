using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerGridMovement))]
public class ProceduralPlayerRenderer : MonoBehaviour
{
    public enum PlayerVisualPose
    {
        Idle,
        StepLeft,
        StepRight
    }

    private static readonly string[] IdleShape =
    {
        "..TTTT..",
        ".TCHHCT.",
        ".CHSSHC.",
        "..HMMH..",
        ".CHMMHC.",
        ".CCMMCC.",
        ".L....L.",
        "..L..L.."
    };

    private static readonly string[] StepLeftShape =
    {
        "..TTTT..",
        ".TCHHCT.",
        ".CHSSHC.",
        "..HMMH..",
        ".CHMMHC.",
        ".CCMMCC.",
        "LL....L.",
        "L..L...."
    };

    private static readonly string[] StepRightShape =
    {
        "..TTTT..",
        ".TCHHCT.",
        ".CHSSHC.",
        "..HMMH..",
        ".CHMMHC.",
        ".CCMMCC.",
        ".L....LL",
        "....L..L"
    };

    [Header("Pixels")]
    [SerializeField] private float pixelSize = 0.1f;
    [SerializeField] private int sortingOrder = 11;
    [SerializeField] private Color trimColor = new Color32(234, 203, 116, 255);
    [SerializeField] private Color cloakColor = new Color32(44, 116, 139, 255);
    [SerializeField] private Color hoodColor = new Color32(25, 73, 93, 255);
    [SerializeField] private Color skinColor = new Color32(231, 188, 145, 255);
    [SerializeField] private Color metalColor = new Color32(171, 180, 194, 255);
    [SerializeField] private Color leatherColor = new Color32(117, 77, 49, 255);

    [Header("Animation")]
    [SerializeField] private float stepDuration = 0.12f;
    [SerializeField] private float movementThreshold = 0.001f;
    [SerializeField] private float teleportThresholdMultiplier = 1.5f;

    private readonly List<GameObject> generatedPixels = new();
    private readonly List<SpriteRenderer> generatedPixelRenderers = new();

    private PlayerVisualPose currentPose = PlayerVisualPose.Idle;
    private SpriteRenderer baseSpriteRenderer;
    private Vector3 lastObservedPosition;
    private float walkTimer;
    private bool hasObservedPosition;

    public IReadOnlyList<GameObject> GeneratedPixels => generatedPixels;
    public PlayerVisualPose CurrentPose => currentPose;

    void Awake()
    {
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        HideBaseSprite();
    }

    void Start()
    {
        lastObservedPosition = transform.position;
        hasObservedPosition = true;
        SetPose(PlayerVisualPose.Idle, true);
    }

    void OnValidate()
    {
        pixelSize = Mathf.Max(0.01f, pixelSize);
        stepDuration = Mathf.Max(0.01f, stepDuration);
        movementThreshold = Mathf.Max(0.0001f, movementThreshold);
        teleportThresholdMultiplier = Mathf.Max(1f, teleportThresholdMultiplier);
    }

    void LateUpdate()
    {
        if (!hasObservedPosition)
        {
            lastObservedPosition = transform.position;
            hasObservedPosition = true;
        }

        UpdateAnimation();
        lastObservedPosition = transform.position;
    }

    public void SetPose(PlayerVisualPose pose, bool forceRefresh = false)
    {
        if (!forceRefresh && currentPose == pose)
            return;

        currentPose = pose;
        ApplyShape(GetShapeForPose(pose));
    }

    public void ShowIdle()
    {
        walkTimer = 0f;
        SetPose(PlayerVisualPose.Idle);
    }

    public void ShowStepLeft()
    {
        SetPose(PlayerVisualPose.StepLeft);
    }

    public void ShowStepRight()
    {
        SetPose(PlayerVisualPose.StepRight);
    }

    private void UpdateAnimation()
    {
        Vector3 currentPosition = transform.position;
        float deltaDistance = Vector3.Distance(currentPosition, lastObservedPosition);
        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        float teleportThreshold = cellSize * teleportThresholdMultiplier;
        bool isMoving = deltaDistance > movementThreshold && deltaDistance < teleportThreshold;

        if (!isMoving)
        {
            ShowIdle();
            return;
        }

        walkTimer += Time.deltaTime;
        float cycleDuration = stepDuration * 2f;
        if (walkTimer >= cycleDuration)
        {
            walkTimer %= cycleDuration;
        }

        SetPose(walkTimer < stepDuration ? PlayerVisualPose.StepLeft : PlayerVisualPose.StepRight);
    }

    private void ApplyShape(string[] shape)
    {
        HideBaseSprite();

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
                pixel.transform.localPosition = new Vector3(
                    (column - halfWidth) * scaledPixelSize,
                    (halfHeight - row) * scaledPixelSize,
                    0f
                );
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
            GameObject pixel = new($"Proc_PlayerPixel_{index}");
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
        {
            baseSpriteRenderer.enabled = false;
        }
    }

    private float GetScaledPixelSize()
    {
        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        return pixelSize * cellSize;
    }

    private static bool TryGetShapeSize(string[] shape, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (shape == null || shape.Length == 0)
            return false;

        height = shape.Length;
        width = shape[0].Length;

        if (width == 0)
            return false;

        for (int i = 1; i < shape.Length; i++)
        {
            if (shape[i].Length != width)
            {
                Debug.LogWarning("ProceduralPlayerRenderer ha rebut una forma amb files de longitud inconsistent.");
                return false;
            }
        }

        return true;
    }

    private bool TryResolvePixelColor(char cell, out Color color)
    {
        switch (cell)
        {
            case 'T':
                color = trimColor;
                return true;
            case 'C':
                color = cloakColor;
                return true;
            case 'H':
                color = hoodColor;
                return true;
            case 'S':
                color = skinColor;
                return true;
            case 'M':
                color = metalColor;
                return true;
            case 'L':
                color = leatherColor;
                return true;
            default:
                color = default;
                return false;
        }
    }

    private static string[] GetShapeForPose(PlayerVisualPose pose)
    {
        return pose switch
        {
            PlayerVisualPose.StepLeft => StepLeftShape,
            PlayerVisualPose.StepRight => StepRightShape,
            _ => IdleShape
        };
    }

}
