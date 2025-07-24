// MeteorProjectileSpell.cs
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Meteor Spell",
    menuName = "Spells/Meteor Projectile Spell"
)]
public class MeteorProjectileSpell : EnemyProjectileSpell
{
    [Header("Meteor Exclusivo")]
    [Tooltip("Quanto tempo (em segundos) o meteorito demora para cair")]
    public float FallDuration = 1f;

    [Tooltip("Raio de impacto do meteorito")]
    public float ImpactRadius = 2f;
}