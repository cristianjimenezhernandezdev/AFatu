using UnityEngine;

public class GoalTile : MonoBehaviour
{
    public static GoalTile Instance { get; private set; }

    [SerializeField] private Vector2Int gridPosition;
    public Vector2Int GridPosition => gridPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetGridPosition(Vector2Int newPosition)
    {
        gridPosition = newPosition;

        if (WorldGrid.Instance != null)
        {
            transform.position = WorldGrid.Instance.GridToWorld(gridPosition);
        }
    }
}
