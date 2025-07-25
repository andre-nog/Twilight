using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyState))]
[AddComponentMenu("Enemy/Detection and Attack")]
public class EnemyDetectionAndAttack : MonoBehaviour, IAggroReceiver
{
    /* ---------- Config ----------- */
    [Header("Detection & Attack")]
    [SerializeField] private float detectionRange   = 4f;
    [SerializeField] private float aggroLoseRange   = 15f;
    [SerializeField] private float attackDistance   = 1.5f;
    [SerializeField] private float attackSpeed      = 1.5f;
    [SerializeField] private float attackDelay      = 0.3f;
    [SerializeField] private int   attackDamage     = 1;
    [SerializeField] private ParticleSystem hitEffect;

    /* ---------- Runtime ----------- */
    private NavMeshAgent agent;
    private Animator animator;
    private Transform target;
    private EnemyState enemyState;
    private Actor actor;
    private Vector3 initialPosition;

    private bool hasAggro      = false;
    private bool isReturning   = false;
    private bool playerIsDead  = false;   // ← NOVO
    private bool didDamage     = false;
    private float nextAttackTime = 0f;
    private float healRate       = 0.1f;

    public bool HasAggro => hasAggro;

    /* ---------- Setup ----------- */
    private void Awake()
    {
        agent      = GetComponent<NavMeshAgent>();
        animator   = GetComponent<Animator>();
        enemyState = GetComponent<EnemyState>() ?? gameObject.AddComponent<EnemyState>();
        actor      = GetComponent<Actor>();
    }

    private void Start()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            Init(player, transform.position);
        else
            Debug.LogWarning($"[Enemy] {gameObject.name} não encontrou Player com tag.");
    }

    public void Init(Transform player, Vector3 origin)
    {
        target           = player;
        initialPosition  = origin;
        playerIsDead     = false;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = attackDistance;
        agent.updateRotation   = false; // rotação manual
    }

    /* ---------- Loop ----------- */
    private void Update()
    {
        if (enemyState.IsBusy) return;

        if (PlayerIsDead())
        {
            if (!playerIsDead && hasAggro)        // só entra 1x
                HandlePlayerDeath();

            // segue update normal para retorno / animação
        }

        TickTargeting();
        TickChase();
        TickAttack();
        TickHeal();
        TickAnimation();
    }

    /* ---------- Helpers ----------- */
    private bool PlayerIsDead()
    {
        if (playerIsDead) return true;

        if (target == null || !target.gameObject.activeInHierarchy)
            return playerIsDead = true;

        if (target.TryGetComponent<Actor>(out var a))      playerIsDead = a.CurrentHealth <= 0f;
        else if (target.TryGetComponent<PlayerActor>(out var p)) playerIsDead = p.CurrentHealth <= 0f;

        return playerIsDead;
    }

    private void HandlePlayerDeath()
    {
        hasAggro    = false;
        isReturning = true;
        playerIsDead = true;

        // rotação instantânea p/ origem
        Vector3 dir = initialPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        agent.SetDestination(initialPosition);
        target = null;                      // evita re-aggro automático
    }

    private void TickTargeting()
    {
        if (hasAggro && target != null)
            FacePosition(target.position);
    }

    private void TickChase()
    {
        /* early outs */
        if (playerIsDead && !isReturning) return;
        if (target == null && !isReturning) return;

        float distToPlayer = target ? Vector3.Distance(transform.position, target.position) : Mathf.Infinity;
        float distFromOrigin = Vector3.Distance(transform.position, initialPosition);

        /* ganho de aggro */
        if (!playerIsDead && !hasAggro && !isReturning &&
            distToPlayer <= detectionRange && distFromOrigin <= aggroLoseRange)
        {
            hasAggro = true;
        }

        /* logic when aggro */
        if (hasAggro)
        {
            if (distFromOrigin > aggroLoseRange)
            {
                hasAggro = false;
                isReturning = true;

                transform.rotation = Quaternion.LookRotation(initialPosition - transform.position);
                agent.SetDestination(initialPosition);
                return;
            }

            if (distToPlayer > attackDistance)
                agent.SetDestination(target.position);
        }
        /* logic when returning */
        else if (isReturning)
        {
            // Mantém orientação para a base durante todo o retorno
            if (agent.velocity.sqrMagnitude > 0.05f)
                FacePosition(initialPosition);

            // Ao chegar no ponto de origem
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isReturning = false;
                agent.ResetPath();
                actor?.HealFull();
            }
        }
    }

    private void TickAttack()
    {
        if (!hasAggro || target == null || enemyState.IsBusy || Time.time < nextAttackTime)
            return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackDistance)
        {
            nextAttackTime = Time.time + 1f / attackSpeed;
            StartCoroutine(MeleeAttackRoutine());
        }
    }

    private IEnumerator MeleeAttackRoutine()
    {
        didDamage = false;
        enemyState.SetBusy(true);

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity  = Vector3.zero;

        FacePosition(target.position);
        animator.SetFloat("AttackSpeed", attackSpeed);
        animator.ResetTrigger("AttackTrigger");
        animator.SetTrigger("AttackTrigger");

        yield return new WaitForSeconds(attackDelay);
        enemyState.SetBusy(false);
    }

    /* ---------- Damage Event ---------- */
    public void OnAttackFrame()
    {
        if (didDamage || target == null || !target.gameObject.activeInHierarchy)
            return;

        bool hit = false;

        if (target.TryGetComponent<Actor>(out var a) && a.CurrentHealth > 0)
        { a.TakeDamage(attackDamage); hit = true; }
        else if (target.TryGetComponent<PlayerActor>(out var p) && p.CurrentHealth > 0)
        { p.TakeDamage(attackDamage); hit = true; }

        if (hit)
        {
            didDamage = true;
            if (hitEffect != null)
                Instantiate(hitEffect, target.position + Vector3.up, Quaternion.identity);
        }
    }

    /* ---------- Misc Ticks ---------- */
    private void TickHeal()
    {
        if (isReturning && actor && actor.CurrentHealth > 0f)
        {
            float heal = actor.MaxHealth * healRate * Time.deltaTime;
            actor.Heal(heal);
        }
    }

    private void TickAnimation()
    {
        animator.SetFloat("Speed", enemyState.IsBusy ? 0f : agent.velocity.magnitude);
    }

    /* ---------- Utilities ---------- */
    private void FacePosition(Vector3 pos)
    {
        Vector3 dir = pos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
    }

    /* ---------- Interfaces / helpers ---------- */
    public void TakeAggro()
    {
        if (!playerIsDead && !hasAggro && !isReturning)
            hasAggro = true;
    }

    public void ForceAggroFromNearby(Vector3 _) => TakeAggro();

    public void ForceResetToOrigin()
    {
        hasAggro  = false;
        isReturning = true;
        agent.isStopped = false;
        agent.SetDestination(initialPosition);
    }

    public void ReassignPlayerTarget(Transform newPlayer)
    {
        target = newPlayer;
        playerIsDead = false;
    }

    public void ResetEnemyStateAfterRespawn()
    {
        enemyState.SetBusy(false);
        hasAggro      = false;
        isReturning   = false;
        playerIsDead  = false;
        didDamage     = false;
    }
}

/* ---------- Support ---------- */
public interface IAggroReceiver { void TakeAggro(); }