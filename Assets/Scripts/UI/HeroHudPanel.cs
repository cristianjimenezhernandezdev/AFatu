using UnityEngine;

public static class HeroHudPanel
{
    public static void Draw(RunManager runManager)
    {
        PlayerGridMovement player = runManager.Player;
        Rect area = new Rect(12f, 12f, 340f, 220f);
        GUILayout.BeginArea(area, GUI.skin.window);
        GUILayout.Label($"Estat: {runManager.CurrentState}");

        if (runManager.CurrentRun != null)
        {
            GUILayout.Label($"Segment: {Mathf.Min(runManager.CurrentRun.segmentsCleared + 1, runManager.CurrentRun.targetSegmentCount)}/{runManager.CurrentRun.targetSegmentCount}");
            GUILayout.Label($"Or: {runManager.CurrentGold} | Esmeraldes: {runManager.CurrentEmeralds}");
            GUILayout.Label($"Mode heroi: {runManager.EffectiveHeroMode}");
        }

        if (player != null)
        {
            GUILayout.Label($"Vida: {player.CurrentHealth}/{player.MaxHealth}");
            GUILayout.Label($"Atac: {player.Attack} | Defensa: {player.Defense} | Velocitat: {player.CombatSpeed:0.00}");
        }

        if (runManager.CurrentSegment != null)
        {
            GUILayout.Label($"Carta actual: {runManager.CurrentSegment.card.displayName}");
            GUILayout.Label($"Bioma: {runManager.CurrentSegment.card.biomeId} | Enemics: {runManager.CurrentSegment.enemySpawns.Count}");
        }

        if (!string.IsNullOrWhiteSpace(runManager.FeedbackMessage))
            GUILayout.Label(runManager.FeedbackMessage);

        GUILayout.Space(8f);
        GUILayout.Label("Poders divins");
        for (int i = 0; i < runManager.EquippedDivinePowers.Count; i++)
        {
            DivinePowerSeedData power = runManager.EquippedDivinePowers[i];
            float cooldown = runManager.GetDivinePowerCooldownSeconds(i);
            string cooldownText = cooldown > 0.01f ? $" ({Mathf.CeilToInt(cooldown)}s)" : string.Empty;

            if (GUILayout.Button($"{i + 1}. {power.displayName}{cooldownText}", GUILayout.Height(30f)))
                runManager.TryActivateDivinePowerSlot(i);
        }

        GUILayout.EndArea();
    }
}
