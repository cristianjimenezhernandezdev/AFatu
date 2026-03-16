using System.Collections.Generic;
using UnityEngine;

public static class CardChoiceOverlayPanel
{
    private static readonly Dictionary<int, Vector2> ScrollPositions = new Dictionary<int, Vector2>();

    public static void Draw(RunManager runManager)
    {
        CardSeedData[] cards = SnapshotCards(runManager);
        if (cards.Length == 0)
            return;

        float width = Mathf.Min(1380f, Screen.width - 36f);
        float height = Mathf.Min(460f, Screen.height - 40f);
        Rect area = new Rect((Screen.width - width) * 0.5f, Mathf.Max(20f, Screen.height - height - 18f), width, height);
        RunUiTheme.DrawPanel(area, new Color32(19, 16, 23, 244), new Color32(184, 151, 95, 255));

        GUI.Label(new Rect(area.x + 24f, area.y + 18f, area.width - 48f, 34f), "Porta del desti", RunUiTheme.TitleStyle);
        GUI.Label(new Rect(area.x + 24f, area.y + 54f, area.width - 48f, 28f), "Tria la carta que definira el proxim segment. Ara es mostren les dades clau del seed carregat des de la BDD local.", RunUiTheme.BodyStyle);
        RunUiTheme.DrawDivider(new Rect(area.x + 24f, area.y + 92f, area.width - 48f, 2f), new Color32(124, 105, 78, 255));

        float contentY = area.y + 108f;
        float gap = 16f;
        float cardWidth = (area.width - 48f - gap * (cards.Length - 1)) / cards.Length;
        float cardHeight = area.height - 132f;

        for (int i = 0; i < cards.Length; i++)
        {
            Rect cardRect = new Rect(area.x + 24f + (cardWidth + gap) * i, contentY, cardWidth, cardHeight);
            if (DrawCard(cardRect, cards[i], i, runManager))
                return;
        }
    }

    private static bool DrawCard(Rect rect, CardSeedData card, int index, RunManager runManager)
    {
        Color biomeColor = RunUiTheme.GetBiomeColor(card.biomeId);
        RunUiTheme.DrawPanel(rect, Color.Lerp(new Color32(29, 31, 37, 255), biomeColor, 0.24f), biomeColor);

        Rect inner = new Rect(rect.x + 16f, rect.y + 16f, rect.width - 32f, rect.height - 32f);
        GUI.BeginGroup(inner);
        GUI.Label(new Rect(0f, 0f, inner.width, 28f), $"{index + 1}. {card.displayName}", RunUiTheme.SubtitleStyle);
        GUI.Label(new Rect(0f, 28f, inner.width, 20f), $"ID: {card.cardId}", RunUiTheme.MutedStyle);
        GUI.Label(new Rect(0f, 50f, inner.width, 46f), card.description, RunUiTheme.BodyStyle);

        float badgeWidth = Mathf.Max(90f, (inner.width - 16f) / 3f);
        RunUiTheme.DrawBadge(new Rect(0f, 100f, badgeWidth, 28f), RunUiTheme.FormatBiome(card.biomeId), biomeColor, new Color32(16, 18, 23, 255));
        RunUiTheme.DrawBadge(new Rect(badgeWidth + 8f, 100f, badgeWidth - 8f, 28f), card.cardType.ToUpperInvariant(), new Color32(223, 205, 124, 255), new Color32(24, 20, 16, 255));
        RunUiTheme.DrawBadge(new Rect((badgeWidth + 8f) * 2f, 100f, inner.width - (badgeWidth + 8f) * 2f, 28f), $"Dificultat {card.baseDifficulty}", new Color32(121, 139, 184, 255), new Color32(22, 23, 30, 255));

        Rect scrollRect = new Rect(0f, 138f, inner.width, inner.height - 196f);
        float viewHeight = 240f + Mathf.Max(0, ((card.enemyIds?.Length ?? 0) - 3) * 16f) + Mathf.Max(0, ((card.generationTags?.Length ?? 0) - 2) * 16f);
        Rect viewRect = new Rect(0f, 0f, inner.width - 20f, viewHeight);
        Vector2 scroll = ScrollPositions.TryGetValue(index, out Vector2 existing) ? existing : Vector2.zero;
        scroll = GUI.BeginScrollView(scrollRect, scroll, viewRect, false, true);
        ScrollPositions[index] = scroll;

        GUI.Label(new Rect(0f, 0f, viewRect.width, 20f), $"Mida del segment: {card.segmentWidth}x{card.segmentHeight}", RunUiTheme.BodyStyle);
        GUI.Label(new Rect(0f, 22f, viewRect.width, 20f), $"Entrada X: {card.entryX}   |   Sortida X: {card.exitX}", RunUiTheme.BodyStyle);
        GUI.Label(new Rect(0f, 44f, viewRect.width, 20f), $"Reward tier: {card.rewardTier}   |   Sort order: {card.sortOrder}", RunUiTheme.MutedStyle);
        GUI.Label(new Rect(0f, 66f, viewRect.width, 20f), $"Obstacle chance: {(card.obstacleChance * 100f):0}%", RunUiTheme.MutedStyle);
        GUI.Label(new Rect(0f, 86f, viewRect.width, 20f), $"Enemy chance: {(card.enemyChance * 100f):0}%", RunUiTheme.MutedStyle);
        GUI.Label(new Rect(0f, 112f, viewRect.width, 42f), $"Enemics del seed: {string.Join(", ", card.enemyIds ?? System.Array.Empty<string>())}", RunUiTheme.BodyStyle);
        GUI.Label(new Rect(0f, 156f, viewRect.width, 42f), $"Tags de generacio: {string.Join(", ", card.generationTags ?? System.Array.Empty<string>())}", RunUiTheme.BodyStyle);
        GUI.Label(new Rect(0f, 202f, viewRect.width, 22f), $"Starts unlocked: {(card.startsUnlocked ? "si" : "no")}", RunUiTheme.MutedStyle);
        GUI.EndScrollView();

        Rect chooseRect = new Rect(0f, inner.height - 46f, inner.width, 46f);
        RunUiTheme.DrawButtonBackground(chooseRect, new Color32(224, 191, 114, 255));
        bool selected = GUI.Button(chooseRect, "Escollir carta", RunUiTheme.PrimaryButtonStyle);
        GUI.EndGroup();

        if (selected)
        {
            runManager.SelectCard(index);
            return true;
        }

        return false;
    }

    private static CardSeedData[] SnapshotCards(RunManager runManager)
    {
        int count = runManager.CurrentCardChoices.Count;
        CardSeedData[] snapshot = new CardSeedData[count];
        for (int i = 0; i < count; i++)
            snapshot[i] = runManager.CurrentCardChoices[i];
        return snapshot;
    }
}
