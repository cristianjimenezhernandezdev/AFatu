using UnityEngine;

public static class HeroHudPanel
{
    public static void Draw(RunManager runManager)
    {
        PlayerGridMovement player = runManager.Player;
        Rect area = new Rect(16f, 16f, 430f, 320f);
        RunUiTheme.DrawPanel(area, new Color32(18, 24, 31, 235), new Color32(108, 145, 176, 255));

        GUILayout.BeginArea(new Rect(area.x + 18f, area.y + 18f, area.width - 36f, area.height - 36f));
        GUILayout.Label("Architectus Fati", RunUiTheme.TitleStyle);
        GUILayout.Label($"Estat de la run: <b>{runManager.CurrentState}</b>", RunUiTheme.BodyStyle);

        if (runManager.CurrentRun != null)
        {
            GUILayout.Space(6f);
            DrawBadgeRow(runManager);
        }

        if (player != null)
        {
            GUILayout.Space(12f);
            GUILayout.Label("Heroi", RunUiTheme.SubtitleStyle);
            DrawHeroStats(player);
        }

        if (runManager.CurrentSegment != null)
        {
            GUILayout.Space(12f);
            GUILayout.Label("Segment actual", RunUiTheme.SubtitleStyle);
            string cardName = runManager.CurrentSegment.card.displayName;
            string biomeName = RunUiTheme.FormatBiome(runManager.CurrentSegment.card.biomeId);
            GUILayout.Label($"<b>{cardName}</b>", RunUiTheme.BodyStyle);
            GUILayout.Label($"Bioma: {biomeName}  |  Tipus: {runManager.CurrentSegment.card.cardType}  |  Enemics: {runManager.CurrentSegment.enemySpawns.Count}", RunUiTheme.MutedStyle);
        }

        if (!string.IsNullOrWhiteSpace(runManager.FeedbackMessage))
        {
            GUILayout.Space(10f);
            Rect feedbackRect = GUILayoutUtility.GetRect(1f, 44f, GUILayout.ExpandWidth(true));
            RunUiTheme.DrawPanel(feedbackRect, new Color32(32, 41, 52, 245), new Color32(201, 176, 103, 255));
            GUI.Label(new Rect(feedbackRect.x + 12f, feedbackRect.y + 10f, feedbackRect.width - 24f, feedbackRect.height - 20f), runManager.FeedbackMessage, RunUiTheme.BodyStyle);
        }

        GUILayout.Space(12f);
        GUILayout.Label("Poders divins", RunUiTheme.SubtitleStyle);
        for (int i = 0; i < runManager.EquippedDivinePowers.Count; i++)
        {
            DivinePowerSeedData power = runManager.EquippedDivinePowers[i];
            float cooldown = runManager.GetDivinePowerCooldownSeconds(i);
            string suffix = cooldown > 0.01f ? $" ({Mathf.CeilToInt(cooldown)}s)" : "";
            Rect buttonRect = GUILayoutUtility.GetRect(1f, 42f, GUILayout.ExpandWidth(true));
            RunUiTheme.DrawButtonBackground(buttonRect, cooldown > 0.01f ? new Color32(108, 118, 132, 255) : new Color32(222, 193, 119, 255));
            if (GUI.Button(buttonRect, $"{i + 1}. {power.displayName}{suffix}", RunUiTheme.PrimaryButtonStyle))
                runManager.TryActivateDivinePowerSlot(i);
        }

        GUILayout.EndArea();
    }

    private static void DrawBadgeRow(RunManager runManager)
    {
        Rect rowRect = GUILayoutUtility.GetRect(1f, 32f, GUILayout.ExpandWidth(true));
        float gap = 8f;
        float badgeWidth = (rowRect.width - gap * 2f) / 3f;

        RunUiTheme.DrawBadge(new Rect(rowRect.x, rowRect.y, badgeWidth, rowRect.height),
            $"Segment {Mathf.Min(runManager.CurrentRun.segmentsCleared + 1, runManager.CurrentRun.targetSegmentCount)}/{runManager.CurrentRun.targetSegmentCount}",
            new Color32(95, 128, 173, 255), new Color32(16, 20, 28, 255));
        RunUiTheme.DrawBadge(new Rect(rowRect.x + badgeWidth + gap, rowRect.y, badgeWidth, rowRect.height),
            $"Or {runManager.CurrentGold} | Esmeraldes {runManager.CurrentEmeralds}",
            new Color32(216, 183, 102, 255), new Color32(21, 18, 14, 255));
        RunUiTheme.DrawBadge(new Rect(rowRect.x + (badgeWidth + gap) * 2f, rowRect.y, badgeWidth, rowRect.height),
            runManager.EffectiveHeroMode,
            new Color32(129, 173, 128, 255), new Color32(19, 24, 17, 255));
    }

    private static void DrawHeroStats(PlayerGridMovement player)
    {
        Rect statsRect = GUILayoutUtility.GetRect(1f, 82f, GUILayout.ExpandWidth(true));
        float gap = 8f;
        float cardWidth = (statsRect.width - gap * 3f) / 4f;
        DrawStatCard(new Rect(statsRect.x, statsRect.y, cardWidth, statsRect.height), "Vida", $"{player.CurrentHealth}/{player.MaxHealth}", new Color32(128, 62, 66, 255));
        DrawStatCard(new Rect(statsRect.x + (cardWidth + gap), statsRect.y, cardWidth, statsRect.height), "Atac", player.Attack.ToString(), new Color32(128, 86, 51, 255));
        DrawStatCard(new Rect(statsRect.x + (cardWidth + gap) * 2f, statsRect.y, cardWidth, statsRect.height), "Defensa", player.Defense.ToString(), new Color32(68, 92, 126, 255));
        DrawStatCard(new Rect(statsRect.x + (cardWidth + gap) * 3f, statsRect.y, cardWidth, statsRect.height), "Velocitat", player.CombatSpeed.ToString("0.00"), new Color32(77, 118, 101, 255));
    }

    private static void DrawStatCard(Rect rect, string label, string value, Color color)
    {
        RunUiTheme.DrawPanel(rect, color, Color.Lerp(color, Color.white, 0.45f));
        GUI.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 22f), label, RunUiTheme.MutedStyle);
        GUI.Label(new Rect(rect.x + 10f, rect.y + 34f, rect.width - 20f, 30f), value, RunUiTheme.StatValueStyle);
    }
}
