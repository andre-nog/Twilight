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

        // Não instanciamos projétil aqui — a criação fica por conta do Animation Event (OnAttackFrame),
        // garantindo total sincronismo entre animação e lógica de attack.
        // Ainda podemos girar durante o cast:
        float timer = 0f;
        float delay = Mathf.Max(0.05f, mageAttackData.CastDelay);
        while (timer < delay)
        {
            timer += Time.deltaTime;

            Vector3 dir = aimPoint - caster.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                caster.transform.rotation = Quaternion.Slerp(
                    caster.transform.rotation,
                    targetRot,
                    Time.deltaTime * 8f
                );
            }

            yield return null;
        }

        setCasting(false);
        yield return new WaitForSeconds(0.05f);
        setBusy(false);

        ResumeAgent(agent);
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