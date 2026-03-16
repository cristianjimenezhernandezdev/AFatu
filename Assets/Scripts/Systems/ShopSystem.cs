using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct ShopPurchaseResult
{
    public bool success;
    public bool rerollCardChoices;
    public string feedback;
}

public sealed class ShopSystem
{
    private readonly List<ShopOfferData> catalog = new List<ShopOfferData>();

    public ShopSystem(IContentRepository contentRepository)
    {
        BuildCatalog(contentRepository);
    }

    public IReadOnlyList<ShopOfferData> GenerateOffers()
    {
        List<ShopOfferData> available = new List<ShopOfferData>(catalog);
        List<ShopOfferData> offers = new List<ShopOfferData>();

        while (offers.Count < 3 && available.Count > 0)
        {
            int index = Random.Range(0, available.Count);
            ShopOfferData picked = available[index];
            available.RemoveAt(index);
            offers.Add(Clone(picked));
        }

        return offers;
    }

    public ShopPurchaseResult TryPurchase(ShopOfferData offer, EconomySystem economy, PlayerGridMovement hero, IList<PendingHeroBonusData> pendingBonuses)
    {
        ShopPurchaseResult result = new ShopPurchaseResult();
        if (offer == null)
        {
            result.feedback = "Oferta no valida.";
            return result;
        }

        if (!economy.TrySpendRunGold(offer.cost))
        {
            result.feedback = "No tens prou or.";
            return result;
        }

        ShopOfferEffectConfigData effect = JsonSeedParser.ParseShopOfferEffect(offer.effectConfigJson);
        switch (offer.offerType)
        {
            case "heal":
                int healAmount = effect.heal > 0 ? effect.heal : offer.quantity;
                hero.Heal(healAmount);
                result.feedback = $"{offer.title} aplicada.";
                break;
            case "utility":
                if (effect.rerollCards || offer.rewardId == "reroll_cards")
                {
                    result.rerollCardChoices = true;
                    result.feedback = "Les cartes han estat renovades.";
                    break;
                }

                result.feedback = "Utilitat desconeguda.";
                return result;
            case "buff":
            case "summon":
            case "equipment":
                pendingBonuses.Add(BuildBonus(offer, effect));
                result.feedback = $"{offer.title} preparada per al proxim segment.";
                break;
            default:
                result.feedback = "Oferta desconeguda.";
                return result;
        }

        result.success = true;
        return result;
    }

    private void BuildCatalog(IContentRepository contentRepository)
    {
        catalog.Clear();
        IReadOnlyList<ShopOfferSeedData> seedOffers = contentRepository?.GetShopOffers();
        if (seedOffers == null || seedOffers.Count == 0)
        {
            BuildFallbackCatalog();
            return;
        }

        foreach (ShopOfferSeedData offer in seedOffers.Where(item => item != null && item.isActive).OrderBy(item => item.sortOrder))
        {
            ShopOfferEffectConfigData effect = JsonSeedParser.ParseShopOfferEffect(offer.effectConfigJson);
            catalog.Add(new ShopOfferData
            {
                offerId = offer.offerId,
                title = offer.displayName,
                description = offer.description,
                artKey = offer.artKey,
                offerType = offer.offerType,
                rewardType = offer.rewardType,
                rewardId = offer.rewardId,
                quantity = offer.rewardQuantity,
                durationSegments = offer.durationSegments,
                cost = offer.costGold,
                value = effect.speedMultiplierBonus,
                effectConfigJson = offer.effectConfigJson
            });
        }
    }

