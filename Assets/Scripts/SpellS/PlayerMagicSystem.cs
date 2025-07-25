using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class PlayerMagicSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private ProjectileSpell fireballData;
    [SerializeField] private GameObject mageAttackPrefab;
    [SerializeField] private TeleportSpell teleportData;
    [SerializeField] private CooldownSkillUI teleportCooldownUI;

    [SerializeField] private WindWallSpell windWallData;
    [SerializeField] private GameObject windWallPrefab;
    [SerializeField] private CooldownSkillUI windWallCooldownUI;
    [SerializeField] public PlayerStats playerStats;
    [SerializeField] private CooldownSkillUI fireballCooldownUI;

    private CustomActions input;
    private NavMeshAgent agent;
    private Animator animator;
    private bool isAutoAttacking = false;

    private Transform currentAttackTarget;
    private bool fireballBusy, mageAttackBusy, teleportBusy, windWallBusy, isCasting;

    [HideInInspector] public float fireballReadyTime;
    [HideInInspector] public float teleportReadyTime;
    [HideInInspector] public float mageAttackReadyTime;
    private float windWallReadyTime;

    public bool IsBusy => fireballBusy || mageAttackBusy || teleportBusy || windWallBusy;
    public bool IsCasting => isCasting;

    private void Awake()
    {
        input = new CustomActions();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        input.Main.Teleport.performed += _ => TryTeleport();
        input.Main.CastWindWall.performed += _ => TryCastWindWall();
        input.Enable();
    }

    private void OnDisable()
    {
        input.Main.Teleport.performed -= _ => TryTeleport();
        input.Main.CastWindWall.performed -= _ => TryCastWindWall();
        input.Disable();
    }

    private void Update()
    {
        playerStats.CurrentMana = Mathf.Min(
            playerStats.CurrentMana + playerStats.ManaRechargeRate * Time.deltaTime,
            playerStats.MaxMana
        );
    }

    private void TryCastFireball()
    {
        if (IsBusy || fireballData == null || fireballPrefab == null) return;
        if (playerStats.CurrentMana < fireballData.ManaCost || Time.time < fireballReadyTime) return;

        isAutoAttacking = false;

        playerStats.CurrentMana -= fireballData.ManaCost;
        fireballReadyTime = Time.time + fireballData.Cooldown;
        fireballCooldownUI?.TriggerCooldown();

        StartCoroutine(FireballSpell.Cast(
            gameObject,
            castPoint,
            fireballPrefab,
            fireballData,
            animator,
            agent,
            busy => fireballBusy = busy,
            casting => isCasting = casting,
            GetMouseWorldPoint()
        ));
    }

    public void TryCastFireballAt(Vector3 aimPoint)
    {
        if (IsBusy || fireballData == null || fireballPrefab == null) return;
        if (playerStats.CurrentMana < fireballData.ManaCost || Time.time < fireballReadyTime) return;

        isAutoAttacking = false;

        playerStats.CurrentMana -= fireballData.ManaCost;
        fireballReadyTime = Time.time + fireballData.Cooldown;
        fireballCooldownUI?.TriggerCooldown();

        StartCoroutine(FireballSpell.Cast(
            gameObject,
            castPoint,
            fireballPrefab,
            fireballData,
            animator,
            agent,
            busy => fireballBusy = busy,
            casting => isCasting = casting,
            aimPoint
        ));
    }

