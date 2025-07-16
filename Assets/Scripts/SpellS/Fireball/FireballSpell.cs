using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class FireballSpell
{
    public static IEnumerator Cast(
        GameObject caster,
        Transform castPoint,
        GameObject fireballPrefab,
        ProjectileSpell fireballData,
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

        animator.ResetTrigger("AttackTrigger");
        animator.SetTrigger("AttackTrigger");

        float timer = 0f;
        float delay = Mathf.Max(0.15f, fireballData.CastDelay);
        while (timer < delay)
        {
            timer += Time.deltaTime;

            Vector3 dir = (aimPoint - caster.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                caster.transform.rotation = Quaternion.Slerp(caster.transform.rotation, targetRot, Time.deltaTime * 8f);
            }

            yield return null;
        }

        LaunchProjectile(caster, castPoint, fireballPrefab, fireballData, aimPoint);
        setCasting(false);
        yield return new WaitForSeconds(0.05f);
        setBusy(false);
        ResumeAgent(agent);
    }

    private static void LaunchProjectile(GameObject caster, Transform castPoint, GameObject prefab, ProjectileSpell data, Vector3 aimPoint)
    {
        Vector3 spawn = castPoint != null
            ? castPoint.position
            : caster.transform.position + caster.transform.forward * 0.5f + Vector3.up * 0.5f;

        Vector3 direction = (aimPoint - spawn).normalized;
        direction.y = 0f;

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject fireball = GameObject.Instantiate(prefab, spawn, rotation);

        if (fireball.TryGetComponent<Fireball_Script>(out var spell))
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