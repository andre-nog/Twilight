using UnityEngine;

[CreateAssetMenu(
    fileName = "New Projectile Spell",
    menuName = "Spells/Projectile Spell")]
public class ProjectileSpell : SpellBase
{
    [Header("Dano / Mana")]
    public float ManaCost = 5f;

    [Header("Tempos")]
    public float CastDelay = 0.15f;
    public float Lifetime = 2f;

    [Header("Movimento / Distância")]
    public float Speed = 15f;
    public float SpellRadius = 0.5f;
    public float Range = 6f;

    public override float CalculateDamage(PlayerStats stats)
    {
        return DamageCalculator.CalculateFireballDamage(DamageAmount, stats);
    }
}