// BossSpellCast.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#region DATA
[System.Serializable]
public class BossSpell
{
    public string               Name;
    public GameObject           SpellPrefab;
    public EnemyProjectileSpell SpellData;
    public Transform            CastPoint;
    public string               AnimatorTrigger;
    public bool                 usesAnimationEvent;
    public GameObject           AlertPrefab;
    public bool                 AlertBillboard = true;

    [Tooltip("Quanto tempo (em segundos) o alerta fica visível")]
    public float                AlertDuration = 1f;

    [HideInInspector] public float NextCastTime = float.MaxValue;
}
#endregion

[AddComponentMenu("Enemy/Boss Spell Cast")]

public class BossSpellCast : DebuggableMonoBehaviour
{
    [Header("Spells")] public List<BossSpell> Spells;

    private readonly Vector3 alertOffset = new Vector3(-0.265f, 3.2f, 0f);

    EnemyDetectionAndAttack attackModule;
    EnemyState              enemyState;
    NavMeshAgent            agent;
    Animator                animator;
    Transform               target;

    bool        hasAggro;
    bool        isCasting;
    bool        alertBillboard;
    GameObject  currentAlert;
    Vector3?    meteorTargetPos;

    void Start()
    {
        enableDebugLogs = false;
        DebugLog("[Boss] Start() chamado");
    }

    void Awake()
    {
        DebugLog("[Boss] Awake() chamado");
        animator     = GetComponent<Animator>();
        agent        = GetComponent<NavMeshAgent>();
        enemyState   = GetComponent<EnemyState>();
        attackModule = GetComponent<EnemyDetectionAndAttack>();
        target       = GameObject.FindWithTag("Player")?.transform;
    }

    void Update()
    {
        DebugLog("[Boss][Update] Entrou no Update");

        if (attackModule == null)
        {
            DebugLog("[Boss][Update] attackModule está null");
            return;
        }

        if (enemyState == null)
        {
            DebugLog("[Boss][Update] enemyState está null");
            return;
        }

        bool nowAggro = attackModule.HasAggro;
        DebugLog($"[Boss][Update] hasAggro atual: {hasAggro}, attackModule.HasAggro: {nowAggro}");

        if (nowAggro && !hasAggro)
        {
            DebugLog("[Boss][Update] Mudou para COM AGGRO - chamando ScheduleFirstCast");
            hasAggro = true;
            ScheduleFirstCast();
        }
        else if (!nowAggro && hasAggro)
        {
            DebugLog("[Boss][Update] Perdeu o AGGRO - abortando cast");
            hasAggro = false;
            AbortCurrentCast();
        }

        if (!hasAggro)
        {
            DebugLog("[Boss][Update] Não tem aggro. Saindo...");
            return;
        }

        if (isCasting)
        {
            DebugLog("[Boss][Update] Está no meio de um cast. Aguardando...");
            return;
        }

        for (int i = 0; i < Spells.Count; i++)
        {
            var s = Spells[i];
            DebugLog($"[Boss][Update] Spell '{s.Name}': Time.time={Time.time:F2}, NextCastTime={s.NextCastTime:F2}");

            if (Time.time >= s.NextCastTime)
            {
                DebugLog($"[Boss][Update] Iniciando cast de: {s.Name}");
                StartCoroutine(CastSpell(s));
                break;
            }
        }

        if (currentAlert && alertBillboard && Camera.main)
        {
            DebugLog("[Boss][Update] Rotacionando alerta para câmera");
            currentAlert.transform.LookAt(Camera.main.transform);
        }
    }


    /*──── Helpers ────*/
    void ScheduleFirstCast()
    {
        foreach (var s in Spells)
        {
            float offs = Random.Range(-s.SpellData.RandomStartOffset, s.SpellData.RandomStartOffset);
            s.NextCastTime = Time.time + s.SpellData.TimeUntilFirstCast + offs;
        }
    }

    void AbortCurrentCast()
    {
        StopAllCoroutines();
        isCasting = false;

        enemyState.SetBusy(false);
        if (agent) agent.isStopped = false;
        if (attackModule) attackModule.enabled = true;

        alertBillboard = false;
        if (currentAlert) Destroy(currentAlert);
        currentAlert = null;
    }

