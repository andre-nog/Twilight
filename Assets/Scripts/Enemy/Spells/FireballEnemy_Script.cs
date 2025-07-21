using UnityEngine;
using System.Collections.Generic;

public class FireballEnemy_Script : MonoBehaviour
{
    public EnemyProjectileSpell SpellData;
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
        if (SpellData == null) return;

        if (SpellData.Speed > 0)
        {
            transform.Translate(
                transform.forward * SpellData.Speed * Time.deltaTime,
                Space.World);
        }

        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= SpellData.Range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == Caster) return;
        if (alreadyHit.Contains(other.gameObject)) return;

        PlayerActor player = other.GetComponentInParent<PlayerActor>();
        if (player != null)
        {
            alreadyHit.Add(other.gameObject);
            player.TakeDamage((int)SpellData.DamageAmount);
        }
    }
}