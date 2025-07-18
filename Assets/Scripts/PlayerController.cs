using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    private CustomActions input;
    private NavMeshAgent  agent;
    private Animator      animator;
    private PlayerMagicSystem magic;

    [Header("Movement")]
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private LayerMask      clickableLayers;
    [SerializeField] private float          stopTolerance = 0.05f;

    private float lookRotationSpeed = 8f;
    private Interactable target;

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        input    = new CustomActions();
        magic    = GetComponent<PlayerMagicSystem>();
    }

    void OnEnable()
    {
        input.Main.Move.performed += _ => ClickToMove();
        input.Enable();
    }

    void OnDisable()
    {
        input.Main.Move.performed -= _ => ClickToMove();
        input.Disable();
    }

    void Update()
    {
        FaceTarget();
        HandleMovementStop();
        TryMageAttackIfTargetInRange();
        SetAnimations();
    }

    private void ClickToMove()
    {
        if (!Camera.main || Mouse.current == null) return;

        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 100f, clickableLayers)) return;

        var actor = hit.transform.GetComponentInParent<Actor>();
        if (actor != null)
        {
            // clicou num inimigo
            target = actor.GetComponent<Interactable>();
            agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }
        else
        {
            // clicou no chão
            target = null;
            agent.isStopped = false;
            agent.SetDestination(hit.point);
        }

        if (clickEffect)
            Instantiate(clickEffect, hit.point + Vector3.up * .1f, clickEffect.transform.rotation);
    }

    private void TryMageAttackIfTargetInRange()
    {
        if (target == null) 
            return;

        float attackRange = magic.GetPlayerStats().AutoAttackRange;
        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist <= attackRange && magic.CanCastMageAttack)
        {
            magic.SetCurrentAttackTarget(target.transform);
            magic.TryCastMageAttackAt(target.transform);
        }
    }

    private void HandleMovementStop()
    {
        if (target == null) 
            return;

        float attackRange = magic.GetPlayerStats().AutoAttackRange;
        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist <= attackRange)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
        else
        {
            if (agent.isStopped)
                agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }

        // Se não tiver alvo e chegou ao destino de clique, pare o agente
        if (target == null && agent.hasPath && agent.remainingDistance <= stopTolerance)
            agent.ResetPath();
    }

    private void FaceTarget()
    {
        // Se estiver lançando spell, olhe para o mouse
        if (magic.IsCasting)
        {
            Vector3 mp = magic.MouseWorldPoint;
            Vector3 dir = mp - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude >= 0.001f)
            {
                var tgt = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, tgt, Time.deltaTime * lookRotationSpeed);
            }
            return;
        }

        // Caso contrário, olhe para o alvo ou direção de movimento
        Vector3 focus = target != null ? target.transform.position : agent.destination;
        Vector3 d = focus - transform.position; d.y = 0f;
        if (d.sqrMagnitude >= 0.001f)
        {
            var rot = Quaternion.LookRotation(d);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * lookRotationSpeed);
        }
    }

    private void SetAnimations()
    {
        if (magic.IsCasting)
            return;
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }

    public Interactable CurrentTarget => target;
}