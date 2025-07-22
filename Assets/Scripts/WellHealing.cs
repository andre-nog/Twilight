using UnityEngine;

public class WellHealingZone : MonoBehaviour
{
    [SerializeField] private float radius = 3.5f;
    [SerializeField] private float healPercentPerSecond = 0.10f;
    private Transform player;

    private void Start()
    {
        // Encontra o player por tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("[WellHealingZone] Player encontrado.");
        }
        else
        {
            Debug.LogWarning("[WellHealingZone] Player não encontrado!");
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= radius)
        {
            PlayerActor actor = player.GetComponent<PlayerActor>();
            if (actor != null && actor.Stats.CurrentHealth < actor.Stats.MaxHealth)
            {
                float healAmount = actor.Stats.MaxHealth * healPercentPerSecond * Time.deltaTime;
                actor.Stats.CurrentHealth += healAmount;
                actor.Stats.CurrentHealth = Mathf.Min(actor.Stats.CurrentHealth, actor.Stats.MaxHealth);

                // Atualiza barra de vida se tiver
                var healthbar = player.GetComponentInChildren<Healthbar>();
                if (healthbar != null)
                    healthbar.UpdateHealth(actor.Stats.CurrentHealth, actor.Stats.MaxHealth);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}