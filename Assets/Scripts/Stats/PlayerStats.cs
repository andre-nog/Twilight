using UnityEngine;

[CreateAssetMenu(
    fileName = "New Player Stats",
    menuName = "Stats/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Ofensivo")]
    public float AttackDamage = 1f;    // dano base por projétil
    public float AttackSpeed  = 1f;    // golpes (MageAttack) por segundo

    [Header("Atributos RPG")]
    public int Level = 1;
    public int Experience = 0;
    public int ExperienceToNextLevel => Mathf.FloorToInt(25 * Level);
    public int Strength = 0;   // Pode aumentar dano físico (futuro)
    public int Intelligence = 0; // Afeta dano mágico (MageAttack)
    public int Agility = 0;    // Pode afetar velocidade de ataque, esquiva etc
    //public int Armor = 0;      // Reduz dano recebido
}