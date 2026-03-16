using UnityEngine;

public static class BalanceConfig
{
    public const string LocalPlayerId = "local-player";
    public const string DefaultHeroMode = "prudent";
    public const string HeroModeAggressive = "aggressive";
    public const string HeroModePrudent = "prudent";
    public const string HeroModeEscape = "escape";

    public const int HeroBaseMaxHealth = 30;
    public const int HeroBaseAttack = 5;
    public const int HeroBaseDefense = 1;
    public const float HeroBaseSpeed = 1.0f;

    public const float GridMoveSpeedScale = 4.0f;
    public const float HeroDecisionIntervalSeconds = 0.25f;
    public const float HeroDangerRadius = 6f;
    public const float HeroPrudentDistance = 3f;
    public const float FightHealthThreshold = 0.50f;
    public const float RetreatHealthThreshold = 0.35f;

    public const int DefaultShortRunLength = 5;
    public const int DefaultLongRunLength = 7;
    public const int CardChoiceCount = 3;
    public const int MaxDivinePowerSlots = 2;

    public const int ShopStartSegmentForRun5 = 3;
    public const int ShopStartSegmentForRun7 = 5;

    public const int CompleteRunEmeraldReward = 3;
    public const int FailedRunEmeraldReward = 1;

    private static readonly float[] ShortRunDifficultyCurve = { 1f, 1.1f, 1.25f, 1.4f, 1.6f };
    private static readonly float[] LongRunDifficultyCurve = { 1f, 1.1f, 1.25f, 1.4f, 1.6f, 1.8f, 2f };

    public static float GetDifficultyMultiplier(int segmentIndex, int targetSegmentCount)
    {
        float[] curve = targetSegmentCount >= DefaultLongRunLength ? LongRunDifficultyCurve : ShortRunDifficultyCurve;
        int index = Mathf.Clamp(segmentIndex - 1, 0, curve.Length - 1);
        return curve[index];
    }

    public static int GetBaseEnemyBudget(int segmentIndex, string cardType)
    {
        int baseCount = segmentIndex switch
        {
            <= 1 => 3,
            2 => 4,
            3 => 5,
            4 => 6,
            _ => 7
        };

        return cardType switch
        {
            "safe" => Mathf.Max(2, baseCount - 1),
            "risky" => baseCount + 1,
            "elite" => baseCount + 1,
            _ => baseCount
        };
    }

    public static int GetChestCount(int segmentIndex, string cardType)
    {
        float baseChance = segmentIndex switch
        {
            <= 1 => 0.22f,
            2 => 0.30f,
            3 => 0.40f,
            4 => 0.50f,
            _ => 0.60f
        };

        baseChance += cardType switch
        {
            "safe" => -0.05f,
            "risky" => 0.08f,
            "elite" => 0.10f,
            _ => 0f
        };

        int chestCount = Random.value < Mathf.Clamp01(baseChance) ? 1 : 0;
        if (segmentIndex >= 3 && Random.value < Mathf.Clamp01(baseChance * 0.35f))
            chestCount += 1;

        return chestCount;
    }

    public static float GetChestEmeraldChance(int segmentIndex, string cardType)
    {
        float chance = segmentIndex switch
        {
            <= 1 => 0.03f,
            2 => 0.04f,
            3 => 0.05f,
            4 => 0.06f,
            _ => 0.08f
        };

        chance += cardType switch
        {
            "safe" => -0.01f,
            "risky" => 0.02f,
            "elite" => 0.03f,
            _ => 0f
        };

        return Mathf.Clamp01(chance);
    }

    public static int GetChestGoldAmount(int segmentIndex, string cardType)
    {
        int minimum = segmentIndex switch
        {
            <= 1 => 4,
            2 => 5,
            3 => 6,
            4 => 7,
            _ => 8
        };

        int maximum = segmentIndex switch
        {
            <= 1 => 6,
            2 => 7,
            3 => 9,
            4 => 10,
            _ => 12
        };

        switch (cardType)
        {
            case "safe":
                maximum = Mathf.Max(minimum, maximum - 1);
                break;
            case "risky":
            case "elite":
                maximum += 1;
                break;
        }

        return Random.Range(minimum, maximum + 1);
    }

    public static bool ShouldOpenShop(int segmentsCleared, int targetSegmentCount)
    {
        int threshold = targetSegmentCount >= DefaultLongRunLength
            ? ShopStartSegmentForRun7
            : ShopStartSegmentForRun5;

        return segmentsCleared >= threshold;
    }

    public static bool TryParseHtmlColor(string htmlColor, out Color color)
    {
        if (!string.IsNullOrWhiteSpace(htmlColor) && ColorUtility.TryParseHtmlString(htmlColor, out color))
            return true;

        color = Color.white;
        return false;
    }
}
