using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopOfferCanvasSlot : MonoBehaviour
{
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text metaText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    public void Bind(RunManager runManager, int index)
    {
        if (runManager == null || index < 0 || index >= runManager.CurrentShopOffers.Count)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        ShopOfferData offer = runManager.CurrentShopOffers[index];
        if (artworkImage != null)
        {
            Sprite sprite = CardArtSpriteCache.Load(offer.artKey, "ShopArt", "ConsumableArt", "RelicArt", "ModifierArt", "ContentArt");
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
        }
        if (titleText != null)
            titleText.text = offer.title;
        if (descriptionText != null)
            descriptionText.text = offer.description;
        if (metaText != null)
        {
            string rewardLabel = string.IsNullOrWhiteSpace(offer.rewardId) ? offer.offerType : offer.rewardId;
            metaText.text = $"Cost {offer.cost} | Tipus {offer.offerType} | Recompensa {rewardLabel}";
        }
        if (buyButtonText != null)
            buyButtonText.text = runManager.CurrentGold >= offer.cost ? "Comprar" : $"Falten {offer.cost - runManager.CurrentGold}";
        if (buyButton != null)
        {
            buyButton.interactable = runManager.CurrentGold >= offer.cost;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => runManager.BuyShopOffer(index));
        }
    }
}
