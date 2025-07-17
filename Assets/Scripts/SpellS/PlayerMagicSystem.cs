using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class PlayerMagicSystem : MonoBehaviour
{
    //───────────────────────────────────────
    //              CONFIGURAÇÕES
    //───────────────────────────────────────

    [Header("Mana")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float currentMana = 100f;
    [SerializeField] private float manaRechargeRate = 2f;

    [Header("References")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private ProjectileSpell fireballData;
    [SerializeField] private GameObject mageAttackPrefab;
    [SerializeField] private MageAttackSpell mageAttackData;
    [SerializeField] private TeleportSpell teleportData;
    [SerializeField] public PlayerStats playerStats;

    //───────────────────────────────────────
    //             COMPONENTES
    //───────────────────────────────────────
    private Transform currentAttackTarget;
    public void SetCurrentAttackTarget(Transform t) => currentAttackTarget = t;
    private CustomActions input;
    private NavMeshAgent agent;
    private Animator animator;

    //───────────────────────────────────────
    //          CONTROLE DE STATUS
    //───────────────────────────────────────

    private bool fireballBusy = false;
    private bool mageAttackBusy = false;
    private bool teleportBusy = false;
    private bool isCasting = false;

    private float fireballReadyTime = 0f;
    private float mageAttackReadyTime = 0f;
    private float teleportReadyTime = 0f;

    public bool IsBusy => fireballBusy || mageAttackBusy || teleportBusy;
    public bool IsCasting => isCasting;

    //───────────────────────────────────────
    //               UNITY
    //───────────────────────────────────────

    private void Awake()
    {
        input = new CustomActions();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start() => ApplyStats();

    private void OnEnable()
    {
        input.Main.SpellCast.performed += OnSpellCast;
        input.Main.Teleport.performed += OnTeleport;
        input.Enable();
    }

    private void OnDisable()
    {
        input.Main.SpellCast.performed -= OnSpellCast;
        input.Main.Teleport.performed -= OnTeleport;
        input.Disable();
    }

    private void Update()
    {
        // Regenera mana com o tempo
        currentMana = Mathf.Min(currentMana + manaRechargeRate * Time.deltaTime, maxMana);
    }

    public void ApplyStats()
    {
        if (playerStats == null || mageAttackData == null) return;

        // Atualiza o Cooldown com base no AttackSpeed
        mageAttackData.Cooldown = playerStats.AttackSpeed > 0
            ? 1f / playerStats.AttackSpeed
            : 0.1f;

        // Atualiza o DamageAmount com base no AttackDamage e Inteligência
        float baseDamage = playerStats.AttackDamage;
        int scaledDamage = DamageCalculator.CalculateMagicDamage(baseDamage, playerStats);
        mageAttackData.DamageAmount = scaledDamage;
    }


    //───────────────────────────────────────
    //               INPUTS
    //───────────────────────────────────────

    private void OnSpellCast(InputAction.CallbackContext ctx) => TryCastFireball();
    private void OnTeleport(InputAction.CallbackContext ctx) => TryTeleport();

    //───────────────────────────────────────
    //              FIREBALL
    //───────────────────────────────────────

    private void TryCastFireball()
    {
        if (IsBusy || fireballData == null || fireballPrefab == null) return;
        if (currentMana < fireballData.ManaCost || Time.time < fireballReadyTime) return;

        currentMana -= fireballData.ManaCost;
        fireballReadyTime = Time.time + fireballData.Cooldown;

        StartCoroutine(FireballSpell.Cast(
            gameObject,
            castPoint,
            fireballPrefab,
            fireballData,
            animator,
            agent,
            (busy) => fireballBusy = busy,
            (casting) => isCasting = casting,
            GetMouseWorldPoint()
        ));
    }

    //───────────────────────────────────────
    //              TELEPORT
    //───────────────────────────────────────

    private void TryTeleport()
    {
        if (IsBusy || teleportData == null) return;
        if (currentMana < teleportData.ManaCost || Time.time < teleportReadyTime) return;

        currentMana -= teleportData.ManaCost;
        teleportReadyTime = Time.time + teleportData.Cooldown;

        teleportBusy = true;

        StartCoroutine(TeleportSpellExecutor.Cast(
            gameObject,
            teleportData,
            agent,
            (busy) => teleportBusy = busy,
            GetMouseWorldPoint()
        ));
    }

    //───────────────────────────────────────
    //            MAGE ATTACK
    //───────────────────────────────────────

    private void TryCastMageAttack()
    {
        if (IsBusy || mageAttackData == null || mageAttackPrefab == null) return;
        if (currentMana < mageAttackData.ManaCost || Time.time < mageAttackReadyTime) return;

        currentMana -= mageAttackData.ManaCost;
        mageAttackReadyTime = Time.time + mageAttackData.Cooldown;

        StartCoroutine(MageAttackSpellExecutor.Cast(
            gameObject,
            castPoint,
            mageAttackPrefab,
            mageAttackData,
            animator,
            agent,
            (busy) => mageAttackBusy = busy,
            (casting) => isCasting = casting,
            GetMouseWorldPoint()
        ));
    }

    public void TryCastMageAttackAt(Vector3 position)
    {
        if (IsBusy || mageAttackData == null || mageAttackPrefab == null) return;
        if (currentMana < mageAttackData.ManaCost || Time.time < mageAttackReadyTime) return;

            animator.SetFloat("AttackSpeed", playerStats.AttackSpeed);
    animator.ResetTrigger("AttackTrigger");
    animator.SetTrigger("AttackTrigger");

        currentMana -= mageAttackData.ManaCost;
        mageAttackReadyTime = Time.time + mageAttackData.Cooldown;

        StartCoroutine(MageAttackSpellExecutor.Cast(
            gameObject,
            castPoint,
            mageAttackPrefab,
            mageAttackData,
            animator,
            agent,
            (busy) => mageAttackBusy = busy,
            (casting) => isCasting = casting,
            position
        ));
    }
    public void OnAttackFrame()
    {
        if (mageAttackPrefab == null || mageAttackData == null || castPoint == null) return;
        if (currentAttackTarget == null) return;

        Vector3 aimPos = currentAttackTarget.position + Vector3.up * 0.5f;
        Vector3 dir = (aimPos - castPoint.position).normalized;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        var proj = Instantiate(mageAttackPrefab, castPoint.position, rot);
        if (proj.TryGetComponent<MageAttack_Script>(out var script))
            script.Init(mageAttackData, gameObject, currentAttackTarget);
    }


    //───────────────────────────────────────
    //                UTILS
    //───────────────────────────────────────

    private Vector3 GetMouseWorldPoint()
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out RaycastHit hit, 100f) ? hit.point : ray.GetPoint(10f);
    }

    public bool CanCastMageAttack =>
        !IsBusy &&
        mageAttackData != null &&
        mageAttackPrefab != null &&
        currentMana >= mageAttackData.ManaCost &&
        Time.time >= mageAttackReadyTime;

    public Vector3 MouseWorldPoint => GetMouseWorldPoint();

    public float MageAttackRange => mageAttackData != null ? mageAttackData.Range : 6f;

    public PlayerStats GetPlayerStats()
    {
        return playerStats;
    }
}