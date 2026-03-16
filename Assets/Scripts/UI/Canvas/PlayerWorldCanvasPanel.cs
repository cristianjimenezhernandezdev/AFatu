using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWorldCanvasPanel : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, -30f);
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text combatText;

    public void Refresh(RunManager runManager)
    {
        if (runManager == null || runManager.Player == null || root == null)
            return;

        PlayerGridMovement player = runManager.Player;
        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 screenPoint = camera.WorldToScreenPoint(player.transform.position + new Vector3(0f, 1.15f, 0f));
        bool visible = screenPoint.z > 0f && runManager.CurrentState == RunManager.RunState.ExploringSegment;
        root.gameObject.SetActive(visible);
        if (!visible)
            return;

        root.position = screenPoint + (Vector3)screenOffset;
        if (healthFill != null)
            healthFill.fillAmount = player.MaxHealth <= 0 ? 0f : player.CurrentHealth / (float)player.MaxHealth;
        if (healthText != null)
            healthText.text = $"{player.CurrentHealth}/{player.MaxHealth}";
        if (combatText != null)
            combatText.text = player.HasRecentCombat ? $"+{player.LastCombatDamageDealt} / -{player.LastCombatDamageTaken}" : string.Empty;
    }
}
