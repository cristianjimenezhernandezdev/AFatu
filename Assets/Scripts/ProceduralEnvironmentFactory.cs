using UnityEngine;

public static class ProceduralEnvironmentFactory
{
    private readonly struct BiomePalette
    {
        public readonly Color[] FloorColors;
        public readonly Color[] AccentColors;
        public readonly Color[] TreeColors;
        public readonly Color[] RuinColors;
        public readonly bool UsesTrees;

        public BiomePalette(Color[] floorColors, Color[] accentColors, Color[] treeColors, Color[] ruinColors, bool usesTrees)
        {
            FloorColors = floorColors;
            AccentColors = accentColors;
            TreeColors = treeColors;
            RuinColors = ruinColors;
            UsesTrees = usesTrees;
        }
    }

    public static void ApplyCellVisual(GameObject cell, SpriteRenderer baseRenderer, string biomeId, Color floorColor, Color wallColor, Vector2Int gridPosition, bool blocked)
    {
        if (cell == null || baseRenderer == null)
            return;

        baseRenderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
        baseRenderer.sortingOrder = 0;
        baseRenderer.color = blocked
            ? ProceduralPixelUtility.Multiply(wallColor, 0.76f)
            : ProceduralPixelUtility.Multiply(floorColor, 0.92f);

        ProceduralPixelUtility.DestroyGeneratedChildren(cell.transform);

        BiomePalette palette = GetPalette(biomeId, floorColor, wallColor);
        BuildFloorMosaic(cell.transform, palette, gridPosition);

        if (blocked)
        {
            if (palette.UsesTrees)
                BuildTree(cell.transform, palette, gridPosition);
            else
                BuildRuinWall(cell.transform, palette, gridPosition);
        }
        else
        {
            BuildGroundAccent(cell.transform, palette, gridPosition);
        }
    }

