using System.Linq;
using UnityEngine;

public class MainMenuCanvasController : MonoBehaviour
{
    private enum MenuScreen
    {
        Hidden,
        Home,
        TechTree
    }

    [Header("References")]
    [SerializeField] private RunManager runManager;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private MainMenuHomeCanvasPanel homePanel;
    [SerializeField] private GameObject homePanelRoot;
    [SerializeField] private TechTreeCanvasPanel techTreePanel;
    [SerializeField] private GameObject techTreePanelRoot;
    [SerializeField] private GameObject[] gameplayRootsToHide;

    [Header("Behavior")]
    [SerializeField] private bool bootstrapRunManagerOnAwake = true;
    [SerializeField] private bool showMenuOnAwake = true;

    private MenuScreen currentScreen = MenuScreen.Hidden;
    public RunManager RunManager => runManager;

    void Awake()
    {
        if (runManager == null)
            runManager = FindFirstObjectByType<RunManager>();

        if (showMenuOnAwake)
            RunManager.SuppressAutoStartForMenu = true;

        if (bootstrapRunManagerOnAwake && runManager != null && !runManager.IsBootstrapped)
            runManager.Bootstrap();

        homePanel?.Initialize(this);
        techTreePanel?.Initialize(this);
    }

    void Start()
    {
        if (showMenuOnAwake)
            ShowHome();
        else
            HideMenu();
    }

    void Update()
    {
        if (currentScreen == MenuScreen.Hidden || runManager == null)
            return;

        switch (currentScreen)
        {
            case MenuScreen.Home:
                homePanel?.Refresh(runManager);
                break;
            case MenuScreen.TechTree:
                techTreePanel?.Refresh(runManager);
                break;
        }
    }

    public void ShowHome()
    {
        currentScreen = MenuScreen.Home;
        SetMenuVisible(true);
        if (homePanelRoot != null)
            homePanelRoot.SetActive(true);
        if (techTreePanelRoot != null)
            techTreePanelRoot.SetActive(false);
        homePanel?.Refresh(runManager);
    }

    public void ShowTechTree()
    {
        currentScreen = MenuScreen.TechTree;
        SetMenuVisible(true);
        if (homePanelRoot != null)
            homePanelRoot.SetActive(false);
        if (techTreePanelRoot != null)
            techTreePanelRoot.SetActive(true);
        techTreePanel?.Refresh(runManager);
    }

    public void HideMenu()
    {
        currentScreen = MenuScreen.Hidden;
        SetMenuVisible(false);
    }

    public void StartGameFromMenu()
    {
        if (runManager == null)
            return;

        RunManager.SuppressAutoStartForMenu = false;
        HideMenu();
        runManager.StartRun();
    }

    public void SelectRunLength(int runLength)
    {
        runManager?.SetPreferredRunLength(runLength);
        homePanel?.Refresh(runManager);
        techTreePanel?.Refresh(runManager);
    }

    public void SelectHeroMode(string heroMode)
    {
        runManager?.SetPreferredHeroMode(heroMode);
        homePanel?.Refresh(runManager);
    }

    public bool TrySelectProfile(string playerId, out string feedback)
    {
        feedback = "No hi ha cap RunManager disponible.";
        bool result = runManager != null && runManager.TrySelectProfile(playerId, out feedback);
        RefreshPanels();
        return result;
    }

    public bool TryCreateProfile(string displayName, out string feedback)
    {
        feedback = "No hi ha cap RunManager disponible.";
        bool result = runManager != null && runManager.TryCreateProfile(displayName, out feedback);
        RefreshPanels();
        return result;
    }

    public void RefreshPanels()
    {
        homePanel?.Refresh(runManager);
        techTreePanel?.Refresh(runManager);
    }

    public bool TryPurchaseTechNode(string nodeId, out string feedback)
    {
        feedback = "Node no valid.";
        if (runManager == null || string.IsNullOrWhiteSpace(nodeId))
            return false;

        if (nodeId == "meta:run7")
            return runManager.TryUnlockLongRunFromMenu(12, out feedback);

        if (nodeId.StartsWith("power:"))
            return runManager.TryUnlockDivinePowerFromMenu(nodeId.Substring("power:".Length), out feedback);

        if (nodeId.StartsWith("biome:"))
        {
            string biomeId = nodeId.Substring("biome:".Length);
            return runManager.TryUnlockBiomeFromMenu(biomeId, GetBiomeUnlockCost(biomeId), out feedback);
        }

        if (nodeId.StartsWith("card:"))
        {
            string cardId = nodeId.Substring("card:".Length);
            CardSeedData card = runManager.AllCards.FirstOrDefault(item => item.cardId == cardId);
            if (card == null)
            {
                feedback = "La carta indicada no existeix.";
                return false;
            }

            return runManager.TryUnlockCardFromMenu(cardId, GetCardUnlockCost(card), out feedback);
        }

        return false;
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuRoot != null)
            menuRoot.SetActive(visible);

        for (int i = 0; i < gameplayRootsToHide.Length; i++)
        {
            if (gameplayRootsToHide[i] != null)
                gameplayRootsToHide[i].SetActive(!visible);
        }
    }

    private static int GetCardUnlockCost(CardSeedData card)
    {
        return card == null ? 0 : Mathf.Max(4, 4 + card.baseDifficulty * 3 + card.rewardTier * 2);
    }

    private static int GetBiomeUnlockCost(string biomeId)
    {
        switch ((biomeId ?? string.Empty).ToLowerInvariant())
        {
            case "dark_forest":
            case "swamp":
                return 6;
            case "crypt":
                return 8;
            case "ruins":
            default:
                return 5;
        }
    }
}
