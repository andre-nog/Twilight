using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class MageAttackSpellExecutor
{
    /// <summary>
    /// Executa o auto-attack homing sem instanciar o projétil imediatamente.
    /// A instância ficará a cargo do AnimationEvent (OnAttackFrame).
    /// </summary>
    public static IEnumerator CastAutoAttack(
        GameObject caster,
        Transform castPoint,
        GameObject projectilePrefab,
        float damage,
        float projectileSpeed,
        float range,
        Animator animator,
        NavMeshAgent agent,
        Action<bool> setBusy,
        Action<bool> setCasting,
        Transform homingTarget
    )
    {
        // marca busy e casting
        setBusy(true);
        setCasting(true);

        // pausa o agente para animação de ataque
        if (agent != null)
        {
            agent.isStopped  = true;
            agent.ResetPath();
            agent.velocity   = Vector3.zero;
        }

        // apenas aguarda o AnimationEvent disparar a bolinha
        yield return null;

        // libera casting e busy
        setCasting(false);
        setBusy(false);

        // retoma movimento
        if (agent != null)
            agent.isStopped = false;
    }
}