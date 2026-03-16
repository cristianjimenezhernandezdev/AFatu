using UnityEngine;

public class Chest : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;

    private bool isOpened;

    public Vector2Int GridPosition => gridPosition;
    public int GoldReward { get; private set; }
    public int EmeraldReward { get; private set; }
    public string ChestTier { get; private set; } = "small";
    public bool IsOpened => isOpened;

    void Start()
    {
        if (WorldGrid.Instance != null)
        {
            transform.position = WorldGrid.Instance.GridToWorld(gridPosition);
            WorldGrid.Instance.RegisterChest(this);
        }
    }

    public void Configure(GeneratedChestSpawnData spawn)
    {
        if (spawn == null)
            return;

        gridPosition = spawn.gridPosition;
        GoldReward = Mathf.Max(0, spawn.reward != null ? spawn.reward.gold : 0);
        EmeraldReward = Mathf.Max(0, spawn.reward != null ? spawn.reward.emeralds : 0);
        ChestTier = spawn.reward != null && !string.IsNullOrWhiteSpace(spawn.reward.chestTier) ? spawn.reward.chestTier : "small";
        isOpened = false;

        if (WorldGrid.Instance != null)
            transform.position = WorldGrid.Instance.GridToWorld(gridPosition);

        ProceduralChestRenderer renderer = GetComponent<ProceduralChestRenderer>();
        if (renderer != null)
            renderer.SetOpened(false, ChestTier);
    }

    public void Open()
    {
        if (isOpened)
            return;

        isOpened = true;
        WorldGrid.Instance?.RemoveChest(this);

        ProceduralChestRenderer renderer = GetComponent<ProceduralChestRenderer>();
        if (renderer != null)
            renderer.SetOpened(true, ChestTier);
    }
}
