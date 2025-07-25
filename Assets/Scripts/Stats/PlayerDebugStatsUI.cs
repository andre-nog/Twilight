using TMPro;
using UnityEngine;

public class PlayerDebugStatsUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMagicSystem magicSystem;
    [SerializeField] private TextMeshProUGUI textUI;

    void Update()
    {
        if (playerStats == null || magicSystem == null || textUI == null) return;

        float fireballDamage = 0f;
        if (playerStats.fireballData != null)
        {
            fireballDamage = DamageCalculator.CalculateFireballDamage(
                playerStats.fireballData.DamageAmount, playerStats);
        }

        textUI.text = $@"
<color=#FFDD88><b>HP:</b></color> {playerStats.CurrentHealth:F0} / {playerStats.MaxHealth:F0}
<color=#4488FF><b>Mana:</b></color> {playerStats.CurrentMana:F0} / {playerStats.MaxMana:F0}

<color=#FF4444><b>Final ATK:</b></color> {playerStats.FinalAttackDamage:F2}
<color=#FF8844><b>Final Speed:</b></color> {playerStats.FinalAttackSpeed:F2}
<color=#44BBFF><b>Regen:</b></color> {playerStats.ManaRechargeRate:F2}/s

<color=#AAAAFF><b>Inteligência:</b></color> {playerStats.Intelligence}
<color=#FFCC66><b>Dano Fireball:</b></color> {fireballDamage:F1}

<color=#FFFF44><b>Level:</b></color> {playerStats.Level}
<color=#FFFF88><b>XP:</b></color> {playerStats.Experience} / {playerStats.ExperienceToNextLevel}
";
    }
}