using UnityEngine;

public static class HeroHudPanel
{
    public static void Draw(RunManager runManager)
    {
        PlayerGridMovement player = runManager.Player;
        float width = Mathf.Min(490f, Screen.width * 0.31f);
        Rect area = new Rect(18f, 18f, width, 308f);
        RunUiTheme.DrawPanel(area, new Color32(18, 24, 31, 238), new Color32(108, 145, 176, 255));

        GUI.Label(new Rect(area.x + 18f, area.y + 16f, area.width - 36f, 30f), "Architectus Fati", RunUiTheme.TitleStyle);
        GUI.Label(new Rect(area.x + 18f, area.y + 46f, area.width - 36f, 22f), $"Estat de la run: <b>{runManager.CurrentState}</b>", RunUiTheme.BodyStyle);

        if (runManager.CurrentRun != null)
            DrawBadgeRow(new Rect(area.x + 18f, area.y + 78f, area.width - 36f, 34f), runManager);

        if (player != null)
        {
            GUI.Label(new Rect(area.x + 18f, area.y + 122f, area.width - 36f, 24f), "Heroi", RunUiTheme.SubtitleStyle);
            DrawHeroHealthBlock(new Rect(area.x + 18f, area.y + 150f, area.width - 36f, 54f), player);
            DrawHeroStats(new Rect(area.x + 18f, area.y + 212f, area.width - 36f, 82f), player);
        }

        if (runManager.CurrentSegment != null)
        {
            Rect segmentRect = new Rect(area.x + area.width + 18f, 18f, Mathf.Min(360f, Screen.width * 0.23f), 132f);
            RunUiTheme.DrawPanel(segmentRect, new Color32(20, 26, 34, 226), new Color32(110, 132, 164, 255));
            GUI.Label(new Rect(segmentRect.x + 16f, segmentRect.y + 14f, segmentRect.width - 32f, 24f), "Segment actual", RunUiTheme.SubtitleStyle);
            GUI.Label(new Rect(segmentRect.x + 16f, segmentRect.y + 40f, segmentRect.width - 32f, 24f), runManager.CurrentSegment.card.displayName, RunUiTheme.BodyStyle);
            GUI.Label(new Rect(segmentRect.x + 16f, segmentRect.y + 64f, segmentRect.width - 32f, 20f), $"Bioma: {RunUiTheme.FormatBiome(runManager.CurrentSegment.card.biomeId)}", RunUiTheme.MutedStyle);
            GUI.Label(new Rect(segmentRect.x + 16f, segmentRect.y + 84f, segmentRect.width - 32f, 20f), $"Tipus: {runManager.CurrentSegment.card.cardType}  |  Enemics: {runManager.CurrentSegment.enemySpawns.Count}", RunUiTheme.MutedStyle);
            GUI.Label(new Rect(segmentRect.x + 16f, segmentRect.y + 104f, segmentRect.width - 32f, 20f), $"Feedback: {TrimFeedback(runManager.FeedbackMessage)}", RunUiTheme.MutedStyle);
        }
    }

    private static void DrawBadgeRow(Rect rect, RunManager runManager)
    {
        float gap = 8f;
        float badgeWidth = (rect.width - gap * 2f) / 3f;

        RunUiTheme.DrawBadge(new Rect(rect.x, rect.y, badgeWidth, rect.height),
            $"Segment {Mathf.Min(runManager.CurrentRun.segmentsCleared + 1, runManager.CurrentRun.targetSegmentCount)}/{runManager.CurrentRun.targetSegmentCount}",
            new Color32(95, 128, 173, 255), new Color32(16, 20, 28, 255));
        RunUiTheme.DrawBadge(new Rect(rect.x + badgeWidth + gap, rect.y, badgeWidth, rect.height),
            $"Or {runManager.CurrentGold}  |  Esm. {runManager.CurrentEmeralds}",
            new Color32(216, 183, 102, 255), new Color32(21, 18, 14, 255));
        RunUiTheme.DrawBadge(new Rect(rect.x + (badgeWidth + gap) * 2f, rect.y, badgeWidth, rect.height),
            runManager.EffectiveHeroMode,
            new Color32(129, 173, 128, 255), new Color32(19, 24, 17, 255));
    }

    private static void DrawHeroHealthBlock(Rect rect, PlayerGridMovement player)
    {
        RunUiTheme.DrawPanel(rect, new Color32(36, 28, 31, 245), new Color32(158, 90, 97, 255));
        GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, 140f, 18f), "Vitalitat", RunUiTheme.MutedStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 24f, 160f, 24f), $"{player.CurrentHealth}/{player.MaxHealth}", RunUiTheme.SubtitleStyle);

        Rect barRect = new Rect(rect.x + 148f, rect.y + 16f, rect.width - 160f, 18f);
        DrawProgressBar(barRect, player.MaxHealth <= 0 ? 0f : player.CurrentHealth / (float)player.MaxHealth, new Color32(176, 70, 76, 255), new Color32(67, 31, 34, 255));

        string combatText = player.HasRecentCombat
            ? $"Ultim combat  +{player.LastCombatDamageDealt} / -{player.LastCombatDamageTaken}"
            : "Sense combat recent";
        GUI.Label(new Rect(rect.x + 148f, rect.y + 36f, rect.width - 160f, 16f), combatText, RunUiTheme.MutedStyle);
    }

    private static void DrawHeroStats(Rect rect, PlayerGridMovement player)
    {
        float gap = 8f;
        float cardWidth = (rect.width - gap * 3f) / 4f;
        DrawStatCard(new Rect(rect.x, rect.y, cardWidth, rect.height), "Vida max", player.MaxHealth.ToString(), new Color32(128, 62, 66, 255));
        DrawStatCard(new Rect(rect.x + (cardWidth + gap), rect.y, cardWidth, rect.height), "Atac", player.Attack.ToString(), new Color32(128, 86, 51, 255));
        DrawStatCard(new Rect(rect.x + (cardWidth + gap) * 2f, rect.y, cardWidth, rect.height), "Def", player.Defense.ToString(), new Color32(68, 92, 126, 255));
        DrawStatCard(new Rect(rect.x + (cardWidth + gap) * 3f, rect.y, cardWidth, rect.height), "Vel", player.CombatSpeed.ToString("0.00"), new Color32(77, 118, 101, 255));
    }

    private static void DrawStatCard(Rect rect, string label, string value, Color color)
    {
        RunUiTheme.DrawPanel(rect, color, Color.Lerp(color, Color.white, 0.45f));
        GUI.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 18f), label, RunUiTheme.MutedStyle);
        GUI.Label(new Rect(rect.x + 10f, rect.y + 32f, rect.width - 20f, 28f), value, RunUiTheme.StatValueStyle);
    }

    private static void DrawProgressBar(Rect rect, float normalized, Color fillColor, Color backgroundColor)
    {
        normalized = Mathf.Clamp01(normalized);
        Color previous = GUI.color;
        GUI.color = backgroundColor;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * normalized, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static string TrimFeedback(string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
            return "esperant esdeveniments";

        return feedback.Length > 42 ? feedback.Substring(0, 39) + "..." : feedback;
    }
}
