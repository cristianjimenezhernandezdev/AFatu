using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridPosition;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int attack = 2;

    private int currentHealth;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int Attack => attack;

    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    void Start()
    {
        if (WorldGrid.Instance != null)
        {
            transform.position = WorldGrid.Instance.GridToWorld(gridPosition);
            WorldGrid.Instance.RegisterEnemy(this);
        }
        else
        {
            Debug.LogWarning("WorldGrid.Instance no trobat quan s'inicialitza Enemy.");
        }
    }

    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (WorldGrid.Instance != null)
        {
            WorldGrid.Instance.RemoveEnemy(this);
        }

        Destroy(gameObject);
    }
}
