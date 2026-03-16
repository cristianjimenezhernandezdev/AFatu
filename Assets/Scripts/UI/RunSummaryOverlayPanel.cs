using UnityEngine;

public static class RunSummaryOverlayPanel
{
    public static void Draw(RunManager runManager)
    {
        Rect area = new Rect(Mathf.Max(20f, (Screen.width - 560f) * 0.5f), Mathf.Max(20f, (Screen.height - 280f) * 0.5f), Mathf.Min(560f, Screen.width - 40f), 260f);
        RunUiTheme.DrawPanel(area, new Color32(20, 24, 30, 244), new Color32(188, 160, 103, 255));

        GUI.Label(new Rect(area.x + 24f, area.y + 22f, area.width - 48f, 34f), runManager.SummaryTitle, RunUiTheme.TitleStyle);
        GUI.Label(new Rect(area.x + 24f, area.y + 66f, area.width - 48f, 90f), runManager.SummaryMessage, RunUiTheme.BodyStyle);

        Rect infoRect = new Rect(area.x + 24f, area.y + 154f, area.width - 48f, 38f);
        RunUiTheme.DrawPanel(infoRect, new Color32(31, 39, 49, 255), new Color32(104, 135, 163, 255));
        GUI.Label(new Rect(infoRect.x + 12f, infoRect.y + 10f, infoRect.width - 24f, 20f), $"Or final: {runManager.CurrentGold}   |   Esmeraldes: {runManager.CurrentEmeralds}", RunUiTheme.BodyStyle);

        Rect buttonRect = new Rect(area.x + 24f, area.y + area.height - 56f, area.width - 48f, 38f);
        RunUiTheme.DrawButtonBackground(buttonRect, new Color32(220, 190, 111, 255));
        if (GUI.Button(buttonRect, "Comencar nova run", RunUiTheme.SummaryButtonStyle))
            runManager.StartRun();
    }
}
