using UnityEngine;

[CreateAssetMenu(
    fileName = "New Mage Attack Spell",
    menuName = "Spells/Mage Attack")]
public class MageAttackSpell : ScriptableObject
{
    [Header("Dano / Mana")]
    public float DamageAmount = 10f;
    public float ManaCost     = 0f;

    [Header("Tempos")]
    public float CastDelay = 0.2f;
    public float Cooldown  = 1f;
    public float Lifetime  = 2f;

    [Header("Movimento / Distância")]
    public float Speed       = 20f;
    public float SpellRadius = 0.4f;
    public float Range       = 6f;
}