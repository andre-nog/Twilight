// EnemySpellCast.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[AddComponentMenu("Enemy/Spell Cast")]
public class EnemySpellCast : MonoBehaviour
{
    const string CAST = "Cast";

    [Header("Skill de Projétil")]
    [SerializeField] private GameObject spellPrefab;
    [SerializeField] private ProjectileSpell spellData;
    [SerializeField] private Transform castPoint;
    [SerializeField] private float spellCooldown       = 5f;
    [SerializeField] private float timeUntilFirstSpell = 2f;

    private Transform                  target;
    private Animator                   animator;
    private NavMeshAgent               agent;
    private EnemyState                 enemyState;
    private EnemyDetectionAndAttack    attackModule;

    private float  spellTimer = 0f;
    private bool   spellReady = false;

    public void Init(Transform player)
    {
        target       = player;
        animator     = GetComponent<Animator>();
        agent        = GetComponent<NavMeshAgent>();
        enemyState   = GetComponent<EnemyState>();
        attackModule = GetComponent<EnemyDetectionAndAttack>();

        spellTimer = 0f;
        spellReady = false;
    }

    public void Tick()
    {
        // 1) só dispara se tiver aggro e não estiver em outro busy
        if (attackModule == null || !attackModule.HasAggro) return;
        if (enemyState != null && enemyState.IsBusy) return;

        // 2) checa pré-condições
        if (target == null || spellData == null || spellPrefab == null || castPoint == null) return;

        // 3) acumula tempo para o primeiro cast e para cooldown subsequente
        spellTimer += Time.deltaTime;
        if (!spellReady && spellTimer >= timeUntilFirstSpell)
        {
            spellReady = true;
            spellTimer = 0f;
            StartCoroutine(CastRoutine());
        }
        else if (spellReady && spellTimer >= spellCooldown)
        {
            spellTimer = 0f;
            StartCoroutine(CastRoutine());
        }
    }

    private IEnumerator CastRoutine()
    {
        // A) bloqueia tudo: para o agente e marca busy
        enemyState?.SetBusy(true);
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        // B) gira de frente pro alvo
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);

        // C) animação de cast
        animator.Play(CAST);

        // D) pausa pelo tempo de conjuração definido no ScriptableObject
        yield return new WaitForSeconds(spellData.CastDelay);

        // E) instancia o projétil
        if (spellPrefab != null && castPoint != null)
        {
            Vector3 shootDir = (target.position - castPoint.position);
            shootDir.y = 0f;
            Quaternion rot = Quaternion.LookRotation(shootDir.normalized);

            GameObject proj = Instantiate(spellPrefab, castPoint.position, rot);
            if (proj.TryGetComponent<Fireball_Script>(out var projectile))
            {
                projectile.SpellToCast = spellData;
                projectile.Caster      = gameObject;
            }
        }

        // F) retoma movimento e libera busy—o cooldown para o próximo lance já está sendo contado em Tick()
        agent.isStopped = false;
        enemyState?.SetBusy(false);
    }
}