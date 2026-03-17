using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GoalTile))]
public class ProceduralGoalRenderer : MonoBehaviour
{
    private static readonly string[] DoorShape =
    {
        "...OFFFFO...",
        "..OFAGAFO...",
        ".OFAGGGGAFO.",
        ".FASSSSSSAF.",
        ".FSDDDDDSAF.",
        "OFSDPPPDSAFO",
        "OFSDPLPDSAFO",
        "OFSDPPPDSAFO",
        ".FSDDDDDSAF.",
        ".FSSMMMSSAF.",
        ".OFM..MMFO..",
        "..OFFFFFO..."
    };

    [SerializeField] private float pixelSize = 0.08f;
    [SerializeField] private int sortingOrder = 14;

    private readonly List<GameObject> pixels = new List<GameObject>();
    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private SpriteRenderer baseSpriteRenderer;
    private string lastBiomeId;
    private float pulse;

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
        pulse += Time.deltaTime * 2.5f;
        string biomeId = WorldGrid.Instance != null && WorldGrid.Instance.CurrentSegment != null
            ? WorldGrid.Instance.CurrentSegment.card.biomeId
            : string.Empty;

        if (biomeId != lastBiomeId)
            Refresh();
        else
            UpdatePortalGlow();
    }

    public void Refresh()
    {
        string biomeId = WorldGrid.Instance != null && WorldGrid.Instance.CurrentSegment != null
            ? WorldGrid.Instance.CurrentSegment.card.biomeId
            : string.Empty;
        lastBiomeId = biomeId;

        float scaledPixelSize = (WorldGrid.Instance != null ? WorldGrid.Instance.CellSize : 1f) * pixelSize;
        float halfWidth = (DoorShape[0].Length - 1) * 0.5f;
        float halfHeight = (DoorShape.Length - 1) * 0.5f;
        int index = 0;

        for (int row = 0; row < DoorShape.Length; row++)
        {
            for (int column = 0; column < DoorShape[row].Length; column++)
            {
                if (!TryResolveColor(DoorShape[row][column], biomeId, out Color color))
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

    private void UpdatePortalGlow()
    {
        for (int i = 0; i < pixels.Count; i++)
        {
            if (!pixels[i].activeSelf)
                continue;

            Vector3 local = pixels[i].transform.localPosition;
            if (Mathf.Abs(local.x) < 0.20f && local.y < 0.18f && local.y > -0.24f)
            {
                if (TryResolveColor('P', lastBiomeId, out Color baseColor))
                    renderers[i].color = Color.Lerp(baseColor, Color.white, (Mathf.Sin(pulse) + 1f) * 0.18f);
            }
        }
    }

    private void EnsurePool(int requiredCount)
    {
        while (pixels.Count < requiredCount)
        {
            GameObject pixel = new GameObject($"GoalPixel_{pixels.Count}");
            pixel.transform.SetParent(transform, false);
            SpriteRenderer renderer = pixel.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
            pixels.Add(pixel);
            renderers.Add(renderer);
        }
    }

    private bool TryResolveColor(char cell, string biomeId, out Color color)
    {
        GetPalette(biomeId, out Color frame, out Color stone, out Color accent, out Color portal, out Color outline, out Color metal);
        switch (cell)
        {
            case 'O': color = outline; return true;
            case 'F': color = frame; return true;
            case 'S': color = stone; return true;
            case 'A': color = accent; return true;
            case 'D': color = ProceduralPixelUtility.Multiply(frame, 0.7f); return true;
            case 'G': color = Color.Lerp(accent, Color.white, 0.24f); return true;
            case 'L': color = Color.Lerp(portal, Color.white, 0.38f); return true;
            case 'M': color = metal; return true;
            case 'P': color = portal; return true;
            default: color = default; return false;
        }
    }

    private void GetPalette(string biomeId, out Color frame, out Color stone, out Color accent, out Color portal, out Color outline, out Color metal)
    {
        switch ((biomeId ?? string.Empty).ToLowerInvariant())
        {
            case "dark_forest":
                frame = new Color32(73, 98, 56, 255);
                stone = new Color32(42, 57, 35, 255);
                accent = new Color32(174, 214, 136, 255);
                portal = new Color32(103, 226, 175, 255);
                outline = new Color32(25, 40, 25, 255);
                metal = new Color32(129, 146, 110, 255);
                break;
            case "swamp":
                frame = new Color32(88, 98, 64, 255);
                stone = new Color32(54, 73, 52, 255);
                accent = new Color32(186, 173, 92, 255);
                portal = new Color32(120, 216, 171, 255);
                outline = new Color32(33, 44, 34, 255);
                metal = new Color32(118, 125, 92, 255);
                break;
            case "crypt":
                frame = new Color32(121, 127, 160, 255);
                stone = new Color32(72, 75, 97, 255);
                accent = new Color32(210, 220, 255, 255);
                portal = new Color32(168, 176, 255, 255);
                outline = new Color32(34, 36, 53, 255);
                metal = new Color32(146, 152, 182, 255);
                break;
            case "ruins":
            default:
                frame = new Color32(161, 134, 94, 255);
                stone = new Color32(86, 78, 70, 255);
                accent = new Color32(237, 207, 124, 255);
                portal = new Color32(128, 215, 239, 255);
                outline = new Color32(44, 37, 32, 255);
                metal = new Color32(160, 155, 145, 255);
                break;
        }
    }
}
