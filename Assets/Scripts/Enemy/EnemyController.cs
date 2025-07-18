// EnemyController.cs
using UnityEngine;

[AddComponentMenu("Enemy/Controller")]
public class EnemyController : MonoBehaviour
{
    private EnemySpellCast spell;

    void Awake()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Vector3 origin = transform.position;

        var attack = GetComponent<EnemyDetectionAndAttack>();
        spell = GetComponent<EnemySpellCast>();

        attack?.Init(player, origin);
        spell?.Init(player);
    }

    void Update()
    {
        spell?.Tick();
    }
}