using System.Collections.Generic;
using UnityEngine;

public struct ShopPurchaseResult
{
    public bool success;
    public bool rerollCardChoices;
    public string feedback;
}

public sealed class ShopSystem
{
    private readonly List<ShopOfferData> catalog = new List<ShopOfferData>
    {
        new ShopOfferData { offerId = "heal_small", title = "Curacio Petita", description = "Recupera 8 de vida.", offerType = "heal", quantity = 8, cost = 6 },
        new ShopOfferData { offerId = "heal_large", title = "Curacio Gran", description = "Recupera 15 de vida.", offerType = "heal", quantity = 15, cost = 10 },
        new ShopOfferData { offerId = "buff_strength", title = "Buff Forca", description = "+2 atac al proxim segment.", offerType = "segment_bonus", rewardId = "strength", quantity = 2, cost = 7 },
        new ShopOfferData { offerId = "buff_speed", title = "Buff Velocitat", description = "+25% velocitat al proxim segment.", offerType = "segment_bonus", rewardId = "speed", cost = 6, value = 1.25f },
        new ShopOfferData { offerId = "buff_shield", title = "Escut Temporal", description = "+2 defensa al proxim segment.", offerType = "segment_bonus", rewardId = "shield", quantity = 2, cost = 7 },
        new ShopOfferData { offerId = "reroll_cards", title = "Reroll Cartes", description = "Renova les opcions de cartes.", offerType = "reroll", cost = 5 },
        new ShopOfferData { offerId = "summon_companion", title = "Invocar Company", description = "Ajuda temporal al proxim segment.", offerType = "segment_bonus", rewardId = "companion", cost = 12 },
        new ShopOfferData { offerId = "temporary_equipment", title = "Equip Temporal", description = "+1 atac i +1 defensa durant 2 segments.", offerType = "segment_bonus", rewardId = "equipment", cost = 9 }
    };

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

        switch (offer.offerType)
        {
            case "heal":
                hero.Heal(offer.quantity);
                result.feedback = $"{offer.title} aplicada.";
                break;
            case "reroll":
                result.rerollCardChoices = true;
                result.feedback = "Les cartes han estat renovades.";
                break;
            case "segment_bonus":
                pendingBonuses.Add(BuildBonus(offer));
                result.feedback = $"{offer.title} preparada per al proxim segment.";
                break;
            default:
                result.feedback = "Oferta desconeguda.";
                return result;
        }

        result.success = true;
        return result;
    }

    private static PendingHeroBonusData BuildBonus(ShopOfferData offer)
    {
        PendingHeroBonusData bonus = new PendingHeroBonusData
        {
            sourceId = offer.offerId,
            durationSegments = offer.offerId == "temporary_equipment" ? 2 : 1,
            speedMultiplierBonus = 1f
        };

        switch (offer.rewardId)
        {
            case "strength":
                bonus.attackBonus = offer.quantity;
                break;
            case "speed":
                bonus.speedMultiplierBonus = Mathf.Max(offer.value, 1f);
                break;
            case "shield":
                bonus.defenseBonus = offer.quantity;
                break;
            case "companion":
                bonus.attackBonus = 1;
                bonus.defenseBonus = 1;
                break;
            case "equipment":
                bonus.attackBonus = 1;
                bonus.defenseBonus = 1;
                break;
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
            offerType = offer.offerType,
            rewardId = offer.rewardId,
            quantity = offer.quantity,
            cost = offer.cost,
            value = offer.value
        };
    }
}
