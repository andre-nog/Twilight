using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    const string IDLE = "Idle";
    const string WALK = "Walk";

    private string currentAnimation;
    private CustomActions input;
    private NavMeshAgent agent;
    private Animator animator;
    private PlayerMagicSystem magic;

    [Header("Movement")]
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private LayerMask clickableLayers;
    [SerializeField] private float stopTolerance = 0.05f;

    private float lookRotationSpeed = 8f;

    private Interactable target;

    /*──────────── Setup ───────────*/

    void Start()
{
    target = null; // evita ataque automático no primeiro frame
}
    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        input    = new CustomActions();
        magic    = GetComponent<PlayerMagicSystem>();
    }

    void OnEnable()  { input.Main.Move.performed += _ => ClickToMove(); input.Enable(); }
    void OnDisable() => input.Disable();

    /*──────────── Input ───────────*/
    void ClickToMove()
    {
        if (!Camera.main || Mouse.current == null) return;

        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers)) return;

        var actor = hit.transform.GetComponentInParent<Actor>();
        if (actor != null)                                // clicou em inimigo
        {
            target = actor.GetComponent<Interactable>();

            if (target)
            {
                agent.isStopped = false;
                agent.SetDestination(target.transform.position);
            }
        }
        else                                              // clicou no chão
        {
            target = null;
            agent.isStopped = false;
            agent.SetDestination(hit.point);
        }

        if (clickEffect)
            Instantiate(clickEffect, hit.point + Vector3.up * .1f, clickEffect.transform.rotation);
    }

    /*──────────── Update ───────────*/
    void Update()
    {
        FaceTarget();
        HandleMovementStop();
        TryMageAttackIfTargetInRange();
        SetAnimations();
    }

    /*──────────── Auto-Attack ──────*/
    void TryMageAttackIfTargetInRange()
    {
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist <= magic.MageAttackRange && magic.CanCastMageAttack)
        {
            magic.TryCastMageAttackAt(target.transform.position);
        }
    }

    /*──────────── Parar/Andar ──────*/
    void HandleMovementStop()
    {
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist <= magic.MageAttackRange)               // dentro do alcance → para
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
        else                                             // fora do alcance → anda
        {
            if (agent.isStopped) agent.isStopped = false;
        }

        if (!target && agent.hasPath && agent.remainingDistance <= stopTolerance)
            agent.ResetPath();
    }

    /*──────────── Visuals ──────────*/
    void FaceTarget()
    {
        if (magic.IsCasting)
        {
            Vector3 mp  = magic.MouseWorldPoint;
            Vector3 dir = mp - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < .001f) return;

            Quaternion tgt = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, tgt, Time.deltaTime * lookRotationSpeed);
            return;
        }

        Vector3 facing = target ? target.transform.position : agent.destination;
        Vector3 d      = facing - transform.position; d.y = 0f;
        if (d.sqrMagnitude < .001f) return;

        Quaternion rot = Quaternion.LookRotation(d);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * lookRotationSpeed);
    }

    void SetAnimations()
    {
        if (agent.velocity.magnitude > 1f)
            PlayAnimation(WALK);
        else
            PlayAnimation(IDLE);

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    void PlayAnimation(string anim)
    {
        if (currentAnimation == anim) return;
        animator.Play(anim);
        currentAnimation = anim;
    }

    public Interactable CurrentTarget => target;
}