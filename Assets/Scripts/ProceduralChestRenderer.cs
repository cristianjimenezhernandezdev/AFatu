using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Chest))]
public class ProceduralChestRenderer : MonoBehaviour
{
    private static readonly string[] ClosedShape =
    {
        "..OGGGGO...",
        ".OGYYYYGO..",
        "OGGFFFFGGO.",
        "OGBBBBBBGO.",
        "GGBMLLMBGG.",
        "GGBBBBBBGG.",
        "OGBMMMMBGO.",
        ".GGSSSSGG..",
        "..G....G...",
        "..OSSSSO..."
    };

    private static readonly string[] OpenShape =
    {
        ".OGGGGGGO..",
        "OGY....YGO.",
        "GGGFFFFGGG.",
        "..GBBBBB...",
        ".GGBLLMBG..",
        ".GGB....G..",
        ".OGBM..MG..",
        "..GGSSGG...",
        "..OG..GO...",
        "...OSSO...."
    };

    [SerializeField] private float pixelSize = 0.085f;
    [SerializeField] private int sortingOrder = 11;
    [SerializeField] private float shimmerSpeed = 3.2f;

    private readonly List<GameObject> pixels = new List<GameObject>();
    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private SpriteRenderer baseSpriteRenderer;
    private bool isOpened;
    private string chestTier = "small";
    private float shimmerTimer;

    void Awake()
    {
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        if (baseSpriteRenderer != null)
            baseSpriteRenderer.enabled = false;
    }

    void Start()
    {
        Refresh();
    }

    void LateUpdate()
    {
        shimmerTimer += Time.deltaTime * shimmerSpeed;
        UpdateShimmer();
    }

    public void SetOpened(bool opened, string tier)
    {
        isOpened = opened;
        chestTier = string.IsNullOrWhiteSpace(tier) ? "small" : tier;
        Refresh();
    }

    private void Refresh()
    {
        string[] shape = isOpened ? OpenShape : ClosedShape;
        float scaledPixelSize = (WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f) * pixelSize;
        float halfWidth = (shape[0].Length - 1) * 0.5f;
        float halfHeight = (shape.Length - 1) * 0.5f;
        int index = 0;

        for (int row = 0; row < shape.Length; row++)
        {
            for (int column = 0; column < shape[row].Length; column++)
            {
                if (!TryResolveColor(shape[row][column], out Color color))
                    continue;

                EnsurePool(index + 1);
                GameObject pixel = pixels[index];
                SpriteRenderer spriteRenderer = renderers[index];
                pixel.SetActive(true);
                pixel.transform.localPosition = new Vector3((column - halfWidth) * scaledPixelSize, (halfHeight - row) * scaledPixelSize, 0f);
                pixel.transform.localScale = Vector3.one * scaledPixelSize;
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder = sortingOrder;
                index++;
            }
        }

        for (int i = index; i < pixels.Count; i++)
            pixels[i].SetActive(false);
    }

    private void EnsurePool(int requiredCount)
    {
        while (pixels.Count < requiredCount)
        {
            GameObject pixel = new GameObject($"ChestPixel_{pixels.Count}");
            pixel.transform.SetParent(transform, false);
            SpriteRenderer spriteRenderer = pixel.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
            pixels.Add(pixel);
            renderers.Add(spriteRenderer);
        }
    }

    private bool TryResolveColor(char cell, out Color color)
    {
        switch (cell)
        {
            case 'O':
                color = chestTier == "rare" ? new Color32(29, 38, 66, 255) : new Color32(52, 35, 19, 255);
                return true;
            case 'G':
                color = chestTier == "rare" ? new Color32(126, 187, 255, 255) : new Color32(226, 177, 76, 255);
                return true;
            case 'Y':
                color = chestTier == "rare" ? new Color32(210, 247, 255, 255) : new Color32(255, 231, 132, 255);
                return true;
            case 'F':
                color = chestTier == "rare" ? new Color32(74, 104, 154, 255) : new Color32(149, 96, 42, 255);
                return true;
            case 'B':
                color = chestTier == "rare" ? new Color32(58, 78, 124, 255) : new Color32(98, 63, 33, 255);
                return true;
            case 'L':
                color = isOpened ? new Color32(62, 34, 21, 255) : new Color32(204, 184, 126, 255);
                return true;
            case 'M':
                color = new Color32(114, 118, 128, 255);
                return true;
            case 'S':
                color = chestTier == "rare" ? new Color32(66, 84, 136, 255) : new Color32(80, 52, 27, 255);
                return true;
            default:
                color = default;
                return false;
        }
    }

    private void UpdateShimmer()
    {
        for (int i = 0; i < pixels.Count; i++)
        {
            if (!pixels[i].activeSelf)
                continue;

            Vector3 local = pixels[i].transform.localPosition;
            if (local.y < 0.05f)
                continue;

            if (!TryResolveColor('Y', out Color brightColor) || !TryResolveColor('G', out Color trimColor))
                continue;

            float shimmer = Mathf.Sin(shimmerTimer + local.x * 8f + local.y * 4f);
            if (shimmer <= 0.35f)
                continue;

            renderers[i].color = Color.Lerp(trimColor, brightColor, (shimmer - 0.35f) * 0.65f);
        }
    }
}
