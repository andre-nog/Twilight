using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.Netcode;

[AddComponentMenu("Enemy/Spell Cast")]
public class EnemySpellCast : NetworkBehaviour
{
    [Header("Skill de Projétil")]
    [SerializeField] private GameObject spellPrefab;
    [SerializeField] private EnemyProjectileSpell spellData;
    [SerializeField] private Transform castPoint;

    [Header("Alerta Visual")]
    [SerializeField] private GameObject alertPrefab;
    private GameObject currentAlert;
    private readonly Vector3 alertOffset = new(0.28f, 2.2f, 0f);

    private EnemyDetectionAndAttack attackModule;
    private EnemyState enemyState;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform target;

    private Coroutine castLoop;
    private float timeUntilNextCast = -1f;
    private bool lastAggroState = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyState = GetComponent<EnemyState>();
        attackModule = GetComponent<EnemyDetectionAndAttack>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Aguarda 1 frame para que Player esteja presente
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        yield return null;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsOwner)
            {
                target = p.transform;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning("[EnemySpellCast] Nenhum Player encontrado para mirar.");
            yield break;
        }

        RestartCastLoop(spellData.TimeUntilFirstCast + Random.Range(-spellData.RandomStartOffset, spellData.RandomStartOffset));
    }

    private void Update()
    {
        if (!IsServer || attackModule == null) return;

        bool hasAggroNow = attackModule.HasAggro;

        if (hasAggroNow && !lastAggroState)
        {
            float offset = Random.Range(-spellData.RandomStartOffset, spellData.RandomStartOffset);
            RestartCastLoop(spellData.TimeUntilFirstCast + offset);
        }
        else if (!hasAggroNow && lastAggroState)
        {
            StopCastLoop();
        }

        lastAggroState = hasAggroNow;

        if (currentAlert != null && Camera.main != null)
            currentAlert.transform.LookAt(Camera.main.transform);
    }

    private void RestartCastLoop(float initialDelay)
    {
        StopCastLoop();
        castLoop = StartCoroutine(CastLoop(initialDelay));
    }

    private void StopCastLoop()
    {
        if (castLoop != null)
            StopCoroutine(castLoop);

        castLoop = null;
        timeUntilNextCast = -1f;

        if (currentAlert != null)
        {
            Destroy(currentAlert);
            currentAlert = null;
        }
    }

    private IEnumerator CastLoop(float initialDelay)
    {
        timeUntilNextCast = initialDelay;

        while (attackModule != null && attackModule.HasAggro)
        {
            while (timeUntilNextCast > 0f)
            {
                timeUntilNextCast -= Time.deltaTime;

                if (timeUntilNextCast <= 1f && currentAlert == null && alertPrefab != null)
                {
                    Vector3 alertPosition = transform.position + alertOffset;
                    currentAlert = Instantiate(alertPrefab, alertPosition, Quaternion.identity, transform);
                }

                yield return null;
            }

            if (animator != null)
            {
                animator.ResetTrigger("SpellTrigger");
                animator.SetTrigger("SpellTrigger");
            }

            if (currentAlert != null)
            {
                Destroy(currentAlert);
                currentAlert = null;
            }

            yield return new WaitForSeconds(Mathf.Max(0.1f, spellData.CastDelay));

            enemyState?.SetBusy(false);
            if (agent != null) agent.isStopped = false;

            timeUntilNextCast = spellData.Cooldown;
        }

        castLoop = null;
    }

    // Chamado por AnimationEvent
    public void OnSpellFrame()
    {
        if (!IsServer || spellPrefab == null || castPoint == null || target == null) return;

        Vector3 dir = target.position - castPoint.position;
        dir.y = 0f;
        Quaternion rot = Quaternion.LookRotation(dir.normalized);

        var projObj = Instantiate(spellPrefab, castPoint.position, rot);
        if (projObj.TryGetComponent<NetworkObject>(out var netObj))
            netObj.Spawn();

        if (projObj.TryGetComponent<FireballEnemy_Script>(out var script))
            script.Init(spellData, gameObject);
    }
}
