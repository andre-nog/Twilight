using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[AddComponentMenu("Enemy/Spell Cast")]
public class EnemySpellCast : MonoBehaviour
{
    [Header("Skill de Projétil")]
    [SerializeField] private GameObject           spellPrefab;
    [SerializeField] private EnemyProjectileSpell spellData;
    [SerializeField] private Transform            castPoint;

    [Header("Alerta Visual")]
    [SerializeField] private GameObject           alertPrefab;
    private GameObject currentAlert;

    private EnemyDetectionAndAttack attackModule;
    private EnemyState              enemyState;
    private NavMeshAgent            agent;
    private Animator                animator;
    private Transform               target;

    private Coroutine castLoop;
    private float     timeUntilNextCast = -1f;  // -1 = aguardando aggro
    private bool      lastAggroState    = true; // evita trigger no frame 0

    private void Awake()
    {
        target       = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator     = GetComponent<Animator>();
        agent        = GetComponent<NavMeshAgent>();
        enemyState   = GetComponent<EnemyState>();
        attackModule = GetComponent<EnemyDetectionAndAttack>();
    }

    private void Update()
    {
        if (attackModule == null) return;

        bool hasAggroNow = attackModule.HasAggro;

        if (hasAggroNow && !lastAggroState)
        {
            RestartCastLoop(spellData.TimeUntilFirstCast);
        }
        else if (!hasAggroNow && lastAggroState)
        {
            StopCastLoop();
        }

        lastAggroState = hasAggroNow;
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
            Destroy(currentAlert);
    }

    private IEnumerator CastLoop(float initialDelay)
    {
        timeUntilNextCast = initialDelay;

        while (attackModule != null && attackModule.HasAggro)
        {
            // espera até o próximo cast
            while (timeUntilNextCast > 0f)
            {
                timeUntilNextCast -= Time.deltaTime;

                // mostra alerta 1s antes
                if (timeUntilNextCast <= 1f && currentAlert == null && alertPrefab != null)
                {
                    currentAlert = Instantiate(alertPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);
                }

                yield return null;
            }

            // dispara a animação
            animator.SetTrigger("SpellTrigger");

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

    public void OnSpellFrame()
    {
        if (spellPrefab == null || castPoint == null || target == null) return;

        Vector3 dir = target.position - castPoint.position;
        dir.y = 0f;
        Quaternion rot = Quaternion.LookRotation(dir.normalized);

        var proj = Instantiate(spellPrefab, castPoint.position, rot);
        if (proj.TryGetComponent<FireballEnemy_Script>(out var script))
            script.Init(spellData, gameObject);
    }
}