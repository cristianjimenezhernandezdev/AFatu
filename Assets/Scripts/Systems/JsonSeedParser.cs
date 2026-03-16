using UnityEngine;

public static class JsonSeedParser
{
    public static EnemyBehaviorConfigData ParseEnemyBehavior(string json)
    {
        return ParseJson(json, new EnemyBehaviorConfigData());
    }

    public static EnemyRewardConfigData ParseEnemyReward(string json)
    {
        return ParseJson(json, new EnemyRewardConfigData());
    }

    public static ModifierEffectConfigData ParseModifierEffect(string json)
    {
        ModifierEffectConfigData data = ParseJson(json, new ModifierEffectConfigData());
        if (data.heroSpeedMultiplier <= 0f)
            data.heroSpeedMultiplier = 1f;
        return data;
    }

    public static DivinePowerEffectConfigData ParseDivinePowerEffect(string json)
    {
        DivinePowerEffectConfigData data = ParseJson(json, new DivinePowerEffectConfigData());
        if (data.speedMultiplier <= 0f)
            data.speedMultiplier = 1f;
        return data;
    }

    public static ConsumableEffectConfigData ParseConsumableEffect(string json)
    {
        ConsumableEffectConfigData data = ParseJson(json, new ConsumableEffectConfigData());
        if (data.speedMultiplier <= 0f)
            data.speedMultiplier = 1f;
        return data;
    }

    public static RelicEffectConfigData ParseRelicEffect(string json)
    {
        return ParseJson(json, new RelicEffectConfigData());
    }

    public static ShopOfferEffectConfigData ParseShopOfferEffect(string json)
    {
        ShopOfferEffectConfigData data = ParseJson(json, new ShopOfferEffectConfigData());
        if (data.speedMultiplierBonus <= 0f)
            data.speedMultiplierBonus = 1f;
        return data;
    }

    private static T ParseJson<T>(string json, T fallback) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return fallback;

        try
        {
            T parsed = JsonUtility.FromJson<T>(json);
            return parsed ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
