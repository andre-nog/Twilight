using UnityEngine;

public static class DamageCalculator
{
    public static int CalculateMagicDamage(float baseDamage, PlayerStats stats)
    {
        if (stats == null)
            return Mathf.RoundToInt(baseDamage);

        float multiplier = 1f + stats.Intelligence * 0.05f;
        return Mathf.RoundToInt(baseDamage * multiplier);
    }

    public static int ReduceDamageWithArmor(float incomingDamage, int armor)
    {
        float reduction = 1f + armor / 100f;
        return Mathf.RoundToInt(incomingDamage / reduction);
    }
}