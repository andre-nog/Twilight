using UnityEngine;

[CreateAssetMenu(fileName = "New Wind Wall", menuName = "Spells/Wind Wall Spell")]
public class WindWallSpell : SpellBase
{
    public float ManaCost = 20f;
    public float Range = 4f;
    public float Duration = 5f; // ← NOVO: duração da barreira

    public override float CalculateDamage(PlayerStats stats)
    {
        return 0f;
    }
}
