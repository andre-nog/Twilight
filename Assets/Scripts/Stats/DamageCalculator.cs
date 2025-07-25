using UnityEngine;

public static class DamageCalculator
{
    /* ---------------------------------------------------------
       Fórmulas de dano por tipo de spell
       --------------------------------------------------------- */
    
    // Fireball – 40 % da INT
    public static float CalculateFireballDamage(float baseDamage, PlayerStats stats)
    {
        if (stats == null) return baseDamage;
        return baseDamage + stats.Intelligence * 0.40f;
    }

    // MageAttack – 10 % da INT  (exemplo ― mantenha ou remova)
    public static int CalculateMagicDamage(float baseDamage, PlayerStats stats)
    {
        if (stats == null) return Mathf.RoundToInt(baseDamage);
        float bonus = stats.Intelligence * 0.10f;
        return Mathf.RoundToInt(baseDamage + bonus);
    }

    /* ---------------------------------------------------------
       Utilidades
       --------------------------------------------------------- */

    public static int ReduceDamageWithArmor(float incomingDamage, int armor)
    {
        float reduction = 1f + armor / 100f;
        return Mathf.RoundToInt(incomingDamage / reduction);
    }

    /// <summary>Formata a fórmula (base + X % de INT)</summary>
    public static string GetFormulaText(float baseDamage, float scalingPercent, int intelligence)
    {
        float scalingValue = intelligence * scalingPercent;
        return $"<i><color=#888888>({baseDamage} base + " +
               $"<color=#AAAAFF>{scalingPercent * 100:F0}% de INT</color> " +
               $"(<color=#AAAAFF>{scalingValue:F1}</color>))</color></i>";
    }
}
