using UnityEngine;

public static class DamageCalculator
{
    // MageAttack: escala com 5%
    public static int CalculateMagicDamage(float baseDamage, PlayerStats stats)
    {
        if (stats == null)
            return Mathf.RoundToInt(baseDamage);

        float bonus = stats.Intelligence * 0.10f;
        return Mathf.RoundToInt(baseDamage + bonus);
    }

    // Fireball: escala com 10%
    public static float CalculateFireballDamage(float baseDamage, PlayerStats stats)
    {
        if (stats == null)
            return baseDamage;

        float bonus = stats.Intelligence * 0.40f;
        return baseDamage + bonus;
    }


    public static int ReduceDamageWithArmor(float incomingDamage, int armor)
    {
        float reduction = 1f + armor / 100f;
        return Mathf.RoundToInt(incomingDamage / reduction);
    }
}