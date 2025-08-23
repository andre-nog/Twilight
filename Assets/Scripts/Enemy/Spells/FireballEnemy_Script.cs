using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
public class FireballEnemy_Script : NetworkBehaviour
{
    private EnemyProjectileSpell SpellData;
    private GameObject Caster;

    private SphereCollider myCollider;
    private Rigidbody myRigidbody;
    private Vector3 startPosition;
    private HashSet<GameObject> alreadyHit = new();

    void Awake()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;
    }

    public void Init(EnemyProjectileSpell data, GameObject caster)
    {
        SpellData = data;
        Caster = caster;

        foreach (var myCol in GetComponentsInChildren<Collider>())
            foreach (var casterCol in Caster.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(myCol, casterCol, true);

        startPosition = transform.position;
    }

    void Update()
    {
        if (!IsServer || SpellData == null) return;

        if (SpellData.Speed > 0f)
        {
            transform.Translate(
                transform.forward * SpellData.Speed * Time.deltaTime,
                Space.World
            );
        }

        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= SpellData.Range)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || SpellData == null) return;
        if (other.gameObject == Caster || alreadyHit.Contains(other.gameObject)) return;

        var player = other.GetComponentInParent<PlayerActor>();
        if (player != null)
        {
            alreadyHit.Add(other.gameObject);
            player.TakeDamage((int)SpellData.DamageAmount);
        }
    }
}