    /*──── Cast Coroutine ────*/
    IEnumerator CastSpell(BossSpell spell)
    {
        DebugLog($"[Boss] → Entrando em CastSpell({spell.Name})");

        if (attackModule) attackModule.enabled = false;
        animator.ResetTrigger("AttackTrigger");

        isCasting = true;
        enemyState.SetBusy(true);
        if (agent) agent.isStopped = true;

        float lead     = 1f;
        bool  isMeteor = spell.Name.ToLower().Contains("meteor");

        float waitPre  = isMeteor ? 0f : Mathf.Max(0f, spell.SpellData.CastDelay - lead);
        float waitPost = isMeteor ? 0f : Mathf.Min(lead, spell.SpellData.CastDelay);

        yield return new WaitForSeconds(waitPre);

        DebugLog($"[Boss] Preparando alerta de {spell.Name}");

        if (spell.AlertPrefab)
        {
            Vector3 pos; Quaternion rot; Transform parent;
            if (isMeteor)
            {
                if (target == null)
                {
                    DebugLog("[Boss] Target é null na hora do alerta do Meteor!");
                    yield break;
                }

                pos             = target.position + Vector3.up * .1f;
                rot             = Quaternion.Euler(90, 0, 0);
                parent          = null;
                meteorTargetPos = target.position;
            }
            else
            {
                pos    = transform.position + alertOffset;
                rot    = Quaternion.identity;
                parent = transform;
            }

            DebugLog($"[Boss] Instanciando alerta {spell.AlertPrefab.name} em {pos}");
            currentAlert = Instantiate(spell.AlertPrefab, pos, rot, parent);
            alertBillboard = spell.AlertBillboard;
            Destroy(currentAlert, spell.AlertDuration);
        }
        else
        {
            DebugLog($"[Boss] Nenhum AlertPrefab definido para {spell.Name}");
        }

        yield return new WaitForSeconds(waitPost);

        DebugLog($"[Boss] Disparando trigger de animação: {spell.AnimatorTrigger}");
        animator.SetTrigger(spell.AnimatorTrigger);

        if (!spell.usesAnimationEvent)
        {
            DebugLog($"[Boss] Spell {spell.Name} não usa evento — executando direto");
            ExecuteSpell(spell);
        }
    }


    /*──── Execução efetiva ────*/
    void ExecuteSpell(BossSpell spell)
    {
        DebugLog($"[Boss] Executando spell: {spell.Name}");

        if (spell.SpellPrefab == null)
        {
            DebugLog($"[Boss] Spell {spell.Name} não tem prefab!");
            return;
        }

        if (spell.Name.ToLower().Contains("meteor") && meteorTargetPos.HasValue)
        {
            Vector3 spawn = meteorTargetPos.Value + Vector3.up * 10f;
            var proj = Instantiate(spell.SpellPrefab, spawn, Quaternion.identity);
            DebugLog($"[Boss] Spawn do meteorito em {spawn}");

            if (proj.TryGetComponent<MeteorStrikeProjectile>(out var scr))
                scr.Init(spell.SpellData, gameObject, meteorTargetPos.Value);
        }
        else if (spell.CastPoint && target)
        {
            Vector3 dir = target.position - spell.CastPoint.position; dir.y = 0f;
            var proj = Instantiate(
                spell.SpellPrefab,
                spell.CastPoint.position,
                Quaternion.LookRotation(dir.normalized));

            DebugLog($"[Boss] Spawn da magia em {spell.CastPoint.position} mirando {target.position}");

            if (proj.TryGetComponent<FireballEnemy_Script>(out var scr))
                scr.Init(spell.SpellData, gameObject);
        }

        spell.NextCastTime = Time.time + spell.SpellData.Cooldown;

        isCasting = false;
        enemyState.SetBusy(false);
        if (agent) agent.isStopped = false;
        if (attackModule) attackModule.enabled = true;
        alertBillboard = false;
    }


    /*──── Animation Events ────*/
    public void OnSpellFrame()       => ExecuteByKey("fireball");
    public void OnSpellFrameMeteor() => ExecuteByKey("meteor");

    void ExecuteByKey(string key)
    {
        foreach (var s in Spells)
            if (s.Name.ToLower().Contains(key) && Time.time >= s.NextCastTime)
            { ExecuteSpell(s); break; }
    }
}