    private static void BuildFloorMosaic(Transform parent, BiomePalette palette, Vector2Int gridPosition)
    {
        float subCellSize = 0.34f;
        int seed = ProceduralPixelUtility.Hash(gridPosition.x, gridPosition.y, 11);
        int colorCount = palette.FloorColors.Length;

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                int hash = ProceduralPixelUtility.Hash(gridPosition.x + column, gridPosition.y + row, seed);
                Color color = palette.FloorColors[Mathf.Abs(hash) % colorCount];
                color = ProceduralPixelUtility.Blend(color, Color.black, (Mathf.Abs(hash / 7) % 12) * 0.01f);

                Vector3 localPosition = new Vector3((column - 1) * subCellSize, (1 - row) * subCellSize, 0f);
                ProceduralPixelUtility.CreatePixel(parent, $"Proc_Floor_{column}_{row}", localPosition, subCellSize, color, 1);
            }
        }
    }

    private static void BuildGroundAccent(Transform parent, BiomePalette palette, Vector2Int gridPosition)
    {
        int hash = Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x, gridPosition.y, 23));
        if (hash % 4 != 0)
            return;

        int accentCount = palette.AccentColors.Length;
        int pixelCount = 1 + hash % 2;

        for (int i = 0; i < pixelCount; i++)
        {
            float x = ((hash >> (i + 1)) % 5 - 2) * 0.12f;
            float y = ((hash >> (i + 3)) % 5 - 2) * 0.12f;
            Color color = palette.AccentColors[(hash + i) % accentCount];
            ProceduralPixelUtility.CreatePixel(parent, $"Proc_Accent_{i}", new Vector3(x, y, 0f), 0.16f, color, 2);
        }
    }

    private static void BuildTree(Transform parent, BiomePalette palette, Vector2Int gridPosition)
    {
        string[] canopyShape =
        {
            ".LLLL.",
            "LLLLLL",
            "LLLLLL",
            ".LLLL.",
            "..TT..",
            "..TT.."
        };

        BuildShape(parent, canopyShape, palette.TreeColors, gridPosition, 0.14f, 3);
    }

    private static void BuildRuinWall(Transform parent, BiomePalette palette, Vector2Int gridPosition)
    {
        string[] ruinShape =
        {
            "SSSSSS",
            "SCCCSS",
            "SSSSCS",
            "SCCCSS",
            "SCSCSS",
            ".S..S."
        };

        BuildShape(parent, ruinShape, palette.RuinColors, gridPosition, 0.14f, 3);
    }

    private static void BuildShape(Transform parent, string[] shape, Color[] colors, Vector2Int gridPosition, float pixelSize, int sortingOrder)
    {
        float halfWidth = (shape[0].Length - 1) * 0.5f;
        float halfHeight = (shape.Length - 1) * 0.5f;

        for (int row = 0; row < shape.Length; row++)
        {
            for (int column = 0; column < shape[row].Length; column++)
            {
                char cell = shape[row][column];
                if (cell == '.')
                    continue;

                int colorIndex = Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x + column, gridPosition.y + row, cell)) % colors.Length;
                Vector3 localPosition = new Vector3((column - halfWidth) * pixelSize, (halfHeight - row) * pixelSize, 0f);
                ProceduralPixelUtility.CreatePixel(parent, $"Proc_Shape_{column}_{row}", localPosition, pixelSize, colors[colorIndex], sortingOrder);
            }
        }
    }

    private static BiomePalette GetPalette(string biomeId, Color floorColor, Color wallColor)
    {
        string biome = string.IsNullOrWhiteSpace(biomeId) ? "default" : biomeId.ToLowerInvariant();

        switch (biome)
        {
            case "dark_forest":
            case "forest":
                return new BiomePalette(
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(55, 79, 43, 255), 0.20f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(121, 151, 88, 255), 0.25f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(95, 121, 63, 255), 0.18f)
                    },
                    new Color[]
                    {
                        new Color32(171, 124, 73, 255),
                        new Color32(99, 133, 63, 255)
                    },
                    new Color[]
                    {
                        new Color32(40, 87, 43, 255),
                        new Color32(64, 112, 58, 255),
                        new Color32(110, 73, 41, 255)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Multiply(wallColor, 0.82f),
                        new Color32(102, 92, 86, 255)
                    },
                    true);
            case "swamp":
                return new BiomePalette(
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(44, 71, 58, 255), 0.24f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(74, 105, 86, 255), 0.28f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(89, 99, 60, 255), 0.18f)
                    },
                    new Color[]
                    {
                        new Color32(126, 110, 71, 255),
                        new Color32(78, 134, 111, 255)
                    },
                    new Color[]
                    {
                        new Color32(45, 75, 52, 255),
                        new Color32(68, 97, 71, 255),
                        new Color32(102, 78, 50, 255)
                    },
                    new Color[]
                    {
                        new Color32(85, 87, 81, 255),
                        new Color32(62, 63, 58, 255),
                        new Color32(114, 96, 68, 255)
                    },
                    true);
            case "crypt":
                return new BiomePalette(
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(192, 191, 213, 255), 0.24f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(146, 148, 171, 255), 0.22f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(119, 120, 145, 255), 0.18f)
                    },
                    new Color[]
                    {
                        new Color32(170, 177, 196, 255),
                        new Color32(124, 103, 146, 255)
                    },
                    new Color[]
                    {
                        new Color32(95, 116, 105, 255),
                        new Color32(132, 112, 94, 255)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Blend(wallColor, new Color32(118, 119, 143, 255), 0.28f),
                        new Color32(77, 80, 97, 255)
                    },
                    false);
            case "ruins":
                return new BiomePalette(
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(169, 153, 136, 255), 0.24f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(132, 126, 115, 255), 0.18f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(113, 133, 109, 255), 0.12f)
                    },
                    new Color[]
                    {
                        new Color32(111, 128, 105, 255),
                        new Color32(181, 159, 118, 255)
                    },
                    new Color[]
                    {
                        new Color32(89, 108, 77, 255),
                        new Color32(124, 91, 56, 255)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Blend(wallColor, new Color32(147, 143, 136, 255), 0.30f),
                        new Color32(89, 93, 90, 255)
                    },
                    false);
            default:
                return new BiomePalette(
                    new Color[]
                    {
                        floorColor,
                        ProceduralPixelUtility.Multiply(floorColor, 0.88f),
                        ProceduralPixelUtility.Blend(floorColor, Color.white, 0.12f)
                    },
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, wallColor, 0.25f),
                        ProceduralPixelUtility.Blend(floorColor, Color.white, 0.18f)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Multiply(wallColor, 0.84f),
                        ProceduralPixelUtility.Blend(wallColor, floorColor, 0.20f)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Multiply(wallColor, 0.84f),
                        ProceduralPixelUtility.Blend(wallColor, floorColor, 0.20f)
                    },
                    false);
        }
    }
}

