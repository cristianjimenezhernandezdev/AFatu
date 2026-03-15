using UnityEngine;
using UnityEngine.InputSystem;

public class WorldModifier : MonoBehaviour
{
    public Camera cam;

    void Update()
    {
        if (WorldGrid.Instance == null || cam == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWorldPos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2Int gridPos = WorldGrid.Instance.WorldToGrid(mouseWorldPos);

            WorldGrid.Instance.ToggleWall(gridPos);
        }
    }
}
