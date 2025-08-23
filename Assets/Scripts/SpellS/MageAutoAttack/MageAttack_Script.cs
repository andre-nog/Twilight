using UnityEngine;
using Unity.Netcode;

public class MageAttack_Script : NetworkBehaviour
{
    private float damage;
    private float speed;
    private float range;

    private GameObject caster;
    private Transform homingTarget;

    private SphereCollider myCollider;
    private Rigidbody myRigidbody;
    private bool hasHit = false;

    private NetworkVariable<ulong> targetNetId = new NetworkVariable<ulong>();

    void Awake()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;
    }

    public void Init(
        float damage,
        float speed,
        float range,
        GameObject caster,
        Transform target
    )
    {
        this.damage = damage;
        this.speed = speed;
        this.range = range;
        this.caster = caster;

        var targetNetObj = target.GetComponent<NetworkObject>();
        if (targetNetObj != null)
        {
            targetNetId.Value = targetNetObj.NetworkObjectId;
            homingTarget = target;
        }

        IgnoreCasterCollision();
        InitClientRpc(damage, speed, range, targetNetObj);
    }

    [ClientRpc]
    private void InitClientRpc(float dmg, float spd, float rng, NetworkObjectReference targetRef)
    {
        if (IsServer) return;

        damage = dmg;
        speed = spd;
        range = rng;

        if (targetRef.TryGet(out var targetObj))
            homingTarget = targetObj.transform;

        IgnoreCasterCollision();
    }

    private void IgnoreCasterCollision()
    {
        if (caster == null) return;

        foreach (var myCol in GetComponentsInChildren<Collider>())
            foreach (var casterCol in caster.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(myCol, casterCol, true);
    }

    void Update()
    {
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            if (IsServer && NetworkObject != null)
                NetworkObject.Despawn();
            return;
        }

        Vector3 aim = homingTarget.position + Vector3.up * 0.5f;
        Vector3 dir = aim - transform.position;

        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
            transform.position += dir * speed * Time.deltaTime;
            transform.forward = dir;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || hasHit || other.gameObject == caster) return;

        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            if (NetworkObject != null)
                NetworkObject.Despawn();
            return;
        }

        if (other.transform != homingTarget && other.transform.root != homingTarget) return;

        var actor = other.GetComponentInParent<Actor>();
        if (actor != null)
        {
            ApplyDamage(actor.TakeDamage);
            return;
        }

        var pa = other.GetComponentInParent<PlayerActor>();
        if (pa != null)
        {
            ApplyDamage(pa.TakeDamage);
        }
    }

private void ApplyDamage(System.Action<int> takeDamage)
{
    if (!IsServer) return;

    hasHit = true;
    int finalDmg = Mathf.RoundToInt(damage);
    takeDamage(finalDmg);
    NetworkObject.Despawn();
}

}
