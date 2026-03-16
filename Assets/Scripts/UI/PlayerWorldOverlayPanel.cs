using UnityEngine;

public static class PlayerWorldOverlayPanel
{
    public static void Draw(RunManager runManager)
    {
        if (runManager == null || runManager.Player == null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        PlayerGridMovement player = runManager.Player;
        Vector3 anchor = player.transform.position + new Vector3(0f, 1.15f, 0f);
        Vector3 screenPoint = camera.WorldToScreenPoint(anchor);
        if (screenPoint.z <= 0f)
            return;

        float guiX = screenPoint.x - 44f;
        float guiY = Screen.height - screenPoint.y - 28f;
        Rect panelRect = new Rect(guiX, guiY, 88f, 28f);

        DrawRect(new Rect(panelRect.x - 2f, panelRect.y - 2f, panelRect.width + 4f, panelRect.height + 4f), new Color(0f, 0f, 0f, 0.42f));
        DrawRect(panelRect, new Color32(28, 31, 36, 230));
        DrawHealthBar(new Rect(panelRect.x + 4f, panelRect.y + 4f, panelRect.width - 8f, 10f), player.CurrentHealth, player.MaxHealth);
        GUI.Label(new Rect(panelRect.x + 4f, panelRect.y + 14f, panelRect.width - 8f, 12f), $"{player.CurrentHealth}/{player.MaxHealth}", RunUiTheme.MutedStyle);

        if (!player.HasRecentCombat)
            return;

        float pulse = player.RecentCombatPulse;
        Rect combatRect = new Rect(panelRect.x - 8f, panelRect.y - 20f, panelRect.width + 16f, 16f);
        DrawRect(combatRect, Color.Lerp(new Color32(121, 54, 46, 255), new Color32(211, 159, 81, 255), Mathf.Clamp01(player.LastCombatDamageDealt * 0.15f)) * new Color(1f, 1f, 1f, 0.55f + pulse * 0.25f));
        GUI.Label(combatRect, $"+{player.LastCombatDamageDealt} / -{player.LastCombatDamageTaken}", RunUiTheme.MutedStyle);
    }

    private static void DrawHealthBar(Rect rect, int current, int max)
    {
        float normalized = max <= 0 ? 0f : current / (float)max;
        DrawRect(rect, new Color32(70, 31, 33, 255));
        DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(normalized), rect.height), new Color32(193, 72, 79, 255));
        DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), Color.white);
        DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Color.white);
    }

    private static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
