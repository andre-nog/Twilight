// MeteorStrikeProjectile.cs
using UnityEngine;
using System.Collections;

public class MeteorStrikeProjectile : MonoBehaviour
{
    private EnemyProjectileSpell SpellData;
    private GameObject           Caster;
    private Vector3              targetPos;

    private float fallDuration;
    private float impactRadius;

    /// <summary>
    /// Inicializa o meteorito extraindo campos de MeteorProjectileSpell,
    /// mas aceita também um EnemyProjectileSpell genérico.
    /// </summary>
    public void Init(EnemyProjectileSpell data, GameObject caster, Vector3 targetPosition)
    {
        SpellData   = data;
        Caster      = caster;
        targetPos   = targetPosition;

        // Se for MeteorProjectileSpell, pega FallDuration e ImpactRadius; senão dá fallback
        if (data is MeteorProjectileSpell md)
        {
            fallDuration = md.FallDuration;
            impactRadius = md.ImpactRadius;
        }
        else
        {
            fallDuration = data.CastDelay > 0f ? data.CastDelay : 1f;
            impactRadius = 2f;
        }

        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 end   = new Vector3(targetPos.x, targetPos.y, targetPos.z);

        while (elapsed < fallDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / fallDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = end;

        // Aplica dano na área com o raio apropriado
        Collider[] hits = Physics.OverlapSphere(targetPos, impactRadius);
        foreach (var hit in hits)
            if (hit.TryGetComponent<PlayerActor>(out var player))
                player.TakeDamage((int)SpellData.DamageAmount);

        Destroy(gameObject);
    }
}