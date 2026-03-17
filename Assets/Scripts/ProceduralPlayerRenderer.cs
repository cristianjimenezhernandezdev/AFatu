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
        "...TTTT.....",
        "..THHHHT....",
        "..THSSHT....",
        ".OTHAAHTO...",
        ".OTCCCCCTMM.",
        ".THCCMMCCTM.",
        ".OHCCMMCCTM.",
        ".OCCLLLLCTM.",
        ".OLLLLLLTTM.",
        "..LL..LL.TM.",
        ".LDL..LDL.M.",
        "..D....D...."
    };

    private static readonly string[] StepLeftShape =
    {
        "...TTTT.....",
        "..THHHHT....",
        "..THSSHT....",
        ".OTHAAHTO...",
        ".OTCCCCCTMM.",
        ".THCCMMCCTM.",
        ".OHCCMMCCTT.",
        ".OCCLLLLCTM.",
        ".LLLLLLL.TM.",
        "LL....L.LMM.",
        "D..L.L..DTM.",
        "...D....D..."
    };

    private static readonly string[] StepRightShape =
    {
        "...TTTT.....",
        "..THHHHT....",
        "..THSSHT....",
        ".OTHAAHTO...",
        ".OTCCCCCTMM.",
        ".THCCMMCCTT.",
        ".OHCCMMCCTM.",
        ".OCCLLLLCTM.",
        ".LLLLLLLTM..",
        ".MML.L....LL",
        ".MTD..L.L..D",
        "...D....D..."
    };

    [Header("Pixels")]
    [SerializeField] private float pixelSize = 0.085f;
    [SerializeField] private int sortingOrder = 11;
    [SerializeField] private Color trimColor = new Color32(234, 203, 116, 255);
    [SerializeField] private Color accentColor = new Color32(245, 228, 172, 255);
    [SerializeField] private Color cloakColor = new Color32(44, 116, 139, 255);
    [SerializeField] private Color hoodColor = new Color32(25, 73, 93, 255);
    [SerializeField] private Color skinColor = new Color32(231, 188, 145, 255);
    [SerializeField] private Color metalColor = new Color32(171, 180, 194, 255);
    [SerializeField] private Color leatherColor = new Color32(117, 77, 49, 255);
    [SerializeField] private Color outlineColor = new Color32(26, 25, 34, 255);
    [SerializeField] private Color shadowColor = new Color32(53, 53, 72, 255);

    [Header("Animation")]
    [SerializeField] private float stepDuration = 0.12f;
    [SerializeField] private float movementThreshold = 0.001f;
    [SerializeField] private float teleportThresholdMultiplier = 1.5f;
    [SerializeField] private float movementBobAmplitude = 0.045f;
    [SerializeField] private float idleBreathAmplitude = 0.015f;
    [SerializeField] private float horizontalSwayAmplitude = 0.02f;
    [SerializeField] private float movementLeanAngle = 4f;

    private readonly List<GameObject> generatedPixels = new List<GameObject>();
    private readonly List<SpriteRenderer> generatedPixelRenderers = new List<SpriteRenderer>();

    private PlayerVisualPose currentPose = PlayerVisualPose.Idle;
    private SpriteRenderer baseSpriteRenderer;
    private Transform renderRoot;
    private Vector3 lastObservedPosition;
    private float walkTimer;
    private bool hasObservedPosition;
    private int facingDirection = 1;

    public IReadOnlyList<GameObject> GeneratedPixels => generatedPixels;
    public PlayerVisualPose CurrentPose => currentPose;

    void Awake()
    {
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        EnsureRenderRoot();
        HideBaseSprite();
    }

    void Start()
    {
        lastObservedPosition = transform.position;
        hasObservedPosition = true;
        SetPose(PlayerVisualPose.Idle, true);
        ApplyRenderMotion(false);
    }

    void OnValidate()
    {
        pixelSize = Mathf.Max(0.01f, pixelSize);
        stepDuration = Mathf.Max(0.01f, stepDuration);
        movementThreshold = Mathf.Max(0.0001f, movementThreshold);
        teleportThresholdMultiplier = Mathf.Max(1f, teleportThresholdMultiplier);
        movementBobAmplitude = Mathf.Max(0f, movementBobAmplitude);
        idleBreathAmplitude = Mathf.Max(0f, idleBreathAmplitude);
        horizontalSwayAmplitude = Mathf.Max(0f, horizontalSwayAmplitude);
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

        float deltaDistance = worldDelta.magnitude;
        float cellSize = WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f;
        float teleportThreshold = cellSize * teleportThresholdMultiplier;
        bool isMoving = deltaDistance > movementThreshold && deltaDistance < teleportThreshold;

        if (!isMoving)
        {
            walkTimer = 0f;
            SetPose(PlayerVisualPose.Idle, facingChanged);
            ApplyRenderMotion(false);
            return;
        }

        walkTimer += Time.deltaTime;
        float cycleDuration = stepDuration * 2f;
        if (walkTimer >= cycleDuration)
            walkTimer %= cycleDuration;

        SetPose(walkTimer < stepDuration ? PlayerVisualPose.StepLeft : PlayerVisualPose.StepRight, facingChanged);
        ApplyRenderMotion(true);
    }

    private void ApplyShape(string[] shape)
    {
        EnsureRenderRoot();
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
            GameObject pixel = new GameObject($"Proc_PlayerPixel_{generatedPixels.Count}");
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

        Transform existingRoot = transform.Find("ProceduralPlayerVisualRoot");
        if (existingRoot != null)
        {
            renderRoot = existingRoot;
            return;
        }

        GameObject root = new GameObject("ProceduralPlayerVisualRoot");
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
        float cycle = stepDuration > Mathf.Epsilon ? walkTimer / stepDuration : 0f;
        if (isMoving)
        {
            float bob = Mathf.Abs(Mathf.Sin(cycle * Mathf.PI)) * movementBobAmplitude * cellSize;
            float sway = Mathf.Sin(cycle * Mathf.PI) * horizontalSwayAmplitude * cellSize * facingDirection;
            renderRoot.localPosition = new Vector3(sway, bob, 0f);
            renderRoot.localRotation = Quaternion.Euler(0f, 0f, -movementLeanAngle * facingDirection);
            return;
        }

        float idleBreath = Mathf.Sin(Time.time * 2.2f) * idleBreathAmplitude * cellSize;
        renderRoot.localPosition = new Vector3(0f, idleBreath, 0f);
        renderRoot.localRotation = Quaternion.identity;
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
            if (shape[i].Length != width)
                return false;

        return true;
    }

    private bool TryResolvePixelColor(char cell, out Color color)
    {
        switch (cell)
        {
            case 'O': color = outlineColor; return true;
            case 'T': color = trimColor; return true;
            case 'A': color = accentColor; return true;
            case 'C': color = cloakColor; return true;
            case 'H': color = hoodColor; return true;
            case 'S': color = skinColor; return true;
            case 'M': color = metalColor; return true;
            case 'L': color = leatherColor; return true;
            case 'D': color = shadowColor; return true;
            default: color = default; return false;
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
