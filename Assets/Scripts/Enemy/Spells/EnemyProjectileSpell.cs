using UnityEngine;

[CreateAssetMenu(
    fileName = "New Enemy Projectile Spell",
    menuName = "Spells/Enemy Projectile Spell")]
public class EnemyProjectileSpell : ScriptableObject
{
    //──────────────────────────────────────────────
    // Dano e Comportamento
    //──────────────────────────────────────────────
    [Header("Dano / Duração")]
    public float DamageAmount;

    //──────────────────────────────────────────────
    // Casting (Timers e Delays)
    //──────────────────────────────────────────────
    [Header("Casting / Cooldown")]
    public float CastDelay;           // tempo até o evento de disparo
    public float TimeUntilFirstCast;  // tempo até o primeiro cast após aggro
    public float Cooldown;            // intervalo entre casts subsequentes

    //──────────────────────────────────────────────
    // Movimento do Projétil
    //──────────────────────────────────────────────
    [Header("Movimento / Alcance")]
    public float Speed;
    public float Range;

    [Header("Randomização")]
    [Tooltip("Variação aleatória aplicada ao tempo inicial do primeiro cast")]
    [Range(0f, 5f)]
    public float RandomStartOffset = 0.5f;
}