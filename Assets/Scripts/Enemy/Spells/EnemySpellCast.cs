using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.Netcode;

[AddComponentMenu("Enemy/Spell Cast")]
public class EnemySpellCast : MonoBehaviour
{
    [Header("Skill de Projétil")]
    [SerializeField] private GameObject spellPrefab;
    [SerializeField] private EnemyProjectileSpell spellData;
    [SerializeField] private Transform castPoint;

    [Header("Alerta Visual")]
    [SerializeField] private GameObject alertPrefab;
    private GameObject currentAlert;

    // Posição do alerta em relação ao inimigo (ajuste X, Y, Z aqui)
    private readonly Vector3 alertOffset = new Vector3(0.28f, 2.2f, 0f);

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
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyState = GetComponent<EnemyState>();
        attackModule = GetComponent<EnemyDetectionAndAttack>();
    }

    private void Update()
    {
        if (attackModule == null) return;

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

                // alerta 1s antes do cast
                if (timeUntilNextCast <= 1f && currentAlert == null && alertPrefab != null)
                {
                    Vector3 alertPosition = transform.position + alertOffset;
                    currentAlert = Instantiate(alertPrefab, alertPosition, Quaternion.identity, transform);
                }

                yield return null;
            }

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

        var projObj = Instantiate(spellPrefab, castPoint.position, rot);
        if (projObj.TryGetComponent<NetworkObject>(out var netObj))
            netObj.Spawn(); // agora visível na rede

        if (projObj.TryGetComponent<FireballEnemy_Script>(out var script))
            script.Init(spellData, gameObject);
    }
}