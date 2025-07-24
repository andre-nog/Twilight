using UnityEngine;
using System.Collections.Generic;

public class FireballEnemy_Script : MonoBehaviour
{
    // Agora privados: serão setados apenas em Init(...)
    private EnemyProjectileSpell SpellData;
    private GameObject Caster;

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

    /// <summary>
    /// Inicializa o projétil com os dados da spell e quem é o caster.
    /// Deve ser chamado pelo SpellCast (EnemySpellCast ou BossSpellCast).
    /// </summary>
    public void Init(EnemyProjectileSpell data, GameObject caster)
    {
        SpellData = data;
        Caster    = caster;

        // Ignora colisão com o caster
        foreach (var myCol in GetComponentsInChildren<Collider>())
            foreach (var casterCol in Caster.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(myCol, casterCol, true);

        startPosition = transform.position;
    }

    void Update()
    {
        if (SpellData == null) return;

        // Move-se à frente, conforme a velocidade definida
        if (SpellData.Speed > 0f)
        {
            transform.Translate(
                transform.forward * SpellData.Speed * Time.deltaTime,
                Space.World
            );
        }

        // Destrói após ultrapassar o alcance
        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= SpellData.Range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (SpellData == null) return;
        if (other.gameObject == Caster) return;
        if (alreadyHit.Contains(other.gameObject)) return;

        // Só atinge o PlayerActor uma vez
        var player = other.GetComponentInParent<PlayerActor>();
        if (player != null)
        {
            alreadyHit.Add(other.gameObject);
            player.TakeDamage((int)SpellData.DamageAmount);
        }
    }
}