using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    /* refs & data --------------------------------------------------------- */
    private CustomActions     input;
    private NavMeshAgent      agent;
    private Animator          anim;
    private PlayerMagicSystem magic;

    [Header("Layers / FX")]
    [SerializeField]  LayerMask clickableLayers;      // chão + inimigos
    [SerializeField]  LayerMask enemyLayer;           // só Enemy
    [SerializeField]  ParticleSystem clickFx;
    [Header("Attack-Move (A) Cursor")]
    [SerializeField]  Texture2D attackCursor;
    [SerializeField]  Vector2   cursorHotspot = new (16,16);

    /* runtime */
    Interactable target;
    bool waitingAttackClick;
    float lookSpeed = 8f;

    /* life-cycle ----------------------------------------------------------- */
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();
        magic = GetComponent<PlayerMagicSystem>();
        input = new CustomActions();
    }
    void OnEnable()  { input.Main.Move.performed += _ => RightClick(); input.Enable();  }
    void OnDisable() { input.Main.Move.performed -= _ => RightClick(); input.Disable(); }

    /* main loop ----------------------------------------------------------- */
    void Update()
    {
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

        if (!MouseRay(out var hit)) return;

        // 1. Se clicou diretamente num inimigo, ele vira o alvo
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

        // 2. Caso contrário, tenta pegar o inimigo mais próximo do player
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

        // 3. Nenhum inimigo — apenas anda até o ponto clicado
        MoveTo(hit.point);
    }

    /* clique direito ------------------------------------------------------ */
    void RightClick()
    {
        if (!MouseRay(out var hit)) return;

        if (hit.transform.GetComponentInParent<Actor>() is Actor enemy)
        {
            target = enemy.GetComponent<Interactable>();
            MoveTo(enemy.transform.position);
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
    bool MouseRay(out RaycastHit hit)
    {
        hit = default;
        if (!Camera.main) return false;
        Ray r = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(r, out hit, 100f, clickableLayers);
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