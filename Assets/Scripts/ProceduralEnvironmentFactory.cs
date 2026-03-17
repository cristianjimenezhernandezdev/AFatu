using UnityEngine;

public static class ProceduralEnvironmentFactory
{
    private readonly struct BiomePalette
    {
        public readonly Color[] FloorColors;
        public readonly Color[] AccentColors;
        public readonly Color[] FoliageColors;
        public readonly Color[] BarkColors;
        public readonly Color[] StoneColors;
        public readonly Color[] LiquidColors;
        public readonly Color[] HighlightColors;
        public readonly Color[] ShadowColors;
        public readonly Color BackdropColor;
        public readonly bool UsesTrees;
        public readonly bool UsesWater;

        public BiomePalette(
            Color[] floorColors,
            Color[] accentColors,
            Color[] foliageColors,
            Color[] barkColors,
            Color[] stoneColors,
            Color[] liquidColors,
            Color[] highlightColors,
            Color[] shadowColors,
            Color backdropColor,
            bool usesTrees,
            bool usesWater)
        {
            FloorColors = floorColors;
            AccentColors = accentColors;
            FoliageColors = foliageColors;
            BarkColors = barkColors;
            StoneColors = stoneColors;
            LiquidColors = liquidColors;
            HighlightColors = highlightColors;
            ShadowColors = shadowColors;
            BackdropColor = backdropColor;
            UsesTrees = usesTrees;
            UsesWater = usesWater;
        }
    }

    public static void ApplyCellVisual(GameObject cell, SpriteRenderer baseRenderer, string biomeId, Color floorColor, Color wallColor, Vector2Int gridPosition, bool blocked)
    {
        if (cell == null || baseRenderer == null)
            return;

        BiomePalette palette = GetPalette(biomeId, floorColor, wallColor);
        baseRenderer.sprite = ProceduralPixelUtility.GetOrCreateSquareSprite();
        baseRenderer.sortingOrder = 0;
        baseRenderer.color = blocked
            ? ProceduralPixelUtility.Blend(wallColor, palette.ShadowColors[0], 0.30f)
            : ProceduralPixelUtility.Blend(floorColor, palette.ShadowColors[0], 0.10f);

        ProceduralPixelUtility.DestroyGeneratedChildren(cell.transform);

        BuildFloorFoundation(cell.transform, palette, gridPosition, blocked);
        BuildCellDepth(cell.transform, palette, gridPosition, blocked);

        if (blocked)
        {
            BuildObstacleFooting(cell.transform, palette, gridPosition);
            if (palette.UsesTrees)
                BuildTree(cell.transform, palette, gridPosition);
            else
                BuildRuinWall(cell.transform, palette, gridPosition);
        }
        else
        {
            BuildGroundDetail(cell.transform, palette, biomeId, gridPosition);
        }
    }

    public static Color GetBackdropColor(string biomeId, Color floorColor, Color wallColor)
    {
        return GetPalette(biomeId, floorColor, wallColor).BackdropColor;
    }

