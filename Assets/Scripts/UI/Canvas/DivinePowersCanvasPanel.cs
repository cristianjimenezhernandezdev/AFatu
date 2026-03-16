using TMPro;
using UnityEngine;

public class DivinePowersCanvasPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private DivinePowerCanvasSlot[] slots;

    public void Refresh(RunManager runManager)
    {
        if (runManager == null)
            return;

        if (headerText != null)
            headerText.text = "Poders divins";
        if (hintText != null)
            hintText.text = runManager.CanUseDivinePowers
                ? "Activa'ls mentre l'heroi avanca. Tecles 1 i 2."
                : "Els poders només es poden usar durant l'exploracio del segment.";

        int count = slots != null ? slots.Length : 0;
        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null)
                continue;
            slots[i].Bind(runManager, i);
        }
    }
}
