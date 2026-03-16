using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardChoiceCanvasSlot : MonoBehaviour
{
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text biomeText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text metaText;
    [SerializeField] private Button chooseButton;
    [SerializeField] private TMP_Text chooseButtonText;

    public void Bind(RunManager runManager, int index)
    {
        if (runManager == null || index < 0 || index >= runManager.CurrentCardChoices.Count)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        CardSeedData card = runManager.CurrentCardChoices[index];
        if (titleText != null)
            titleText.text = card.displayName;
        if (biomeText != null)
            biomeText.text = RunUiTheme.FormatBiome(card.biomeId);
        if (typeText != null)
            typeText.text = $"{card.cardType.ToUpperInvariant()} | Dif. {card.baseDifficulty}";
        if (descriptionText != null)
            descriptionText.text = card.description;
        if (metaText != null)
            metaText.text = $"Segment {card.segmentWidth}x{card.segmentHeight} | Enemics {Mathf.RoundToInt(card.enemyChance * 100f)}% | Obstacles {Mathf.RoundToInt(card.obstacleChance * 100f)}%";
        if (artworkImage != null)
        {
            Sprite sprite = CardArtSpriteCache.Load(card.artKey);
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
        }
        if (chooseButtonText != null)
            chooseButtonText.text = "Escollir carta";
        if (chooseButton != null)
        {
            chooseButton.onClick.RemoveAllListeners();
            chooseButton.onClick.AddListener(() => runManager.SelectCard(index));
        }
    }
}
