using System.Collections.Generic;
using System.Linq;

public sealed class EconomySystem
{
    private readonly PlayerProgressData progress;
    private readonly List<PlayerConsumableStackData> consumableStacks;
    private RunSessionData currentRun;

    public EconomySystem(PlayerProgressData progress, IReadOnlyList<PlayerConsumableStackData> consumables)
    {
        this.progress = progress;
        consumableStacks = consumables != null
            ? new List<PlayerConsumableStackData>(consumables)
            : new List<PlayerConsumableStackData>();
    }

    public int CurrentRunGold => currentRun == null ? 0 : currentRun.goldEarned - currentRun.goldSpent;
    public int CurrentEmeralds => progress.hardCurrency;
    public IReadOnlyList<PlayerConsumableStackData> ConsumableStacks => consumableStacks;

    public void AttachRun(RunSessionData run)
    {
        currentRun = run;
    }

    public void GrantRunGold(int amount)
    {
        if (currentRun == null || amount <= 0)
            return;

        currentRun.goldEarned += amount;
    }

    public bool TrySpendRunGold(int amount)
    {
        if (currentRun == null || amount <= 0)
            return false;

        if (CurrentRunGold < amount)
            return false;

        currentRun.goldSpent += amount;
        return true;
    }

    public void GrantEmeralds(int amount)
    {
        if (amount <= 0)
            return;

        progress.hardCurrency += amount;
    }

    public bool TrySpendEmeralds(int amount)
    {
        if (amount <= 0)
            return false;

        if (progress.hardCurrency < amount)
            return false;

        progress.hardCurrency -= amount;
        return true;
    }

    public void GrantConsumable(string consumableId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(consumableId) || quantity <= 0)
            return;

        PlayerConsumableStackData stack = consumableStacks.FirstOrDefault(item => item.consumableId == consumableId);
        if (stack == null)
        {
            stack = new PlayerConsumableStackData
            {
                playerId = progress.playerId,
                consumableId = consumableId,
                quantity = 0
            };
            consumableStacks.Add(stack);
        }

        stack.quantity += quantity;
    }

    public bool TryConsume(string consumableId)
    {
        PlayerConsumableStackData stack = consumableStacks.FirstOrDefault(item => item.consumableId == consumableId);
        if (stack == null || stack.quantity <= 0)
            return false;

        stack.quantity -= 1;
        return true;
    }

    public int GetConsumableQuantity(string consumableId)
    {
        PlayerConsumableStackData stack = consumableStacks.FirstOrDefault(item => item.consumableId == consumableId);
        return stack == null ? 0 : stack.quantity;
    }
}
