using UnityEngine;

public class MageAttack_Script : MonoBehaviour
{
    public MageAttackSpell SpellToCast;   // dados do feitiço
    public GameObject      Caster;        // quem lançou (para não causar dano nele)

    private SphereCollider myCollider;
    private Rigidbody      myRigidbody;
    private bool           hasHit = false;

    /*──────────── Setup ────────────*/
    void Awake()
    {
        myCollider           = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;

        myRigidbody          = GetComponent<Rigidbody>();
        myRigidbody.isKinematic = true;   // sem gravidade / forças
    }

    /// <summary>
    /// Deve ser chamado logo após Instantiate.
    /// Ajusta raio, lifetime e registra quem lançou.
    /// </summary>
    public void Init(MageAttackSpell data, GameObject caster)
    {
        SpellToCast = data;
        Caster      = caster;

        // Ignora colisão entre a spell e quem lançou
        foreach (var myCol in GetComponentsInChildren<Collider>())
        foreach (var casterCol in Caster.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(myCol, casterCol, true);

        Debug.Log("[MageAttack] Caster definido como: " + Caster.name);

        myCollider.radius = SpellToCast.SpellRadius;
        Destroy(gameObject, SpellToCast.Lifetime);
    }

    /*──────────── Movimento ────────────*/
    void Update()
    {
        if (SpellToCast == null) return;

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

    // Tenta inimigo
    Actor actor = other.GetComponentInParent<Actor>();
    if (actor != null)
    {
        ApplyDamage(actor.TakeDamage);
        return;
    }

    // Tenta o próprio jogador
    PlayerActor playerActor = other.GetComponentInParent<PlayerActor>();
    if (playerActor != null)
    {
        ApplyDamage(playerActor.TakeDamage);
    }
}

    private void ApplyDamage(System.Action<int> takeDamage)
    {
        hasHit = true;
        var stats = Caster.GetComponent<PlayerMagicSystem>()?.GetPlayerStats();
        int finalDamage = Mathf.RoundToInt(SpellToCast.DamageAmount);
        takeDamage(finalDamage);
        Destroy(gameObject);
    }
    
}