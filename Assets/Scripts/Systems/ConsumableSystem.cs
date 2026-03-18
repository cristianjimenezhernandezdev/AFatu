using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ConsumableSystem
{
    private sealed class ActiveTimedEffect
    {
        public string consumableId;
        public float remainingSeconds;
        public ConsumableEffectConfigData effect;
    }

    private readonly IContentRepository contentRepository;
    private readonly EconomySystem economySystem;
    private readonly List<ConsumableSeedData> availableConsumables;
    private readonly List<ActiveTimedEffect> activeTimedEffects = new List<ActiveTimedEffect>();

    private bool skipEncounterArmed;

    public ConsumableSystem(IContentRepository contentRepository, EconomySystem economySystem)
    {
        this.contentRepository = contentRepository;
        this.economySystem = economySystem;
        availableConsumables = contentRepository != null
            ? contentRepository.GetConsumables().Where(item => item != null && item.isActive).ToList()
            : new List<ConsumableSeedData>();
    }

    public IReadOnlyList<ConsumableSeedData> AvailableConsumables => availableConsumables;
    public bool HasSkipEncounterCharge => skipEncounterArmed;

    public void Tick(float deltaTime, PlayerGridMovement hero)
    {
        if (deltaTime <= 0f)
            return;

        for (int i = activeTimedEffects.Count - 1; i >= 0; i--)
        {
            activeTimedEffects[i].remainingSeconds -= deltaTime;
            if (activeTimedEffects[i].remainingSeconds <= 0f)
                activeTimedEffects.RemoveAt(i);
        }

        ApplyCurrentBonuses(hero);
    }

    public void ResetForRun(PlayerGridMovement hero)
    {
        activeTimedEffects.Clear();
        skipEncounterArmed = false;
        ApplyCurrentBonuses(hero);
    }

    public ConsumableSeedData GetConsumableAtSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < availableConsumables.Count ? availableConsumables[slotIndex] : null;
    }

    public int GetQuantity(string consumableId)
    {
        return economySystem != null ? economySystem.GetConsumableQuantity(consumableId) : 0;
    }

    public float GetActiveSeconds(string consumableId)
    {
        ActiveTimedEffect effect = activeTimedEffects.Find(item => item.consumableId == consumableId);
        return effect != null ? Mathf.Max(0f, effect.remainingSeconds) : 0f;
    }

    public bool TryUseSlot(int slotIndex, PlayerGridMovement hero, out string feedback)
    {
        feedback = "Consumible no disponible.";
        if (hero == null || economySystem == null)
            return false;

        ConsumableSeedData consumable = GetConsumableAtSlot(slotIndex);
        if (consumable == null)
            return false;

        int quantity = economySystem.GetConsumableQuantity(consumable.consumableId);
        if (quantity <= 0)
        {
            feedback = $"{consumable.displayName} esgotat.";
            return false;
        }

        ConsumableEffectConfigData effect = JsonSeedParser.ParseConsumableEffect(consumable.effectConfigJson);
        if (effect.heal > 0 && hero.CurrentHealth >= hero.MaxHealth)
        {
            feedback = "La vida ja esta plena.";
            return false;
        }

        if (effect.skipEncounter && skipEncounterArmed)
        {
            feedback = "Ja tens una bomba de fum preparada.";
            return false;
        }

        if (!economySystem.TryConsume(consumable.consumableId))
        {
            feedback = $"{consumable.displayName} esgotat.";
            return false;
        }

        if (effect.heal > 0)
            hero.Heal(effect.heal);

        if (effect.durationSeconds > 0 && (effect.defenseBonus != 0 || effect.speedMultiplier > 1f))
            ApplyTimedEffect(consumable.consumableId, effect);

        if (effect.skipEncounter)
            skipEncounterArmed = true;

        ApplyCurrentBonuses(hero);
        feedback = BuildUseFeedback(consumable, effect);
        return true;
    }

    public bool TryPreventEncounter(Enemy enemy, out string feedback)
    {
        feedback = string.Empty;
        if (!skipEncounterArmed || enemy == null || !enemy.IsAlive())
            return false;

        skipEncounterArmed = false;
        enemy.DespawnWithoutReward();
        feedback = "Bomba de fum usada: combat evitat.";
        return true;
    }

    private void ApplyTimedEffect(string consumableId, ConsumableEffectConfigData effect)
    {
        ActiveTimedEffect activeEffect = activeTimedEffects.Find(item => item.consumableId == consumableId);
        if (activeEffect == null)
        {
            activeTimedEffects.Add(new ActiveTimedEffect
            {
                consumableId = consumableId,
                remainingSeconds = effect.durationSeconds,
                effect = effect
            });
            return;
        }

        activeEffect.remainingSeconds = effect.durationSeconds;
        activeEffect.effect = effect;
    }

    private void ApplyCurrentBonuses(PlayerGridMovement hero)
    {
        if (hero == null)
            return;

        int defenseBonus = 0;
        float speedMultiplier = 1f;

        for (int i = 0; i < activeTimedEffects.Count; i++)
        {
            ConsumableEffectConfigData effect = activeTimedEffects[i].effect;
            defenseBonus += effect.defenseBonus;
            if (effect.speedMultiplier > 0f)
                speedMultiplier *= effect.speedMultiplier;
        }

        hero.SetConsumableBonuses(defenseBonus, speedMultiplier);
    }

    private static string BuildUseFeedback(ConsumableSeedData consumable, ConsumableEffectConfigData effect)
    {
        if (effect.heal > 0)
            return $"{consumable.displayName} usada: +{effect.heal} vida.";
        if (effect.skipEncounter)
            return $"{consumable.displayName} preparada: evitaras el proxim combat.";
        if (effect.defenseBonus > 0)
            return $"{consumable.displayName} usada: +{effect.defenseBonus} defensa temporal.";
        if (effect.speedMultiplier > 1f)
            return $"{consumable.displayName} usada: velocitat millorada temporalment.";

        return $"{consumable.displayName} usada.";
    }
}
