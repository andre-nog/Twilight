using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;

    public void GainXP(int amount)
    {
        stats.Experience += amount;

        while (stats.Experience >= stats.ExperienceToNextLevel)
        {
            stats.Experience -= stats.ExperienceToNextLevel;
            stats.Level++;

            // Aumento de ataque por nível
            stats.AttackDamage += 0;

            Debug.Log($"[LEVEL UP] Agora é nível {stats.Level}! Novo dano: {stats.AttackDamage}");

            // Recalcula o dano aplicado nas spells
            if (TryGetComponent<PlayerMagicSystem>(out var magic))
            {
                magic.ApplyStats();
            }
        }
    }
}