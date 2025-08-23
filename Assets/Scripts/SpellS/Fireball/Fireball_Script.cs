using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class Fireball_Script : NetworkBehaviour
{
    private ProjectileSpell spellData;
    private GameObject caster;
    private Vector3 startPosition;

    private SphereCollider myCollider;
    private Rigidbody myRigidbody;
    private HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    void Awake()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;
    }

    public void Init(ProjectileSpell data, GameObject casterGO)
    {
        spellData = data;
        caster = casterGO;
        startPosition = transform.position;

        foreach (var myCol in GetComponentsInChildren<Collider>())
            foreach (var casterCol in caster.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(myCol, casterCol, true);

        myCollider.radius = spellData.SpellRadius;
    }

    void Update()
    {
        // Apenas o servidor movimenta e destrói o projétil
        if (!IsServer || spellData == null) return;

        transform.Translate(
            transform.forward * spellData.Speed * Time.deltaTime,
            Space.World
        );

        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= spellData.Range)
            NetworkObject.Despawn(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || spellData == null || other.gameObject == caster || alreadyHit.Contains(other.gameObject))
            return;

        alreadyHit.Add(other.gameObject);

        var stats = caster.GetComponent<PlayerMagicSystem>()?.GetPlayerStats();
        float rawDamage = DamageCalculator.CalculateFireballDamage(spellData.DamageAmount, stats);
        int finalDamage = Mathf.RoundToInt(rawDamage);

        if (other.GetComponentInParent<Actor>() is Actor enemy)
        {
            enemy.TakeDamage(finalDamage);
            return;
        }

        if (other.GetComponentInParent<PlayerActor>() is PlayerActor player)
        {
            player.TakeDamage(finalDamage);
        }
    }
}
