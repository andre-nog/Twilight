using UnityEngine;

public class FireballEnemy_Script : MonoBehaviour
{
    public EnemyProjectileSpell SpellData;
    public GameObject Caster;

    private SphereCollider myCollider;
    private Rigidbody myRigidbody;
    private bool hasHit = false;

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

        // Destruir manualmente após ultrapassar o alcance
        float maxDistance = SpellData.Range;
        if (Vector3.Distance(transform.position, Caster.transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.gameObject == Caster) return;

        // Jogador
        PlayerActor playerActor = other.GetComponentInParent<PlayerActor>();
        if (playerActor != null)
        {
            hasHit = true;
            playerActor.TakeDamage((int)SpellData.DamageAmount);
            Destroy(gameObject);
        }

        // (opcional) Outros inimigos
        Actor actor = other.GetComponentInParent<Actor>();
        if (actor != null)
        {
            hasHit = true;
            actor.TakeDamage((int)SpellData.DamageAmount);
            Destroy(gameObject);
        }
    }
}