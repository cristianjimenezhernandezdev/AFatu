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

    private sealed class PowerRuntimeState
    {
        public DivinePowerSeedData power;
        public int currentCharges;
        public readonly List<float> rechargeTimers = new List<float>();
    }

    private readonly IContentRepository contentRepository;
    private readonly List<DivinePowerSeedData> equippedPowers = new List<DivinePowerSeedData>();
    private readonly Dictionary<string, PowerRuntimeState> powerStatesById = new Dictionary<string, PowerRuntimeState>();
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
        powerStatesById.Clear();
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
            powerStatesById[power.powerId] = new PowerRuntimeState
            {
                power = power,
                currentCharges = BalanceConfig.DivinePowerMaxCharges
            };
        }
    }

    public void Tick(float deltaTime, PlayerGridMovement hero)
    {
        if (deltaTime <= 0f)
            return;

        foreach (PowerRuntimeState state in powerStatesById.Values)
        {
            for (int i = state.rechargeTimers.Count - 1; i >= 0; i--)
            {
                state.rechargeTimers[i] -= deltaTime;
                if (state.rechargeTimers[i] <= 0f)
                {
                    state.rechargeTimers.RemoveAt(i);
                    state.currentCharges = Mathf.Min(GetMaxCharges(state.power.powerId), state.currentCharges + 1);
                }
            }
        }

        for (int i = activeTimedEffects.Count - 1; i >= 0; i--)
        {
            activeTimedEffects[i].remainingSeconds -= deltaTime;
            if (activeTimedEffects[i].remainingSeconds <= 0f)
                activeTimedEffects.RemoveAt(i);
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
        if (!powerStatesById.TryGetValue(power.powerId, out PowerRuntimeState state))
        {
            feedback = "Poder no disponible.";
            return false;
        }

        if (state.currentCharges <= 0)
        {
            float cooldown = GetCooldownRemaining(power.powerId);
            feedback = cooldown > 0.01f
                ? $"{power.displayName} sense carregues. Propera en {Mathf.CeilToInt(cooldown)}s."
                : $"{power.displayName} sense carregues.";
            return false;
        }

        state.currentCharges -= 1;
        if (power.cooldownSeconds > 0)
            state.rechargeTimers.Add(power.cooldownSeconds);

        DivinePowerEffectConfigData effect = JsonSeedParser.ParseDivinePowerEffect(power.effectConfigJson);
        if (power.durationSeconds > 0)
        {
            ActiveTimedEffect activeEffect = activeTimedEffects.Find(item => item.powerId == power.powerId);
            if (activeEffect == null)
            {
                activeTimedEffects.Add(new ActiveTimedEffect
                {
                    powerId = power.powerId,
                    remainingSeconds = power.durationSeconds,
                    effect = effect
                });
            }
            else
            {
                activeEffect.remainingSeconds = power.durationSeconds;
                activeEffect.effect = effect;
            }
        }

        if (effect.spawnCompanion)
            companionSegmentsRemaining = Mathf.Max(companionSegmentsRemaining, Mathf.Max(1, effect.durationSegments));

        ApplyCurrentBonuses(hero);
        feedback = $"Activat: {power.displayName}. Carregues {state.currentCharges}/{GetMaxCharges(power.powerId)}.";
        return true;
    }

    public int GetCurrentCharges(string powerId)
    {
        return powerStatesById.TryGetValue(powerId, out PowerRuntimeState state) ? state.currentCharges : 0;
    }

    public int GetMaxCharges(string powerId)
    {
        return BalanceConfig.DivinePowerMaxCharges;
    }

    public float GetCooldownRemaining(string powerId)
    {
        if (!powerStatesById.TryGetValue(powerId, out PowerRuntimeState state) || state.rechargeTimers.Count == 0)
            return 0f;

        float next = state.rechargeTimers[0];
        for (int i = 1; i < state.rechargeTimers.Count; i++)
            next = Mathf.Min(next, state.rechargeTimers[i]);
        return Mathf.Max(0f, next);
    }

    public float GetCooldownNormalized(string powerId)
    {
        if (!powerStatesById.TryGetValue(powerId, out PowerRuntimeState state) || state.power == null || state.power.cooldownSeconds <= 0)
            return 0f;

        float remaining = GetCooldownRemaining(powerId);
        if (remaining <= 0f)
            return 0f;

        return Mathf.Clamp01(remaining / state.power.cooldownSeconds);
    }

    public float GetActiveRemaining(string powerId)
    {
        ActiveTimedEffect activeEffect = activeTimedEffects.Find(item => item.powerId == powerId);
        return activeEffect != null ? Mathf.Max(0f, activeEffect.remainingSeconds) : 0f;
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
