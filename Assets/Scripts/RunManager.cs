using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public enum RunState
    {
        None,
        Bootstrapping,
        Transitioning,
        ExploringSegment,
        AwaitingCardChoice,
        Completed,
        Failed
    }

    public static RunManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private WorldGrid worldGrid;
    [SerializeField] private PlayerGridMovement player;
    [SerializeField] private GameObject fallbackEnemyTemplate;

    [Header("Run Setup")]
    [SerializeField] private List<MapCardData> unlockedCards = new();
    [SerializeField] private List<MapCardData> localCardLibrary = new();
    [SerializeField] private MapCardData startingCard;
    [SerializeField] private int cardChoiceCount = 3;
    [SerializeField] private int segmentsToClearForVictory = 5;
    [SerializeField] private int cardsUnlockedPerVictory = 1;

    [Header("Local Progress")]
    [SerializeField] private string progressFileName = "architectusfati_progress.json";
    [SerializeField] private bool useBuiltInUi = true;
    [SerializeField] private bool resetLocalProgressOnPlay;

    [Header("Debug")]
    [SerializeField] private bool autoPickFirstCardInConsole;

    private readonly List<MapCardData> allCards = new();
    private readonly Dictionary<string, MapCardData> cardsById = new();

    private bool waitingForCardChoice;
    private bool isBootstrapped;
    private int segmentsClearedThisRun;
    private string runSummaryMessage = string.Empty;
    private string rewardSummaryMessage = string.Empty;
    private List<MapCardData> currentChoices = new();
    private RunState currentState = RunState.None;
    private LocalPlayerProgress progressData;
    private GameObject runtimeEnemyTemplateInstance;

    public IReadOnlyList<MapCardData> CurrentChoices => currentChoices;
    public bool WaitingForCardChoice => waitingForCardChoice;
    public RunState CurrentState => currentState;
    public bool AllowsActorTurns => currentState == RunState.ExploringSegment;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (worldGrid == null)
            worldGrid = FindFirstObjectByType<WorldGrid>();

        if (player == null)
            player = FindFirstObjectByType<PlayerGridMovement>();

        BootstrapLocalMode();
        StartRun();
    }

    public void StartRun()
    {
        BootstrapLocalMode();

        if (startingCard == null)
        {
            Debug.LogError("No hi ha cap startingCard valida per iniciar la run.");
            return;
        }

        waitingForCardChoice = false;
        currentChoices.Clear();
        runSummaryMessage = string.Empty;
        rewardSummaryMessage = string.Empty;
        segmentsClearedThisRun = 0;

        SetState(RunState.Transitioning);

        if (player != null)
        {
            player.ResetForRun();
        }

        progressData.totalRunsStarted++;
        SaveProgress();

        GenerateSegmentFromCard(startingCard);
    }

    public void OnPlayerReachedSegmentGoal()
    {
        if (currentState != RunState.ExploringSegment)
            return;

        segmentsClearedThisRun++;

        if (segmentsClearedThisRun >= Mathf.Max(1, segmentsToClearForVictory))
        {
            CompleteRun();
            return;
        }

        waitingForCardChoice = true;
        currentChoices = DrawCardChoices(cardChoiceCount);
        SetState(RunState.AwaitingCardChoice);

        if (currentChoices.Count == 0)
        {
            Debug.LogWarning("No hi ha cartes desbloquejades per generar el seguent segment.");
            CompleteRun();
            return;
        }

        Debug.Log("Escull una carta per al seguent segment:");

        for (int i = 0; i < currentChoices.Count; i++)
        {
            Debug.Log($"[{i}] {currentChoices[i].displayName}");
        }

        if (autoPickFirstCardInConsole && !useBuiltInUi)
        {
            SelectCard(0);
        }
    }

    public void SelectCard(int index)
    {
        if (!waitingForCardChoice)
            return;

        if (index < 0 || index >= currentChoices.Count)
        {
            Debug.LogWarning("Index de carta invalid.");
            return;
        }

        MapCardData selectedCard = currentChoices[index];
        waitingForCardChoice = false;
        currentChoices.Clear();
        SetState(RunState.Transitioning);

        GenerateSegmentFromCard(selectedCard);
    }

    public void FailRun()
    {
        if (currentState == RunState.Failed)
            return;

        waitingForCardChoice = false;
        currentChoices.Clear();

        if (player != null)
            player.PauseHero();

        progressData.failedRuns++;
        SaveProgress();

        rewardSummaryMessage = string.Empty;
        runSummaryMessage = $"Run fallida al tram {segmentsClearedThisRun + 1}.";
        SetState(RunState.Failed);
        Debug.Log("Run fallida");
    }

    public void CompleteRun()
    {
        if (currentState == RunState.Completed)
            return;

        waitingForCardChoice = false;
        currentChoices.Clear();

        if (player != null)
            player.PauseHero();

        progressData.completedRuns++;
        rewardSummaryMessage = GrantVictoryRewards();
        runSummaryMessage = $"Run completada. Trams superats: {segmentsClearedThisRun}/{Mathf.Max(1, segmentsToClearForVictory)}.";
        SaveProgress();

        SetState(RunState.Completed);
        Debug.Log("Run completada");
    }

    void OnGUI()
    {
        if (!useBuiltInUi || !isBootstrapped)
            return;

        DrawHud();

        if (currentState == RunState.AwaitingCardChoice)
        {
            DrawCardSelectionOverlay();
        }
        else if (currentState == RunState.Completed || currentState == RunState.Failed)
        {
            DrawRunSummaryOverlay();
        }
    }

    void BootstrapLocalMode()
    {
        if (isBootstrapped)
            return;

        SetState(RunState.Bootstrapping);

        List<MapCardData> configuredUnlockedCards = new(unlockedCards);

        if (resetLocalProgressOnPlay)
        {
            DeleteSavedProgress();
        }

        runtimeEnemyTemplateInstance = ResolveEnemyTemplate();
        BuildCardLibrary(configuredUnlockedCards);
        EnsureStartingCardReady();
        LoadOrCreateProgress(configuredUnlockedCards);
        SyncUnlockedCardsFromProgress();
        EnsureStartingCardReady();

        isBootstrapped = true;
    }

    void BuildCardLibrary(List<MapCardData> configuredUnlockedCards)
    {
        allCards.Clear();
        cardsById.Clear();

        RegisterCard(startingCard);

        foreach (MapCardData configuredCard in configuredUnlockedCards)
        {
            RegisterCard(configuredCard);
        }

        foreach (MapCardData configuredCard in localCardLibrary)
        {
            RegisterCard(configuredCard);
        }

        List<MapCardData> fallbackCards = MapCardRuntimeFactory.BuildFallbackLibrary(runtimeEnemyTemplateInstance);
        foreach (MapCardData fallbackCard in fallbackCards)
        {
            RegisterCard(fallbackCard);
        }
    }

    void RegisterCard(MapCardData card)
    {
        if (card == null)
            return;

        string cardId = MapCardRuntimeFactory.EnsureCardId(card);
        if (string.IsNullOrEmpty(cardId) || cardsById.ContainsKey(cardId))
            return;

        allCards.Add(card);
        cardsById.Add(cardId, card);
    }

    void EnsureStartingCardReady()
    {
        if (startingCard != null && cardsById.ContainsKey(MapCardRuntimeFactory.EnsureCardId(startingCard)))
            return;

        foreach (MapCardData card in allCards)
        {
            if (card != null && card.startsUnlocked)
            {
                startingCard = card;
                return;
            }
        }

        if (allCards.Count > 0)
        {
            startingCard = allCards[0];
        }
    }

    void LoadOrCreateProgress(List<MapCardData> configuredUnlockedCards)
    {
        progressData = LocalProgressStore.Load(GetProgressFilePath()) ?? new LocalPlayerProgress();
        progressData.unlockedCardIds ??= new List<string>();

        bool hasValidUnlock = false;
        for (int i = progressData.unlockedCardIds.Count - 1; i >= 0; i--)
        {
            string cardId = progressData.unlockedCardIds[i];
            if (!cardsById.ContainsKey(cardId))
            {
                progressData.unlockedCardIds.RemoveAt(i);
                continue;
            }

            hasValidUnlock = true;
        }

        if (!hasValidUnlock)
        {
            CreateDefaultProgress(configuredUnlockedCards);
        }

        progressData.totalCardsUnlocked = progressData.unlockedCardIds.Count;

        SaveProgress();
    }

    void CreateDefaultProgress(List<MapCardData> configuredUnlockedCards)
    {
        progressData = new LocalPlayerProgress();

        foreach (MapCardData configuredCard in configuredUnlockedCards)
        {
            UnlockCard(configuredCard);
        }

        foreach (MapCardData card in allCards)
        {
            if (card != null && card.startsUnlocked)
            {
                UnlockCard(card);
            }
        }

        UnlockCard(startingCard);

        if (progressData.unlockedCardIds.Count == 0 && allCards.Count > 0)
        {
            UnlockCard(allCards[0]);
        }
    }

    void SyncUnlockedCardsFromProgress()
    {
        unlockedCards = new List<MapCardData>();

        foreach (string cardId in progressData.unlockedCardIds)
        {
            if (cardsById.TryGetValue(cardId, out MapCardData card) && card != null)
            {
                unlockedCards.Add(card);
            }
        }
    }

    void GenerateSegmentFromCard(MapCardData card)
    {
        if (card == null || worldGrid == null || player == null)
        {
            Debug.LogError("No s'ha pogut generar el segment perque falten referencies.");
            return;
        }

        worldGrid.GenerateSegment(card);
        player.SetGridPosition(worldGrid.EntryPosition, true);
        player.ResumeHero();
        SetState(RunState.ExploringSegment);

        Debug.Log($"Segment generat: {card.displayName}");
    }

    List<MapCardData> DrawCardChoices(int amount)
    {
        List<MapCardData> result = new();
        if (unlockedCards == null || unlockedCards.Count == 0)
            return result;

        List<MapCardData> pool = new(unlockedCards);

        while (result.Count < amount && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    string GrantVictoryRewards()
    {
        List<string> unlockedRewardNames = new();

        for (int i = 0; i < Mathf.Max(0, cardsUnlockedPerVictory); i++)
        {
            List<MapCardData> lockedCards = GetLockedCards();
            if (lockedCards.Count == 0)
                break;

            MapCardData unlockedCard = lockedCards[Random.Range(0, lockedCards.Count)];
            if (!UnlockCard(unlockedCard))
                continue;

            unlockedRewardNames.Add(unlockedCard.displayName);
        }

        SyncUnlockedCardsFromProgress();

        if (unlockedRewardNames.Count == 0)
            return "No hi havia cap carta pendent de desbloquejar.";

        return $"Nova carta desbloquejada: {string.Join(", ", unlockedRewardNames)}.";
    }

    List<MapCardData> GetLockedCards()
    {
        List<MapCardData> lockedCards = new();

        foreach (MapCardData card in allCards)
        {
            if (card == null)
                continue;

            string cardId = MapCardRuntimeFactory.EnsureCardId(card);
            if (progressData.unlockedCardIds.Contains(cardId))
                continue;

            lockedCards.Add(card);
        }

        return lockedCards;
    }

    bool UnlockCard(MapCardData card)
    {
        if (card == null)
            return false;

        string cardId = MapCardRuntimeFactory.EnsureCardId(card);
        if (string.IsNullOrEmpty(cardId) || progressData.unlockedCardIds.Contains(cardId))
            return false;

        progressData.unlockedCardIds.Add(cardId);
        progressData.totalCardsUnlocked = progressData.unlockedCardIds.Count;
        return true;
    }

    void DrawHud()
    {
        Rect area = new(12f, 12f, 280f, 140f);
        GUILayout.BeginArea(area, GUI.skin.window);
        GUILayout.Label($"Estat: {currentState}");
        GUILayout.Label($"Tram: {Mathf.Min(segmentsClearedThisRun + 1, Mathf.Max(1, segmentsToClearForVictory))}/{Mathf.Max(1, segmentsToClearForVictory)}");
        GUILayout.Label($"Cartes desbloquejades: {unlockedCards.Count}/{allCards.Count}");

        if (player != null)
        {
            GUILayout.Label($"Vida heroi: {player.CurrentHealth}");
        }

        GUILayout.EndArea();
    }

    void DrawCardSelectionOverlay()
    {
        Rect area = new(
            Mathf.Max(20f, (Screen.width - 700f) * 0.5f),
            Mathf.Max(20f, Screen.height - 300f),
            Mathf.Min(700f, Screen.width - 40f),
            260f
        );

        GUILayout.BeginArea(area, GUI.skin.window);
        GUILayout.Label("Porta del desti");
        GUILayout.Label($"Tria la seguent carta per al tram {segmentsClearedThisRun + 1}/{Mathf.Max(1, segmentsToClearForVictory)}.");

        for (int i = 0; i < currentChoices.Count; i++)
        {
            MapCardData card = currentChoices[i];
            if (card == null)
                continue;

            string buttonLabel =
                $"{i + 1}. {card.displayName}\n" +
                $"{card.description}\n" +
                $"Bioma: {card.biomeId} | Obstacles: {card.obstacleChance:0.00} | Enemics: {card.enemyChance:0.00}";

            if (GUILayout.Button(buttonLabel, GUILayout.Height(62f)))
            {
                SelectCard(i);
            }
        }

        GUILayout.EndArea();
    }

    void DrawRunSummaryOverlay()
    {
        Rect area = new(
            Mathf.Max(20f, (Screen.width - 460f) * 0.5f),
            Mathf.Max(20f, (Screen.height - 260f) * 0.5f),
            Mathf.Min(460f, Screen.width - 40f),
            240f
        );

        GUILayout.BeginArea(area, GUI.skin.window);
        GUILayout.Label(currentState == RunState.Completed ? "Run completada" : "Run fallida");
        GUILayout.Label(runSummaryMessage);

        if (!string.IsNullOrEmpty(rewardSummaryMessage))
        {
            GUILayout.Label(rewardSummaryMessage);
        }

        GUILayout.Space(8f);
        GUILayout.Label($"Runs completades: {progressData.completedRuns}");
        GUILayout.Label($"Runs fallides: {progressData.failedRuns}");
        GUILayout.Label($"Cartes desbloquejades: {unlockedCards.Count}/{allCards.Count}");

        GUILayout.Space(12f);

        if (GUILayout.Button("Comencar una nova run", GUILayout.Height(34f)))
        {
            StartRun();
        }

        GUILayout.EndArea();
    }

    GameObject ResolveEnemyTemplate()
    {
        if (fallbackEnemyTemplate != null)
        {
            GameObject template = Instantiate(fallbackEnemyTemplate, transform);
            template.name = "RuntimeEnemyTemplate";
            template.SetActive(false);
            return template;
        }

        Enemy sceneEnemy = FindFirstObjectByType<Enemy>();
        if (sceneEnemy != null)
        {
            GameObject template = Instantiate(sceneEnemy.gameObject, transform);
            template.name = "RuntimeEnemyTemplate";
            template.SetActive(false);
            return template;
        }

        GameObject generatedTemplate = CreateRuntimeEnemyTemplate();
        generatedTemplate.transform.SetParent(transform);
        generatedTemplate.SetActive(false);
        return generatedTemplate;
    }

    GameObject CreateRuntimeEnemyTemplate()
    {
        GameObject template = new("RuntimeEnemyTemplate");
        SpriteRenderer spriteRenderer = template.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateSquareSprite();
        spriteRenderer.color = new Color32(168, 52, 90, 255);
        spriteRenderer.sortingOrder = 10;

        template.AddComponent<Enemy>();
        template.AddComponent<EnemyGridMovement>();
        return template;
    }

    Sprite CreateSquareSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    string GetProgressFilePath()
    {
        return Path.Combine(Application.persistentDataPath, progressFileName);
    }

    void SaveProgress()
    {
        if (progressData == null)
            return;

        LocalProgressStore.Save(GetProgressFilePath(), progressData);
    }

    void DeleteSavedProgress()
    {
        string path = GetProgressFilePath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    void SetState(RunState newState)
    {
        currentState = newState;
    }
}

[System.Serializable]
public class LocalPlayerProgress
{
    public List<string> unlockedCardIds = new();
    public int completedRuns;
    public int failedRuns;
    public int totalRunsStarted;
    public int totalCardsUnlocked;
}

public static class LocalProgressStore
{
    public static LocalPlayerProgress Load(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonUtility.FromJson<LocalPlayerProgress>(json);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"No s'ha pogut carregar el progres local: {exception.Message}");
            return null;
        }
    }

    public static void Save(string path, LocalPlayerProgress progress)
    {
        if (progress == null)
            return;

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(progress, true);
            File.WriteAllText(path, json);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"No s'ha pogut guardar el progres local: {exception.Message}");
        }
    }
}
