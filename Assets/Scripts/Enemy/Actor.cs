
using UnityEngine;
using System.Collections;

public class Actor : MonoBehaviour
{
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private GameObject healthbarPrefab;
    [SerializeField] private Vector3 healthbarOffset = new Vector3(0, 2.5f, 0);
    [SerializeField] private int experienceReward = 25;

    private Healthbar healthbar;
    private float currentHealth;
    private Vector3 initialPosition;

    public float CurrentHealth
    {
        get => currentHealth;
        private set => currentHealth = value;
    }

    public float MaxHealth => maxHealth;

    private void Start()
    {
        initialPosition = transform.position;
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
            healthbar.UpdateHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        IAggroReceiver aggroReceiver = GetComponent<IAggroReceiver>();
        aggroReceiver?.TakeAggro();

        if (healthbar != null)
            healthbar.UpdateHealth(CurrentHealth, maxHealth);

        Debug.Log($"{gameObject.name} recebeu {amount} de dano. HP restante: {CurrentHealth}");

        if (CurrentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (TryGetComponent<EnemyDetectionAndAttack>(out var enemy))
        {
            Debug.Log($"[Enemy] {gameObject.name} morreu. Respawn em 45 segundos.");

            // ✅ Concede XP ao player antes de iniciar respawn
            PlayerXP playerXP = FindFirstObjectByType<PlayerXP>();
            if (playerXP != null)
            {
                playerXP.GainXP(experienceReward);
            }

            // ✅ Notifica o QuestTracker
            QuestTracker tracker = FindFirstObjectByType<QuestTracker>();
            if (tracker != null)
            {
                tracker.ContarInimigoDerrotado();
            }

            // ✅ Notifica se for o Boss final (tag "Boss1")
            if (CompareTag("Boss1"))
            {
                QuestTracker bossTracker = FindFirstObjectByType<QuestTracker>();
                if (bossTracker != null)
                {
                    bossTracker.BossFinalDerrotado();
                }
            }

            // ✅ Limpa alvo do player se estiver atacando este inimigo
            var controller = FindFirstObjectByType<PlayerController>();
            if (controller != null && controller.CurrentTarget != null &&
                controller.CurrentTarget.transform == transform)
            {
                controller.ClearTarget();
            }

            if (EnemyRespawnManager.Instance != null)
            {
                EnemyRespawnManager.Instance.ScheduleRespawn(gameObject, initialPosition, 45f);
            }

            return;
        }

        // (mantém essa parte para outros tipos de Actor, se houver)
        PlayerXP fallbackXP = FindFirstObjectByType<PlayerXP>();
        if (fallbackXP != null)
        {
            fallbackXP.GainXP(experienceReward);
        }

        // ✅ Notifica o QuestTracker (fallback)
        QuestTracker fallbackTracker = FindFirstObjectByType<QuestTracker>();
        if (fallbackTracker != null)
        {
            fallbackTracker.ContarInimigoDerrotado();
        }

        // ✅ Notifica se for o Boss final (tag "Boss1") - fallback
        if (CompareTag("Boss1"))
        {
            QuestTracker bossTracker = FindFirstObjectByType<QuestTracker>();
            if (bossTracker != null)
            {
                bossTracker.BossFinalDerrotado();
            }
        }

        Debug.Log($"[Actor] {gameObject.name} morreu e deu {experienceReward} XP!");
        Destroy(gameObject);
    }


    public void Heal(float amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        if (healthbar != null)
            healthbar.UpdateHealth(CurrentHealth, maxHealth);
    }
    public void HealFull()
    {
        // Corrigido: sempre cura totalmente, mesmo se estiver com 0 de vida
        currentHealth = maxHealth;

        if (healthbar != null)
            healthbar.UpdateHealth(currentHealth, maxHealth);
    }
}