using UnityEngine;

public class RunCanvasHudController : MonoBehaviour
{
    [SerializeField] private RunManager runManager;
    [SerializeField] private RunHudController immediateGuiHud;
    [SerializeField] private bool disableImmediateGui = true;

    [Header("Panels")]
    [SerializeField] private HeroHudCanvasPanel heroHudPanel;
    [SerializeField] private DivinePowersCanvasPanel divinePowersPanel;
    [SerializeField] private CardChoiceCanvasPanel cardChoicePanel;
    [SerializeField] private ShopCanvasPanel shopPanel;
    [SerializeField] private RunSummaryCanvasPanel runSummaryPanel;
    [SerializeField] private PlayerWorldCanvasPanel playerWorldPanel;

    void Awake()
    {
        if (runManager == null)
            runManager = FindFirstObjectByType<RunManager>();
        if (immediateGuiHud == null)
            immediateGuiHud = FindFirstObjectByType<RunHudController>();

        if (disableImmediateGui && immediateGuiHud != null)
            immediateGuiHud.SetShowUi(false);
    }

    void Update()
    {
        if (runManager == null)
            return;

        heroHudPanel?.Refresh(runManager);
        divinePowersPanel?.Refresh(runManager);
        cardChoicePanel?.Refresh(runManager);
        shopPanel?.Refresh(runManager);
        runSummaryPanel?.Refresh(runManager);
        playerWorldPanel?.Refresh(runManager);
    }
}
