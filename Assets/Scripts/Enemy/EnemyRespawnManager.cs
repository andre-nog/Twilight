using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Unity.Netcode.Components;

public class EnemyRespawnManager : NetworkBehaviour
{
    public static EnemyRespawnManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ScheduleRespawn(GameObject enemy, Vector3 position, float delay)
    {
        if (!IsServer || enemy == null) return;
        StartCoroutine(RespawnRoutine(enemy, position, delay));
    }

    private IEnumerator RespawnRoutine(GameObject enemy, Vector3 position, float delay)
    {
        var no = enemy.GetComponent<NetworkObject>();
        var enemyRef = no ? new NetworkObjectReference(no) : default;

        // 1) esconder todos
        SetActiveClientRpc(enemyRef, false);
        enemy.SetActive(false);

        // 2) cooldown
        yield return new WaitForSeconds(delay);

        // 3) mover no servidor ANTES de mostrar (Warp evita resíduos do NavMeshAgent)
        if (enemy.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent) && agent.isOnNavMesh)
            agent.Warp(position);
        else
            enemy.transform.position = position;

        // 3.1) snap em TODOS os clients ainda inativos, com o componente de sync desligado
        SnapPositionClientRpc(enemyRef, position);

        // 4) reset lógico
        if (enemy.TryGetComponent<Actor>(out var actor))
            actor.HealFull();

        if (enemy.TryGetComponent<EnemyDetectionAndAttack>(out var ai))
        {
            var newTarget = FindBestServerPlayerNear(position);
            ai.ReassignPlayerTarget(newTarget);
            ai.ResetEnemyStateAfterRespawn();
        }

        // 5) garantia do agent
        if (enemy.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent2) && agent2.isOnNavMesh)
        {
            agent2.ResetPath();
            agent2.velocity = Vector3.zero;
            agent2.isStopped = false;
        }

        // 6) mostrar de volta (componentes de sync serão religados no RPC)
        enemy.SetActive(true);
        SetActiveClientRpc(enemyRef, true);
    }

    [ClientRpc]
    private void SnapPositionClientRpc(NetworkObjectReference objRef, Vector3 pos)
    {
        if (!objRef.TryGet(out var no)) return;
        var go = no.gameObject;
        if (!go) return;

        // desligar QUALQUER sync de transform enquanto faz o snap
        var cnt = go.GetComponent<ClientNetworkTransform>();
        var nt = go.GetComponent<NetworkTransform>();
        if (cnt) cnt.enabled = false;
        if (nt) nt.enabled = false;

        // snap local (GO ainda está inativo)
        var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent && agent.isOnNavMesh) agent.Warp(pos); else go.transform.position = pos;
    }


    [ClientRpc]
    private void SetActiveClientRpc(NetworkObjectReference objRef, bool active)
    {
        if (!objRef.TryGet(out var no)) return;
        var go = no.gameObject;
        if (!go) return;

        go.SetActive(active);

        // ao reativar, religar o componente de sync (vai partir do pos já snappado)
        if (active)
        {
            var cnt = go.GetComponent<ClientNetworkTransform>();
            var nt = go.GetComponent<NetworkTransform>(); // Server authority (NPC)
            if (cnt) cnt.enabled = true;
            if (nt) nt.enabled = true;
        }
    }


    private Transform FindBestServerPlayerNear(Vector3 origin)
    {
        PlayerActor best = null;
        float bestDistSq = float.MaxValue;

        foreach (var p in PlayerRegistry.AllPlayers)
        {
            if (p == null || !p.gameObject.activeInHierarchy) continue;
            if (p.CurrentHealth.Value <= 0f) continue;

            float dSq = (p.transform.position - origin).sqrMagnitude;
            if (dSq < bestDistSq) { bestDistSq = dSq; best = p; }
        }
        return best ? best.transform : null;
    }
}
