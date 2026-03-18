using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableCanvasSlot : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private Button useButton;

    private RunManager runManager;
    private int slotIndex;

    void Awake()
    {
        if (useButton != null)
            useButton.onClick.AddListener(HandleUseClicked);
    }

    public void Bind(RunManager sourceRunManager, int sourceSlotIndex)
    {
        runManager = sourceRunManager;
        slotIndex = sourceSlotIndex;

        ConsumableSeedData consumable = runManager != null ? runManager.GetConsumableBySlot(slotIndex) : null;
        bool visible = consumable != null;

        if (root != null)
            root.SetActive(visible);
        else
            gameObject.SetActive(visible);

        if (!visible)
            return;

        int quantity = runManager.GetConsumableQuantity(consumable.consumableId);
        float activeSeconds = runManager.GetConsumableActiveSeconds(consumable.consumableId);
        bool skipEncounterArmed = runManager.IsConsumableSkipEncounterArmed(consumable.consumableId);

        if (hotkeyText != null)
            hotkeyText.text = $"[{slotIndex + 3}]";
        if (titleText != null)
            titleText.text = consumable.displayName;
        if (quantityText != null)
            quantityText.text = $"x{quantity}";
        if (descriptionText != null)
            descriptionText.text = consumable.description;
        if (stateText != null)
        {
            if (skipEncounterArmed)
                stateText.text = "Preparat";
            else if (activeSeconds > 0.01f)
                stateText.text = $"Actiu {Mathf.CeilToInt(activeSeconds)}s";
            else
                stateText.text = quantity > 0 ? "Disponible" : "Esgotat";
        }

        if (artworkImage != null)
        {
            Sprite sprite = CardArtSpriteCache.Load(consumable.artKey);
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
            artworkImage.color = quantity > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        if (useButton != null)
            useButton.interactable = runManager.CanUseConsumables && quantity > 0;
    }

    private void HandleUseClicked()
    {
        runManager?.TryUseConsumableSlot(slotIndex);
    }
}
