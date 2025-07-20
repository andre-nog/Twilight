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

    // -------------------------------------
    // Auto-Attack (substitui MageAttackSpell)
    // -------------------------------------
    [Header("Auto-Attack")]
    public float AutoAttackRange;              // usado como stoppingDistance
    public float AutoAttackProjectileSpeed;    // velocidade da bolinha

    // -------------------------------------
    // Derived Stats (Editor)
    // -------------------------------------
    public int   ExperienceToNextLevel => Mathf.FloorToInt(25 * Level);
    public float FinalAttackDamage    => AttackDamage * (1 + Intelligence * 0.05f);
    public float FinalAttackSpeed     => AttackSpeed  * (1);

}