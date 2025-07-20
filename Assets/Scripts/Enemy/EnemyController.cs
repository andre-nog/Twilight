// Assets/Scripts/Enemy/EnemyController.cs

using UnityEngine;

[AddComponentMenu("Enemy/Controller")]
public class EnemyController : MonoBehaviour
{
    private EnemyDetectionAndAttack attackModule;

    private void Awake()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Vector3 origin   = transform.position;

        attackModule = GetComponent<EnemyDetectionAndAttack>();
        if (attackModule != null)
            attackModule.Init(player, origin);

        // Não precisamos mais chamar Init ou Tick do EnemySpellCast aqui.
    }

    // Remova totalmente o Update() deste arquivo
}