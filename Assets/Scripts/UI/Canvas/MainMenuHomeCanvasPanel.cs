using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuHomeCanvasPanel : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text profileNameText;
    [SerializeField] private TMP_Text emeraldsText;
    [SerializeField] private TMP_Text progressSummaryText;
    [SerializeField] private TMP_Text unlockedSummaryText;
    [SerializeField] private TMP_Text selectionSummaryText;
    [SerializeField] private TMP_Text footerHintText;
    [SerializeField] private TMP_Text profileFeedbackText;

    [Header("Actions")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button techTreeButton;
    [SerializeField] private Button shortRunButton;
    [SerializeField] private Button longRunButton;
    [SerializeField] private Button prudentModeButton;
    [SerializeField] private Button aggressiveModeButton;
    [SerializeField] private Button escapeModeButton;
    [SerializeField] private TMP_Dropdown profileDropdown;
    [SerializeField] private TMP_InputField newProfileNameInput;
    [SerializeField] private Button createProfileButton;

    private MainMenuCanvasController owner;
    private readonly List<string> profileIds = new List<string>();
    private int lastProfileSignature = int.MinValue;
    private string lastBoundPlayerId = string.Empty;
    private string profileFeedbackMessage = string.Empty;
    private bool syncingProfileDropdown;

    public void Initialize(MainMenuCanvasController controller)
    {
        owner = controller;

        playButton?.onClick.AddListener(() => owner?.StartGameFromMenu());
        techTreeButton?.onClick.AddListener(() => owner?.ShowTechTree());
        shortRunButton?.onClick.AddListener(() => owner?.SelectRunLength(BalanceConfig.DefaultShortRunLength));
        longRunButton?.onClick.AddListener(() => owner?.SelectRunLength(BalanceConfig.DefaultLongRunLength));
        prudentModeButton?.onClick.AddListener(() => owner?.SelectHeroMode("prudent"));
        aggressiveModeButton?.onClick.AddListener(() => owner?.SelectHeroMode("aggressive"));
        escapeModeButton?.onClick.AddListener(() => owner?.SelectHeroMode("escape"));
        profileDropdown?.onValueChanged.AddListener(HandleProfileSelected);
        createProfileButton?.onClick.AddListener(HandleCreateProfile);
    }

    public void Refresh(RunManager runManager)
    {
        if (runManager == null || runManager.PlayerProfile == null || runManager.PlayerProgress == null)
            return;

        PlayerProfileData profile = runManager.PlayerProfile;
        PlayerProgressData progress = runManager.PlayerProgress;
        string longRunHint = progress.run7Unlocked
            ? "La run llarga ja esta disponible. Pots llançar partida o continuar desbloquejant nodes."
            : "La run llarga es desbloqueja des de l'arbre de millores.";
        RefreshProfileSelector(runManager);

        if (titleText != null)
            titleText.text = "Architectus Fati";
        if (profileNameText != null)
            profileNameText.text = $"Perfil actiu: {profile.displayName}";
        if (emeraldsText != null)
            emeraldsText.text = $"Esmeraldes: {progress.hardCurrency}";
        if (progressSummaryText != null)
            progressSummaryText.text = $"Runs {progress.completedRuns} completes | {progress.failedRuns} fallides | Segment max {progress.highestSegmentReached}";
        if (unlockedSummaryText != null)
            unlockedSummaryText.text = $"Cartes {progress.unlockedCardIds.Length} | Poders {progress.unlockedDivinePowerIds.Length} | Biomes {progress.biomesUnlocked.Length} | Reliquies {runManager.PlayerRelics.Count}";
        if (selectionSummaryText != null)
            selectionSummaryText.text = $"Run {profile.preferredRunLength} segments | Mode {profile.selectedHeroMode}\n{RelicPresentationUtility.BuildAggregateSummary(runManager.PlayerRelics, runManager.AllRelics)}";
        if (footerHintText != null)
            footerHintText.text = $"{longRunHint}\n{RelicPresentationUtility.BuildInventorySummary(runManager.PlayerRelics, runManager.AllRelics, 3)}";
        if (profileFeedbackText != null)
            profileFeedbackText.text = string.IsNullOrWhiteSpace(profileFeedbackMessage)
                ? "Pots seleccionar un perfil existent o crear-ne un de nou."
                : profileFeedbackMessage;

        if (longRunButton != null)
            longRunButton.interactable = progress.run7Unlocked;
    }

    private void RefreshProfileSelector(RunManager runManager)
    {
        if (profileDropdown == null)
            return;

        IReadOnlyList<PlayerProfileSummaryData> profiles = runManager.AvailableProfiles;
        int signature = BuildProfileSignature(profiles);
        bool mustRebuild = signature != lastProfileSignature || lastBoundPlayerId != runManager.CurrentPlayerId;
        if (!mustRebuild)
            return;

        syncingProfileDropdown = true;
        profileDropdown.ClearOptions();
        profileIds.Clear();

        List<TMP_Dropdown.OptionData> options = profiles
            .Select(profile =>
            {
                profileIds.Add(profile.playerId);
                return new TMP_Dropdown.OptionData($"{profile.displayName} | {profile.emeralds} es. | Runs {profile.completedRuns}");
            })
            .ToList();

        if (options.Count == 0)
        {
            options.Add(new TMP_Dropdown.OptionData("Sense perfils"));
            profileIds.Add(string.Empty);
        }

        profileDropdown.AddOptions(options);
        int selectedIndex = Mathf.Max(0, profileIds.IndexOf(runManager.CurrentPlayerId));
        profileDropdown.SetValueWithoutNotify(selectedIndex);
        profileDropdown.interactable = profileIds.Any(item => !string.IsNullOrWhiteSpace(item));
        syncingProfileDropdown = false;

        lastProfileSignature = signature;
        lastBoundPlayerId = runManager.CurrentPlayerId;
    }

    private void HandleProfileSelected(int optionIndex)
    {
        if (syncingProfileDropdown || owner == null || optionIndex < 0 || optionIndex >= profileIds.Count)
            return;

        string playerId = profileIds[optionIndex];
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        owner.TrySelectProfile(playerId, out profileFeedbackMessage);
    }

    private void HandleCreateProfile()
    {
        if (owner == null)
            return;

        string requestedName = newProfileNameInput != null ? newProfileNameInput.text : string.Empty;
        if (owner.TryCreateProfile(requestedName, out profileFeedbackMessage) && newProfileNameInput != null)
            newProfileNameInput.text = string.Empty;
    }

    private static int BuildProfileSignature(IReadOnlyList<PlayerProfileSummaryData> profiles)
    {
        unchecked
        {
            int signature = 17;
            for (int i = 0; i < profiles.Count; i++)
            {
                PlayerProfileSummaryData profile = profiles[i];
                signature = signature * 31 + (profile?.playerId?.GetHashCode() ?? 0);
                signature = signature * 31 + (profile?.displayName?.GetHashCode() ?? 0);
                signature = signature * 31 + (profile?.emeralds ?? 0);
                signature = signature * 31 + (profile?.completedRuns ?? 0);
            }

            return signature;
        }
    }
}

