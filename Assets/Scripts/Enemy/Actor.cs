using UnityEngine;

public class Actor : MonoBehaviour
{
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private GameObject healthbarPrefab;
    [SerializeField] private Vector3 healthbarOffset = new Vector3(0, 2.5f, 0);
    [SerializeField] private int experienceReward = 25;


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
        else
        {
            Debug.LogWarning($"[Actor] Nenhum prefab de barra de vida atribuído em {gameObject.name}");
        }

        if (healthbar != null)
        {
            healthbar.UpdateHealth(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        // Chama qualquer sistema que implemente IAggroReceiver (ex: EnemyDetectionAndAttack)
        IAggroReceiver aggroReceiver = GetComponent<IAggroReceiver>();
        aggroReceiver?.TakeAggro();

        if (healthbar != null)
        {
            healthbar.UpdateHealth(CurrentHealth, maxHealth);
        }

        Debug.Log($"{gameObject.name} recebeu {amount} de dano. HP restante: {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Dá XP ao jogador
        PlayerXP playerXP = FindFirstObjectByType<PlayerXP>();
        if (playerXP != null)
        {
            playerXP.GainXP(experienceReward);
        }

        Debug.Log($"[Enemy] {gameObject.name} morreu e deu {experienceReward} XP!");
        Destroy(gameObject);
    }

}