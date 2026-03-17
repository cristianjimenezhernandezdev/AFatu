using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class TechTreeNodePresentationData
{
    public string nodeId;
    public string branchLabel;
    public string title;
    public string description;
    public string statusLabel;
    public string actionLabel;
    public string artKey;
    public int cost;
    public Color accentColor = Color.white;
    public bool isUnlocked;
    public bool canPurchase;
}

public class TechTreeNodeCanvasSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text branchText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image accentBarImage;
    [SerializeField] private Button actionButton;

    private string boundNodeId;

    public void Bind(TechTreeNodePresentationData data, Action<string> onClick)
    {
        boundNodeId = data != null ? data.nodeId : string.Empty;

        if (branchText != null)
            branchText.text = data?.branchLabel ?? string.Empty;
        if (titleText != null)
            titleText.text = data?.title ?? string.Empty;
        if (descriptionText != null)
            descriptionText.text = data?.description ?? string.Empty;
        if (costText != null)
            costText.text = data == null ? "Cost: --" : $"Cost: {data.cost}";
        if (statusText != null)
            statusText.text = data?.statusLabel ?? string.Empty;
        if (actionButtonText != null)
            actionButtonText.text = data?.actionLabel ?? string.Empty;

        if (frameImage != null)
            frameImage.color = data != null ? data.accentColor : Color.white;
        if (accentBarImage != null)
            accentBarImage.color = data != null ? data.accentColor : Color.white;

        if (artworkImage != null)
        {
            Sprite sprite = data != null ? CardArtSpriteCache.Load(data.artKey) : null;
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
            artworkImage.color = data != null && data.isUnlocked ? Color.white : new Color(1f, 1f, 1f, 0.82f);
        }

        if (actionButton != null)
        {
            actionButton.interactable = data != null && data.canPurchase;
            actionButton.onClick.RemoveAllListeners();
            if (data != null && onClick != null)
                actionButton.onClick.AddListener(() => onClick(boundNodeId));
        }
    }
}
