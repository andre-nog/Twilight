using UnityEngine;
using Unity.Netcode;

public class PlayerActor : NetworkBehaviour
{
    [Header("Referências")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private GameObject healthbarPrefab;
    [SerializeField] private GameObject playerCameraPrefab;
    [SerializeField] private Vector3 healthbarOffset = new Vector3(0, 2.5f, 0);

    private Healthbar healthbar;
    private Vector3 initialPosition;

    // 🔁 Campos sincronizados
    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<float> CurrentMana = new NetworkVariable<float>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    public float MaxHealth => stats.MaxHealth;
    public float MaxMana => stats.MaxMana;
    public PlayerStats Stats => stats;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = stats.MaxHealth;
            CurrentMana.Value = stats.MaxMana;
            initialPosition = transform.position;

            // ✅ Registra no sistema global para IA
            PlayerRegistry.Register(this);
        }

        // 🎥 Câmera local
        if (IsOwner && playerCameraPrefab != null)
        {
            GameObject camInstance = Instantiate(playerCameraPrefab);

            if (camInstance.TryGetComponent(out CameraController cc))
                cc.target = transform;

            Camera camComp = camInstance.GetComponent<Camera>();
            if (camComp != null)
                camComp.tag = "MainCamera";
        }

        // ❤️ Healthbar
        if (healthbarPrefab != null)
        {
            var barInstance = Instantiate(healthbarPrefab, transform);
            barInstance.transform.localPosition = healthbarOffset;
            healthbar = barInstance.GetComponentInChildren<Healthbar>();
        }

        CurrentHealth.OnValueChanged += OnHealthChanged;

        if (healthbar != null)
            healthbar.UpdateHealth(CurrentHealth.Value, MaxHealth);

        Debug.Log($"[PlayerActor] OnNetworkSpawn — Vida = {CurrentHealth.Value}/{MaxHealth}");
    }

    protected new void OnDestroy()
    {
        if (IsSpawned)
            CurrentHealth.OnValueChanged -= OnHealthChanged;

        if (IsServer)
            PlayerRegistry.Unregister(this);
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        healthbar?.UpdateHealth(newValue, MaxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer || CurrentHealth.Value <= 0f)
            return;

        CurrentHealth.Value = Mathf.Max(CurrentHealth.Value - amount, 0f);
        Debug.Log($"[Player] {gameObject.name} recebeu {amount} de dano. HP restante: {CurrentHealth.Value}");

        if (CurrentHealth.Value <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        if (!IsServer || CurrentHealth.Value <= 0f)
            return;

        CurrentHealth.Value = Mathf.Min(CurrentHealth.Value + amount, MaxHealth);
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (!IsServer)
            return;

        stats.MaxHealth += amount;
        CurrentHealth.Value += amount;
        CurrentHealth.Value = Mathf.Min(CurrentHealth.Value, stats.MaxHealth);
    }

    private void Die()
    {
        Debug.Log($"[Player] {gameObject.name} morreu. Respawnando...");

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.RespawnPlayer(this, initialPosition);
        }
        else
        {
            Debug.LogError("[PlayerActor] PlayerRespawnManager não encontrado na cena!");
        }
    }
}
