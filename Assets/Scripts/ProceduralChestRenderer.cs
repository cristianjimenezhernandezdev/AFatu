using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Chest))]
public class ProceduralChestRenderer : MonoBehaviour
{
    private static readonly string[] ClosedShape =
    {
        "..GGGG..",
        ".GYYYYG.",
        ".GBBBBG.",
        ".GBLLBG.",
        ".GBBBBG.",
        ".GBBBBG.",
        ".G....G.",
        "..GGGG.."
    };

    private static readonly string[] OpenShape =
    {
        ".GGGGGG.",
        "GY....YG",
        "G.BBBBG.",
        ".GBLLBG.",
        ".GBBBBG.",
        ".GBBBBG.",
        ".G....G.",
        "..GGGG.."
    };

    [SerializeField] private float pixelSize = 0.1f;
    [SerializeField] private int sortingOrder = 11;

    private readonly List<GameObject> pixels = new List<GameObject>();
    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private SpriteRenderer baseSpriteRenderer;
    private bool isOpened;
    private string chestTier = "small";

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
            case 'G':
                color = chestTier == "rare" ? new Color32(141, 198, 255, 255) : new Color32(224, 174, 72, 255);
                return true;
            case 'Y':
                color = chestTier == "rare" ? new Color32(179, 255, 241, 255) : new Color32(255, 228, 116, 255);
                return true;
            case 'B':
                color = chestTier == "rare" ? new Color32(78, 106, 148, 255) : new Color32(126, 83, 45, 255);
                return true;
            case 'L':
                color = new Color32(190, 214, 239, 255);
                return true;
            case '.':
            default:
                color = default;
                return false;
        }
    }
}
