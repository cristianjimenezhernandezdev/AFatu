using System.Collections.Generic;
using UnityEngine;

public static class CardArtSpriteCache
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    private static readonly string[] DefaultFolders =
    {
        "CardArt",
        "DivinePowerArt",
        "ShopArt",
        "RelicArt",
        "ConsumableArt",
        "ModifierArt",
        "RunResultArt",
        "ContentArt"
    };

    public static Sprite Load(string artKey)
    {
        return Load(artKey, DefaultFolders);
    }

    public static Sprite Load(string artKey, params string[] folders)
    {
        if (string.IsNullOrWhiteSpace(artKey))
            return null;

        string[] searchFolders = folders == null || folders.Length == 0 ? DefaultFolders : folders;
        for (int i = 0; i < searchFolders.Length; i++)
        {
            string folder = searchFolders[i];
            if (string.IsNullOrWhiteSpace(folder))
                continue;

            string cacheKey = $"{folder}/{artKey}";
            if (Cache.TryGetValue(cacheKey, out Sprite cachedSprite))
                return cachedSprite;

            Sprite sprite = Resources.Load<Sprite>(cacheKey);
            Cache[cacheKey] = sprite;
            if (sprite != null)
                return sprite;
        }

        return null;
    }
}
