using UnityEngine;

public static class RunUiTheme
{
    private static bool initialized;
    private static GUIStyle panelStyle;
    private static GUIStyle titleStyle;
    private static GUIStyle subtitleStyle;
    private static GUIStyle bodyStyle;
    private static GUIStyle mutedStyle;
    private static GUIStyle statValueStyle;
    private static GUIStyle badgeStyle;
    private static GUIStyle primaryButtonStyle;
    private static GUIStyle cardButtonStyle;
    private static GUIStyle summaryButtonStyle;
    private static Texture2D whiteTexture;

    public static GUIStyle PanelStyle => panelStyle;
    public static GUIStyle TitleStyle => titleStyle;
    public static GUIStyle SubtitleStyle => subtitleStyle;
    public static GUIStyle BodyStyle => bodyStyle;
    public static GUIStyle MutedStyle => mutedStyle;
    public static GUIStyle StatValueStyle => statValueStyle;
    public static GUIStyle BadgeStyle => badgeStyle;
    public static GUIStyle PrimaryButtonStyle => primaryButtonStyle;
    public static GUIStyle CardButtonStyle => cardButtonStyle;
    public static GUIStyle SummaryButtonStyle => summaryButtonStyle;

    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        whiteTexture = Texture2D.whiteTexture;
        float scale = Mathf.Clamp(Screen.height / 1080f, 0.9f, 1.25f);

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(18, 18, 18, 18),
            margin = new RectOffset(0, 0, 0, 0),
            normal = { background = whiteTexture, textColor = Color.white }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(24f * scale),
            fontStyle = FontStyle.Bold,
            richText = true,
            wordWrap = true,
            normal = { textColor = new Color32(244, 233, 209, 255) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16f * scale),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color32(220, 208, 176, 255) }
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(14f * scale),
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color32(233, 230, 221, 255) }
        };

        mutedStyle = new GUIStyle(bodyStyle)
        {
            fontSize = Mathf.RoundToInt(12f * scale),
            normal = { textColor = new Color32(173, 178, 185, 255) }
        };

        statValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(18f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color32(255, 240, 198, 255) }
        };

        badgeStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(12f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 6, 6),
            normal = { background = whiteTexture, textColor = new Color32(38, 30, 24, 255) }
        };

        primaryButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(15f * scale),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(14, 14, 10, 10),
            normal = { background = whiteTexture, textColor = new Color32(23, 18, 16, 255) },
            hover = { background = whiteTexture, textColor = new Color32(23, 18, 16, 255) },
            active = { background = whiteTexture, textColor = new Color32(23, 18, 16, 255) }
        };

        cardButtonStyle = new GUIStyle(primaryButtonStyle)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.RoundToInt(14f * scale),
            padding = new RectOffset(16, 16, 16, 16)
        };

        summaryButtonStyle = new GUIStyle(primaryButtonStyle)
        {
            fontSize = Mathf.RoundToInt(18f * scale)
        };

        initialized = true;
    }

    public static void DrawPanel(Rect rect, Color fillColor, Color borderColor)
    {
        Color previous = GUI.color;
        GUI.color = fillColor;
        GUI.Box(rect, GUIContent.none, panelStyle);
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 3f, rect.y, 3f, rect.height), whiteTexture);
        GUI.color = previous;
    }

    public static void DrawBadge(Rect rect, string text, Color fillColor, Color textColor)
    {
        Color previous = GUI.color;
        GUI.color = fillColor;
        GUI.Box(rect, GUIContent.none, badgeStyle);
        GUI.color = previous;

        Color oldContent = GUI.contentColor;
        GUI.contentColor = textColor;
        GUI.Label(rect, text, badgeStyle);
        GUI.contentColor = oldContent;
    }

    public static void DrawButtonBackground(Rect rect, Color fillColor)
    {
        Color previous = GUI.color;
        GUI.color = fillColor;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = previous;
    }

    public static void DrawDivider(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = previous;
    }

    public static Color GetBiomeColor(string biomeId)
    {
        switch ((biomeId ?? string.Empty).ToLowerInvariant())
        {
            case "dark_forest": return new Color32(88, 132, 82, 255);
            case "swamp": return new Color32(121, 140, 86, 255);
            case "crypt": return new Color32(135, 145, 198, 255);
            case "ruins":
            default: return new Color32(183, 144, 94, 255);
        }
    }

    public static string FormatBiome(string biomeId)
    {
        if (string.IsNullOrWhiteSpace(biomeId))
            return "Desconegut";

        string[] parts = biomeId.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
                continue;
            parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
        }

        return string.Join(" ", parts);
    }
}
