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

        float time = Time.time;

        float qCooldown = Mathf.Max(0f, magicSystem.FireballReadyTime - time);
        float eCooldown = Mathf.Max(0f, magicSystem.TeleportReadyTime - time);

        textUI.text = $@"
<color=#FF4444><b>Final ATK:</b></color> {playerStats.FinalAttackDamage:F2}
<color=#FF8844><b>Final Speed:</b></color> {playerStats.FinalAttackSpeed:F2}
<color=#4488FF><b>Mana:</b></color> {playerStats.CurrentMana:F0} / {playerStats.MaxMana:F0}
<color=#44BBFF><b>Regen:</b></color> {playerStats.ManaRechargeRate:F2}/s

<color=#AAFFAA><b>Cooldown Q:</b></color> {qCooldown:F1}s
<color=#AAFFAA><b>Cooldown E:</b></color> {eCooldown:F1}s

<color=#FFFF44><b>Level:</b></color> {playerStats.Level}
<color=#FFFF88><b>XP:</b></color> {playerStats.Experience} / {playerStats.ExperienceToNextLevel}
";
    }
}