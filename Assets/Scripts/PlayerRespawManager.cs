using UnityEngine;
using System.Collections;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RespawnPlayer(PlayerActor player, Vector3 spawnPosition, float delay = 2f)
    {
        StartCoroutine(RespawnRoutine(player, spawnPosition, delay));
    }

    private IEnumerator RespawnRoutine(PlayerActor player, Vector3 spawnPosition, float delay)
    {
        // Desativa o player e reseta inimigos imediatamente
        player.gameObject.SetActive(false);
        ResetAllEnemies();

        yield return new WaitForSeconds(delay);

        // Reposiciona e cura o player
        player.transform.position = spawnPosition;
        player.Stats.CurrentHealth = player.Stats.MaxHealth;

        // Reativa o player
        player.gameObject.SetActive(true);

        // Garante que o agente renasça parado
        var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

        // Limpa o alvo do player para evitar perseguição antiga
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.ClearTarget();
        }

        // Atualiza barra de vida
        var healthbar = player.GetComponentInChildren<Healthbar>();
        if (healthbar != null)
            healthbar.UpdateHealth(player.Stats.CurrentHealth, player.Stats.MaxHealth);

        // ⚠️ Reseta estado de casting para evitar travamento
        var magicSystem = player.GetComponent<PlayerMagicSystem>();
        if (magicSystem != null)
        {
            magicSystem.ResetCastingState();
        }

        // Atualiza o alvo nos inimigos
        ReassignEnemiesToPlayer(player.transform);
    }

    private void ResetAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyDetectionAndAttack>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.ForceResetToOrigin();
        }
    }

    private void ReassignEnemiesToPlayer(Transform playerTransform)
    {
        var enemies = FindObjectsByType<EnemyDetectionAndAttack>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.ReassignPlayerTarget(playerTransform);
        }
    }
}