private void TryTeleport()
{
    if (IsBusy || teleportData == null) return;
    if (playerStats.CurrentMana < teleportData.ManaCost || Time.time < teleportReadyTime) return;

    playerStats.CurrentMana -= teleportData.ManaCost;
    teleportReadyTime = Time.time + teleportData.Cooldown;

    teleportCooldownUI?.TriggerCooldown(); // ← cooldown só se teleport for lançado

    teleportBusy = true;
    StartCoroutine(TeleportSpellExecutor.Cast(
        gameObject,
        teleportData,
        agent,
        busy => teleportBusy = busy,
        GetMouseWorldPoint()
    ));
}


    private void TryCastMageAttack()
    {
        if (IsBusy || mageAttackPrefab == null || currentAttackTarget == null) return;

        if (Time.time < mageAttackReadyTime) return;

        animator.SetFloat("AttackSpeed", playerStats.FinalAttackSpeed);
        animator.ResetTrigger("AttackTrigger");
        animator.SetTrigger("AttackTrigger");

        isAutoAttacking = true;
        mageAttackReadyTime = Time.time + (1f / playerStats.FinalAttackSpeed);

        StartCoroutine(MageAttackSpellExecutor.CastAutoAttack(
            gameObject,
            castPoint,
            mageAttackPrefab,
            playerStats.FinalAttackDamage,
            playerStats.AutoAttackProjectileSpeed,
            playerStats.AutoAttackRange,
            animator,
            agent,
            busy => mageAttackBusy = busy,
            casting => isCasting = casting,
            currentAttackTarget
        ));
    }

    public void SetCurrentAttackTarget(Transform t)
    {
        currentAttackTarget = t;
    }

    public void TryCastMageAttackAt(Transform target)
    {
        if (target == null) return;
        SetCurrentAttackTarget(target);
        TryCastMageAttack();
    }

    public void OnAttackFrame()
    {
        if (!isAutoAttacking) return;
        isAutoAttacking = false;

        if (mageAttackPrefab == null || currentAttackTarget == null) return;

        Vector3 aim = currentAttackTarget.position + Vector3.up * 0.5f;
        Vector3 dir = (aim - castPoint.position).normalized;
        Quaternion rot = Quaternion.LookRotation(dir);

        var proj = Instantiate(mageAttackPrefab, castPoint.position, rot);
        if (proj.TryGetComponent<MageAttack_Script>(out var script))
        {
            script.Init(
                playerStats.FinalAttackDamage,
                playerStats.AutoAttackProjectileSpeed,
                playerStats.AutoAttackRange,
                gameObject,
                currentAttackTarget
            );
        }
    }

private void TryCastWindWall()
{
    if (IsBusy || windWallData == null) return;
    if (playerStats.CurrentMana < windWallData.ManaCost || Time.time < windWallReadyTime) return;

    // Gira o player na direção do mouse
    Vector3 aimPoint = GetMouseWorldPoint();
    Vector3 dir = (aimPoint - transform.position);
    dir.y = 0f;

    if (dir.sqrMagnitude > 0.01f)
    {
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = targetRot;
    }

    // Calcula posição final a uma distância fixa na direção
    Vector3 spawnPos = transform.position + dir.normalized * windWallData.Range + Vector3.up * 0.5f;
    Quaternion rot = Quaternion.LookRotation(dir.normalized);

    // Mana e cooldown
    windWallBusy = true;
    isCasting = true;

    playerStats.CurrentMana -= windWallData.ManaCost;
    windWallReadyTime = Time.time + windWallData.Cooldown;

    // Instancia barreira
    Instantiate(windWallPrefab, spawnPos, rot);

    // Trigger do cooldown na HUD
    windWallCooldownUI?.TriggerCooldown();

    StartCoroutine(ResetCastFlags(0.3f));
}


    private IEnumerator ResetCastFlags(float delay)
    {
        yield return new WaitForSeconds(delay);
        windWallBusy = false;
        isCasting = false;
    }

    private Vector3 GetMouseWorldPoint()
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out var hit, 100f)
            ? hit.point
            : ray.GetPoint(10f);
    }

    public Vector3 MouseWorldPoint => GetMouseWorldPoint();
    public PlayerStats GetPlayerStats() => playerStats;

    public bool CanCastMageAttack =>
        !IsBusy &&
        mageAttackPrefab != null &&
        Time.time >= mageAttackReadyTime;

    public float FireballReadyTime => fireballReadyTime;
    public float TeleportReadyTime => teleportReadyTime;

    public void ResetCastingState()
    {
        fireballBusy = false;
        mageAttackBusy = false;
        teleportBusy = false;
        windWallBusy = false;
        isCasting = false;
    }

    public void SetFireballBusy(bool value) => fireballBusy = value;
    public void SetIsCasting(bool value) => isCasting = value;
}