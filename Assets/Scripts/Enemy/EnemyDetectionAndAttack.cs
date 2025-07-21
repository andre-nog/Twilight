// EnemyDetectionAndAttack.cs
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyState))]
[AddComponentMenu("Enemy/Detection and Attack")]
public class EnemyDetectionAndAttack : MonoBehaviour, IAggroReceiver
{

    [Header("Detection & Attack")]
    [SerializeField] private float detectionRange = 1f;
    [SerializeField] private float aggroLoseRange = 15f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackSpeed = 1.5f;
    [SerializeField] private float attackDelay = 0.3f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private ParticleSystem hitEffect;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform target;
    private EnemyState enemyState;
    private Vector3 initialPosition;

    private bool hasAggro = false;
    private bool isReturning = false;
    private string currentAnimation;
    private float nextAttackTime = 0f;

    // controla aplicação única de dano por ataque
    private bool didDamage;

    public bool HasAggro => hasAggro;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyState = GetComponent<EnemyState>()
                   ?? gameObject.AddComponent<EnemyState>();
    }

    public void Init(Transform player, Vector3 origin)
    {
        target = player;
        initialPosition = origin;
        agent.stoppingDistance = attackDistance;
    }

    private void Update()
    {
        if (enemyState.IsBusy) return;

        // só olha pro player quando tiver aggro
        if (hasAggro)
            FaceTarget();

        HandleAggroAndChase();
        HandleMovementStop();
        TryMeleeAttackIfTargetInRange();
        SetAnimations();
    }

    private void HandleAggroAndChase()
    {
        if (target == null) return;

        float distToPlayer = Vector3.Distance(transform.position, target.position);
        float distFromOrigin = Vector3.Distance(transform.position, initialPosition);

        // 1) RE-AGGRO: ignora enquanto estiver retornando ao ponto inicial
        if (!hasAggro && !isReturning &&
            distToPlayer <= detectionRange &&
            distFromOrigin <= aggroLoseRange)
        {
            hasAggro = true;
        }

        // 2) enquanto tiver aggro, chase ou reseta aggro ao sair do aggroLoseRange
        if (hasAggro)
        {
            if (distFromOrigin > aggroLoseRange)
            {
                hasAggro = false;
                isReturning = true;

                // gira de frente pro ponto inicial antes de voltar
                Vector3 dir = initialPosition - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);

                agent.SetDestination(initialPosition);
                return;
            }

            if (distToPlayer > attackDistance)
            {
                agent.SetDestination(target.position);
            }
        }
        // 3) final do retorno: identifica chegada pela NavMesh e limpa isReturning
        else if (isReturning && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isReturning = false;
            agent.ResetPath();
        }
    }

    private void HandleMovementStop()
    {
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= attackDistance && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        else if (hasAggro && dist > attackDistance)
        {
            // retoma o movimento, se estava parado
            if (agent.isStopped)
                agent.isStopped = false;
            // atualiza destino para perseguir o player enquanto ainda tiver aggro
            agent.SetDestination(target.position);
        }
    }

    private void TryMeleeAttackIfTargetInRange()
    {
        // só ataca se não estiver em cooldown, não estiver ocupado e estiver parado
        if (target == null || !hasAggro || enemyState.IsBusy || Time.time < nextAttackTime)
            return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackDistance) //&& agent.velocity.magnitude < 0.1f)
        {
            // tempo entre ataques = 1 / ataques por segundo
            nextAttackTime = Time.time + (1f / attackSpeed);
            StartCoroutine(MeleeAttackRoutine());
        }
    }

    private IEnumerator MeleeAttackRoutine()
    {
        // reset do flag para garantir um único dano por ataque
        didDamage = false;

        // trava o inimigo durante a animação
        enemyState.SetBusy(true);
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        // vira para o alvo e dispara o trigger
        FaceTarget();
        animator.SetFloat("AttackSpeed", attackSpeed);
        animator.ResetTrigger("AttackTrigger");
        animator.SetTrigger("AttackTrigger");

        // espera o momento do hit (dano só no OnAttackFrame)
        yield return new WaitForSeconds(attackDelay);

        // libera o estado busy logo após o ataque, permitindo chase imediato
        enemyState.SetBusy(false);
    }

    void SetAnimations()
    {
        if (enemyState.IsBusy)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }

    private void FaceTarget()
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
    }

    public void TakeAggro()
    {
        if (!isReturning && !hasAggro)
        {
            hasAggro = true;

            // Agro em cadeia: procura outros inimigos próximos
            Collider[] hits = Physics.OverlapSphere(transform.position, 5f, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue; // ignora a si mesmo

                var other = hit.GetComponent<EnemyDetectionAndAttack>();
                if (other != null)
                {
                    other.ForceAggroFromNearby(transform.position);
                }
            }
        }
    }

    // chamado pelo AnimationEvent “OnAttackFrame”
    public void OnAttackFrame()
    {
        if (didDamage || target == null)
            return;

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

    public void ForceAggroFromNearby(Vector3 sourcePosition)
    {
        if (!hasAggro && !isReturning)
        {
            hasAggro = true;

            // opcional: vira de frente para o player imediatamente
            if (target != null)
            {
                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            // opcional: log para depuração
            // Debug.Log($"{gameObject.name} aggrado por proximidade de outro inimigo em {sourcePosition}");
        }
    }

}

public interface IAggroReceiver
{
    void TakeAggro();
}

public class EnemyState : MonoBehaviour
{
    public bool IsBusy { get; private set; } = false;
    public void SetBusy(bool busy) => IsBusy = busy;
}
