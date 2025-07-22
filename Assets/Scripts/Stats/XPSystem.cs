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
        stats.Intelligence += 10;

        if (TryGetComponent<PlayerActor>(out var actor))
        {
            actor.IncreaseMaxHealth(5f);
        }

        Debug.Log($"[LEVEL UP] Agora é nível {stats.Level}! INT: {stats.Intelligence}");
    }
}
}