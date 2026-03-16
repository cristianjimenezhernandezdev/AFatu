using UnityEngine;

public class RunHudController : MonoBehaviour
{
    [SerializeField] private RunManager runManager;
    [SerializeField] private bool showUi = true;

    public bool ShowUi => showUi;

    void Awake()
    {
        if (runManager == null)
            runManager = FindFirstObjectByType<RunManager>();
    }

    public void SetShowUi(bool value)
    {
        showUi = value;
    }

    void OnGUI()
    {
        if (!showUi || runManager == null)
            return;

        RunUiTheme.EnsureInitialized();
        HeroHudPanel.Draw(runManager);
        DivinePowersPanel.Draw(runManager);
        PlayerWorldOverlayPanel.Draw(runManager);

        switch (runManager.CurrentState)
        {
            case RunManager.RunState.AwaitingShopChoice:
                ShopOverlayPanel.Draw(runManager);
                break;
            case RunManager.RunState.AwaitingCardChoice:
                CardChoiceOverlayPanel.Draw(runManager);
                break;
            case RunManager.RunState.Completed:
            case RunManager.RunState.Failed:
                RunSummaryOverlayPanel.Draw(runManager);
                break;
        }
    }
}
