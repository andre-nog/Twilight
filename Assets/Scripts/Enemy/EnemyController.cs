using UnityEngine;

[AddComponentMenu("Enemy/Controller")]
public class EnemyController : MonoBehaviour
{
    private EnemyDetectionAndAttack attack;
    private EnemySpellCast spell;

    void Awake()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Vector3 origin = transform.position;

        attack = GetComponent<EnemyDetectionAndAttack>();
        spell = GetComponent<EnemySpellCast>();

        attack?.Init(player, origin);
        spell?.Init(player);
    }

    void Update()
    {
        attack?.Tick();
        spell?.Tick();
    }
}