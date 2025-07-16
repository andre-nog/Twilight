using UnityEngine;

[CreateAssetMenu(
    fileName = "New Teleport Spell",
    menuName = "Spells/Teleport Spell")]
public class TeleportSpell : ScriptableObject
{
    [Header("Custo / Tempos")]
    public float ManaCost  = 20f;
    public float CastDelay = 0.1f;
    public float Cooldown  = 6f;

    [Header("Distância")]
    public float Range = 6f;          // distância do salto
}