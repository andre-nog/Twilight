using UnityEngine;

[CreateAssetMenu(
    fileName = "New Player Stats",
    menuName = "Stats/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Ofensivo")]
    public float AttackDamage;
    public float AttackSpeed;

    [Header("Atributos RPG")]
    public int Level;
    public int Experience;
    public int Strength;
    public int Intelligence;
    public int Agility;

    // -------------------------------------
    // Seção de Mana (fonte única de verdade)
    // -------------------------------------
    [Header("Mana")]
    public float MaxMana;
    public float CurrentMana;
    public float ManaRechargeRate;

    [Header("Vida")]
    public float MaxHealth;
    public float CurrentHealth;

    // -------------------------------------
    // Auto-Attack
    // -------------------------------------
    [Header("Auto-Attack")]
    public float AutoAttackRange;
    public float AutoAttackProjectileSpeed;

    [Header("Skills")]
    public ProjectileSpell fireballData;  // usado no cálculo do dano da Fireball

    // -------------------------------------
    // Derived Stats (Editor)
    // -------------------------------------
    public int ExperienceToNextLevel => Mathf.FloorToInt(50 * Level);
    public float FinalAttackDamage => AttackDamage + Intelligence * 0.10f;
    public float FinalAttackSpeed => AttackSpeed;
}