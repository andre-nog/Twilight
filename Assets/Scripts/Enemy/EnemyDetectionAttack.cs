using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(EnemyState))]
[AddComponentMenu("Enemy/Detection and Attack")]
public class EnemyDetectionAndAttack : NetworkBehaviour, IAggroReceiver
{
    [Header("Detection & Attack")]
    [SerializeField] float detectionRange = 4f;
    [SerializeField] float aggroLoseRange = 15f;
    [SerializeField] float attackDistance = 1.5f;
    [SerializeField] float attackSpeed = 1.5f;
    [SerializeField] float attackDelay = 0.3f;
    [SerializeField] int attackDamage = 1;
    [SerializeField] ParticleSystem hitEffect;

    NavMeshAgent agent;
    Animator animator;
    EnemyState enemyState;
    Actor actor;

    Transform target;
    Vector3 initialPosition;

    bool hasAggro, isReturning, playerIsDead, didDamage;
    float nextAttackTime;
    const float healRate = 0.1f;

    public bool HasAggro => hasAggro;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyState = GetComponent<EnemyState>();
        actor = GetComponent<Actor>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            InitServerSide(); // mantenha sua inicialização atual do servidor
            PlayerRegistry.OnPlayerRegistered += HandleNewPlayerRegistered; // ✅ ouve spawns de players
        }
    }
    private void HandleNewPlayerRegistered(PlayerActor p)
    {
        if (!IsServer || p == null || !p.gameObject.activeInHierarchy) return;
        if (isReturning) return;

        float newDistSq = (p.transform.position - transform.position).sqrMagnitude;
        float curDistSq = target ? (target.position - transform.position).sqrMagnitude : float.MaxValue;

        // Caso clássico: ainda sem aggro, apenas prefere o mais próximo
        if (!hasAggro)
        {
            if (target == null || newDistSq + 0.01f < curDistSq)
            {
                target = p.transform;
                playerIsDead = false;
                // Debug.Log($"[Enemy] Retarget (sem aggro) para {p.name}");
            }
            return;
        }

        // Já está em aggro: permitir UMA troca inicial se ainda não causou dano,
        // o novo player está dentro do detectionRange e é mais próximo.
        if (!didDamage)
        {
            float distFromOrigin = Vector3.Distance(transform.position, initialPosition);
            bool inDetection = newDistSq <= detectionRange * detectionRange;
            bool withinAggroArea = distFromOrigin <= aggroLoseRange;

            if (inDetection && withinAggroArea && (newDistSq + 0.01f < curDistSq))
            {
                target = p.transform;
                playerIsDead = false;
                Debug.Log("[Enemy] Retarget inicial (sem dano) para player recém-registrado mais próximo.");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            PlayerRegistry.OnPlayerRegistered -= HandleNewPlayerRegistered;

        base.OnNetworkDespawn();
    }

    private bool hasInitialized = false;

    private void InitServerSide()
    {
        if (hasInitialized) return; // ✅ Garante que só inicializa uma vez
        hasInitialized = true;

        Debug.Log($"[Enemy] InitServerSide — {gameObject.name}");

        initialPosition = transform.position;
        target = FindServerTarget();
        StartCoroutine(RefreshTargetRoutine());

        agent.stoppingDistance = attackDistance;
        agent.updateRotation = false;
    }
    void Update()
    {
        if (!IsServer || enemyState.IsBusy) return;

        if (PlayerIsDead())
        {
            if (!playerIsDead && hasAggro)
            {
                Debug.Log("[Enemy] Jogador morreu, iniciando HandlePlayerDeath");
                HandlePlayerDeath();
            }
        }

        TickTargeting();
        TickChase();
        TickAttack();
        TickHeal();
        TickAnimation();
    }

    bool PlayerIsDead()
    {
        if (playerIsDead) return true;
        if (target == null || !target.gameObject.activeInHierarchy)
            return playerIsDead = true;

        if (target.TryGetComponent<Actor>(out var a))
            playerIsDead = a.CurrentHealth.Value <= 0f;
        else if (target.TryGetComponent<PlayerActor>(out var p))
            playerIsDead = p.CurrentHealth.Value <= 0f;

        return playerIsDead;
    }

    void HandlePlayerDeath()
    {
        hasAggro = false;
        isReturning = true;
        playerIsDead = true;

        Debug.Log("[Enemy] HandlePlayerDeath: retornando ao ponto inicial");

        Vector3 dir = initialPosition - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        agent.SetDestination(initialPosition);
        target = null;
    }

    void TickTargeting()
    {
        if (hasAggro && target != null)
            FacePosition(target.position);
    }

    void TickChase()
    {
        if (!isReturning && target == null)
        {
            target = FindServerTarget();
            if (target == null) return;
        }
        if (playerIsDead && !isReturning) return;
        if (target == null && !isReturning) return;

        float distToPlayer = target ? Vector3.Distance(transform.position, target.position) : Mathf.Infinity;
        float distFromOrigin = Vector3.Distance(transform.position, initialPosition);

        if (!playerIsDead && !hasAggro && !isReturning &&
            distToPlayer <= detectionRange && distFromOrigin <= aggroLoseRange)
        {
            hasAggro = true;
            Debug.Log($"[Enemy] Ganhou aggro! distToPlayer={distToPlayer}, detectionRange={detectionRange}");
        }

        if (hasAggro)
        {
            if (distFromOrigin > aggroLoseRange)
            {
                hasAggro = false;
                isReturning = true;
                Debug.Log($"[Enemy] Perdeu aggro, retornando. distFromOrigin={distFromOrigin}");
                agent.SetDestination(initialPosition);
                return;
            }

            if (distToPlayer > attackDistance)
                agent.SetDestination(target.position);
        }
        else if (isReturning)
        {
            if (agent.velocity.sqrMagnitude > 0.05f)
                FacePosition(initialPosition);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isReturning = false;
                agent.ResetPath();
                actor?.HealFull();
                Debug.Log("[Enemy] Chegou à origem, HealFull e reset retorno");
            }
        }
    }

    void TickAttack()
    {
        if (!hasAggro || target == null || enemyState.IsBusy || Time.time < nextAttackTime)
            return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackDistance)
        {
            Debug.Log($"[Enemy] Está em alcance de ataque (dist={dist}), iniciando ataque");
            nextAttackTime = Time.time + 1f / attackSpeed;
            StartCoroutine(MeleeAttackRoutine());
        }
    }

    IEnumerator MeleeAttackRoutine()
    {
        didDamage = false;
        enemyState.SetBusy(true);

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        FacePosition(target.position);
        animator.SetFloat("AttackSpeed", attackSpeed);
        animator.ResetTrigger("AttackTrigger");
        animator.SetTrigger("AttackTrigger");

        yield return new WaitForSeconds(attackDelay);
        enemyState.SetBusy(false);
    }

    public void OnAttackFrame()
    {
        if (!IsServer || didDamage || target == null || !target.gameObject.activeInHierarchy) return;

        bool hit = false;
        if (target.TryGetComponent<Actor>(out var a) && a.CurrentHealth.Value > 0f)
        {
            a.TakeDamage(attackDamage);
            hit = true;
        }
        else if (target.TryGetComponent<PlayerActor>(out var p) && p.CurrentHealth.Value > 0f)
        {
            p.TakeDamage(attackDamage);
            hit = true;
        }

        if (hit)
        {
            didDamage = true;
            Debug.Log($"[Enemy] OnAttackFrame: acertou o alvo e aplicou {attackDamage} de dano");
            if (hitEffect != null)
                Instantiate(hitEffect, target.position + Vector3.up, Quaternion.identity);
        }
    }

    void TickHeal()
    {
        if (isReturning && actor && actor.CurrentHealth.Value > 0f)
        {
            actor.Heal(actor.MaxHealth * healRate * Time.deltaTime);
        }
    }

    void TickAnimation()
    {
        animator.SetFloat("Speed", enemyState.IsBusy ? 0f : agent.velocity.magnitude);
    }

    void FacePosition(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
    }

    public void TakeAggro(Transform attacker)
    {
        if (isReturning) return;               // não pega aggro enquanto retorna
        hasAggro = true;                       // garante estado de aggro

        if (attacker != null)
        {
            target = attacker;                 // ✅ sempre prioriza quem bateu
            playerIsDead = false;              // estamos mirando alguém vivo
            Debug.Log($"[Enemy] TakeAggro: novo alvo = {attacker.name}");
        }
        else if (target == null)
        {
            // fallback: se não veio atacante explícito, escolhe por proximidade
            target = FindServerTarget();
            Debug.Log("[Enemy] TakeAggro: sem attacker, selecionando alvo por proximidade");
        }
    }

    public void ForceAggroFromNearby(Vector3 _) => TakeAggro();

    public void ForceResetToOrigin()
    {
        hasAggro = false;
        isReturning = true;
        agent.isStopped = false;
        agent.SetDestination(initialPosition);
        Debug.Log("[Enemy] ForceResetToOrigin() chamado");
    }

    public void ReassignPlayerTarget(Transform newPlayer)
    {
        target = newPlayer;
        playerIsDead = false;
        Debug.Log($"[Enemy] ReassignPlayerTarget: novo alvo = {newPlayer?.name}");
    }

    public void ResetEnemyStateAfterRespawn()
    {
        initialPosition = transform.position;
        enemyState.SetBusy(false);
        hasAggro = false;
        isReturning = false;
        playerIsDead = false;
        didDamage = false;
        nextAttackTime = 0f;
        Debug.Log("[Enemy] ResetEnemyStateAfterRespawn() completo");
    }

    IEnumerator RefreshTargetRoutine()
    {
        var wait = new WaitForSeconds(0.25f);
        while (true)
        {
            // só retenta procurar target se NÃO estamos retornando
            // e se o target atual é nulo/inválido (não sobrescreve quem acabou de bater)
            if (!isReturning && (target == null || !target.gameObject.activeInHierarchy))
            {
                var newTarget = FindServerTarget();
                if (newTarget != target)
                {
                    target = newTarget;
                    if (target != null)
                        playerIsDead = false;
                    Debug.Log("[Enemy] RefreshTargetRoutine: target reatribuído");
                }
            }
            yield return wait;
        }
    }
    private Transform FindServerTarget()
    {
        PlayerActor best = null;
        float bestDistSq = float.MaxValue;
        Vector3 pos = transform.position;

        foreach (var player in PlayerRegistry.AllPlayers)
        {
            if (player == null || !player.gameObject.activeInHierarchy) continue;
            if (player.CurrentHealth.Value <= 0f) continue;

            float dSq = (player.transform.position - pos).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                best = player;
            }
        }

        return best ? best.transform : null;
    }

    public void TakeAggro()
    {
        TakeAggro(null); // redireciona para a versão que já existe
    }

}
