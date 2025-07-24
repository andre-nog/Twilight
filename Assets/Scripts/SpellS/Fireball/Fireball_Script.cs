using UnityEngine;
using System.Collections.Generic;

public class Fireball_Script : MonoBehaviour
{
    // Agora privados: setados apenas em Init(...)
    private ProjectileSpell SpellToCast;
    private GameObject    Caster;

    private SphereCollider       myCollider;
    private Rigidbody            myRigidbody;
    private Vector3              startPosition;
    private HashSet<GameObject>  alreadyHit = new HashSet<GameObject>();

    void Awake()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;
    }

    /// <summary>
    /// Inicializa o projétil com os dados da spell e quem é o caster.
    /// Deve ser chamado pelo PlayerMagicSystem.
    /// </summary>
    public void Init(ProjectileSpell data, GameObject caster)
    {
        SpellToCast = data;
        Caster      = caster;

        // Ignora colisão com o caster
        foreach (var myCol     in GetComponentsInChildren<Collider>())
        foreach (var casterCol in Caster.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(myCol, casterCol, true);

        // Ajusta o raio do trigger conforme a spell
        myCollider.radius  = SpellToCast.SpellRadius;
        startPosition      = transform.position;
    }

    void Update()
    {
        if (SpellToCast == null) return;

        // Move o projétil à frente
        if (SpellToCast.Speed > 0f)
        {
            transform.Translate(
                transform.forward * SpellToCast.Speed * Time.deltaTime,
                Space.World
            );
        }

        // Destrói após ultrapassar o alcance
        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= SpellToCast.Range)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (SpellToCast == null) return;
        if (other.gameObject == Caster) return;
        if (alreadyHit.Contains(other.gameObject)) return;

        // Calcula dano
        var stats     = Caster.GetComponent<PlayerMagicSystem>()?.GetPlayerStats();
        float dmgF    = DamageCalculator.CalculateFireballDamage(
                            SpellToCast.DamageAmount, stats);
        int finalDmg  = Mathf.RoundToInt(dmgF);

        // Inimigos
        var actor = other.GetComponentInParent<Actor>();
        if (actor != null)
        {
            alreadyHit.Add(other.gameObject);
            actor.TakeDamage(finalDmg);
            return;
        }

        // Outros players (PvP)
        var playerActor = other.GetComponentInParent<PlayerActor>();
        if (playerActor != null)
        {
            alreadyHit.Add(other.gameObject);
            playerActor.TakeDamage(finalDmg);
        }
    }
}