using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCanvasPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private ShopOfferCanvasSlot[] slots;
    [SerializeField] private Button skipButton;

    void Awake()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(HandleSkip);
    }

    public RunManager RunManager { get; set; }

    public void Refresh(RunManager runManager)
    {
        RunManager = runManager;
        bool visible = runManager != null && runManager.CurrentState == RunManager.RunState.AwaitingShopChoice;
        if (root != null)
            root.SetActive(visible);
        else
            gameObject.SetActive(visible);

        if (!visible)
            return;

        if (titleText != null)
            titleText.text = "Botiga de la run";
        if (goldText != null)
            goldText.text = $"Or disponible: {runManager.CurrentGold}";

        int count = slots != null ? slots.Length : 0;
        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null)
                continue;
            slots[i].Bind(runManager, i);
        }
    }

    private void HandleSkip()
    {
        RunManager?.SkipShop();
    }
}
