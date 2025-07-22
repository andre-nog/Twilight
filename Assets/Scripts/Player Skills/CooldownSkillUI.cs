    using UnityEngine;
using UnityEngine.UI;

public class CooldownSkillUI : MonoBehaviour
{
    [Header("Cooldown Settings")]
    public Image cooldownOverlay;
    public float cooldownTime = 5f;

    private float cooldownRemaining = 0f;
    private bool isOnCooldown = false;

    void Update()
    {
        if (isOnCooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            cooldownOverlay.fillAmount = cooldownRemaining / cooldownTime;

            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                cooldownOverlay.fillAmount = 0f;
            }
        }
    }

    public void TriggerCooldown()
    {
        isOnCooldown = true;
        cooldownRemaining = cooldownTime;
        cooldownOverlay.fillAmount = 1f;
    }

    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
}