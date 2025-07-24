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

    [Tooltip("Quanto tempo (em segundos) o alerta deve ficar visível")]
    public float                AlertDuration = 1f;

    [HideInInspector] public float NextCastTime = float.MaxValue;
}
#endregion

[AddComponentMenu("Enemy/Boss Spell Cast")]
public class BossSpellCast : MonoBehaviour
{
    [Header("Spells")] public List<BossSpell> Spells;

    private readonly Vector3 alertOffset = new Vector3(-0.265f, 3.2f, 0f);

    private EnemyDetectionAndAttack attackModule;
    private EnemyState              enemyState;
    private NavMeshAgent            agent;
    private Animator                animator;
    private Transform               target;

    private bool      hasAggro;
    private bool      isCasting;
    private bool      alertBillboard;
    private GameObject currentAlert;
    private Vector3?   meteorTargetPos;

    void Awake()
    {
        animator     = GetComponent<Animator>();
        agent        = GetComponent<NavMeshAgent>();
        enemyState   = GetComponent<EnemyState>();
        attackModule = GetComponent<EnemyDetectionAndAttack>();
        target       = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (attackModule == null || enemyState == null) return;

        if (attackModule.HasAggro && !hasAggro)          { hasAggro = true;  ScheduleFirstCast(); }
        else if (!attackModule.HasAggro && hasAggro)     { hasAggro = false; AbortCurrentCast(); }

        if (!hasAggro || isCasting) return;

        foreach (var s in Spells)
            if (Time.time >= s.NextCastTime) { StartCoroutine(CastSpell(s)); break; }

        if (currentAlert && alertBillboard && Camera.main)
            currentAlert.transform.LookAt(Camera.main.transform);
    }

    /* ---------- helpers ---------- */
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

        if (attackModule) attackModule.enabled = true;      // reativa ataques
        if (agent)       agent.isStopped = false;

        alertBillboard = false;
        if (currentAlert) Destroy(currentAlert);
        currentAlert = null;
    }

    /* ---------- cast coroutine ---------- */
    IEnumerator CastSpell(BossSpell spell)
    {
        // 1) Bloqueia ataques imediatamente
        if (attackModule) attackModule.enabled = false;

        // 2) Marca busy e interrompe movimento
        isCasting = true;
        enemyState.SetBusy(true);
        if (agent) agent.isStopped = true;

        // 3) Calcula pré-e-pós atraso (lead = 1 s)
        float lead     = 1f;
        bool  isMeteor = spell.Name.ToLower().Contains("meteor");
        float waitPre  = isMeteor ? 0f : Mathf.Max(0f, spell.SpellData.CastDelay - lead);
        float waitPost = isMeteor ? 0f : Mathf.Min(lead, spell.SpellData.CastDelay);

        yield return new WaitForSeconds(waitPre);

        /* alerta visual */
        if (spell.AlertPrefab)
        {
            Vector3 pos; Quaternion rot; Transform parent;
            if (isMeteor)
            {
                pos = target.position + Vector3.up * .1f; rot = Quaternion.Euler(90, 0, 0);
                parent = null;  meteorTargetPos = target.position;
            }
            else
            {
                pos = transform.position + alertOffset; rot = Quaternion.identity; parent = transform;
            }

            currentAlert = Instantiate(spell.AlertPrefab, pos, rot, parent);
            alertBillboard = spell.AlertBillboard;
            Destroy(currentAlert, spell.AlertDuration);
        }

        yield return new WaitForSeconds(waitPost);

        animator.SetTrigger(spell.AnimatorTrigger);
        if (!spell.usesAnimationEvent)
            ExecuteSpell(spell);                     // senão será chamado pelo AnimationEvent
    }

    /* ---------- execução efetiva ---------- */
    void ExecuteSpell(BossSpell spell)
    {
        if (spell.SpellPrefab == null) return;

        // Meteor
        if (spell.Name.ToLower().Contains("meteor") && meteorTargetPos.HasValue)
        {
            Vector3 spawn = meteorTargetPos.Value + Vector3.up * 10f;
            var proj = Instantiate(spell.SpellPrefab, spawn, Quaternion.identity);
            if (proj.TryGetComponent<MeteorStrikeProjectile>(out var scr))
                scr.Init(spell.SpellData, gameObject, meteorTargetPos.Value);
        }
        // Fireball
        else if (spell.CastPoint && target)
        {
            Vector3 dir = target.position - spell.CastPoint.position; dir.y = 0f;
            var proj = Instantiate(
                spell.SpellPrefab,
                spell.CastPoint.position,
                Quaternion.LookRotation(dir.normalized));
            if (proj.TryGetComponent<FireballEnemy_Script>(out var scr))
                scr.Init(spell.SpellData, gameObject);
        }

        /* reset */
        spell.NextCastTime = Time.time + spell.SpellData.Cooldown;
        isCasting = false;
        enemyState.SetBusy(false);

        if (attackModule) attackModule.enabled = true;      // reabilita AI de ataque
        if (agent)       agent.isStopped = false;
        alertBillboard = false;
    }

    /* ---------- Animation Events ---------- */
    public void OnSpellFrame()       => ExecuteByKey("fireball");
    public void OnSpellFrameMeteor() => ExecuteByKey("meteor");

    void ExecuteByKey(string key)
    {
        foreach (var s in Spells)
            if (s.Name.ToLower().Contains(key) && Time.time >= s.NextCastTime)
            { ExecuteSpell(s); break; }
    }
}