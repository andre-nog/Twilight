using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class PlayerController : NetworkBehaviour
{
    private CustomActions input;
    private NavMeshAgent agent;
    private Animator anim;
    private PlayerMagicSystem magic;

    [Header("Layers / FX")]
    [SerializeField] LayerMask clickableLayers;
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] ParticleSystem clickFx;

    [Header("Attack-Move (A) Cursor")]
    [SerializeField] Texture2D attackCursor;
    [SerializeField] Vector2 cursorHotspot = new(16, 16);

    private Interactable target;
    private bool waitingAttackClick;
    private bool isHoldingRight;
    private float lookSpeed = 8f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        agent = GetComponent<NavMeshAgent>();

        // ⚙️ Config replicando o Inspector + correção de rotação
        agent.speed = 3.5f;
        agent.updateRotation = false;        // ✅ evita conflito com nosso LookAt manual
        agent.angularSpeed = 0f;             // redundante quando updateRotation=false, mas mantido por clareza
        agent.acceleration = 99999f;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.radius = 0.15f;
        agent.height = 2f;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.areaMask = NavMesh.AllAreas;

        anim = GetComponent<Animator>();
        magic = GetComponent<PlayerMagicSystem>();
        input = new CustomActions();
        input.Enable();

        // ✅ subscribe nomeado para permitir unsubscribe correto
        input.Main.Move.performed += OnMovePerformed;

        Debug.Log($"[PlayerController] OnNetworkSpawn — IsOwner={IsOwner}, IsServer={IsServer}");
    }
    private void OnEnable()
    {
        if (!IsOwner) return;

        if (input != null)
        {
            // evita duplicidade de inscrição
            input.Main.Move.performed -= OnMovePerformed;
            input.Enable();
            input.Main.Move.performed += OnMovePerformed;
        }

        // garantir estado limpo ao reativar
        isHoldingRight = false;
        waitingAttackClick = false;
    }


    private void OnDisable()
    {
        if (IsOwner && input != null)
        {
            input.Main.Move.performed -= OnMovePerformed;
            input.Disable();
        }

        // limpar estados “colados” ao desativar
        isHoldingRight = false;
        waitingAttackClick = false;
    }


    private void Update()
    {
        if (!IsOwner) return;

        // 1) garantir que alvo inválido seja limpo
        CheckAndClearInvalidTarget();

        // 2) leitura robusta do estado atual do botão (evita “colar” após respawn/alt-tab)
        isHoldingRight = Mouse.current.rightButton.isPressed;
        if (isHoldingRight) HoldMove();

        // 3) Attack-Move: tecla A arma o clique de ataque
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            waitingAttackClick = true;
            if (attackCursor) Cursor.SetCursor(attackCursor, cursorHotspot, CursorMode.Auto);
        }

        // 4) Se estiver esperando clique de ataque, processa no próximo clique esquerdo
        if (waitingAttackClick && Mouse.current.leftButton.wasPressedThisFrame)
            AttackMove();

        // 5) Rotação / movimento / auto-ataque
        Face();
        StopNearTarget();
        AutoAttack();

        // 6) Animação
        anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    // --------------------------------------------------------------------
    // Input handlers
    // --------------------------------------------------------------------
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        RightClick();
    }

    void AttackMove()
    {
        waitingAttackClick = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (!TryGetMouseHit(out var hit)) return;

        if (hit.transform.GetComponentInParent<Actor>() is Actor clickedEnemy &&
            ((1 << clickedEnemy.gameObject.layer) & enemyLayer.value) != 0 &&
            IsAlive(clickedEnemy.transform))
        {
            target = clickedEnemy.GetComponent<Interactable>();
            magic.SetCurrentAttackTarget(clickedEnemy.transform);

            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            if (magic.CanCastMageAttack)
                magic.TryCastMageAttackAt(clickedEnemy.transform);
            return;
        }

        float range = magic.GetPlayerStats().AutoAttackRange;
        var hits = Physics.OverlapSphere(transform.position, range, enemyLayer);
        if (hits.Length > 0)
        {
            Transform closest = ClosestAlive(hits);
            if (closest != null)
            {
                target = closest.GetComponent<Interactable>();
                magic.SetCurrentAttackTarget(closest);

                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;

                if (magic.CanCastMageAttack)
                    magic.TryCastMageAttackAt(closest);
                return;
            }
        }

        target = null;
        MoveTo(hit.point);
    }

    private void RightClick()
    {
        if (!TryGetMouseHit(out var hit)) return;

        // Se clicou em inimigo vivo, seta como alvo e tenta atacar (se em alcance)
        if (hit.transform.GetComponentInParent<Actor>() is Actor enemy && IsAlive(enemy.transform))
        {
            target = enemy.GetComponent<Interactable>();
            magic.SetCurrentAttackTarget(enemy.transform);

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            float range = magic.GetPlayerStats().AutoAttackRange;

            if (dist <= range && magic.CanCastMageAttack)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                magic.TryCastMageAttackAt(enemy.transform);
            }
            else
            {
                MoveTo(enemy.transform.position);
            }
        }
        else
        {
            // Click no chão: limpar alvo e mover
            target = null;
            MoveTo(hit.point);
        }

        // limpar estado visual do Attack-Move
        waitingAttackClick = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // VFX com auto-destroy (evita leak/acúmulo)
        if (clickFx)
        {
            var fx = Instantiate(clickFx, hit.point + Vector3.up * .1f, clickFx.transform.rotation);
            Destroy(fx.gameObject, 3f);
        }
    }

    void HoldMove()
    {
        if (!TryGetMouseHit(out var hit)) return;
        if (Vector3.Distance(agent.destination, hit.point) > 0.2f)
            MoveTo(hit.point);
    }

    // --------------------------------------------------------------------
    // Core actions
    // --------------------------------------------------------------------
    void MoveTo(Vector3 pos)
    {
        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    void AutoAttack()
    {
        if (!target) return;
        if (!IsTargetValid(target.transform)) { ClearTarget(); return; }

        if (InRange(target.transform) && magic.CanCastMageAttack)
        {
            magic.SetCurrentAttackTarget(target.transform);
            magic.TryCastMageAttackAt(target.transform);
        }
    }

    void StopNearTarget()
    {
        if (!target) return;
        if (!IsTargetValid(target.transform)) { ClearTarget(); return; }

        if (InRange(target.transform))
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            // 🧹 evita Face() usar destino antigo quando parado
            agent.nextPosition = transform.position;
        }
        else
        {
            if (agent.isStopped) agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }
    }

    private void Face()
    {
        // Durante o cast, olhar para o ponto do mouse
        if (magic.IsCasting)
        {
            LookAt(magic.MouseWorldPoint);
            return;
        }

        // Se há alvo válido, olhar para ele
        if (target && IsTargetValid(target.transform))
        {
            LookAt(target.transform.position);
            return;
        }

        // Sem alvo: só alinhar se realmente estiver se movendo
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f)
        {
            // steeringTarget é mais estável que destination
            LookAt(agent.steeringTarget);
            return;
        }

        // parado e sem alvo: não forçar rotação (evita “giro do nada”)
    }
    void LookAt(Vector3 pos)
    {
        pos.y = transform.position.y;
        Vector3 dir = pos - transform.position;
        if (dir.sqrMagnitude < .001f) return;
        Quaternion q = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * lookSpeed);
    }

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------
    bool TryGetMouseHit(out RaycastHit hit)
    {
        hit = default;
        if (!Camera.main) return false;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        var hits = Physics.RaycastAll(ray, 100f, clickableLayers);
        var enemyHits = System.Array.FindAll(hits, h => ((1 << h.transform.gameObject.layer) & enemyLayer.value) != 0);
        var prioritizedHits = enemyHits.Length > 0 ? enemyHits : hits;

        foreach (var h in prioritizedHits)
        {
            // ignora clicar em si mesmo
            if (h.transform.GetComponentInParent<PlayerActor>() != null &&
                h.transform.GetComponentInParent<PlayerController>() == this)
                continue;

            // se for inimigo, só aceita se estiver vivo
            if (h.transform.GetComponentInParent<Actor>() is Actor maybeEnemy)
            {
                if (!IsAlive(maybeEnemy.transform)) continue;
            }

            hit = h;
            return true;
        }

        return false;
    }

    Transform ClosestAlive(Collider[] cols)
    {
        Transform best = null; float dMin = float.MaxValue;
        foreach (var c in cols)
        {
            if (!IsAlive(c.transform)) continue;
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < dMin) { dMin = d; best = c.transform; }
        }
        return best;
    }

    bool InRange(Transform t)
        => Vector3.Distance(transform.position, t.position) <= magic.GetPlayerStats().AutoAttackRange;

    bool IsAlive(Transform t)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return false;
        if (t.TryGetComponent<Actor>(out var a)) return a.CurrentHealth.Value > 0f;
        if (t.TryGetComponent<PlayerActor>(out var p)) return p.CurrentHealth.Value > 0f;
        return true; // se não souber, assume vivo
    }

    bool IsTargetValid(Transform t) => t != null && t.gameObject.activeInHierarchy && IsAlive(t);

    void CheckAndClearInvalidTarget()
    {
        if (target == null) return;
        if (!IsTargetValid(target.transform)) ClearTarget();
    }

    public void ResetInputState()
    {
        isHoldingRight = false;
        waitingAttackClick = false;
        ClearTarget();

        var a = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (a && a.isOnNavMesh)
        {
            a.isStopped = true;
            a.ResetPath();
            a.velocity = Vector3.zero;
            a.nextPosition = transform.position;
        }
    }

    public Interactable CurrentTarget => target;
    public void ClearTarget() => target = null;
}