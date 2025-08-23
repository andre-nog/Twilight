using UnityEngine;
using Unity.Netcode;

public class Actor : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private GameObject healthbarPrefab;
    [SerializeField] private Vector3 healthbarOffset = new Vector3(0, 2.5f, 0);
    [SerializeField] private int experienceReward = 25;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    private Healthbar healthbar;
    private Vector3 initialPosition;

    public float MaxHealth => maxHealth;

    private void Awake()
    {
        // reservado para futuras inicializações
        var _ = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[Actor] OnNetworkSpawn — {gameObject.name} IsServer={IsServer}");

        if (IsServer)
        {
            if (CurrentHealth.Value <= 0f || CurrentHealth.Value > maxHealth)
                CurrentHealth.Value = maxHealth;

            initialPosition = transform.position;
        }

        if (healthbarPrefab != null)
        {
            var barInstance = Instantiate(healthbarPrefab, transform);
            barInstance.transform.localPosition = healthbarOffset;
            healthbar = barInstance.GetComponentInChildren<Healthbar>();
        }
        else
        {
            Debug.LogWarning($"[Actor] Sem healthbarPrefab em {gameObject.name}");
        }

        CurrentHealth.OnValueChanged += OnHealthChanged;

        if (healthbar != null)
            healthbar.UpdateHealth(CurrentHealth.Value, maxHealth);
    }

    public override void OnDestroy()
    {
        if (IsSpawned)
            CurrentHealth.OnValueChanged -= OnHealthChanged;

        base.OnDestroy();
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        healthbar?.UpdateHealth(newValue, maxHealth);
    }

    public virtual void TakeDamage(int amount)
    {
        if (!IsServer || CurrentHealth.Value <= 0f)
            return;

        Debug.Log($"[Actor] TakeDamage em {gameObject.name}: dano={amount}, vidaAntes={CurrentHealth.Value}");
        CurrentHealth.Value = Mathf.Max(CurrentHealth.Value - amount, 0f);

        // Fallback de aggro quando não sabemos quem atacou
        GetComponent<IAggroReceiver>()?.TakeAggro();

        if (CurrentHealth.Value <= 0f)
            Die();
    }

    /// <summary>
    /// Versão que recebe o atacante (Transform) para direcionar aggro corretamente.
    /// Chame esta no servidor quando souber quem aplicou o dano (projétil/skill).
    /// </summary>
    public void TakeDamageFrom(int amount, Transform attacker)
    {
        if (!IsServer || CurrentHealth.Value <= 0f)
            return;

        Debug.Log($"[Actor] TakeDamageFrom em {gameObject.name}: dano={amount}, attacker={attacker?.name}");
        CurrentHealth.Value = Mathf.Max(CurrentHealth.Value - amount, 0f);

        if (TryGetComponent<EnemyDetectionAndAttack>(out var enemyAI) && attacker != null)
            enemyAI.TakeAggro(attacker);
        else
            GetComponent<IAggroReceiver>()?.TakeAggro(); // fallback

        if (CurrentHealth.Value <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        if (!IsServer)
            return;

        Debug.Log($"[Actor] Die() chamado em {gameObject.name}");

        if (TryGetComponent<EnemyDetectionAndAttack>(out var enemyAI))
        {
            // XP/Quest hooks existentes
            FindFirstObjectByType<PlayerXP>()?.GainXP(experienceReward);
            FindFirstObjectByType<QuestTracker>()?.ContarInimigoDerrotado();
            if (CompareTag("Boss1"))
                FindFirstObjectByType<QuestTracker>()?.BossFinalDerrotado();

            // 🔔 Limpa alvo em TODOS os clients que estavam mirando este inimigo
            var no = GetComponent<NetworkObject>();
            if (no != null)
                ClearTargetIfMatchesClientRpc(new NetworkObjectReference(no));

            // Não damos SetActive aqui — o RespawnManager faz o hide/show sincronizado
            EnemyRespawnManager.Instance?.ScheduleRespawn(gameObject, initialPosition, 2f);
            return;
        }

        // Fallback se não for inimigo padrão
        Debug.Log($"[Actor] {gameObject.name} morreu e deu {experienceReward} XP");
        var xp = FindFirstObjectByType<PlayerXP>();
        xp?.GainXP(experienceReward);

        var tracker = FindFirstObjectByType<QuestTracker>();
        tracker?.ContarInimigoDerrotado();
        if (CompareTag("Boss1"))
            tracker?.BossFinalDerrotado();

        // Se não for inimigo com respawn gerenciado, pode desativar localmente
        gameObject.SetActive(false);
    }

    public void Heal(float amount)
    {
        if (!IsServer || CurrentHealth.Value <= 0f)
            return;

        CurrentHealth.Value = Mathf.Min(CurrentHealth.Value + amount, maxHealth);
    }

    public void HealFull()
    {
        if (!IsServer)
            return;

        CurrentHealth.Value = maxHealth;
    }

    // -------------------------------------------------------
    // RPCs auxiliares
    // -------------------------------------------------------
    [ClientRpc]
    private void ClearTargetIfMatchesClientRpc(NetworkObjectReference deadRef)
    {
        if (!deadRef.TryGet(out var deadNo)) return;
        var deadId = deadNo.NetworkObjectId;

        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            var t = pc.CurrentTarget;
            if (t == null) continue;

            var tNo = t.GetComponent<NetworkObject>();
            if (tNo != null && tNo.NetworkObjectId == deadId)
                pc.ClearTarget();
        }
    }
}
