using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyRespawnManager : MonoBehaviour
{
    public static EnemyRespawnManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ScheduleRespawn(GameObject enemy, Vector3 respawnPosition, float delay)
    {
        StartCoroutine(RespawnRoutine(enemy, respawnPosition, delay));
    }

    private IEnumerator RespawnRoutine(GameObject enemy, Vector3 position, float delay)
    {
        enemy.SetActive(false);
        yield return new WaitForSeconds(delay);

        // Reposiciona antes de ativar
        enemy.transform.position = position;
        enemy.SetActive(true);

        // Aguarda 1 frame para garantir ativação completa do NavMeshAgent
        yield return null;

        // Reativa vida
        if (enemy.TryGetComponent<Actor>(out var actor))
        {
            actor.HealFull();
        }

        // Reseta aggro e movimento
        if (enemy.TryGetComponent<EnemyDetectionAndAttack>(out var ai))
        {
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            ai.Init(player, enemy.transform.position);
            ai.ResetEnemyStateAfterRespawn();
        }


        // Segurança extra: garante que o agente esteja ativo na NavMesh
        if (enemy.TryGetComponent<NavMeshAgent>(out var agent) && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
        }
    }
}