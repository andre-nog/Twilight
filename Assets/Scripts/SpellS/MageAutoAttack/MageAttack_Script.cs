// MageAttack_Script.cs
using UnityEngine;

public class MageAttack_Script : MonoBehaviour
{
    // parâmetros vindos de PlayerStats
    private float damage;
    private float speed;
    private float range;

    private GameObject caster;
    private Transform homingTarget;

    // componentes de colisão
    private SphereCollider myCollider;
    private Rigidbody      myRigidbody;
    private bool           hasHit = false;

    void Awake()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;
    }

    /// <summary>
    /// Chamar logo após Instantiate.
    /// </summary>
    public void Init(
        float damage,
        float speed,
        float range,
        GameObject caster,
        Transform target
    )
    {
        this.damage       = damage;
        this.speed        = speed;
        this.range        = range;
        this.caster       = caster;
        this.homingTarget = target;

        // ignora colisão com quem lançou
        foreach (var myCol in GetComponentsInChildren<Collider>())
            foreach (var casterCol in caster.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(myCol, casterCol, true);
    }

    void Update()
    {
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 aim = homingTarget.position + Vector3.up * 0.5f;
        Vector3 dir = (aim - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.forward = dir;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.gameObject == caster) return;

        // ✅ Garante que o projétil só cause dano ao homingTarget
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
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
        hasHit = true;
        int finalDmg = Mathf.RoundToInt(damage);
        takeDamage(finalDmg);
        Destroy(gameObject);
    }
}