using System;
using System.Collections.Generic;
using UnityEngine;

public static class CardChoiceOverlayPanel
{
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private static GUIStyle cardNameStyle;
    private static GUIStyle cardMetaStyle;
    private static GUIStyle cardTypeStyle;
    private static GUIStyle cardBodyStyle;
    private static GUIStyle cardSmallStyle;
    private static GUIStyle flavorStyle;
    private static bool stylesReady;

    public static void Draw(RunManager runManager)
    {
        CardSeedData[] cards = SnapshotCards(runManager);
        if (cards.Length == 0)
            return;

        EnsureStyles();
        DrawBackdrop();

        float gap = 24f;
        float sideMargin = 42f;
        float cardWidth = Mathf.Min(320f, (Screen.width - sideMargin * 2f - gap * (cards.Length - 1)) / cards.Length);
        float cardHeight = Mathf.Min(500f, Screen.height - 150f);
        float totalWidth = cardWidth * cards.Length + gap * (cards.Length - 1);
        float startX = (Screen.width - totalWidth) * 0.5f;
        float cardY = Mathf.Clamp((Screen.height - cardHeight) * 0.5f + 24f, 86f, Screen.height - cardHeight - 24f);

        Rect titleRect = new Rect(0f, cardY - 68f, Screen.width, 30f);
        GUI.Label(titleRect, "Porta del desti", RunUiTheme.TitleStyle);
        GUI.Label(new Rect(0f, cardY - 36f, Screen.width, 24f), "Escull la propera carta de bioma", RunUiTheme.BodyStyle);

        for (int i = 0; i < cards.Length; i++)
        {
            Rect cardRect = new Rect(startX + i * (cardWidth + gap), cardY, cardWidth, cardHeight);
            if (DrawCard(cardRect, cards[i], i, runManager))
                return;
        }
    }

    private static bool DrawCard(Rect rect, CardSeedData card, int index, RunManager runManager)
    {
        Color biomeColor = RunUiTheme.GetBiomeColor(card.biomeId);
        Color outerColor = Color.Lerp(new Color32(39, 30, 24, 255), biomeColor, 0.35f);
        Color paperColor = new Color32(233, 226, 207, 255);
        Color inkColor = new Color32(30, 24, 20, 255);

        DrawFilledRect(new Rect(rect.x - 6f, rect.y - 6f, rect.width + 12f, rect.height + 12f), new Color(0f, 0f, 0f, 0.28f));
        RunUiTheme.DrawPanel(rect, outerColor, new Color32(28, 22, 18, 255));

        Rect parchment = new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, rect.height - 20f);
        DrawFilledRect(parchment, paperColor);
        DrawFilledRect(new Rect(parchment.x, parchment.y, parchment.width, 3f), new Color32(96, 82, 63, 255));
        DrawFilledRect(new Rect(parchment.x, parchment.yMax - 3f, parchment.width, 3f), new Color32(96, 82, 63, 255));
        DrawFilledRect(new Rect(parchment.x, parchment.y, 3f, parchment.height), new Color32(96, 82, 63, 255));
        DrawFilledRect(new Rect(parchment.xMax - 3f, parchment.y, 3f, parchment.height), new Color32(96, 82, 63, 255));

        GUI.BeginGroup(parchment);

        Rect nameBar = new Rect(10f, 10f, parchment.width - 20f, 34f);
        DrawFilledRect(nameBar, new Color32(248, 243, 232, 255));
        GUI.Label(new Rect(18f, 13f, parchment.width - 120f, 28f), card.displayName, cardNameStyle);
        RunUiTheme.DrawBadge(new Rect(parchment.width - 74f, 12f, 56f, 28f), $"{card.baseDifficulty}", biomeColor, inkColor);

        Rect artFrame = new Rect(10f, 52f, parchment.width - 20f, 206f);
        DrawFilledRect(artFrame, new Color32(57, 52, 47, 255));
        DrawCardArt(new Rect(artFrame.x + 6f, artFrame.y + 6f, artFrame.width - 12f, artFrame.height - 12f), card, biomeColor);

        Rect typeBar = new Rect(10f, 268f, parchment.width - 20f, 28f);
        DrawFilledRect(typeBar, new Color32(246, 241, 231, 255));
        GUI.Label(new Rect(18f, 272f, parchment.width - 36f, 20f), $"{RunUiTheme.FormatBiome(card.biomeId)}  |  {card.cardType.ToUpperInvariant()}  |  Segment {card.segmentWidth}x{card.segmentHeight}", cardTypeStyle);

        Rect textBox = new Rect(10f, 306f, parchment.width - 20f, 122f);
        DrawFilledRect(textBox, new Color32(250, 247, 238, 255));
        DrawFilledRect(new Rect(textBox.x, textBox.y, textBox.width, 2f), new Color32(190, 175, 149, 255));
        DrawFilledRect(new Rect(textBox.x, textBox.yMax - 2f, textBox.width, 2f), new Color32(190, 175, 149, 255));

