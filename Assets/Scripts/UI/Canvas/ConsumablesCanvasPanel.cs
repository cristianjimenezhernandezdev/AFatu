using TMPro;
using UnityEngine;

public class ConsumablesCanvasPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private ConsumableCanvasSlot[] slots;

    public void Refresh(RunManager runManager)
    {
        if (runManager == null)
            return;

        if (headerText != null)
            headerText.text = "Consumibles";
        if (hintText != null)
            hintText.text = runManager.CanUseConsumables
                ? "Tecles 3, 4, 5 i 6 per usar consumibles."
                : "Els consumibles nom�s es poden usar durant l'exploracio del segment.";

        int count = slots != null ? slots.Length : 0;
        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].Bind(runManager, i);
        }
    }
}
