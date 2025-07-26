using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class PlayerController : DebuggableMonoBehaviour
{
    /* refs & data --------------------------------------------------------- */
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

    /* runtime */
    Interactable target;
    bool waitingAttackClick;
    bool isHoldingRight;
    float lookSpeed = 8f;

    /* life-cycle ----------------------------------------------------------- */
    void Start()
    {
        enableDebugLogs = false;
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        magic = GetComponent<PlayerMagicSystem>();
        input = new CustomActions();
    }
    void OnEnable() { input.Main.Move.performed += _ => RightClick(); input.Enable(); }
    void OnDisable() { input.Main.Move.performed -= _ => RightClick(); input.Disable(); }

    /* main loop ----------------------------------------------------------- */
    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame) isHoldingRight = true;
        if (Mouse.current.rightButton.wasReleasedThisFrame) isHoldingRight = false;
        if (isHoldingRight) HoldMove();

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            waitingAttackClick = true;
            if (attackCursor) Cursor.SetCursor(attackCursor, cursorHotspot, CursorMode.Auto);
        }

        if (waitingAttackClick && Mouse.current.leftButton.wasPressedThisFrame)
            AttackMove();

        Face();
        StopNearTarget();
        AutoAttack();
        anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    /* attack-move --------------------------------------------------------- */
    void AttackMove()
    {
        waitingAttackClick = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (!TryGetMouseHit(out var hit)) return;
        DebugLog($"[PlayerController] AttackMove hit: {hit.transform.name} at {hit.point}");

        if (hit.transform.GetComponentInParent<Actor>() is Actor clickedEnemy &&
            ((1 << clickedEnemy.gameObject.layer) & enemyLayer.value) != 0)
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
            Transform closest = Closest(hits);
            target = closest.GetComponent<Interactable>();
            magic.SetCurrentAttackTarget(closest);

            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            if (magic.CanCastMageAttack)
                magic.TryCastMageAttackAt(closest);
            return;
        }

        target = null;
        MoveTo(hit.point);
    }

    /* clique direito ------------------------------------------------------ */
    void RightClick()
    {
        if (!TryGetMouseHit(out var hit)) return;
        DebugLog($"[PlayerController] RightClick hit: {hit.transform.name} at {hit.point}");

        if (hit.transform.GetComponentInParent<Actor>() is Actor enemy)
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
            target = null;
            MoveTo(hit.point);
        }

        waitingAttackClick = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (clickFx) Instantiate(clickFx, hit.point + Vector3.up * .1f, clickFx.transform.rotation);
    }

    /* movimentação contínua segurando RMB -------------------------------- */
    void HoldMove()
    {
        if (!TryGetMouseHit(out var hit)) return;
        DebugLog($"[PlayerController] HoldMove hit: {hit.transform.name} at {hit.point}");

        if (Vector3.Distance(agent.destination, hit.point) > 0.2f)
        {
            MoveTo(hit.point);
        }
    }

    /* core behaviours ----------------------------------------------------- */
    void MoveTo(Vector3 pos)
    {
        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    void AutoAttack()
    {
        if (!target) return;
        if (InRange(target.transform) && magic.CanCastMageAttack)
        {
            magic.SetCurrentAttackTarget(target.transform);
            magic.TryCastMageAttackAt(target.transform);
        }
    }

    void StopNearTarget()
    {
        if (!target) return;
        if (InRange(target.transform))
        {
            if (!agent.isStopped) { agent.isStopped = true; agent.ResetPath(); }
        }
        else
        {
            if (agent.isStopped) agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }
    }

    void Face()
    {
        if (magic.IsCasting)
        {
            LookAt(magic.MouseWorldPoint);
            return;
        }
        LookAt(target ? target.transform.position : agent.destination);
    }

    /* helpers ------------------------------------------------------------- */
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
            if (h.transform.GetComponentInParent<PlayerActor>() != null &&
                h.transform.GetComponentInParent<PlayerController>() == this)
                continue;

            hit = h;
            return true;
        }

        return false;
    }

    Transform Closest(Collider[] cols)
    {
        Transform best = null; float dMin = float.MaxValue;
        foreach (var c in cols)
        {
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < dMin) { dMin = d; best = c.transform; }
        }
        return best;
    }

    bool InRange(Transform t)
        => Vector3.Distance(transform.position, t.position) <= magic.GetPlayerStats().AutoAttackRange;

    void LookAt(Vector3 pos)
    {
        pos.y = transform.position.y;
        Vector3 dir = pos - transform.position;
        if (dir.sqrMagnitude < .001f) return;
        Quaternion q = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * lookSpeed);
    }

    /* external access ----------------------------------------------------- */
    public Interactable CurrentTarget => target;
    public void ClearTarget() => target = null;
}
