using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ArchitectusFati/Map Card", fileName = "NewMapCard")]
public class MapCardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;
    public string displayName;
    [TextArea] public string description;

    [Header("Progression")]
    public bool startsUnlocked;

    [Header("Biome / Visual")]
    public string biomeId;
    public Color floorColor = Color.gray;
    public Color wallColor = Color.black;

    [Header("Segment")]
    [Min(5)] public int segmentWidth = 16;
    [Min(5)] public int segmentHeight = 10;
    [Min(3)] public int entryX = 1;
    [Min(3)] public int exitX = 14;

    [Header("Generation Chances")]
    [Range(0f, 1f)] public float obstacleChance = 0.12f;
    [Range(0f, 1f)] public float enemyChance = 0.10f;

    [Header("Enemy Prefabs")]
    public GameObject[] possibleEnemyPrefabs;
}

public static class MapCardRuntimeFactory
{
    public static string EnsureCardId(MapCardData card)
    {
        if (card == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(card.cardId))
            return card.cardId;

        string seed = !string.IsNullOrWhiteSpace(card.displayName)
            ? card.displayName
            : (!string.IsNullOrWhiteSpace(card.name) ? card.name : $"card_{card.GetInstanceID()}");

        card.cardId = SanitizeId(seed);
        return card.cardId;
    }

    public static List<MapCardData> BuildFallbackLibrary(GameObject[] enemyTemplates)
    {
        GameObject[] enemyPool = enemyTemplates ?? System.Array.Empty<GameObject>();

        return new List<MapCardData>
        {
            CreateRuntimeCard(
                "verdant_path",
                "Sender Verdant",
                "Boscos oberts amb pocs obstacles i amenaces moderades.",
                true,
                "forest",
                new Color32(111, 150, 86, 255),
                new Color32(57, 87, 50, 255),
                20,
                12,
                0.10f,
                0.12f,
                enemyPool
            ),
            CreateRuntimeCard(
                "ashen_corridor",
                "Corredor Cendros",
                "Ruines estretes amb molts murs i emboscades frequents.",
                true,
                "ruins",
                new Color32(154, 145, 133, 255),
                new Color32(81, 73, 68, 255),
                21,
                12,
                0.18f,
                0.18f,
                enemyPool
            ),
            CreateRuntimeCard(
                "moonlit_bog",
                "Aiguamoll Lunar",
                "Bioma dens amb camins irregulars i enemics puntuals.",
                true,
                "swamp",
                new Color32(92, 126, 103, 255),
                new Color32(44, 67, 58, 255),
                20,
                12,
                0.16f,
                0.14f,
                enemyPool
            ),
            CreateRuntimeCard(
                "saffron_dunes",
                "Dunes Safra",
                "Zona mes oberta on el perill arriba en onades disperses.",
                false,
                "desert",
                new Color32(217, 182, 113, 255),
                new Color32(135, 96, 56, 255),
                21,
                12,
                0.08f,
                0.20f,
                enemyPool
            ),
            CreateRuntimeCard(
                "ivory_citadel",
                "Ciutadella d'Ivori",
                "Segment llarg i perillos on l'heroi ha de forcar rutes alternatives.",
                false,
                "citadel",
                new Color32(203, 208, 214, 255),
                new Color32(85, 92, 108, 255),
                21,
                12,
                0.20f,
                0.16f,
                enemyPool
            ),
            CreateRuntimeCard(
                "ember_depths",
                "Fondaries Brasa",
                "Coves calentes amb passadissos trencats i pressio constant.",
                false,
                "cavern",
                new Color32(171, 100, 69, 255),
                new Color32(93, 47, 36, 255),
                20,
                12,
                0.22f,
                0.22f,
                enemyPool
            )
        };
    }

    public static MapCardData CreateRuntimeCardFromRemote(RemoteMapCardDefinition definition, GameObject[] enemyTemplates)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.cardId))
            return null;

        GameObject[] enemyPool = enemyTemplates ?? System.Array.Empty<GameObject>();

        Color floorColor = ParseColorOrDefault(definition.floorColorHex, Color.gray);
        Color wallColor = ParseColorOrDefault(definition.wallColorHex, Color.black);
        int width = Mathf.Max(5, definition.segmentWidth);
        int height = Mathf.Max(5, definition.segmentHeight);

        MapCardData card = CreateRuntimeCard(
            definition.cardId,
            string.IsNullOrWhiteSpace(definition.displayName) ? definition.cardId : definition.displayName,
            definition.description ?? string.Empty,
            definition.startsUnlocked,
            string.IsNullOrWhiteSpace(definition.biomeId) ? "unknown" : definition.biomeId,
            floorColor,
            wallColor,
            width,
            height,
            Mathf.Clamp01(definition.obstacleChance),
            Mathf.Clamp01(definition.enemyChance),
            enemyPool
        );

        card.entryX = Mathf.Clamp(definition.entryX, 1, Mathf.Max(1, width - 2));
        card.exitX = Mathf.Clamp(definition.exitX, 1, Mathf.Max(1, width - 2));
        return card;
    }

    private static MapCardData CreateRuntimeCard(
        string id,
        string title,
        string description,
        bool startsUnlocked,
        string biomeId,
        Color floorColor,
        Color wallColor,
        int width,
        int height,
        float obstacleChance,
        float enemyChance,
        GameObject[] enemies)
    {
        MapCardData card = ScriptableObject.CreateInstance<MapCardData>();
        card.hideFlags = HideFlags.HideAndDontSave;
        card.name = title;
        card.cardId = SanitizeId(id);
        card.displayName = title;
        card.description = description;
        card.startsUnlocked = startsUnlocked;
        card.biomeId = biomeId;
        card.floorColor = floorColor;
        card.wallColor = wallColor;
        card.segmentWidth = width;
        card.segmentHeight = height;
        card.entryX = 1;
        card.exitX = width - 2;
        card.obstacleChance = obstacleChance;
        card.enemyChance = enemyChance;
        card.possibleEnemyPrefabs = enemies;
        return card;
    }

    private static string SanitizeId(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return "card";

        string lowerValue = rawValue.Trim().ToLowerInvariant();
        System.Text.StringBuilder builder = new(lowerValue.Length);

        foreach (char character in lowerValue)
        {
            if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
            {
                builder.Append(character);
                continue;
            }

            if (character == ' ' || character == '-' || character == '_')
            {
                builder.Append('_');
            }
        }

        string sanitized = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "card" : sanitized;
    }

    private static Color ParseColorOrDefault(string htmlColor, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(htmlColor) && ColorUtility.TryParseHtmlString(htmlColor, out Color parsedColor))
            return parsedColor;

        return fallback;
    }
}
