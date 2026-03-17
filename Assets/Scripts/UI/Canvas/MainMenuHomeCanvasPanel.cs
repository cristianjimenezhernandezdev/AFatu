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

    [Header("Actions")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button techTreeButton;
    [SerializeField] private Button shortRunButton;
    [SerializeField] private Button longRunButton;
    [SerializeField] private Button prudentModeButton;
    [SerializeField] private Button aggressiveModeButton;
    [SerializeField] private Button escapeModeButton;

    private MainMenuCanvasController owner;

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
    }

    public void Refresh(RunManager runManager)
    {
        if (runManager == null || runManager.PlayerProfile == null || runManager.PlayerProgress == null)
            return;

        PlayerProfileData profile = runManager.PlayerProfile;
        PlayerProgressData progress = runManager.PlayerProgress;

        if (titleText != null)
            titleText.text = "Architectus Fati";
        if (profileNameText != null)
            profileNameText.text = $"Perfil actiu: {profile.displayName}";
        if (emeraldsText != null)
            emeraldsText.text = $"Esmeraldes: {progress.hardCurrency}";
        if (progressSummaryText != null)
            progressSummaryText.text = $"Runs {progress.completedRuns} completes | {progress.failedRuns} fallides | Segment max {progress.highestSegmentReached}";
        if (unlockedSummaryText != null)
            unlockedSummaryText.text = $"Cartes {progress.unlockedCardIds.Length} | Poders {progress.unlockedDivinePowerIds.Length} | Biomes {progress.biomesUnlocked.Length}";
        if (selectionSummaryText != null)
            selectionSummaryText.text = $"Run {profile.preferredRunLength} segments | Mode {profile.selectedHeroMode}";
        if (footerHintText != null)
            footerHintText.text = progress.run7Unlocked
                ? "La run llarga ja esta disponible. Pots llançar partida o continuar desbloquejant nodes."
                : "La run llarga es desbloqueja des de l'arbre de millores.";

        if (longRunButton != null)
            longRunButton.interactable = progress.run7Unlocked;
    }
}
