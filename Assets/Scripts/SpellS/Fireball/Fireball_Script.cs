using UnityEngine;
using System.Collections.Generic;

public class Fireball_Script : MonoBehaviour
{
    public ProjectileSpell SpellToCast;
    public GameObject Caster;

    private SphereCollider myCollider;
    private Rigidbody myRigidbody;
    private Vector3 startPosition;
    private HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    void Awake()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;
    }

    public void Init(ProjectileSpell data, GameObject caster)
    {
        SpellToCast = data;
        Caster = caster;

        foreach (var myCol in GetComponentsInChildren<Collider>())
        foreach (var casterCol in Caster.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(myCol, casterCol, true);

        myCollider.radius = SpellToCast.SpellRadius;
        startPosition = transform.position;
    }

    void Update()
    {
        if (SpellToCast == null) return;

        if (SpellToCast.Speed > 0)
        {
            transform.Translate(
                transform.forward * SpellToCast.Speed * Time.deltaTime,
                Space.World
            );
        }

        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= SpellToCast.Range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == Caster) return;
        if (alreadyHit.Contains(other.gameObject)) return;

        var stats = Caster.GetComponent<PlayerMagicSystem>()?.GetPlayerStats();
        float dmg = DamageCalculator.CalculateFireballDamage(SpellToCast.DamageAmount, stats);
        int finalDmg = Mathf.RoundToInt(dmg);



        // Inimigo comum
        var actor = other.GetComponentInParent<Actor>();
        if (actor != null)
        {
            alreadyHit.Add(other.gameObject);
            actor.TakeDamage(finalDmg);
            return;
        }

        // Jogador (ex: PvP)
        var playerActor = other.GetComponentInParent<PlayerActor>();
        if (playerActor != null)
        {
            alreadyHit.Add(other.gameObject);
            playerActor.TakeDamage(finalDmg);
        }
    }
}