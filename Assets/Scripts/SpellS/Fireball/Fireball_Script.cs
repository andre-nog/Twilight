using UnityEngine;

public class Fireball_Script : MonoBehaviour
{
    public ProjectileSpell SpellToCast;   // dados do feitiço
    public GameObject            Caster;        // quem lançou (para não causar dano nele)

    private SphereCollider myCollider;
    private Rigidbody      myRigidbody;
    private bool           hasHit = false;

    /*──────────── Setup ────────────*/
    void Awake()
    {
        myCollider           = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody          = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;        // sem gravidade / forças
    }

    /// <summary>
    /// Deve ser chamado logo após Instantiate.
    /// Ajusta raio, lifetime e registra quem lançou.
    /// </summary>
public void Init(ProjectileSpell data, GameObject caster)
{
    SpellToCast = data;
    Caster      = caster;

    // Ignora colisão entre a spell e quem lançou
    foreach (var myCol in GetComponentsInChildren<Collider>())
    foreach (var casterCol in Caster.GetComponentsInChildren<Collider>())
        Physics.IgnoreCollision(myCol, casterCol, true);
  myCollider.radius = SpellToCast.SpellRadius;
    Destroy(gameObject, SpellToCast.Lifetime);
}


    /*──────────── Movimento ────────────*/
    void Update()
    {
        if (SpellToCast == null) return;       // segurança

        if (SpellToCast.Speed > 0)
        {
            transform.Translate(
                transform.forward * SpellToCast.Speed * Time.deltaTime,
                Space.World);
        }
    }

    /*──────────── Colisão / Dano ────────────*/
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.gameObject == Caster) return;
        

        // Inimigos
        Actor actor = other.GetComponentInParent<Actor>();
        if (actor != null)
        {
            hasHit = true;
            actor.TakeDamage((int)SpellToCast.DamageAmount);
            Destroy(gameObject);
            return;
        }

        // Jogador
        PlayerActor playerActor = other.GetComponentInParent<PlayerActor>();
        if (playerActor != null)
        {
            hasHit = true;
            playerActor.TakeDamage((int)SpellToCast.DamageAmount);
            Destroy(gameObject);
        }
    }
}