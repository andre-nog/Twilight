using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class TeleportSpellExecutor
{
    public static IEnumerator Cast(
        GameObject caster,
        TeleportSpell teleportData,
        NavMeshAgent agent,
        System.Action<bool> setBusy,
        Vector3 targetPoint
    )
    {
        setBusy(true);

        PauseAgent(agent);

        float delay = Mathf.Max(0.1f, teleportData.CastDelay);
        yield return new WaitForSeconds(delay);

        PerformTeleport(caster, agent, teleportData.Range, targetPoint);

        yield return new WaitForSeconds(0.05f);
        setBusy(false);
        ResumeAgent(agent);
    }

    private static void PerformTeleport(GameObject caster, NavMeshAgent agent, float range, Vector3 targetPoint)
    {
        Vector3 dir = targetPoint - caster.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = caster.transform.forward;

        Vector3 dest = caster.transform.position + dir.normalized * (range > 0 ? range : 6f);
        dest.y = caster.transform.position.y;

        agent.Warp(dest);
    }

    private static void PauseAgent(NavMeshAgent agent)
    {
        if (agent == null) return;
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private static void ResumeAgent(NavMeshAgent agent)
    {
        if (agent == null) return;
        agent.isStopped = false;
    }
}