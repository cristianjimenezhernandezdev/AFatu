using TMPro;
using UnityEngine;

public class CardChoiceCanvasPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private CardChoiceCanvasSlot[] slots;

    public void Refresh(RunManager runManager)
    {
        bool visible = runManager != null && runManager.CurrentState == RunManager.RunState.AwaitingCardChoice;
        if (root != null)
            root.SetActive(visible);
        else
            gameObject.SetActive(visible);

        if (!visible)
            return;

        if (titleText != null)
            titleText.text = "Porta del desti";
        if (subtitleText != null)
            subtitleText.text = "Escull la propera carta de bioma";

        int count = slots != null ? slots.Length : 0;
        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null)
                continue;
            slots[i].Bind(runManager, i);
        }
    }
}
