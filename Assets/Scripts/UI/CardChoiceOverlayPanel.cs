using UnityEngine;

public static class CardChoiceOverlayPanel
{
    public static void Draw(RunManager runManager)
    {
        Rect area = new Rect(Mathf.Max(20f, (Screen.width - 700f) * 0.5f), Mathf.Max(20f, Screen.height - 310f), Mathf.Min(700f, Screen.width - 40f), 270f);
        GUILayout.BeginArea(area, GUI.skin.window);
        GUILayout.Label("Porta del desti");
        GUILayout.Label("Tria la carta que definira el proxim segment.");

        for (int i = 0; i < runManager.CurrentCardChoices.Count; i++)
        {
            CardSeedData card = runManager.CurrentCardChoices[i];
            string label =
                $"{i + 1}. {card.displayName}\n" +
                $"{card.description}\n" +
                $"Bioma: {card.biomeId} | Tipus: {card.cardType} | Dificultat base: {card.baseDifficulty}";

            if (GUILayout.Button(label, GUILayout.Height(64f)))
                runManager.SelectCard(i);
        }

        GUILayout.EndArea();
    }
}
