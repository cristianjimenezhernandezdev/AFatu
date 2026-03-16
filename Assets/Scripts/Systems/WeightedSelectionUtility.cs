using System.Collections.Generic;
using UnityEngine;

public static class WeightedSelectionUtility
{
    public static T PickWeighted<T>(IReadOnlyList<T> entries, System.Func<T, int> weightSelector)
    {
        if (entries == null || entries.Count == 0)
            return default;

        int totalWeight = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            totalWeight += Mathf.Max(0, weightSelector(entries[i]));
        }

        if (totalWeight <= 0)
            return entries[Random.Range(0, entries.Count)];

        int roll = Random.Range(0, totalWeight);
        int cursor = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            cursor += Mathf.Max(0, weightSelector(entries[i]));
            if (roll < cursor)
                return entries[i];
        }

        return entries[entries.Count - 1];
    }
}