        GUI.Label(new Rect(18f, 314f, parchment.width - 36f, 48f), card.description, cardBodyStyle);
        GUI.Label(new Rect(18f, 362f, parchment.width - 36f, 18f), $"Enemics: {FormatEnemies(card.enemyIds)}", cardSmallStyle);
        GUI.Label(new Rect(18f, 380f, parchment.width - 36f, 18f), $"Risc: {(card.enemyChance * 100f):0}% enemics  |  {(card.obstacleChance * 100f):0}% obstacles", cardSmallStyle);
        GUI.Label(new Rect(18f, 398f, parchment.width - 36f, 18f), $"Reward tier {card.rewardTier}  |  Desbloqueig botiga: {FormatShopSegment(card.shopUnlockSegment)}", cardSmallStyle);
        GUI.Label(new Rect(18f, 416f, parchment.width - 36f, 18f), FormatFlavor(card), flavorStyle);

        Rect chooseRect = new Rect(10f, parchment.height - 48f, parchment.width - 20f, 38f);
        RunUiTheme.DrawButtonBackground(chooseRect, new Color32(212, 198, 175, 255));
        bool selected = GUI.Button(chooseRect, "Escollir carta", RunUiTheme.PrimaryButtonStyle);

        GUI.EndGroup();

        if (selected)
        {
            runManager.SelectCard(index);
            return true;
        }

        return false;
    }

    private static void DrawCardArt(Rect rect, CardSeedData card, Color biomeColor)
    {
        Sprite sprite = LoadSprite(card.artKey);
        if (sprite != null)
        {
            Rect uv = GetSpriteUvRect(sprite);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            return;
        }

        DrawFilledRect(rect, Color.Lerp(new Color32(28, 31, 36, 255), biomeColor, 0.28f));
        GUI.Label(new Rect(rect.x, rect.y + rect.height * 0.35f, rect.width, 24f), "Artwork pendent", RunUiTheme.SubtitleStyle);
        GUI.Label(new Rect(rect.x, rect.y + rect.height * 0.35f + 26f, rect.width, 20f), FormatValue(card.artKey), RunUiTheme.MutedStyle);
    }

    private static void DrawBackdrop()
    {
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void DrawFilledRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static Sprite LoadSprite(string artKey)
    {
        if (string.IsNullOrWhiteSpace(artKey))
            return null;

        if (SpriteCache.TryGetValue(artKey, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>($"CardArt/{artKey}");
        SpriteCache[artKey] = sprite;
        return sprite;
    }

    private static Rect GetSpriteUvRect(Sprite sprite)
    {
        Rect textureRect = sprite.textureRect;
        Texture texture = sprite.texture;
        return new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);
    }

    private static string FormatEnemies(string[] enemyIds)
    {
        if (enemyIds == null || enemyIds.Length == 0)
            return "sense dades";

        return string.Join(", ", enemyIds);
    }

    private static string FormatShopSegment(int shopUnlockSegment)
    {
        return shopUnlockSegment <= 0 ? "immediat" : shopUnlockSegment.ToString();
    }

    private static string FormatFlavor(CardSeedData card)
    {
        string biome = RunUiTheme.FormatBiome(card.biomeId);
        return $"Un fragment de {biome.ToLowerInvariant()} que obre un nou desti.";
    }

    private static string FormatValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "sense_art" : value;
    }

    private static CardSeedData[] SnapshotCards(RunManager runManager)
    {
        int count = runManager.CurrentCardChoices.Count;
        CardSeedData[] snapshot = new CardSeedData[count];
        for (int i = 0; i < count; i++)
            snapshot[i] = runManager.CurrentCardChoices[i];
        return snapshot;
    }

    private static void EnsureStyles()
    {
        if (stylesReady)
            return;

        float scale = Mathf.Clamp(Screen.height / 1080f, 0.9f, 1.15f);
        Color inkColor = new Color32(31, 25, 20, 255);
        Color softInkColor = new Color32(84, 70, 58, 255);

        cardNameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16f * scale),
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = inkColor }
        };

        cardMetaStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(12f * scale),
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = softInkColor }
        };

        cardTypeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(12f * scale),
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = inkColor }
        };

        cardBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(13f * scale),
            wordWrap = true,
            normal = { textColor = inkColor }
        };

        cardSmallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(11f * scale),
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = softInkColor }
        };

        flavorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(11f * scale),
            fontStyle = FontStyle.Italic,
            wordWrap = true,
            normal = { textColor = new Color32(108, 94, 80, 255) }
        };

        stylesReady = true;
    }
}
