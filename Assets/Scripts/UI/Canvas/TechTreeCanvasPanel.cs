using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TechTreeCanvasPanel : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform nodeContainer;
    [SerializeField] private TechTreeNodeCanvasSlot nodePrefab;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button backButton;

    [Header("Palette")]
    [SerializeField] private Color metaColor = new Color32(205, 166, 96, 255);
    [SerializeField] private Color divineColor = new Color32(120, 170, 245, 255);
    [SerializeField] private Color biomeColor = new Color32(112, 178, 122, 255);
    [SerializeField] private Color cardColor = new Color32(194, 131, 92, 255);

    private readonly List<TechTreeNodeCanvasSlot> spawnedNodes = new List<TechTreeNodeCanvasSlot>();
    private MainMenuCanvasController owner;
    private int lastRefreshSignature = int.MinValue;
    private string lastFeedback = string.Empty;

    public void Initialize(MainMenuCanvasController controller)
    {
        owner = controller;
        backButton?.onClick.AddListener(() => owner?.ShowHome());
    }

    public void Refresh(RunManager runManager)
    {
        if (runManager == null || !runManager.IsBootstrapped)
            return;

        if (headerText != null)
            headerText.text = "Arbre de Millores";
        if (summaryText != null)
            summaryText.text = BuildSummary(runManager);
        if (feedbackText != null && !string.IsNullOrWhiteSpace(lastFeedback))
            feedbackText.text = lastFeedback;

        int signature = BuildRefreshSignature(runManager);
        if (signature == lastRefreshSignature)
            return;

        lastRefreshSignature = signature;
        RebuildNodes(runManager);
    }

    private void RebuildNodes(RunManager runManager)
    {
        if (nodeContainer == null || nodePrefab == null)
            return;

        EnsureNodePool(BuildNodeData(runManager).Count);
        List<TechTreeNodePresentationData> nodes = BuildNodeData(runManager);

        for (int i = 0; i < spawnedNodes.Count; i++)
        {
            bool shouldBeVisible = i < nodes.Count;
            spawnedNodes[i].gameObject.SetActive(shouldBeVisible);
            if (shouldBeVisible)
                spawnedNodes[i].Bind(nodes[i], HandleNodeClicked);
        }
    }

    private void HandleNodeClicked(string nodeId)
    {
        if (owner == null || string.IsNullOrWhiteSpace(nodeId))
            return;

        if (owner.TryPurchaseTechNode(nodeId, out string feedback))
            lastFeedback = feedback;
        else
            lastFeedback = feedback;

        lastRefreshSignature = int.MinValue;
    }

    private List<TechTreeNodePresentationData> BuildNodeData(RunManager runManager)
    {
        List<TechTreeNodePresentationData> nodes = new List<TechTreeNodePresentationData>
        {
            BuildLongRunNode(runManager)
        };

        foreach (BiomeSeedData biome in runManager.AllBiomes.Where(item => item != null && item.isActive).OrderBy(item => item.displayName))
            nodes.Add(BuildBiomeNode(runManager, biome));

        foreach (DivinePowerSeedData power in runManager.AllDivinePowers.Where(item => item != null && item.isActive).OrderBy(item => item.sortOrder))
            nodes.Add(BuildDivinePowerNode(runManager, power));

        foreach (CardSeedData card in runManager.AllCards.Where(item => item != null && item.isActive).OrderBy(item => item.sortOrder))
            nodes.Add(BuildCardNode(runManager, card));

        return nodes;
    }

    private TechTreeNodePresentationData BuildLongRunNode(RunManager runManager)
    {
        const int unlockCost = 12;
        bool unlocked = runManager.PlayerProgress != null && runManager.PlayerProgress.run7Unlocked;
        return new TechTreeNodePresentationData
        {
            nodeId = "meta:run7",
            branchLabel = "Meta",
            title = "Run Llarga",
            description = "Permet sessions de 7 segments i amplia la progressio de partida.",
            statusLabel = unlocked ? "Desbloquejat" : "Millora major",
            actionLabel = unlocked ? "Disponible" : "Desbloquejar",
            artKey = runManager.GetCurrentRunResultArtKey(),
            cost = unlockCost,
            accentColor = metaColor,
            isUnlocked = unlocked,
            canPurchase = !unlocked && runManager.CurrentEmeralds >= unlockCost
        };
    }

    private TechTreeNodePresentationData BuildBiomeNode(RunManager runManager, BiomeSeedData biome)
    {
        int cost = GetBiomeUnlockCost(biome.biomeId);
        bool unlocked = runManager.IsBiomeUnlocked(biome.biomeId);
        string artKey = runManager.AllCards.FirstOrDefault(card => card.biomeId == biome.biomeId)?.artKey ?? string.Empty;

        return new TechTreeNodePresentationData
        {
            nodeId = $"biome:{biome.biomeId}",
            branchLabel = "Bioma",
            title = biome.displayName,
            description = biome.description,
            statusLabel = unlocked ? "Disponible" : "Zona bloquejada",
            actionLabel = unlocked ? "Disponible" : "Desbloquejar",
            artKey = artKey,
            cost = cost,
            accentColor = biomeColor,
            isUnlocked = unlocked,
            canPurchase = !unlocked && runManager.CurrentEmeralds >= cost
        };
    }

    private TechTreeNodePresentationData BuildDivinePowerNode(RunManager runManager, DivinePowerSeedData power)
    {
        bool unlocked = runManager.IsDivinePowerUnlocked(power.powerId);
        int cost = Mathf.Max(0, power.unlockCost);

        return new TechTreeNodePresentationData
        {
            nodeId = $"power:{power.powerId}",
            branchLabel = "Poder Divi",
            title = power.displayName,
            description = power.description,
            statusLabel = unlocked ? "Desbloquejat" : $"Cooldown {power.cooldownSeconds}s",
            actionLabel = unlocked ? "Disponible" : "Desbloquejar",
            artKey = power.artKey,
            cost = cost,
            accentColor = divineColor,
            isUnlocked = unlocked,
            canPurchase = !unlocked && runManager.CurrentEmeralds >= cost
        };
    }

    private TechTreeNodePresentationData BuildCardNode(RunManager runManager, CardSeedData card)
    {
        bool unlocked = runManager.IsCardUnlocked(card.cardId);
        bool biomeUnlocked = runManager.IsBiomeUnlocked(card.biomeId);
        int cost = GetCardUnlockCost(card);
        bool canPurchase = !unlocked && biomeUnlocked && runManager.CurrentEmeralds >= cost;

        return new TechTreeNodePresentationData
        {
            nodeId = $"card:{card.cardId}",
            branchLabel = "Carta",
            title = card.displayName,
            description = biomeUnlocked ? card.description : $"Cal desbloquejar el bioma {RunUiTheme.FormatBiome(card.biomeId)}.",
            statusLabel = unlocked ? "Desbloquejada" : biomeUnlocked ? $"{card.cardType} | Dif. {card.baseDifficulty}" : "Falta bioma",
            actionLabel = unlocked ? "Disponible" : biomeUnlocked ? "Desbloquejar" : "Bloquejat",
            artKey = card.artKey,
            cost = cost,
            accentColor = cardColor,
            isUnlocked = unlocked,
            canPurchase = canPurchase
        };
    }

    private int BuildRefreshSignature(RunManager runManager)
    {
        PlayerProgressData progress = runManager.PlayerProgress;
        PlayerProfileData profile = runManager.PlayerProfile;
        int signature = runManager.CurrentEmeralds * 17;
        signature += progress != null ? progress.unlockedCardIds.Length * 31 : 0;
        signature += progress != null ? progress.unlockedDivinePowerIds.Length * 53 : 0;
        signature += progress != null ? progress.biomesUnlocked.Length * 71 : 0;
        signature += progress != null && progress.run7Unlocked ? 97 : 0;
        signature += profile != null ? profile.preferredRunLength * 3 : 0;
        return signature;
    }

    private string BuildSummary(RunManager runManager)
    {
        PlayerProgressData progress = runManager.PlayerProgress;
        return $"Esmeraldes {runManager.CurrentEmeralds} | Cartes {progress.unlockedCardIds.Length} | Poders {progress.unlockedDivinePowerIds.Length} | Biomes {progress.biomesUnlocked.Length}";
    }

    private void EnsureNodePool(int requiredCount)
    {
        while (spawnedNodes.Count < requiredCount)
        {
            TechTreeNodeCanvasSlot node = Instantiate(nodePrefab, nodeContainer);
            node.gameObject.SetActive(false);
            spawnedNodes.Add(node);
        }
    }

    private static int GetCardUnlockCost(CardSeedData card)
    {
        if (card == null)
            return 0;

        return Mathf.Max(4, 4 + card.baseDifficulty * 3 + card.rewardTier * 2);
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
