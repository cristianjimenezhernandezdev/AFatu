using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunSummaryCanvasPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button restartButton;

    private RunManager runManager;

    void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(HandleRestart);
    }

    public void Refresh(RunManager value)
    {
        runManager = value;
        bool visible = runManager != null && (runManager.CurrentState == RunManager.RunState.Completed || runManager.CurrentState == RunManager.RunState.Failed);
        if (root != null)
            root.SetActive(visible);
        else
            gameObject.SetActive(visible);

        if (!visible)
            return;

        if (artworkImage != null)
        {
            Sprite sprite = CardArtSpriteCache.Load(runManager.GetCurrentRunResultArtKey(), "RunResultArt", "ContentArt");
            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
        }
        if (titleText != null)
            titleText.text = runManager.SummaryTitle;
        if (messageText != null)
            messageText.text = runManager.SummaryMessage;
    }

    private void HandleRestart()
    {
        runManager?.StartRun();
    }
}
