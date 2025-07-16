using UnityEngine;
using System.Collections;

public class PlayerActor : MonoBehaviour
{
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private GameObject healthbarPrefab;
    [SerializeField] private Vector3 healthbarOffset = new Vector3(0, 2.5f, 0);

    private Healthbar healthbar;
    private float currentHealth;

    public float CurrentHealth
    {
        get => currentHealth;
        private set => currentHealth = value;
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (healthbarPrefab != null)
        {
            GameObject barInstance = Instantiate(healthbarPrefab, transform);
            barInstance.transform.localPosition = healthbarOffset;

            healthbar = barInstance.GetComponentInChildren<Healthbar>();
        }

        if (healthbar != null)
        {
            healthbar.UpdateHealth(currentHealth, maxHealth);
        }
    }


    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (healthbar != null)
        {
            healthbar.UpdateHealth(currentHealth, maxHealth);
        }

        Debug.Log($"[Player] {gameObject.name} recebeu {amount} de dano. HP restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Aqui você pode colocar respawn, animação de morte, etc
        Debug.Log($"[Player] {gameObject.name} morreu!");
        Destroy(gameObject);
    }
}