public static class RelicPresentationUtility
{
    public static string BuildEffectSummary(RelicSeedData relic)
    {
        if (relic == null)
            return string.Empty;

        return BuildEffectSummary(JsonSeedParser.ParseRelicEffect(relic.effectConfigJson));
    }

    public static string BuildEffectSummary(RelicEffectConfigData effect)
    {
        if (effect == null)
            return string.Empty;

        List<string> parts = new List<string>();
        if (effect.heroMaxHealthBonus != 0)
            parts.Add($"+{effect.heroMaxHealthBonus} vida");
        if (effect.heroAttackBonus != 0)
            parts.Add($"+{effect.heroAttackBonus} atac");
        if (effect.heroSpeedBonus > 0f)
            parts.Add($"+{effect.heroSpeedBonus * 100f:0}% velocitat");
        if (effect.extraCardChoice > 0)
            parts.Add($"+{effect.extraCardChoice} carta");

        return string.Join(", ", parts);
    }

    public static string BuildAggregateSummary(IReadOnlyList<PlayerRelicData> relics, IReadOnlyList<RelicSeedData> definitions)
    {
        int totalHealth = 0;
        int totalAttack = 0;
        float totalSpeed = 0f;
        int totalExtraCards = 0;
        Dictionary<string, RelicSeedData> byId = (definitions ?? System.Array.Empty<RelicSeedData>())
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.relicId))
            .ToDictionary(item => item.relicId, item => item);

        foreach (PlayerRelicData relic in relics ?? System.Array.Empty<PlayerRelicData>())
        {
            if (relic == null || string.IsNullOrWhiteSpace(relic.relicId) || relic.quantity <= 0 || !byId.TryGetValue(relic.relicId, out RelicSeedData definition))
                continue;

            RelicEffectConfigData effect = JsonSeedParser.ParseRelicEffect(definition.effectConfigJson);
            totalHealth += effect.heroMaxHealthBonus * relic.quantity;
            totalAttack += effect.heroAttackBonus * relic.quantity;
            totalSpeed += effect.heroSpeedBonus * relic.quantity;
            totalExtraCards += effect.extraCardChoice * relic.quantity;
        }

        List<string> parts = new List<string>();
        if (totalHealth != 0)
            parts.Add($"+{totalHealth} vida");
        if (totalAttack != 0)
            parts.Add($"+{totalAttack} atac");
        if (totalSpeed > 0f)
            parts.Add($"+{totalSpeed * 100f:0}% velocitat");
        if (totalExtraCards > 0)
            parts.Add($"+{totalExtraCards} carta");

        return parts.Count == 0 ? "Sense bonus de reliquia." : $"Bonus reliquies: {string.Join(", ", parts)}";
    }

    public static string BuildInventorySummary(IReadOnlyList<PlayerRelicData> relics, IReadOnlyList<RelicSeedData> definitions, int maxEntries)
    {
        Dictionary<string, RelicSeedData> byId = (definitions ?? System.Array.Empty<RelicSeedData>())
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.relicId))
            .ToDictionary(item => item.relicId, item => item);

        List<string> entries = new List<string>();
        foreach (PlayerRelicData relic in (relics ?? System.Array.Empty<PlayerRelicData>()).Where(item => item != null && item.quantity > 0).OrderBy(item => item.relicId).Take(maxEntries))
        {
            if (!byId.TryGetValue(relic.relicId, out RelicSeedData definition))
            {
                entries.Add(relic.quantity > 1 ? $"{relic.relicId} x{relic.quantity}" : relic.relicId);
                continue;
            }

            string quantitySuffix = relic.quantity > 1 ? $" x{relic.quantity}" : string.Empty;
            string effectSummary = BuildEffectSummary(definition);
            entries.Add(string.IsNullOrWhiteSpace(effectSummary)
                ? $"{definition.displayName}{quantitySuffix}"
                : $"{definition.displayName}{quantitySuffix} ({effectSummary})");
        }

        if (entries.Count == 0)
            return "Reliquies: cap.";

        int totalOwned = (relics ?? System.Array.Empty<PlayerRelicData>()).Count(item => item != null && item.quantity > 0);
        string suffix = totalOwned > entries.Count ? " ..." : string.Empty;
        return $"Reliquies: {string.Join(" | ", entries)}{suffix}";
    }
}
