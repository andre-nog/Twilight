using UnityEngine;

public abstract class SpellBase : ScriptableObject
{
    public string SkillName = "Habilidade";
    [TextArea(2, 3)] public string Description = "Descrição com {DANO}";
    [TextArea(1, 3)] public string ExtraDetails = "";
    public float Cooldown = 2f;
    public float DamageAmount = 10f;

    public abstract float CalculateDamage(PlayerStats stats);
}
