using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DivinePowerCanvasSlot : MonoBehaviour
{
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text chargesText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Button activateButton;
    [SerializeField] private TMP_Text activateButtonText;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private Image[] chargePips;

    public void Bind(RunManager runManager, int slotIndex)
    {
        if (runManager == null || slotIndex < 0 || slotIndex >= runManager.EquippedDivinePowers.Count)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        DivinePowerSeedData power = runManager.EquippedDivinePowers[slotIndex];
        int charges = runManager.GetDivinePowerCharges(slotIndex);
        int maxCharges = runManager.GetDivinePowerMaxCharges(slotIndex);
        float cooldown = runManager.GetDivinePowerCooldownSeconds(slotIndex);
        float cooldownNormalized = runManager.GetDivinePowerCooldownNormalized(slotIndex);
        float activeSeconds = runManager.GetDivinePowerActiveSeconds(slotIndex);
        bool canUse = runManager.CanUseDivinePowers && charges > 0;

        if (artworkImage != null)
        {
            Sprite sprite = CardArtSpriteCache.Load(power.artKey, "DivinePowerArt", "ContentArt");
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
        }
        if (titleText != null)
            titleText.text = $"{slotIndex + 1}. {power.displayName}";
        if (descriptionText != null)
            descriptionText.text = power.description;
        if (chargesText != null)
            chargesText.text = $"Carregues {charges}/{maxCharges}";
        if (cooldownText != null)
            cooldownText.text = activeSeconds > 0.01f ? $"Actiu {Mathf.CeilToInt(activeSeconds)}s" : $"Cooldown {Mathf.CeilToInt(cooldown)}s";
        if (stateText != null)
            stateText.text = canUse ? "Disponible" : charges <= 0 ? "Recarregant" : "Bloquejat";
        if (cooldownFill != null)
            cooldownFill.fillAmount = Mathf.Clamp01(cooldownNormalized);

        if (chargePips != null)
        {
            for (int i = 0; i < chargePips.Length; i++)
            {
                if (chargePips[i] == null)
                    continue;
                chargePips[i].enabled = i < maxCharges;
                chargePips[i].color = i < charges ? new Color32(222, 195, 111, 255) : new Color32(88, 90, 104, 255);
            }
        }

        if (activateButtonText != null)
            activateButtonText.text = canUse ? "Activar poder" : charges <= 0 ? "Esperant carrega" : "No disponible";

        if (activateButton != null)
        {
            activateButton.interactable = canUse;
            activateButton.onClick.RemoveAllListeners();
            activateButton.onClick.AddListener(() => runManager.TryActivateDivinePowerSlot(slotIndex));
        }
    }
}
