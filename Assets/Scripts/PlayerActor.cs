using UnityEngine;

public class PlayerActor : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private GameObject healthbarPrefab;
    [SerializeField] private Vector3 healthbarOffset = new Vector3(0, 2.5f, 0);

    private Healthbar healthbar;
    private Vector3 initialPosition;

    public float CurrentHealth => stats.CurrentHealth;
    public PlayerStats Stats => stats; // Getter público seguro

    private void Start()
    {
        initialPosition = transform.position;
        stats.CurrentHealth = stats.MaxHealth;

        if (healthbarPrefab != null)
        {
            GameObject barInstance = Instantiate(healthbarPrefab, transform);
            barInstance.transform.localPosition = healthbarOffset;
            healthbar = barInstance.GetComponentInChildren<Healthbar>();
        }

        if (healthbar != null)
            healthbar.UpdateHealth(stats.CurrentHealth, stats.MaxHealth);
    }

    public void TakeDamage(int amount)
    {
        stats.CurrentHealth -= amount;
        stats.CurrentHealth = Mathf.Max(stats.CurrentHealth, 0);

        if (healthbar != null)
            healthbar.UpdateHealth(stats.CurrentHealth, stats.MaxHealth);

        Debug.Log($"[Player] {gameObject.name} recebeu {amount} de dano. HP restante: {stats.CurrentHealth}");

        if (stats.CurrentHealth <= 0)
            Die();
    }

    public void IncreaseMaxHealth(float amount)
    {
        stats.MaxHealth += amount;
        stats.CurrentHealth += amount;

        if (healthbar != null)
            healthbar.UpdateHealth(stats.CurrentHealth, stats.MaxHealth);
    }

    private void Die()
    {
        Debug.Log("[Player] Morreu. Fazendo respawn...");

        if (PlayerRespawnManager.Instance != null)
            PlayerRespawnManager.Instance.RespawnPlayer(this, initialPosition);
        else
            Debug.LogError("PlayerRespawnManager não encontrado na cena!");
    }
}