using System;
using System.Collections.Generic;

[Serializable]
public class LocalPlayerProgress
{
    public List<string> unlockedCardIds = new List<string>();
    public int completedRuns;
    public int failedRuns;
    public int totalRunsStarted;
    public int totalCardsUnlocked;
}
