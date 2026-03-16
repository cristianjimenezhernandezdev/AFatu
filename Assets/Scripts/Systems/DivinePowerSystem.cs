using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class DivinePowerSystem
{
    private sealed class ActiveTimedEffect
    {
        public string powerId;
        public float remainingSeconds;
        public DivinePowerEffectConfigData effect;
    }

    private readonly IContentRepository contentRepository;
    private readonly List<DivinePowerSeedData> equippedPowers = new List<DivinePowerSeedData>();
    private readonly Dictionary<string, float> cooldownRemainingByPowerId = new Dictionary<string, float>();
    private readonly List<ActiveTimedEffect> activeTimedEffects = new List<ActiveTimedEffect>();

    private int companionSegmentsRemaining;

    public DivinePowerSystem(IContentRepository contentRepository)
    {
        this.contentRepository = contentRepository;
    }

    public IReadOnlyList<DivinePowerSeedData> EquippedPowers => equippedPowers;

    public void EquipPowers(IReadOnlyList<string> powerIds)
    {
        equippedPowers.Clear();
        cooldownRemainingByPowerId.Clear();
        activeTimedEffects.Clear();
        companionSegmentsRemaining = 0;

        if (powerIds == null)
            return;

        foreach (string powerId in powerIds.Where(id => !string.IsNullOrWhiteSpace(id)).Take(BalanceConfig.MaxDivinePowerSlots))
        {
            DivinePowerSeedData power = contentRepository.GetDivinePower(powerId);
            if (power == null || !power.isActive)
                continue;

            equippedPowers.Add(power);
            cooldownRemainingByPowerId[power.powerId] = 0f;
        }
    }

    public void Tick(float deltaTime, PlayerGridMovement hero)
    {
        if (deltaTime <= 0f)
            return;

        List<string> keys = cooldownRemainingByPowerId.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            cooldownRemainingByPowerId[keys[i]] = Mathf.Max(0f, cooldownRemainingByPowerId[keys[i]] - deltaTime);
        }

        for (int i = activeTimedEffects.Count - 1; i >= 0; i--)
        {
            activeTimedEffects[i].remainingSeconds -= deltaTime;
            if (activeTimedEffects[i].remainingSeconds <= 0f)
            {
                activeTimedEffects.RemoveAt(i);
            }
        }

        ApplyCurrentBonuses(hero);
    }

    public void OnSegmentStarted(PlayerGridMovement hero)
    {
        ApplyCurrentBonuses(hero);
    }

    public void OnSegmentEnded(PlayerGridMovement hero)
    {
        if (companionSegmentsRemaining > 0)
            companionSegmentsRemaining -= 1;

        ApplyCurrentBonuses(hero);
    }

    public bool TryActivate(int slotIndex, PlayerGridMovement hero, out string feedback)
    {
        feedback = string.Empty;

        if (slotIndex < 0 || slotIndex >= equippedPowers.Count)
        {
            feedback = "Poder no disponible.";
            return false;
        }

        DivinePowerSeedData power = equippedPowers[slotIndex];
        if (GetCooldownRemaining(power.powerId) > 0f)
        {
            feedback = $"{power.displayName} encara en reutilitzacio.";
            return false;
        }

        DivinePowerEffectConfigData effect = JsonSeedParser.ParseDivinePowerEffect(power.effectConfigJson);
        if (power.durationSeconds > 0)
        {
            activeTimedEffects.Add(new ActiveTimedEffect
            {
                powerId = power.powerId,
                remainingSeconds = power.durationSeconds,
                effect = effect
            });
        }

        if (effect.spawnCompanion)
        {
            companionSegmentsRemaining = Mathf.Max(companionSegmentsRemaining, Mathf.Max(1, effect.durationSegments));
        }

        cooldownRemainingByPowerId[power.powerId] = power.cooldownSeconds;
        ApplyCurrentBonuses(hero);
        feedback = $"Activat: {power.displayName}.";
        return true;
    }

    public float GetCooldownRemaining(string powerId)
    {
        return cooldownRemainingByPowerId.TryGetValue(powerId, out float value) ? value : 0f;
    }

    public string GetEffectiveHeroMode(string baseMode)
    {
        for (int i = activeTimedEffects.Count - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(activeTimedEffects[i].effect.heroMode))
                return activeTimedEffects[i].effect.heroMode;
        }

        return baseMode;
    }

    private void ApplyCurrentBonuses(PlayerGridMovement hero)
    {
        if (hero == null)
            return;

        int attackBonus = companionSegmentsRemaining > 0 ? 1 : 0;
        int defenseBonus = companionSegmentsRemaining > 0 ? 1 : 0;
        float speedMultiplier = 1f;

        for (int i = 0; i < activeTimedEffects.Count; i++)
        {
            DivinePowerEffectConfigData effect = activeTimedEffects[i].effect;
            attackBonus += effect.attackBonus;
            defenseBonus += effect.defenseBonus;
            if (effect.speedMultiplier > 0f)
                speedMultiplier *= effect.speedMultiplier;
        }

        hero.SetDivinePowerBonuses(attackBonus, defenseBonus, speedMultiplier);
    }
}