    private static void BuildFloorFoundation(Transform parent, BiomePalette palette, Vector2Int gridPosition, bool blocked)
    {
        float pixelSize = 0.205f;
        Color[] sourceColors = blocked ? palette.StoneColors : palette.FloorColors;

        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                int hash = Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x * 7 + column, gridPosition.y * 7 + row, 11));
                Color color = sourceColors[hash % sourceColors.Length];

                float distanceToCenter = Mathf.Abs(column - 2) + Mathf.Abs(row - 2);
                float centerHighlight = Mathf.Clamp01(0.22f - distanceToCenter * 0.04f);
                float edgeShade = row >= 3 ? 0.12f : 0.04f;
                color = ProceduralPixelUtility.Blend(color, palette.HighlightColors[hash % palette.HighlightColors.Length], centerHighlight);
                color = ProceduralPixelUtility.Blend(color, palette.ShadowColors[(hash / 5) % palette.ShadowColors.Length], edgeShade);

                if (column == 0 || column == 4)
                    color = ProceduralPixelUtility.Blend(color, palette.ShadowColors[0], 0.08f);

                Vector3 localPosition = new Vector3((column - 2) * pixelSize, (2 - row) * pixelSize, 0f);
                ProceduralPixelUtility.CreatePixel(parent, $"Proc_Floor_{column}_{row}", localPosition, pixelSize, color, 1);
            }
        }
    }

    private static void BuildCellDepth(Transform parent, BiomePalette palette, Vector2Int gridPosition, bool blocked)
    {
        int mask = GetNeighborMask(gridPosition);
        float lineSize = 0.18f;

        if (!blocked)
        {
            if ((mask & 1) != 0)
                BuildStrip(parent, new Vector2(-0.36f, 0.32f), 5, new Vector2(lineSize, 0f), palette.ShadowColors[0], 2, "Proc_ShadeUp");
            if ((mask & 2) != 0)
                BuildStrip(parent, new Vector2(0.36f, 0.32f), 5, new Vector2(0f, -lineSize), palette.ShadowColors[0], 2, "Proc_ShadeRight");
            if ((mask & 4) != 0)
                BuildStrip(parent, new Vector2(-0.36f, -0.36f), 5, new Vector2(lineSize, 0f), palette.ShadowColors[1 % palette.ShadowColors.Length], 2, "Proc_ShadeDown");
            if ((mask & 8) != 0)
                BuildStrip(parent, new Vector2(-0.36f, 0.32f), 5, new Vector2(0f, -lineSize), palette.ShadowColors[0], 2, "Proc_ShadeLeft");

            BuildStrip(parent, new Vector2(-0.24f, 0.40f), 3, new Vector2(0.18f, 0f), ProceduralPixelUtility.Blend(palette.HighlightColors[0], Color.white, 0.12f), 2, "Proc_RimLight");
            return;
        }

        BuildStrip(parent, new Vector2(-0.40f, -0.38f), 5, new Vector2(0.20f, 0f), palette.ShadowColors[0], 2, "Proc_BlockShadow");
        BuildStrip(parent, new Vector2(-0.28f, 0.40f), 3, new Vector2(0.20f, 0f), palette.HighlightColors[0], 2, "Proc_BlockLight");
    }

    private static void BuildGroundDetail(Transform parent, BiomePalette palette, string biomeId, Vector2Int gridPosition)
    {
        int hash = Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x, gridPosition.y, 23));
        int variantCount = palette.UsesWater ? 5 : 4;
        int variant = hash % variantCount;

        switch (variant)
        {
            case 0:
                BuildPebbles(parent, palette, hash);
                break;
            case 1:
                BuildTuft(parent, palette, hash);
                break;
            case 2:
                BuildCrack(parent, palette, hash);
                break;
            case 3:
                BuildAccentPatch(parent, palette, hash, biomeId);
                break;
            case 4:
                BuildPuddle(parent, palette, hash);
                break;
        }
    }

    private static void BuildPebbles(Transform parent, BiomePalette palette, int hash)
    {
        int count = 2 + hash % 3;
        for (int i = 0; i < count; i++)
        {
            float x = (((hash >> (i + 1)) & 7) - 3) * 0.11f;
            float y = (((hash >> (i + 4)) & 5) - 2) * 0.09f - 0.08f;
            Color color = palette.AccentColors[(hash + i) % palette.AccentColors.Length];
            ProceduralPixelUtility.CreatePixel(parent, $"Proc_Pebble_{i}", new Vector3(x, y, 0f), 0.14f, color, 3);
        }
    }

    private static void BuildTuft(Transform parent, BiomePalette palette, int hash)
    {
        string[] shape =
        {
            ".....",
            "..A..",
            ".AAA.",
            "..A..",
            ".S.S."
        };

        float xOffset = ((hash & 3) - 1.5f) * 0.08f;
        float yOffset = -0.06f + (((hash >> 3) & 1) * 0.04f);
        BuildMappedShape(parent, shape, new Vector3(xOffset, yOffset, 0f), 0.12f, 3, cell =>
        {
            return cell switch
            {
                'A' => palette.FoliageColors[hash % palette.FoliageColors.Length],
                'S' => palette.ShadowColors[0],
                _ => default
            };
        });
    }

    private static void BuildCrack(Transform parent, BiomePalette palette, int hash)
    {
        string[] shape = (hash & 1) == 0
            ? new[]
            {
                "D....",
                ".D...",
                "..D..",
                "...D.",
                "..D.."
            }
            : new[]
            {
                "..D..",
                ".D.D.",
                "D...D",
                ".D.D.",
                "..D.."
            };

        BuildMappedShape(parent, shape, new Vector3(0f, -0.02f, 0f), 0.12f, 3, cell =>
        {
            return cell == 'D' ? palette.ShadowColors[hash % palette.ShadowColors.Length] : default;
        });
    }

    private static void BuildAccentPatch(Transform parent, BiomePalette palette, int hash, string biomeId)
    {
        string biome = string.IsNullOrWhiteSpace(biomeId) ? "default" : biomeId.ToLowerInvariant();
        string[] shape = biome switch
        {
            "swamp" => new[]
            {
                "..R..",
                ".R.R.",
                ".RLR.",
                "..R..",
                "..S.."
            },
            "crypt" => new[]
            {
                ".AAA.",
                "A...A",
                "..S..",
                "A...A",
                ".AAA."
            },
            _ => new[]
            {
                "..A..",
                ".AAA.",
                "AALAA",
                ".AAA.",
                "..S.."
            }
        };

        BuildMappedShape(parent, shape, new Vector3(0f, -0.02f, 0f), 0.12f, 3, cell =>
        {
            return cell switch
            {
                'A' => palette.AccentColors[(hash + 1) % palette.AccentColors.Length],
                'L' => palette.HighlightColors[(hash + 2) % palette.HighlightColors.Length],
                'R' => palette.FoliageColors[(hash + 3) % palette.FoliageColors.Length],
                'S' => palette.ShadowColors[0],
                _ => default
            };
        });
    }

    private static void BuildPuddle(Transform parent, BiomePalette palette, int hash)
    {
        string[] shape =
        {
            ".WWW.",
            "WWLWW",
            "WWWWW",
            ".WWW.",
            "..S.."
        };

        BuildMappedShape(parent, shape, new Vector3(0f, -0.04f, 0f), 0.12f, 3, cell =>
        {
            return cell switch
            {
                'W' => palette.LiquidColors[hash % palette.LiquidColors.Length],
                'L' => palette.HighlightColors[(hash + 1) % palette.HighlightColors.Length],
                'S' => palette.ShadowColors[0],
                _ => default
            };
        });
    }

    private static void BuildObstacleFooting(Transform parent, BiomePalette palette, Vector2Int gridPosition)
    {
        int hash = Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x, gridPosition.y, 41));
        BuildStrip(parent, new Vector2(-0.30f, -0.38f), 4, new Vector2(0.18f, 0f), palette.ShadowColors[0], 3, "Proc_FootShadow");

        if (!palette.UsesTrees)
        {
            Color supportColor = palette.StoneColors[hash % palette.StoneColors.Length];
            BuildStrip(parent, new Vector2(-0.22f, -0.20f), 3, new Vector2(0.18f, 0f), supportColor, 4, "Proc_FootStone");
        }
    }

    private static void BuildTree(Transform parent, BiomePalette palette, Vector2Int gridPosition)
    {
        int variant = Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x, gridPosition.y, 53)) % 3;
        string[] shape = variant switch
        {
            0 => new[]
            {
                "...LL....",
                "..LLLL...",
                ".LLLLLL..",
                "LLLLLLLL.",
                ".LLLLLL..",
                "..LTTL...",
                "..TTTT...",
                "..TTTT...",
                ".SSSSSS.."
            },
            1 => new[]
            {
                "...LL....",
                "..LLLL...",
                "..LLLL...",
                "...LL....",
                "..LLLL...",
                ".LLLLLL..",
                "...TT....",
                "...TT....",
                "..SSSS..."
            },
            _ => new[]
            {
                "...LL....",
                "..LLLL...",
                ".LLLCLL..",
                "..LLLL...",
                "...TT....",
                "..TTTT...",
                "..TMTT...",
                "..TTTT...",
                ".SSSSSS.."
            }
        };

        BuildMappedShape(parent, shape, new Vector3(0f, 0.08f, 0f), 0.12f, 4, cell =>
        {
            return cell switch
            {
                'L' => palette.FoliageColors[Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x, gridPosition.y, 67)) % palette.FoliageColors.Length],
                'C' => palette.HighlightColors[0],
                'T' => palette.BarkColors[0],
                'M' => palette.BarkColors[Mathf.Min(1, palette.BarkColors.Length - 1)],
                'S' => palette.ShadowColors[0],
                _ => default
            };
        });
    }

    private static void BuildRuinWall(Transform parent, BiomePalette palette, Vector2Int gridPosition)
    {
        int neighborMask = GetNeighborMask(gridPosition);
        int neighborCount = CountBits(neighborMask);
        string[] shape = neighborCount switch
        {
            0 or 1 => new[]
            {
                "...TT....",
                "..TSSS...",
                ".TSSCSS..",
                ".SSSSSS..",
                ".SSMMSS..",
                ".SSSSSS..",
                "..SDDS...",
                "..S..S..."
            },
            2 when (neighborMask & 5) != 0 => new[]
            {
                "...TT....",
                "..TSSS...",
                "..SSSS...",
                "..SSCS...",
                "..SSSS...",
                "..SSMS...",
                "..SDDS...",
                "..S..S..."
            },
            _ => new[]
            {
                ".TTTTTT..",
                ".TSSSSST.",
                ".SSCSSSS.",
                ".SSSSMSS.",
                ".SSSSSSS.",
                ".SSSDSSS.",
                ".SDDDDDS.",
                ".S.....S."
            }
        };

        BuildMappedShape(parent, shape, new Vector3(0f, 0.10f, 0f), 0.12f, 4, cell =>
        {
            int hash = Mathf.Abs(ProceduralPixelUtility.Hash(gridPosition.x, gridPosition.y, cell));
            return cell switch
            {
                'T' => palette.HighlightColors[hash % palette.HighlightColors.Length],
                'S' => palette.StoneColors[hash % palette.StoneColors.Length],
                'C' => palette.ShadowColors[(hash + 1) % palette.ShadowColors.Length],
                'M' => palette.AccentColors[hash % palette.AccentColors.Length],
                'D' => palette.ShadowColors[0],
                _ => default
            };
        });
    }

    private static void BuildStrip(Transform parent, Vector2 start, int count, Vector2 step, Color color, int sortingOrder, string namePrefix)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 localPosition = new Vector3(start.x + step.x * i, start.y + step.y * i, 0f);
            ProceduralPixelUtility.CreatePixel(parent, $"{namePrefix}_{i}", localPosition, 0.16f, color, sortingOrder);
        }
    }

    private static void BuildMappedShape(Transform parent, string[] shape, Vector3 offset, float pixelSize, int sortingOrder, System.Func<char, Color> colorResolver)
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

                Color color = colorResolver(cell);
                if (color.a <= 0f)
                    continue;

                Vector3 localPosition = new Vector3((column - halfWidth) * pixelSize, (halfHeight - row) * pixelSize, 0f) + offset;
                ProceduralPixelUtility.CreatePixel(parent, $"Proc_Shape_{sortingOrder}_{column}_{row}", localPosition, pixelSize, color, sortingOrder);
            }
        }
    }

    private static int GetNeighborMask(Vector2Int gridPosition)
    {
        WorldGrid worldGrid = WorldGrid.Instance;
        if (worldGrid == null)
            return 0;

        int mask = 0;
        if (worldGrid.HasWallAt(gridPosition + Vector2Int.up))
            mask |= 1;
        if (worldGrid.HasWallAt(gridPosition + Vector2Int.right))
            mask |= 2;
        if (worldGrid.HasWallAt(gridPosition + Vector2Int.down))
            mask |= 4;
        if (worldGrid.HasWallAt(gridPosition + Vector2Int.left))
            mask |= 8;
        return mask;
    }

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            count += mask & 1;
            mask >>= 1;
        }

        return count;
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
                        ProceduralPixelUtility.Blend(floorColor, new Color32(65, 90, 52, 255), 0.26f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(103, 132, 70, 255), 0.22f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(87, 115, 61, 255), 0.18f)
                    },
                    new Color[]
                    {
                        new Color32(128, 158, 92, 255),
                        new Color32(176, 128, 78, 255),
                        new Color32(201, 176, 97, 255)
                    },
                    new Color[]
                    {
                        new Color32(42, 88, 46, 255),
                        new Color32(58, 111, 52, 255),
                        new Color32(90, 136, 72, 255)
                    },
                    new Color[]
                    {
                        new Color32(96, 68, 44, 255),
                        new Color32(123, 89, 56, 255)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Multiply(wallColor, 0.82f),
                        new Color32(107, 96, 82, 255)
                    },
                    new Color[]
                    {
                        new Color32(66, 104, 87, 255),
                        new Color32(88, 132, 112, 255)
                    },
                    new Color[]
                    {
                        new Color32(175, 204, 132, 255),
                        new Color32(223, 204, 143, 255)
                    },
                    new Color[]
                    {
                        new Color32(35, 49, 31, 255),
                        new Color32(48, 63, 40, 255)
                    },
                    ProceduralPixelUtility.Blend(floorColor, new Color32(32, 50, 31, 255), 0.52f),
                    true,
                    false);
            case "swamp":
                return new BiomePalette(
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(47, 71, 57, 255), 0.24f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(70, 92, 71, 255), 0.28f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(92, 92, 56, 255), 0.18f)
                    },
                    new Color[]
                    {
                        new Color32(104, 125, 77, 255),
                        new Color32(136, 119, 74, 255),
                        new Color32(84, 142, 117, 255)
                    },
                    new Color[]
                    {
                        new Color32(58, 86, 59, 255),
                        new Color32(74, 107, 73, 255),
                        new Color32(97, 126, 76, 255)
                    },
                    new Color[]
                    {
                        new Color32(88, 72, 53, 255),
                        new Color32(109, 88, 63, 255)
                    },
                    new Color[]
                    {
                        new Color32(78, 81, 72, 255),
                        new Color32(59, 63, 57, 255),
                        new Color32(108, 94, 70, 255)
                    },
                    new Color[]
                    {
                        new Color32(61, 99, 93, 255),
                        new Color32(81, 131, 118, 255)
                    },
                    new Color[]
                    {
                        new Color32(165, 178, 115, 255),
                        new Color32(176, 200, 159, 255)
                    },
                    new Color[]
                    {
                        new Color32(38, 49, 42, 255),
                        new Color32(54, 64, 54, 255)
                    },
                    ProceduralPixelUtility.Blend(floorColor, new Color32(29, 40, 38, 255), 0.58f),
                    true,
                    true);
            case "crypt":
                return new BiomePalette(
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(180, 182, 204, 255), 0.26f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(145, 149, 173, 255), 0.22f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(116, 119, 144, 255), 0.20f)
                    },
                    new Color[]
                    {
                        new Color32(150, 159, 188, 255),
                        new Color32(126, 103, 154, 255),
                        new Color32(204, 208, 229, 255)
                    },
                    new Color[]
                    {
                        new Color32(88, 104, 99, 255),
                        new Color32(111, 121, 113, 255)
                    },
                    new Color[]
                    {
                        new Color32(109, 93, 78, 255),
                        new Color32(128, 110, 94, 255)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Blend(wallColor, new Color32(118, 121, 145, 255), 0.24f),
                        new Color32(80, 82, 101, 255)
                    },
                    new Color[]
                    {
                        new Color32(111, 128, 187, 255),
                        new Color32(145, 168, 233, 255)
                    },
                    new Color[]
                    {
                        new Color32(213, 219, 248, 255),
                        new Color32(186, 195, 229, 255)
                    },
                    new Color[]
                    {
                        new Color32(53, 58, 77, 255),
                        new Color32(70, 76, 98, 255)
                    },
                    ProceduralPixelUtility.Blend(wallColor, new Color32(34, 38, 59, 255), 0.56f),
                    false,
                    false);
            case "ruins":
                return new BiomePalette(
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(168, 151, 128, 255), 0.24f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(135, 126, 112, 255), 0.20f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(113, 132, 109, 255), 0.12f)
                    },
                    new Color[]
                    {
                        new Color32(113, 127, 101, 255),
                        new Color32(177, 154, 112, 255),
                        new Color32(205, 188, 133, 255)
                    },
                    new Color[]
                    {
                        new Color32(86, 105, 73, 255),
                        new Color32(108, 125, 83, 255)
                    },
                    new Color[]
                    {
                        new Color32(116, 83, 52, 255),
                        new Color32(138, 104, 69, 255)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Blend(wallColor, new Color32(149, 142, 131, 255), 0.28f),
                        new Color32(88, 92, 87, 255)
                    },
                    new Color[]
                    {
                        new Color32(103, 142, 147, 255),
                        new Color32(135, 176, 183, 255)
                    },
                    new Color[]
                    {
                        new Color32(223, 203, 158, 255),
                        new Color32(190, 187, 168, 255)
                    },
                    new Color[]
                    {
                        new Color32(58, 52, 46, 255),
                        new Color32(77, 73, 65, 255)
                    },
                    ProceduralPixelUtility.Blend(floorColor, new Color32(74, 62, 49, 255), 0.44f),
                    false,
                    false);
            default:
                return new BiomePalette(
                    new Color[]
                    {
                        floorColor,
                        ProceduralPixelUtility.Multiply(floorColor, 0.90f),
                        ProceduralPixelUtility.Blend(floorColor, Color.white, 0.10f)
                    },
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, wallColor, 0.24f),
                        ProceduralPixelUtility.Blend(floorColor, Color.white, 0.18f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(135, 148, 104, 255), 0.18f)
                    },
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(wallColor, floorColor, 0.26f),
                        ProceduralPixelUtility.Blend(floorColor, new Color32(82, 121, 76, 255), 0.22f)
                    },
                    new Color[]
                    {
                        new Color32(122, 92, 68, 255),
                        new Color32(146, 111, 81, 255)
                    },
                    new Color[]
                    {
                        wallColor,
                        ProceduralPixelUtility.Multiply(wallColor, 0.84f),
                        ProceduralPixelUtility.Blend(wallColor, floorColor, 0.20f)
                    },
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, new Color32(82, 115, 138, 255), 0.30f),
                        ProceduralPixelUtility.Blend(floorColor, Color.white, 0.10f)
                    },
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(floorColor, Color.white, 0.22f),
                        ProceduralPixelUtility.Blend(wallColor, Color.white, 0.18f)
                    },
                    new Color[]
                    {
                        ProceduralPixelUtility.Blend(wallColor, Color.black, 0.38f),
                        ProceduralPixelUtility.Blend(floorColor, Color.black, 0.28f)
                    },
                    ProceduralPixelUtility.Blend(floorColor, wallColor, 0.42f),
                    false,
                    false);
        }
    }
}