    private void BuildFallbackCatalog()
    {
        catalog.AddRange(new[]
        {
            new ShopOfferData { offerId = "heal_small", title = "Curacio Petita", description = "Recupera 8 de vida.", artKey = "shop_heal_small", offerType = "heal", rewardId = "heal_small", quantity = 8, cost = 6, effectConfigJson = "{\"heal\":8}" },
            new ShopOfferData { offerId = "heal_large", title = "Curacio Gran", description = "Recupera 15 de vida.", artKey = "shop_heal_large", offerType = "heal", rewardId = "heal_large", quantity = 15, cost = 10, effectConfigJson = "{\"heal\":15}" },
            new ShopOfferData { offerId = "buff_strength", title = "Buff Forca", description = "+2 atac al proxim segment.", artKey = "shop_buff_strength", offerType = "buff", rewardId = "strength", quantity = 2, cost = 7, durationSegments = 1, effectConfigJson = "{\"attackBonus\":2,\"durationSegments\":1}" },
            new ShopOfferData { offerId = "buff_speed", title = "Buff Velocitat", description = "+25% velocitat al proxim segment.", artKey = "shop_buff_speed", offerType = "buff", rewardId = "speed", cost = 6, durationSegments = 1, value = 1.25f, effectConfigJson = "{\"speedMultiplierBonus\":1.25,\"durationSegments\":1}" },
            new ShopOfferData { offerId = "buff_shield", title = "Escut Temporal", description = "+2 defensa al proxim segment.", artKey = "shop_buff_shield", offerType = "buff", rewardId = "shield", quantity = 2, cost = 7, durationSegments = 1, effectConfigJson = "{\"defenseBonus\":2,\"durationSegments\":1}" },
            new ShopOfferData { offerId = "reroll_cards", title = "Reroll Cartes", description = "Renova les opcions de cartes.", artKey = "shop_reroll_cards", offerType = "utility", rewardId = "reroll_cards", cost = 5, effectConfigJson = "{\"rerollCards\":true}" },
            new ShopOfferData { offerId = "summon_companion", title = "Invocar Company", description = "Ajuda temporal al proxim segment.", artKey = "shop_summon_companion", offerType = "summon", rewardId = "companion", cost = 12, durationSegments = 1, effectConfigJson = "{\"spawnCompanion\":true,\"durationSegments\":1}" },
            new ShopOfferData { offerId = "temporary_equipment", title = "Equip Temporal", description = "+1 atac i +1 defensa durant 2 segments.", artKey = "shop_temporary_equipment", offerType = "equipment", rewardId = "equipment", cost = 9, durationSegments = 2, effectConfigJson = "{\"attackBonus\":1,\"defenseBonus\":1,\"durationSegments\":2}" }
        });
    }

    private static PendingHeroBonusData BuildBonus(ShopOfferData offer, ShopOfferEffectConfigData effect)
    {
        PendingHeroBonusData bonus = new PendingHeroBonusData
        {
            sourceId = offer.offerId,
            durationSegments = offer.durationSegments > 0 ? offer.durationSegments : Mathf.Max(1, effect.durationSegments),
            speedMultiplierBonus = effect.speedMultiplierBonus > 0f ? effect.speedMultiplierBonus : 1f,
            attackBonus = effect.attackBonus,
            defenseBonus = effect.defenseBonus
        };

        switch (offer.rewardId)
        {
            case "strength":
                if (bonus.attackBonus == 0)
                    bonus.attackBonus = offer.quantity;
                break;
            case "speed":
                if (bonus.speedMultiplierBonus <= 1f && offer.value > 0f)
                    bonus.speedMultiplierBonus = offer.value;
                break;
            case "shield":
                if (bonus.defenseBonus == 0)
                    bonus.defenseBonus = offer.quantity;
                break;
            case "companion":
            case "summon_companion":
                if (bonus.attackBonus == 0)
                    bonus.attackBonus = 1;
                if (bonus.defenseBonus == 0)
                    bonus.defenseBonus = 1;
                break;
            case "equipment":
                if (bonus.attackBonus == 0)
                    bonus.attackBonus = 1;
                if (bonus.defenseBonus == 0)
                    bonus.defenseBonus = 1;
                break;
        }

        if (effect.spawnCompanion)
        {
            if (bonus.attackBonus == 0)
                bonus.attackBonus = 1;
            if (bonus.defenseBonus == 0)
                bonus.defenseBonus = 1;
        }

        return bonus;
    }

    private static ShopOfferData Clone(ShopOfferData offer)
    {
        return new ShopOfferData
        {
            offerId = offer.offerId,
            title = offer.title,
            description = offer.description,
            artKey = offer.artKey,
            offerType = offer.offerType,
            rewardType = offer.rewardType,
            rewardId = offer.rewardId,
            quantity = offer.quantity,
            durationSegments = offer.durationSegments,
            cost = offer.cost,
            value = offer.value,
            effectConfigJson = offer.effectConfigJson
        };
    }
}
