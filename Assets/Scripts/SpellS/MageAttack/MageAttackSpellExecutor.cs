using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class MageAttackSpellExecutor
{
    public static IEnumerator Cast(
        GameObject caster,
        Transform castPoint,
        GameObject mageAttackPrefab,
        MageAttackSpell mageAttackData,
        Animator animator,
        NavMeshAgent agent,
        System.Action<bool> setBusy,
        System.Action<bool> setCasting,
        Vector3 aimPoint
    )
    {
        setBusy(true);
        setCasting(true);

        PauseAgent(agent);

        animator?.ResetTrigger("AttackTrigger");
        animator?.SetTrigger("AttackTrigger");

        yield return new WaitForSeconds(mageAttackData.CastDelay);

        LaunchProjectile(caster, castPoint, mageAttackPrefab, mageAttackData, aimPoint);

        setCasting(false);
        yield return new WaitForSeconds(0.05f);
        setBusy(false);
        ResumeAgent(agent);
    }

    private static void LaunchProjectile(GameObject caster, Transform castPoint, GameObject prefab, MageAttackSpell data, Vector3 aimPoint)
    {
        Vector3 spawn = castPoint != null
            ? castPoint.position
            : caster.transform.position + caster.transform.forward * 0.5f + Vector3.up * 0.5f;

        Vector3 direction = (aimPoint - spawn).normalized;
        direction.y = 0f;

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject projectile = GameObject.Instantiate(prefab, spawn, rotation);

        if (projectile.TryGetComponent<MageAttack_Script>(out var spell))
        {
            spell.Init(data, caster);
        }
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