using UnityEngine;

public static class RunSummaryOverlayPanel
{
    public static void Draw(RunManager runManager)
    {
        Rect area = new Rect(Mathf.Max(20f, (Screen.width - 460f) * 0.5f), Mathf.Max(20f, (Screen.height - 240f) * 0.5f), Mathf.Min(460f, Screen.width - 40f), 220f);
        GUILayout.BeginArea(area, GUI.skin.window);
        GUILayout.Label(runManager.SummaryTitle);
        GUILayout.Label(runManager.SummaryMessage);
        GUILayout.Space(12f);
        if (GUILayout.Button("Comencar nova run", GUILayout.Height(34f)))
            runManager.StartRun();
        GUILayout.EndArea();
    }
}
