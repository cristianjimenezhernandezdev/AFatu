using UnityEngine;

public static class DivinePowersPanel
{
    public static void Draw(RunManager runManager)
    {
        int count = runManager.EquippedDivinePowers.Count;
        if (count <= 0)
            return;

        float width = Mathf.Min(420f, Screen.width * 0.28f);
        float height = Mathf.Min(Screen.height - 36f, 108f + count * 156f);
        Rect area = new Rect(Screen.width - width - 18f, 18f, width, height);
        RunUiTheme.DrawPanel(area, new Color32(20, 19, 28, 242), new Color32(130, 111, 178, 255));

        GUI.Label(new Rect(area.x + 18f, area.y + 16f, area.width - 36f, 30f), "Poders divins", RunUiTheme.TitleStyle);
        GUI.Label(new Rect(area.x + 18f, area.y + 46f, area.width - 36f, 36f), runManager.CanUseDivinePowers ? "Activa'ls mentre l'heroi avanca. El cooldown i les carregues es veuen a cada carta. Tecles 1 i 2." : "Els poders només es poden activar durant l'exploracio del segment.", RunUiTheme.BodyStyle);

        float cardY = area.y + 92f;
        for (int i = 0; i < count; i++)
        {
            Rect cardRect = new Rect(area.x + 16f, cardY, area.width - 32f, 144f);
            DrawPowerCard(cardRect, runManager, i);
            cardY += 152f;
        }
    }

    private static void DrawPowerCard(Rect rect, RunManager runManager, int slotIndex)
    {
        DivinePowerSeedData power = runManager.EquippedDivinePowers[slotIndex];
        int charges = runManager.GetDivinePowerCharges(slotIndex);
        int maxCharges = runManager.GetDivinePowerMaxCharges(slotIndex);
        float cooldown = runManager.GetDivinePowerCooldownSeconds(slotIndex);
        float cooldownNormalized = runManager.GetDivinePowerCooldownNormalized(slotIndex);
        float activeSeconds = runManager.GetDivinePowerActiveSeconds(slotIndex);
        bool canUse = runManager.CanUseDivinePowers && charges > 0;
        Color accent = GetAccentColor(power.powerType);

        RunUiTheme.DrawPanel(rect, new Color32(35, 34, 46, 255), accent);
        GUI.BeginGroup(new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, rect.height - 28f));

        GUI.Label(new Rect(0f, 0f, rect.width - 28f - 78f, 24f), $"{slotIndex + 1}. {power.displayName}", RunUiTheme.SubtitleStyle);
        DrawChargeSlots(new Rect(rect.width - 28f - 74f, 0f, 74f, 22f), charges, maxCharges, accent);
        GUI.Label(new Rect(0f, 26f, rect.width - 28f, 38f), power.description, RunUiTheme.BodyStyle);

        Rect statusRow = new Rect(0f, 70f, rect.width - 28f, 26f);
        RunUiTheme.DrawBadge(new Rect(statusRow.x, statusRow.y, 120f, statusRow.height), $"Carregues {charges}/{maxCharges}", accent, new Color32(19, 18, 24, 255));
        RunUiTheme.DrawBadge(new Rect(statusRow.x + 128f, statusRow.y, 120f, statusRow.height), activeSeconds > 0.01f ? $"Actiu {Mathf.CeilToInt(activeSeconds)}s" : $"Cooldown {Mathf.CeilToInt(cooldown)}s", activeSeconds > 0.01f ? new Color32(118, 190, 140, 255) : new Color32(121, 131, 154, 255), activeSeconds > 0.01f ? new Color32(17, 22, 18, 255) : new Color32(17, 20, 25, 255));
        RunUiTheme.DrawBadge(new Rect(statusRow.x + 256f, statusRow.y, rect.width - 28f - 256f, statusRow.height), canUse ? "Disponible" : charges <= 0 ? "Recarregant" : "Bloquejat", canUse ? new Color32(196, 184, 123, 255) : new Color32(96, 101, 111, 255), new Color32(23, 21, 18, 255));

        Rect progressRect = new Rect(0f, 104f, rect.width - 28f, 12f);
        DrawCooldownBar(progressRect, cooldownNormalized, accent);

        Rect buttonRect = new Rect(0f, 122f, rect.width - 28f, 26f);
        RunUiTheme.DrawButtonBackground(buttonRect, canUse ? accent : new Color32(94, 96, 108, 255));
        string buttonLabel = canUse ? "Activar poder" : charges <= 0 ? "Esperant una nova carrega" : "No disponible ara";
        if (GUI.Button(buttonRect, buttonLabel, RunUiTheme.PrimaryButtonStyle) && canUse)
            runManager.TryActivateDivinePowerSlot(slotIndex);

        GUI.EndGroup();
    }

    private static void DrawCooldownBar(Rect rect, float cooldownNormalized, Color accent)
    {
        Color previous = GUI.color;
        GUI.color = new Color32(61, 64, 75, 255);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(1f - cooldownNormalized), rect.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void DrawChargeSlots(Rect rect, int charges, int maxCharges, Color accent)
    {
        float gap = 6f;
        float size = (rect.width - gap * Mathf.Max(0, maxCharges - 1)) / Mathf.Max(1, maxCharges);
        for (int i = 0; i < maxCharges; i++)
        {
            Rect slotRect = new Rect(rect.x + (size + gap) * i, rect.y, size, rect.height);
            RunUiTheme.DrawPanel(slotRect, i < charges ? accent : new Color32(78, 81, 92, 255), Color.Lerp(accent, Color.white, 0.25f));
        }
    }

    private static Color GetAccentColor(string powerType)
    {
        switch ((powerType ?? string.Empty).ToLowerInvariant())
        {
            case "defense": return new Color32(120, 160, 224, 255);
            case "behavior": return new Color32(208, 154, 100, 255);
            case "summon": return new Color32(145, 196, 138, 255);
            case "buff":
            default: return new Color32(201, 177, 101, 255);
        }
    }
}
