// EnemyDetectionAndAttack.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[AddComponentMenu("Enemy/Detection and Attack")]
public class EnemyDetectionAndAttack : MonoBehaviour, IAggroReceiver
{
    const string IDLE   = "Idle";
    const string WALK   = "Walk";
    const string ATTACK = "Attack";

    [Header("Detection & Attack")]
    [SerializeField] private float detectionRange  = 1f;
    [SerializeField] private float aggroLoseRange  = 15f;
    [SerializeField] private float attackDistance  = 1.5f;
    [SerializeField] private float attackSpeed     = 1.5f;  // tempo entre ataques
    [SerializeField] private float attackDelay     = 0.3f;  // delay antes do dano
    [SerializeField] private int   attackDamage    = 1;
    [SerializeField] private ParticleSystem hitEffect;

    private NavMeshAgent agent;
    private Animator     animator;
    private Transform    target;
    private EnemyState   enemyState;
    private Vector3      initialPosition;

    private bool hasAggro    = false;
    public bool HasAggro => hasAggro;  // <— adiciona esta linha
    private bool isReturning = false;
    private string currentAnimation;

    public void Init(Transform player, Vector3 origin)
    {
        agent           = GetComponent<NavMeshAgent>();
        animator        = GetComponent<Animator>();
        enemyState      = GetComponent<EnemyState>();
        target          = player;
        initialPosition = origin;
    }

    public void Tick()
    {
        HandleTargeting();
    }

    private void HandleTargeting()
    {
        if (target == null)
        {
            SetAnimationByVelocity();
            return;
        }

        float distToPlayer   = Vector3.Distance(transform.position, target.position);
        float distFromOrigin = Vector3.Distance(transform.position, initialPosition);

        // ganha aggro
        if (!hasAggro && !isReturning &&
            distToPlayer <= detectionRange &&
            distFromOrigin <= aggroLoseRange)
        {
            hasAggro = true;
        }

        // se ocupado (skill ou ataque em andamento), mantém animação e retorna
        if (enemyState != null && enemyState.IsBusy)
        {
            SetAnimationByVelocity();
            return;
        }

        if (hasAggro)
        {
            if (distFromOrigin > aggroLoseRange)
            {
                hasAggro    = false;
                isReturning = true;
                agent.isStopped = false;
                agent.SetDestination(initialPosition);
            }
            else if (distToPlayer <= attackDistance)
            {
                StartCoroutine(MeleeAttackRoutine());
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
        else if (isReturning)
        {
            if (distFromOrigin <= 0.5f)
                isReturning = false;
        }

        FaceTarget();
        SetAnimationByVelocity();
    }

    private IEnumerator MeleeAttackRoutine()
    {
        // 1) bloqueia movimento/ações
        enemyState?.SetBusy(true);
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        // 2) animação de ataque + face target
        FaceTarget();
        PlayAnimation(ATTACK);

        // 3) espera o delay antes do dano
        yield return new WaitForSeconds(attackDelay);

        // 4) aplica dano (Actor ou PlayerActor)
        if (target != null)
        {
            bool didDamage = false;
            if (target.TryGetComponent<Actor>(out var actor) && actor.CurrentHealth > 0)
            {
                actor.TakeDamage(attackDamage);
                didDamage = true;
            }
            else if (target.TryGetComponent<PlayerActor>(out var pa) && pa.CurrentHealth > 0)
            {
                pa.TakeDamage(attackDamage);
                didDamage = true;
            }

            if (didDamage && hitEffect != null)
                Instantiate(hitEffect, target.position + Vector3.up, Quaternion.identity);
        }

        // 5) espera o cooldown até liberar
        yield return new WaitForSeconds(attackSpeed);

        agent.isStopped = false;
        enemyState?.SetBusy(false);
    }

    private void FaceTarget()
    {
        if (target == null || !hasAggro) return;
        Vector3 dir = (target.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion look = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
    }

    private void SetAnimationByVelocity()
    {
        if (enemyState != null && enemyState.IsBusy) return;
        if (agent.velocity.magnitude > 0.1f)
            PlayAnimation(WALK);
        else
            PlayAnimation(IDLE);
    }

    private void PlayAnimation(string anim)
    {
        if (currentAnimation == anim) return;
        currentAnimation = anim;
        animator.Play(anim);
    }

    public void TakeAggro()
    {
        if (isReturning) return;
        if (!hasAggro)
            hasAggro = true;
    }
}