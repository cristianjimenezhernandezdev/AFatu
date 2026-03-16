using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroHudCanvasPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text runStateText;
    [SerializeField] private TMP_Text segmentBadgeText;
    [SerializeField] private TMP_Text economyBadgeText;
    [SerializeField] private TMP_Text modeBadgeText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text lastCombatText;
    [SerializeField] private TMP_Text attackValueText;
    [SerializeField] private TMP_Text defenseValueText;
    [SerializeField] private TMP_Text speedValueText;
    [SerializeField] private TMP_Text segmentNameText;
    [SerializeField] private TMP_Text segmentMetaText;
    [SerializeField] private TMP_Text feedbackText;

    public void Refresh(RunManager runManager)
    {
        if (runManager == null || runManager.Player == null)
            return;

        PlayerGridMovement player = runManager.Player;
        if (runStateText != null)
            runStateText.text = $"Estat: {runManager.CurrentState}";

        if (runManager.CurrentRun != null)
        {
            if (segmentBadgeText != null)
                segmentBadgeText.text = $"Segment {Mathf.Min(runManager.CurrentRun.segmentsCleared + 1, runManager.CurrentRun.targetSegmentCount)}/{runManager.CurrentRun.targetSegmentCount}";
            if (economyBadgeText != null)
                economyBadgeText.text = $"Or {runManager.CurrentGold} | Esm. {runManager.CurrentEmeralds}";
            if (modeBadgeText != null)
                modeBadgeText.text = runManager.EffectiveHeroMode;
        }

        if (healthText != null)
            healthText.text = $"{player.CurrentHealth}/{player.MaxHealth}";
        if (healthFill != null)
            healthFill.fillAmount = player.MaxHealth <= 0 ? 0f : player.CurrentHealth / (float)player.MaxHealth;
        if (lastCombatText != null)
            lastCombatText.text = player.HasRecentCombat ? $"Ultim combat  +{player.LastCombatDamageDealt} / -{player.LastCombatDamageTaken}" : "Sense combat recent";
        if (attackValueText != null)
            attackValueText.text = player.Attack.ToString();
        if (defenseValueText != null)
            defenseValueText.text = player.Defense.ToString();
        if (speedValueText != null)
            speedValueText.text = player.CombatSpeed.ToString("0.00");

        if (runManager.CurrentSegment != null)
        {
            if (segmentNameText != null)
                segmentNameText.text = runManager.CurrentSegment.card.displayName;
            if (segmentMetaText != null)
                segmentMetaText.text = $"{RunUiTheme.FormatBiome(runManager.CurrentSegment.card.biomeId)} | {runManager.CurrentSegment.card.cardType} | {runManager.CurrentSegment.enemySpawns.Count} enemics";
        }
        else
        {
            if (segmentNameText != null)
                segmentNameText.text = "Sense segment actiu";
            if (segmentMetaText != null)
                segmentMetaText.text = string.Empty;
        }

        if (feedbackText != null)
            feedbackText.text = string.IsNullOrWhiteSpace(runManager.FeedbackMessage) ? "Esperant esdeveniments..." : runManager.FeedbackMessage;
    }
}
